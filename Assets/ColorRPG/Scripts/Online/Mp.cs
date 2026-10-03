// =====================================================================
//  Mp.cs - MULTIPLAYER: jucatori reali in aceeasi lume.
//   * Trade Plaza: o camera pe fiecare server (max 18 jucatori)
//   * fiecare harta: camere de max 4 jucatori (cand una e plina, se face alta)
//   * cand esti online, jucatorii simulati (cumparatori, booth-uri, chat) dispar
//   * monstrii si loot-ul sunt comune: gazda camerei (primul venit) misca monstrii,
//     cine da ultima lovitura ia kill-ul, loot-ul e al celui care il ia primul
//   * booth-urile din piata sunt ale jucatorilor reali: cumperi de la ei pe tokens
//  Transportul (Relay, camere) e in Online/MP/MpLink.cs.
// =====================================================================
using System;
using System.Collections.Generic;
using System.IO;
using ColorRPG.Net;
using UnityEngine;

namespace ColorRPG
{
    public class Mp : MonoBehaviour
    {
        public static Mp I;
        public const int MapMax = 4, PlazaMax = 18;

        const byte TProfile = 1, TState = 2, TChat = 3, TDm = 4,
                   TSnap = 10, THit = 11, TKill = 12, TDmg = 13, TSlam = 14,
                   TLootSpawn = 20, TLootClaim = 21, TLootTaken = 22,
                   TBooth = 30, TBuyReq = 31, TBuyOk = 32, TBuyFail = 33;

        readonly Dictionary<ulong, RemotePlayer> peers = new Dictionary<ulong, RemotePlayer>();
        class NetBooth { public int server, index; public SimBooth booth; }
        readonly Dictionary<ulong, NetBooth> booths = new Dictionary<ulong, NetBooth>();
        readonly Dictionary<int, ulong> lootTaken = new Dictionary<int, ulong>();   // gazda: cine a luat fiecare loot
        readonly Dictionary<int, float> recentlyDead = new Dictionary<int, float>();
        class PendingBuy { public SimListing listing; public long price; public float time; }
        readonly Dictionary<string, PendingBuy> pendingBuys = new Dictionary<string, PendingBuy>();

        string desired;
        int curMap = -1, curMax = MapMax, failStreak, lootSeq;
        bool attempted;
        float retryT, joinT, stateT, snapT, profileT, boothT, applyT, cleanT, lastSnap = -10f;
        string profileKey = "#", boothKey = "#";
        float connTime;

        // ---------------------------------------------------------------
        //  Stare (folosita de restul jocului)
        // ---------------------------------------------------------------
        /// <summary>Jocul e online si vrea multiplayer -> fara jucatori simulati.</summary>
        public static bool Online { get { return I != null && I.desired != null && I.failStreak < 3; } }
        /// <summary>Esti conectat la camera locului in care esti.</summary>
        public static bool Active { get { return I != null && I.desired != null && MpLink.Connected && MpLink.Loc == I.desired; } }
        public static bool IsHost { get { return Active && MpLink.IsHost; } }
        /// <summary>Monstrii hartii "map" sunt miscati de gazda (tu esti client in camera hartii).</summary>
        public static bool ClientOn(int map) { return Active && !MpLink.IsHost && I.curMap == map && map >= 0; }
        /// <summary>Tu esti gazda camerei hartii "map" (misti monstrii pentru toti).</summary>
        public static bool HostOn(int map) { return Active && MpLink.IsHost && I.curMap == map && map >= 0; }
        public static bool ShareLoot { get { return Active && I.curMap >= 0; } }
        public static int Players { get { return Active ? Mathf.Max(1, MpLink.PlayerCount) : 1; } }
        /// <summary>Pentru ADMIN > TOOLS: starea multiplayer-ului (camera, gazda, jucatori vazuti).</summary>
        public static string DebugInfo
        {
            get
            {
                if (I == null) return "MP off";
                int seen = 0; foreach (var p in I.peers.Values) if (p != null && p.hasState) seen++;
                return "MP " + (I.desired ?? "-") + (MpLink.InSession ? (MpLink.Connected ? (MpLink.IsHost ? " HOST" : " CLIENT") : " connecting") : " no room") +
                       "  players " + MpLink.PlayerCount + "  visible " + seen + "/" + I.peers.Count + (string.IsNullOrEmpty(MpLink.LastError) ? "" : "  err: " + MpLink.LastError);
            }
        }
        public static bool InPlazaRoom { get { return Active && I.desired != null && I.desired[0] == 'p'; } }
        public static IEnumerable<RemotePlayer> Peers { get { return I != null ? I.peers.Values : (IEnumerable<RemotePlayer>)new RemotePlayer[0]; } }

        public static bool HasPeer(string name)
        {
            if (I == null || string.IsNullOrEmpty(name)) return false;
            foreach (var p in I.peers.Values) if (p.pname.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string NameOf(ulong id)
        {
            RemotePlayer p;
            if (I != null && I.peers.TryGetValue(id, out p) && p != null) return p.pname;
            return "another player";
        }

        public static Mp Create(Transform parent)
        {
            var go = new GameObject("Multiplayer");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<Mp>();
            return I;
        }

        void OnEnable()
        {
            MpLink.OnMessage += OnMsg;
            MpLink.OnPeerJoined += OnJoined;
            MpLink.OnPeerLeft += OnLeft;
            MpLink.OnConnected += OnConn;
            MpLink.OnDisconnected += OnDisc;
        }

        void OnDisable()
        {
            MpLink.OnMessage -= OnMsg;
            MpLink.OnPeerJoined -= OnJoined;
            MpLink.OnPeerLeft -= OnLeft;
            MpLink.OnConnected -= OnConn;
            MpLink.OnDisconnected -= OnDisc;
        }

        void OnDestroy() { if (I == this) I = null; }

        // ---------------------------------------------------------------
        //  Unde trebuie sa fim
        // ---------------------------------------------------------------
        // chei de locatie pastrate (fara string nou in fiecare cadru)
        string[] cMapKeys; string cPlazaKey; int cPlazaId = int.MinValue;
        float keyT;
        readonly List<ulong> tmpGone = new List<ulong>();
        readonly List<int> tmpOld = new List<int>();
        readonly List<string> tmpLate = new List<string>();

        string Desired(out int max, out int map)
        {
            max = MapMax; map = -1;
            var g = Game.I;
            if (g == null || g.player == null || !MpLink.Installed || !Ugs.Ready) return null;
            if (!g.InPlaza && !GameSettings.OnlineMaps) return null;   // farmezi singur pe harti (Settings)
            if (g.InPlaza)
            {
                var ts = TradeSystem.I;
                if (ts == null || ts.Server == null || TradeSystem.IsPrivate(ts.Server.id)) return null;
                max = PlazaMax;
                if (ts.Server.id != cPlazaId || cPlazaKey == null) { cPlazaId = ts.Server.id; cPlazaKey = "p" + cPlazaId; }
                return cPlazaKey;
            }
            map = g.mapIndex;
            if (cMapKeys == null || cMapKeys.Length <= map) { cMapKeys = new string[Mathf.Max(8, map + 1)]; for (int i = 0; i < cMapKeys.Length; i++) cMapKeys[i] = "m" + i; }
            return cMapKeys[map];
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            int max, map;
            string want = Desired(out max, out map);
            if (want != desired)
            {
                LeftLocation();
                desired = want; curMap = map; curMax = max;
                retryT = 0f; joinT = 0f; attempted = false;
                if (failStreak >= 3) failStreak = 2;   // loc nou: mai incercam o data
            }
            if (MpLink.Busy) return;
            if (desired == null) { if (MpLink.InSession) MpLink.Leave(); return; }
            if (MpLink.Loc != desired || !MpLink.InSession)
            {
                retryT -= dt;
                if (retryT <= 0f)
                {
                    if (attempted) { failStreak++; if (failStreak == 3) Debug.LogWarning("[ColorRPG] Multiplayer unavailable: " + MpLink.LastError); }
                    attempted = true;
                    retryT = failStreak >= 3 ? 60f : 5f;
                    joinT = 0f;
                    MpLink.Go(desired, curMax);
                }
                return;
            }
            if (!MpLink.Connected)
            {
                // in camera, dar reteaua nu merge (gazda a plecat?) -> iesim si cautam alta
                joinT += dt;
                if (joinT > 20f) { joinT = 0f; retryT = 1f; MpLink.Leave(); }
                return;
            }
            failStreak = 0; attempted = false; joinT = 0f;
            Tick(Time.deltaTime);
        }

        void LeftLocation()
        {
            ClearPeers();
            booths.Clear();
            pendingBuys.Clear();
            RefillIfClient();
        }

        /// <summary>Am fost client pe o harta: monstrii omorati cat am fost conectati revin la noi local.</summary>
        void RefillIfClient()
        {
            if (curMap >= 0 && Game.I != null) Game.I.RefillMap(curMap);
        }

        void ClearPeers()
        {
            foreach (var p in peers.Values) if (p != null) Destroy(p.gameObject);
            peers.Clear();
        }

        // ---------------------------------------------------------------
        //  Evenimente retea
        // ---------------------------------------------------------------
        void OnConn()
        {
            ClearPeers(); booths.Clear(); lootTaken.Clear();
            profileKey = "#"; boothKey = "#";
            connTime = Time.time;
            SendProfile(ulong.MaxValue);
            // (fara mesaj la conectare: jucatorii nu trebuie anuntati la fiecare teleport)
        }

        void OnDisc()
        {
            ClearPeers(); booths.Clear();
            foreach (var pb in pendingBuys.Values) Refund(pb);
            pendingBuys.Clear();
            RefillIfClient();
        }

        void OnJoined(ulong id)
        {
            SendProfile(id);
            SendState(id);
            if (InPlazaRoom) SendBooth(id);
        }

        void OnLeft(ulong id)
        {
            RemotePlayer p;
            if (peers.TryGetValue(id, out p))
            {
                if (p != null)
                {
                    if (p.hero >= 0 && ChatUI.I != null) ChatUI.I.Post(1, p.pname + " left the server.");   // doar in chat-ul SERVER, nu global
                    Fx.Poof(p.Pos + Vector3.up, new Color(0.7f, 0.9f, 1f), 0.8f);
                    Destroy(p.gameObject);
                }
                peers.Remove(id);
            }
            booths.Remove(id);
        }

        RemotePlayer Peer(ulong id)
        {
            RemotePlayer p;
            if (peers.TryGetValue(id, out p) && p != null) return p;
            p = RemotePlayer.Create(id);
            peers[id] = p;
            return p;
        }

        // ---------------------------------------------------------------
        //  Mesaje periodice
        // ---------------------------------------------------------------
        void Tick(float dt)
        {
            var g = Game.I;
            if (g == null || g.player == null) return;
            bool alone = MpLink.PlayerCount <= 1;   // singur in camera: nu are cine sa primeasca pozitia / monstrii
            stateT -= dt;
            if (stateT <= 0f) { stateT = 0.1f; if (!alone) SendState(ulong.MaxValue); }
            profileT -= dt;
            keyT -= dt;
            bool keyCheck = keyT <= 0f;          // cheile (string) le comparam de 4 ori pe secunda, nu in fiecare cadru
            if (keyCheck) keyT = 0.25f;
            if (profileT <= 0f || (keyCheck && ProfileKey() != profileKey)) { profileT = 10f; SendProfile(ulong.MaxValue); }
            if (curMap >= 0 && MpLink.IsHost)
            {
                snapT -= dt;
                if (snapT <= 0f) { snapT = 0.1f; if (!alone) SendSnapshot(); }
            }
            if (InPlazaRoom)
            {
                boothT -= dt;
                if (boothT <= 0f || (keyCheck && BoothKey() != boothKey)) { boothT = 12f; SendBooth(ulong.MaxValue); }
            }
            cleanT -= dt;
            if (cleanT <= 0f)
            {
                cleanT = 1f;
                // jucatori care nu mai trimit nimic (iesiti fara mesaj)
                var gone = tmpGone; gone.Clear();
                foreach (var kv in peers) if (kv.Value == null || (kv.Value.hasState && Time.time - kv.Value.LastStateTime > 15f)) gone.Add(kv.Key);
                foreach (var id in gone) OnLeft(id);
                // monstri care nu mai apar la gazda (morti / disparuti)
                if (curMap >= 0 && !MpLink.IsHost && Time.time - lastSnap < 0.6f)
                    for (int i = Enemy.All.Count - 1; i >= 0; i--)
                    {
                        var e = Enemy.All[i];
                        if (e != null && e.mapIdx == curMap && !e.Dying && e.netId != 0 && Time.time - e.netSeen > 2.5f) e.NetVanish();
                    }
                var old = tmpOld; old.Clear();
                foreach (var kv in recentlyDead) if (Time.time > kv.Value) old.Add(kv.Key);
                foreach (var k in old) recentlyDead.Remove(k);
                // cumparari fara raspuns: banii inapoi
                var late = tmpLate; late.Clear();
                foreach (var kv in pendingBuys) if (Time.time - kv.Value.time > 15f) late.Add(kv.Key);
                foreach (var k in late) { Refund(pendingBuys[k]); pendingBuys.Remove(k); }
            }
        }

        void LateUpdate()
        {
            // booth-urile jucatorilor reali apar pe tarabe (in locul celor simulate)
            if (!Online || Game.I == null || !Game.I.InPlaza) return;
            applyT -= Time.unscaledDeltaTime;
            if (applyT > 0f) return;
            applyT = 0.4f;
            var ts = TradeSystem.I;
            if (ts == null || ts.Server == null || TradeSystem.IsPrivate(ts.Server.id)) return;
            var srv = ts.Server;
            for (int i = 0; i < srv.booths.Length; i++) srv.booths[i] = null;
            foreach (var nb in booths.Values)
                if (nb.server == srv.id && nb.index >= 0 && nb.index < srv.booths.Length && !ts.IsMine(nb.index) && srv.booths[nb.index] == null)
                    srv.booths[nb.index] = nb.booth;
            srv.players = Players;
        }

        // ---------------------------------------------------------------
        //  Scriere / citire
        // ---------------------------------------------------------------
        static BinaryWriter W(byte type, out MemoryStream ms)
        {
            ms = new MemoryStream(64);
            var w = new BinaryWriter(ms);
            w.Write(type);
            return w;
        }

        static void Send(ulong to, MemoryStream ms, bool reliable)
        {
            var data = ms.ToArray();
            if (to == ulong.MaxValue) MpLink.SendAll(data, reliable);
            else MpLink.SendTo(to, data, reliable);
        }

        string ProfileKey()
        {
            var p = Game.I.player;
            var w = p.Equipped(Slot.Weapon);
            return p.D.playerName + "|" + p.D.hero + "|" + p.Level + "|" + (w == null ? "-" : w.id + "+" + w.plus + "r" + w.rarity) + "|" + p.GearKey + "|" + PetList(p) + "|" + p.D.title + "|" + p.D.awakenClass + p.D.awakenTier;
        }

        void SendProfile(ulong to)
        {
            var g = Game.I;
            if (g == null || g.player == null) return;
            var p = g.player;
            profileKey = ProfileKey();
            MemoryStream ms; var w = W(TProfile, out ms);
            w.Write(p.D.playerName ?? "Player");
            w.Write(p.D.hero);
            w.Write(p.Level);
            w.Write(p.Power);
            var wp = p.Equipped(Slot.Weapon);
            w.Write(wp != null ? JsonUtility.ToJson(wp) : "");
            // v2: coif, armura, clasa, titlu, pets (ca toti sa te vada exact ca tine)
            var hp = p.Equipped(Slot.Helmet); var ap = p.Equipped(Slot.Armor);
            w.Write(hp != null ? JsonUtility.ToJson(hp) : "");
            w.Write(ap != null ? JsonUtility.ToJson(ap) : "");
            w.Write(p.D.awakenClass); w.Write(p.D.awakenTier);
            string tn; Color tc; int tt;
            TitleDB.Current(p, out tn, out tc, out tt);
            w.Write(tn ?? ""); w.Write(ColorUtility.ToHtmlStringRGB(tc)); w.Write(tt);
            w.Write(PetList(p));
            Send(to, ms, true);
        }

        /// <summary>Pets echipati: "model:raritate,model:raritate".</summary>
        static string PetList(Player p)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var pd in p.D.pets)
                if (pd.equipped) { if (sb.Length > 0) sb.Append(','); sb.Append(pd.model).Append(':').Append(pd.rarity); }
            return sb.ToString();
        }

        void SendState(ulong to)
        {
            var p = Game.I != null ? Game.I.player : null;
            if (p == null) return;
            MemoryStream ms; var w = W(TState, out ms);
            Vector3 pos = p.transform.position;
            w.Write(pos.x); w.Write(pos.y); w.Write(pos.z);
            w.Write(p.transform.eulerAngles.y);
            byte fl = 0;
            if (p.MovingNow) fl |= 1;
            if (p.SprintNow) fl |= 2;
            if (p.dead) fl |= 4;
            w.Write(fl);
            w.Write(p.SwingCount);
            Send(to, ms, to != ulong.MaxValue);
        }

        // ---------------------------------------------------------------
        //  Primire
        // ---------------------------------------------------------------
        void OnMsg(ulong from, byte[] data)
        {
            if (data == null || data.Length == 0 || Game.I == null || Game.I.player == null) return;
            var r = new BinaryReader(new MemoryStream(data));
            byte t = r.ReadByte();
            switch (t)
            {
                case TProfile:
                    {
                        string n = r.ReadString(); int hero = r.ReadInt32(); int lvl = r.ReadInt32(); long pw = r.ReadInt64(); string wj = r.ReadString();
                        var p = Peer(from);
                        bool fresh = p.hero < 0;
                        p.SetProfile(ChatFilter.CleanName(n), hero, lvl, pw, wj);
                        if (r.BaseStream.Position < r.BaseStream.Length)   // versiunile vechi nu trimit restul
                        {
                            try
                            {
                                string hj = r.ReadString(), aj = r.ReadString();
                                int c = r.ReadInt32(), ct = r.ReadInt32();
                                string tn = r.ReadString(), tcol = r.ReadString(); int tt = r.ReadInt32();
                                string pl = r.ReadString();
                                p.SetExtra(hj, aj, c, ct, tn, tcol, tt, pl);
                            }
                            catch (System.Exception) { }
                        }
                        if (fresh && Time.time - connTime > 5f && ChatUI.I != null) ChatUI.I.Post(1, p.pname + " joined the server.");
                        break;
                    }
                case TState:
                    {
                        var pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        float yaw = r.ReadSingle(); byte fl = r.ReadByte(); byte sw = r.ReadByte();
                        Peer(from).SetState(pos, yaw, fl, sw);
                        break;
                    }
                case TChat:
                    {
                        int ch = r.ReadByte(); string text = r.ReadString();
                        var p = Peer(from);
                        if (ChatSafety.IsBlocked(p.pname)) break;
                        string clean = ChatFilter.CleanIncoming(text);
                        if (clean == null) break;   // link-uri: nu se afiseaza
                        if (ChatUI.I != null) ChatUI.I.Receive(ch, p.pname, clean, p.power);
                        if (ch == 1) p.Say(clean);
                        break;
                    }
                case TDm:
                    {
                        string text = r.ReadString();
                        var p = Peer(from);
                        if (ChatSafety.IsBlocked(p.pname)) break;
                        string clean = ChatFilter.CleanIncoming(text);
                        if (clean == null) break;
                        if (ChatUI.I != null) ChatUI.I.ReceiveDm(p.pname, clean);
                        break;
                    }
                case TSnap: ReadSnapshot(r); break;
                case THit:
                    {
                        int id = r.ReadInt32(); int dmg = r.ReadInt32(); bool crit = r.ReadBoolean();
                        if (!MpLink.IsHost) break;
                        var e = Enemy.ById(id);
                        if (e != null && e.mapIdx == curMap) e.NetHit(Mathf.Clamp(dmg, 0, int.MaxValue / 2), crit, from);
                        break;
                    }
                case TKill:
                    {
                        int id = r.ReadInt32(); ulong killer = r.ReadUInt64();
                        recentlyDead[id] = Time.time + 4f;
                        var e = Enemy.ById(id);
                        if (e != null) e.NetKill(killer == MpLink.LocalId, killer == MpLink.LocalId ? null : NameOf(killer));
                        break;
                    }
                case TDmg:
                    {
                        int dmg = r.ReadInt32();
                        var p = Game.I.player;
                        if (!p.dead && !Game.I.InPlaza && Game.I.mapIndex == curMap) p.TakeDamage(dmg);
                        break;
                    }
                case TSlam:
                    {
                        int id = r.ReadInt32();
                        var e = Enemy.ById(id);
                        if (e != null) e.NetSlam();
                        break;
                    }
                case TLootSpawn: ReadLoot(r); break;
                case TLootClaim:
                    {
                        int id = r.ReadInt32();
                        if (MpLink.IsHost) HostClaim(from, id);
                        break;
                    }
                case TLootTaken:
                    {
                        int id = r.ReadInt32(); ulong who = r.ReadUInt64();
                        Loot.NetTaken(id, who == MpLink.LocalId);
                        break;
                    }
                case TBooth: ReadBooth(from, r); break;
                case TBuyReq:
                    {
                        int idx = r.ReadInt32(); string sig = r.ReadString(); long price = r.ReadInt64(); string buyer = r.ReadString();
                        var ts = TradeSystem.I;
                        var l = ts != null ? ts.NetSell(idx, sig, price, ChatFilter.CleanName(buyer)) : null;
                        MemoryStream ms;
                        if (l != null)
                        {
                            var w = W(TBuyOk, out ms); w.Write(sig); w.Write(JsonUtility.ToJson(l));
                            Send(from, ms, true);
                            boothKey = "#";   // trimitem booth-ul nou tuturor
                        }
                        else { var w = W(TBuyFail, out ms); w.Write(sig); Send(from, ms, true); }
                        break;
                    }
                case TBuyOk:
                    {
                        string sig = r.ReadString(); string json = r.ReadString();
                        PendingBuy pb;
                        if (!pendingBuys.TryGetValue(sig, out pb)) break;
                        pendingBuys.Remove(sig);
                        BoothListing l = null;
                        try { l = JsonUtility.FromJson<BoothListing>(json); } catch (Exception) { }
                        if (l == null) { Refund(pb); break; }
                        if (TradeSystem.I != null) TradeSystem.I.ReceiveNet(l, NameOf(from));
                        break;
                    }
                case TBuyFail:
                    {
                        string sig = r.ReadString();
                        PendingBuy pb;
                        if (!pendingBuys.TryGetValue(sig, out pb)) break;
                        pendingBuys.Remove(sig);
                        Refund(pb);
                        Game.I.Say("Someone else bought it first!", 2.5f);
                        Sfx.Play("Error", 0.5f);
                        break;
                    }
            }
        }

        // ---------------------------------------------------------------
        //  Chat
        // ---------------------------------------------------------------
        public static void SendChat(int channel, string text)
        {
            if (!Active) return;
            MemoryStream ms; var w = W(TChat, out ms);
            w.Write((byte)channel); w.Write(text);
            Send(ulong.MaxValue, ms, true);
        }

        /// <summary>Mesaj privat; false daca prietenul nu e in camera ta.</summary>
        public static bool SendDm(string name, string text)
        {
            if (!Active) return false;
            foreach (var kv in I.peers)
                if (kv.Value != null && kv.Value.pname.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    MemoryStream ms; var w = W(TDm, out ms);
                    w.Write(text);
                    Send(kv.Key, ms, true);
                    return true;
                }
            return false;
        }

        // ---------------------------------------------------------------
        //  Monstri (gazda e autoritatea)
        // ---------------------------------------------------------------
        readonly List<Enemy> snapList = new List<Enemy>();
        void SendSnapshot()
        {
            var list = snapList; list.Clear();
            foreach (var e in Enemy.All) if (e != null && !e.Dying && e.mapIdx == curMap && e.netId != 0) list.Add(e);
            const int Chunk = 40;
            for (int start = 0; start < list.Count || start == 0; start += Chunk)
            {
                MemoryStream ms; var w = W(TSnap, out ms);
                w.Write((byte)curMap);
                int n = Mathf.Min(Chunk, list.Count - start);
                w.Write((short)Mathf.Max(0, n));
                for (int i = 0; i < n; i++)
                {
                    var e = list[start + i];
                    Vector3 p = e.transform.position;
                    w.Write(e.netId); w.Write(p.x); w.Write(p.z);
                    w.Write((byte)Mathf.RoundToInt(e.transform.eulerAngles.y / 360f * 255f));
                    w.Write(e.hp); w.Write(e.maxHp);
                    w.Write(e.NetFlags);
                }
                Send(ulong.MaxValue, ms, false);
                if (list.Count == 0) break;
            }
        }

        void ReadSnapshot(BinaryReader r)
        {
            int map = r.ReadByte();
            int n = r.ReadInt16();
            if (map != curMap || MpLink.IsHost) return;
            lastSnap = Time.time;
            for (int i = 0; i < n; i++)
            {
                int id = r.ReadInt32(); float x = r.ReadSingle(), z = r.ReadSingle();
                float yaw = r.ReadByte() / 255f * 360f; int hp = r.ReadInt32(), maxHp = r.ReadInt32(); byte fl = r.ReadByte();
                if (recentlyDead.ContainsKey(id)) continue;
                var e = Enemy.ById(id);
                var pos = new Vector3(x, 0f, z);
                if (e == null && Game.I != null) e = Game.I.SpawnNetEnemy(id, pos);
                if (e != null) e.NetSnap(pos, yaw, hp, maxHp, fl);
            }
        }

        /// <summary>Client: ai lovit un monstru -> gazda scade viata si decide cine il omoara.</summary>
        public static void SendHit(int id, int dmg, bool crit)
        {
            MemoryStream ms; var w = W(THit, out ms);
            w.Write(id); w.Write(dmg); w.Write(crit);
            MpLink.SendHost(ms.ToArray(), true);
        }

        /// <summary>Gazda: monstrul a murit, "killer" ia recompensa.</summary>
        public static void SendKill(int id, ulong killer)
        {
            if (!Active) return;
            I.recentlyDead[id] = Time.time + 4f;
            MemoryStream ms; var w = W(TKill, out ms);
            w.Write(id); w.Write(killer);
            Send(ulong.MaxValue, ms, true);
        }

        public static void SendSlam(int id)
        {
            if (!Active) return;
            MemoryStream ms; var w = W(TSlam, out ms);
            w.Write(id);
            Send(ulong.MaxValue, ms, true);
        }

        /// <summary>Gazda: un monstru a lovit un jucator din camera.</summary>
        public static void SendDamage(ulong target, int dmg)
        {
            MemoryStream ms; var w = W(TDmg, out ms);
            w.Write(dmg);
            Send(target, ms, true);
        }

        /// <summary>Cel mai apropiat jucator viu (tu sau altii din camera) - tinta monstrilor de la gazda.</summary>
        public static bool NearestTarget(Vector3 from, Player local, out Vector3 pos, out bool dead, out ulong id, out bool isLocal)
        {
            pos = local != null ? local.transform.position : from + Vector3.forward * 9999f;
            dead = local == null || local.dead; id = MpLink.LocalId; isLocal = true;
            float best = local != null && !local.dead ? (pos - from).sqrMagnitude : float.MaxValue;
            if (I == null) return local != null;
            foreach (var kv in I.peers)
            {
                var p = kv.Value;
                if (p == null || !p.hasState || p.Dead) continue;
                float d = (p.Pos - from).sqrMagnitude;
                if (d < best) { best = d; pos = p.Pos; dead = false; id = kv.Key; isLocal = false; }
            }
            return best < float.MaxValue;
        }

        public static bool AnyPeerNear(Vector3 from, float r)
        {
            if (I == null) return false;
            foreach (var p in I.peers.Values) if (p != null && p.hasState && (p.Pos - from).sqrMagnitude < r * r) return true;
            return false;
        }

        /// <summary>Gazda: lovitura in zona (slam-ul boss-ului) pentru ceilalti jucatori.</summary>
        public static void DamagePeersInRadius(Vector3 c, float r, int dmg)
        {
            if (I == null) return;
            foreach (var kv in I.peers)
            {
                var p = kv.Value;
                if (p == null || !p.hasState || p.Dead) continue;
                Vector3 d = p.Pos - c; d.y = 0;
                if (d.magnitude < r) SendDamage(kv.Key, dmg);
            }
        }

        // ---------------------------------------------------------------
        //  Loot comun: primul care il ia
        // ---------------------------------------------------------------
        public static int NewLootId()
        {
            if (I.lootSeq == 0) I.lootSeq = UnityEngine.Random.Range(1, 1 << 20);
            I.lootSeq = (I.lootSeq + 1) & 0xFFFFF;
            return (int)(((MpLink.LocalId & 0x3FF) << 20) | (uint)I.lootSeq) | 1 << 30;
        }

        public static void ShareLootSpawn(Loot l, Vector3 pos, Vector3 vel)
        {
            if (!ShareLoot) return;
            MemoryStream ms; var w = W(TLootSpawn, out ms);
            w.Write(l.netId); w.Write((byte)l.kind); w.Write(l.amount);
            w.Write(pos.x); w.Write(pos.y); w.Write(pos.z);
            w.Write(vel.x); w.Write(vel.y); w.Write(vel.z);
            Color c = l.NetColor; w.Write(c.r); w.Write(c.g); w.Write(c.b);
            w.Write(l.kind == Loot.Kind.Item && l.item != null ? JsonUtility.ToJson(l.item) : "");
            Send(ulong.MaxValue, ms, true);
        }

        void ReadLoot(BinaryReader r)
        {
            int id = r.ReadInt32(); var kind = (Loot.Kind)r.ReadByte(); long amount = r.ReadInt64();
            var pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            var vel = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            var col = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            string json = r.ReadString();
            if (!ShareLoot) return;
            Item it = null;
            if (kind == Loot.Kind.Item) { try { it = JsonUtility.FromJson<Item>(json); } catch (Exception) { } if (it == null) return; }
            Loot.SpawnNet(id, kind, amount, pos, vel, col, it);
        }

        /// <summary>Vrei sa iei loot-ul "id": gazda decide cine a fost primul.</summary>
        public static void ClaimLoot(int id)
        {
            if (MpLink.IsHost) { I.HostClaim(MpLink.LocalId, id); return; }
            MemoryStream ms; var w = W(TLootClaim, out ms);
            w.Write(id);
            MpLink.SendHost(ms.ToArray(), true);
        }

        void HostClaim(ulong who, int id)
        {
            ulong owner;
            if (!lootTaken.TryGetValue(id, out owner)) { owner = who; lootTaken[id] = who; if (lootTaken.Count > 4000) lootTaken.Clear(); }
            MemoryStream ms; var w = W(TLootTaken, out ms);
            w.Write(id); w.Write(owner);
            if (owner == who && who != MpLink.LocalId || owner != who) Send(ulong.MaxValue, ms, true);   // toti afla cine l-a luat
            Loot.NetTaken(id, owner == MpLink.LocalId);
        }

        // ---------------------------------------------------------------
        //  Booth-uri reale in Trade Plaza
        // ---------------------------------------------------------------
        [Serializable] class BoothPack { public List<BoothListing> l = new List<BoothListing>(); }

        public static string Sig(int kind, string name, long price, int idx) { return idx + "|" + kind + "|" + name + "|" + price; }

        string BoothKey()
        {
            var ts = TradeSystem.I;
            if (ts == null || !ts.MyBoothHere || !Game.I.InPlaza) return "none";
            var sb = new System.Text.StringBuilder();
            sb.Append(ts.Server.id).Append(':').Append(Game.I.player.D.boothIndex);
            foreach (var l in Game.I.player.D.booth) sb.Append(',').Append(l.Name).Append(l.price);
            return sb.ToString();
        }

        void SendBooth(ulong to)
        {
            var ts = TradeSystem.I;
            var g = Game.I;
            if (ts == null || g == null) return;
            boothKey = BoothKey();
            bool have = ts.MyBoothHere && g.InPlaza;
            MemoryStream ms; var w = W(TBooth, out ms);
            w.Write(ts.Server != null ? ts.Server.id : 0);
            w.Write(have ? g.player.D.boothIndex : -1);
            w.Write(g.player.D.playerName ?? "Player");
            w.Write(g.player.D.hero);
            var pack = new BoothPack();
            if (have) pack.l.AddRange(g.player.D.booth);
            w.Write(have ? JsonUtility.ToJson(pack) : "");
            Send(to, ms, true);
        }

        void ReadBooth(ulong from, BinaryReader r)
        {
            int server = r.ReadInt32(); int index = r.ReadInt32(); string owner = ChatFilter.CleanName(r.ReadString()); int hero = r.ReadInt32(); string json = r.ReadString();
            if (index < 0 || index >= TradeDB.BoothCount) { booths.Remove(from); return; }
            BoothPack pack = null;
            try { pack = JsonUtility.FromJson<BoothPack>(json); } catch (Exception) { }
            if (pack == null || pack.l == null) pack = new BoothPack();
            var b = new SimBooth { owner = owner, hero = GameData.Heroes[Mathf.Clamp(hero, 0, GameData.Heroes.Length - 1)], col = TradeDB.BoothCols[index], netOwner = from };
            for (int i = 0; i < pack.l.Count && i < TradeDB.MaxListings; i++)
            {
                var l = pack.l[i];
                if (l == null || (l.kind == 0 && l.item == null) || (l.kind == 1 && l.pet == null)) continue;
                var sl = new SimListing { kind = l.kind, item = l.item, pet = l.pet, mat = l.mat, amount = l.amount, price = l.price, netIndex = i };
                sl.sig = Sig(l.kind, l.Name, l.price, i);
                if (pendingBuys.ContainsKey(sl.sig)) sl.sold = true;
                b.stock.Add(sl);
            }
            booths[from] = new NetBooth { server = server, index = index, booth = b };
        }

        /// <summary>Cumperi de la un jucator real: tokenii se rezerva, primesti itemul cand vanzatorul confirma.</summary>
        public static bool RequestBuy(SimBooth b, SimListing l)
        {
            if (!Active || b == null || l == null || b.netOwner == 0 || I.pendingBuys.ContainsKey(l.sig)) return false;
            var D = Game.I.player.D;
            D.tokens -= l.price;
            l.sold = true;
            I.pendingBuys[l.sig] = new PendingBuy { listing = l, price = l.price, time = Time.time };
            MemoryStream ms; var w = W(TBuyReq, out ms);
            w.Write(l.netIndex); w.Write(l.sig); w.Write(l.price); w.Write(D.playerName ?? "Player");
            Send(b.netOwner, ms, true);
            return true;
        }

        void Refund(PendingBuy pb)
        {
            if (pb == null || Game.I == null || Game.I.player == null) return;
            Game.I.player.D.tokens += pb.price;
            if (pb.listing != null) pb.listing.sold = false;
        }
    }
}
