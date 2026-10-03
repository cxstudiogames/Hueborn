// =====================================================================
//  MpLink.cs - multiplayer REAL (Unity Multiplayer Services + Relay +
//  Netcode for GameObjects). Fara servere platite: primul jucator dintr-o
//  "camera" e gazda (host), ceilalti se conecteaza la el prin Relay.
//   * fiecare loc are camerele lui: "p<server>" = Trade Plaza (max 18),
//     "m<harta>" = harta (max 4); cand una e plina se face alta
//   * transporta doar bytes (mesajele jocului sunt in Mp.cs):
//     SendAll / SendTo / SendHost; gazda le trimite mai departe
//  Fara dependinte de restul jocului (la fel ca Ugs.cs).
// =====================================================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if CRPG_MP && CRPG_NGO
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
#endif

namespace ColorRPG.Net
{
    public static class MpLink
    {
        public const string Tag = "hb1";          // versiunea protocolului: jucatorii cu versiuni diferite nu se amesteca
        const string MsgName = "hb";
        const byte ModeAll = 0, ModeTo = 1, ModeHost = 2, ModeSys = 9;
        const byte SysJoin = 1, SysLeave = 2;

        public static string Loc { get; private set; }
        public static bool Busy { get; private set; }
        public static string LastError { get; private set; }
        public static event Action<ulong, byte[]> OnMessage;   // (cine a trimis, date)
        public static event Action<ulong> OnPeerJoined, OnPeerLeft;
        public static event Action OnConnected, OnDisconnected;
        /// <summary>Cati jucatori sunt in fiecare loc ("p3" -> 7), din ultima interogare (RefreshCounts).</summary>
        public static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
        public static float CountsTime { get; private set; } = -999f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Loc = null; Busy = false; LastError = null; OnMessage = null; OnPeerJoined = null; OnPeerLeft = null; OnConnected = null; OnDisconnected = null;
            Counts.Clear(); CountsTime = -999f;
#if CRPG_MP && CRPG_NGO
            session = null; nm = null; pump = null; wasConnected = false; registered = false;
#endif
        }

        public static bool Installed
        {
            get
            {
#if CRPG_MP && CRPG_NGO
                return true;
#else
                return false;
#endif
            }
        }

        static byte[] Envelope(byte mode, ulong origin, ulong target, byte[] payload)
        {
            var ms = new MemoryStream(payload.Length + 17);
            var w = new BinaryWriter(ms);
            w.Write(mode); w.Write(origin); w.Write(target); w.Write(payload);
            return ms.ToArray();
        }

#if CRPG_MP && CRPG_NGO
        static ISession session;
        static NetworkManager nm;
        static bool wasConnected, registered;
        static Pump pump;

        class Pump : MonoBehaviour
        {
            void Update() { Tick(); }
            void OnApplicationQuit() { if (session != null) { try { session.LeaveAsync(); } catch (Exception) { } } }
        }

        static void EnsureManager()
        {
            if (nm != null)
            {
                // dupa o iesire din camera, NetworkManager.Singleton poate ramane gol -> il punem la loc
                if (NetworkManager.Singleton == null) nm.SetSingleton();
                return;
            }
            nm = NetworkManager.Singleton;
            if (nm == null)
            {
                var go = new GameObject("NetworkManager (multiplayer)");
                go.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(go);
                var tr = go.AddComponent<UnityTransport>();
                nm = go.AddComponent<NetworkManager>();
                nm.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = tr,
                    EnableSceneManagement = false,
                    ConnectionApproval = false,
                    PlayerPrefab = null,
                    TickRate = 30,
                    ClientConnectionBufferTimeout = 15,
                };
                nm.RunInBackground = true;
                go.SetActive(true);
            }
            nm.OnClientConnectedCallback += id =>
            {
                if (!nm.IsServer || id == NetworkManager.ServerClientId) return;
                // gazda anunta pe toti ca a venit cineva nou
                SysToAll(SysJoin, id, id);
                if (OnPeerJoined != null) OnPeerJoined(id);
            };
            nm.OnClientDisconnectCallback += id =>
            {
                if (nm.IsServer && id != NetworkManager.ServerClientId)
                {
                    SysToAll(SysLeave, id, ulong.MaxValue);
                    if (OnPeerLeft != null) OnPeerLeft(id);
                }
            };
            if (pump == null)
            {
                var pg = new GameObject("MpLink");
                UnityEngine.Object.DontDestroyOnLoad(pg);
                pump = pg.AddComponent<Pump>();
            }
        }

        /// <summary>Conectat la o camera (reteaua merge).</summary>
        public static bool Connected
        {
            get
            {
                if (session == null || nm == null || !nm.IsListening || nm.ShutdownInProgress) return false;
                return nm.IsServer || nm.IsConnectedClient;
            }
        }
        public static bool IsHost { get { return nm != null && nm.IsServer; } }
        /// <summary>A intrat in camera (chiar daca reteaua inca se conecteaza).</summary>
        public static bool InSession { get { return session != null; } }
        public static ulong LocalId { get { return nm != null ? nm.LocalClientId : 0; } }
        public static int PlayerCount { get { return session != null ? session.PlayerCount : 0; } }
        public static int MaxPlayers { get { return session != null ? session.MaxPlayers : 0; } }

        static void Tick()
        {
            bool c = Connected;
            if (c && !registered) Register();
            if (c != wasConnected)
            {
                wasConnected = c;
                if (c) { if (OnConnected != null) OnConnected(); }
                else
                {
                    registered = false;
                    if (OnDisconnected != null) OnDisconnected();
                    // gazda a plecat: camera NU se inchide - alt jucator devine gazda (host migration) si ceilalti
                    // se reconecteaza singuri. Daca nu reuseste in ~20s, Mp.cs iese si cauta alta camera.
                }
            }
        }

        static void Register()
        {
            if (nm == null || nm.CustomMessagingManager == null) return;
            try { nm.CustomMessagingManager.UnregisterNamedMessageHandler(MsgName); } catch (Exception) { }
            nm.CustomMessagingManager.RegisterNamedMessageHandler(MsgName, Handle);
            registered = true;
        }

        static void Handle(ulong sender, FastBufferReader r)
        {
            byte[] env;
            try
            {
                int len; r.ReadValueSafe(out len);
                if (len <= 0 || len > 1 << 20) return;
                env = new byte[len];
                r.ReadBytesSafe(ref env, len);
            }
            catch (Exception) { return; }
            if (env.Length < 17) return;
            byte mode = env[0];
            ulong origin = BitConverter.ToUInt64(env, 1);
            ulong target = BitConverter.ToUInt64(env, 9);
            var payload = new byte[env.Length - 17];
            Buffer.BlockCopy(env, 17, payload, 0, payload.Length);

            if (nm.IsServer)
            {
                origin = sender;   // gazda nu crede ce scrie in plic: expeditorul e cel real
                if (mode == ModeAll)
                {
                    var fwd = Envelope(ModeAll, origin, 0, payload);
                    foreach (var id in nm.ConnectedClientsIds) if (id != NetworkManager.ServerClientId && id != origin) Raw(id, fwd, payload.Length);
                    Deliver(origin, payload);
                }
                else if (mode == ModeTo)
                {
                    if (target == NetworkManager.ServerClientId) Deliver(origin, payload);
                    else Raw(target, Envelope(ModeTo, origin, target, payload), payload.Length);
                }
                else if (mode == ModeHost) Deliver(origin, payload);
                return;
            }
            if (mode == ModeSys)
            {
                if (payload.Length < 9) return;
                ulong who = BitConverter.ToUInt64(payload, 1);
                if (payload[0] == SysJoin && who != LocalId) { if (OnPeerJoined != null) OnPeerJoined(who); }
                else if (payload[0] == SysLeave) { if (OnPeerLeft != null) OnPeerLeft(who); }
                return;
            }
            Deliver(origin, payload);
        }

        static void Deliver(ulong from, byte[] payload)
        {
            if (OnMessage == null) return;
            try { OnMessage(from, payload); }
            catch (Exception e) { Debug.LogWarning("[ColorRPG] MP message error: " + e.Message); }
        }

        static void SysToAll(byte kind, ulong who, ulong except)
        {
            var p = new byte[9]; p[0] = kind; Buffer.BlockCopy(BitConverter.GetBytes(who), 0, p, 1, 8);
            var env = Envelope(ModeSys, 0, 0, p);
            foreach (var id in nm.ConnectedClientsIds) if (id != NetworkManager.ServerClientId && id != except) Raw(id, env, 9);
        }

        static void Raw(ulong to, byte[] env, int payloadLen, bool reliable = true)
        {
            if (nm == null || nm.CustomMessagingManager == null) return;
            var delivery = !reliable ? NetworkDelivery.UnreliableSequenced : env.Length > 1100 ? NetworkDelivery.ReliableFragmentedSequenced : NetworkDelivery.ReliableSequenced;
            if (!reliable && env.Length > 1100) delivery = NetworkDelivery.ReliableFragmentedSequenced;
            using (var w = new FastBufferWriter(env.Length + 8, Allocator.Temp, env.Length + 8))
            {
                w.WriteValueSafe(env.Length);
                w.WriteBytesSafe(env);
                try { nm.CustomMessagingManager.SendNamedMessage(MsgName, to, w, delivery); }
                catch (Exception e) { LastError = e.Message; }
            }
        }

        // ---------------------------------------------------------------
        //  Trimitere
        // ---------------------------------------------------------------
        /// <summary>Catre toti ceilalti din camera.</summary>
        public static void SendAll(byte[] payload, bool reliable)
        {
            if (!Connected) return;
            if (nm.IsServer)
            {
                var env = Envelope(ModeAll, NetworkManager.ServerClientId, 0, payload);
                foreach (var id in nm.ConnectedClientsIds) if (id != NetworkManager.ServerClientId) Raw(id, env, payload.Length, reliable);
            }
            else Raw(NetworkManager.ServerClientId, Envelope(ModeAll, LocalId, 0, payload), payload.Length, reliable);
        }

        /// <summary>Catre un singur jucator (prin gazda).</summary>
        public static void SendTo(ulong target, byte[] payload, bool reliable)
        {
            if (!Connected) return;
            if (target == LocalId) { Deliver(LocalId, payload); return; }
            if (nm.IsServer) Raw(target, Envelope(ModeTo, NetworkManager.ServerClientId, target, payload), payload.Length, reliable);
            else Raw(NetworkManager.ServerClientId, Envelope(ModeTo, LocalId, target, payload), payload.Length, reliable);
        }

        /// <summary>Doar catre gazda (daca esti tu gazda, mesajul ajunge direct la tine).</summary>
        public static void SendHost(byte[] payload, bool reliable)
        {
            if (!Connected) return;
            if (nm.IsServer) { Deliver(LocalId, payload); return; }
            Raw(NetworkManager.ServerClientId, Envelope(ModeHost, LocalId, 0, payload), payload.Length, reliable);
        }

        // ---------------------------------------------------------------
        //  Camere (sessions)
        // ---------------------------------------------------------------
        /// <summary>Intra intr-o camera pentru locul "loc" (cauta una cu loc liber, altfel o creeaza).</summary>
        public static async void Go(string loc, int max)
        {
            if (Busy) return;
            Busy = true;
            try
            {
                EnsureManager();
                if (session != null) await LeaveInternal();
                if (string.IsNullOrEmpty(loc)) { Loc = null; return; }
                var svc = MultiplayerService.Instance;
                if (svc == null) { LastError = "Multiplayer service not ready"; return; }
                ISession s = null;
                try
                {
                    var q = new QuerySessionsOptions
                    {
                        Count = 25,
                        FilterOptions = new List<FilterOption>
                        {
                            new FilterOption(FilterField.StringIndex1, loc, FilterOperation.Equal),
                            new FilterOption(FilterField.StringIndex2, Tag, FilterOperation.Equal),
                            new FilterOption(FilterField.AvailableSlots, "0", FilterOperation.Greater),
                        }
                    };
                    var res = await svc.QuerySessionsAsync(q);
                    if (res != null && res.Sessions != null)
                        foreach (var info in res.Sessions)
                        {
                            try { s = await svc.JoinSessionByIdAsync(info.Id); break; }
                            catch (Exception e) { LastError = e.Message; }
                        }
                }
                catch (Exception e) { LastError = "query: " + e.Message; }
                if (s == null)
                {
                    var so = new SessionOptions
                    {
                        Name = loc + "-" + UnityEngine.Random.Range(1000, 99999),
                        MaxPlayers = Mathf.Clamp(max, 2, 100),
                        IsPrivate = false,
                        SessionProperties = new Dictionary<string, SessionProperty>
                        {
                            { "loc", new SessionProperty(loc, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                            { "tag", new SessionProperty(Tag, VisibilityPropertyOptions.Public, PropertyIndex.String2) },
                        }
                    }.WithRelayNetwork().WithHostMigration(new KeepRoom());
                    s = await svc.CreateSessionAsync(so);
                }
                session = s;
                Loc = loc;
                if (s != null)
                {
                    s.RemovedFromSession += OnLost;
                    s.Deleted += OnLost;
                    Debug.Log("[ColorRPG] Multiplayer: joined " + loc + " (" + s.PlayerCount + "/" + s.MaxPlayers + ")" + (s.IsHost ? " as HOST" : ""));
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogWarning("[ColorRPG] Multiplayer: could not join " + loc + ": " + e.Message);
                session = null; Loc = null;
            }
            finally { Busy = false; }
        }

        /// <summary>Host migration: camera continua cu alta gazda. Starea jocului e la fiecare jucator, deci nu trimitem nimic.</summary>
        class KeepRoom : IMigrationDataHandler
        {
            public byte[] Generate() { return new byte[] { 1 }; }
            public void Apply(byte[] migrationData) { }
        }

        static void OnLost()
        {
            if (session == null) return;
            Debug.Log("[ColorRPG] Multiplayer: room closed");
            if (!Busy) Leave();
        }

        /// <summary>Iese din camera curenta.</summary>
        public static async void Leave()
        {
            if (Busy) return;
            Busy = true;
            try { await LeaveInternal(); } finally { Busy = false; }
        }

        static async Task LeaveInternal()
        {
            var s = session;
            session = null; Loc = null;
            if (s == null) return;
            try { s.RemovedFromSession -= OnLost; s.Deleted -= OnLost; } catch (Exception) { }
            try { await s.LeaveAsync(); } catch (Exception e) { LastError = e.Message; }
            try { if (nm != null && nm.IsListening) nm.Shutdown(); } catch (Exception) { }
            // asteptam sa se opreasca reteaua inainte de urmatoarea camera
            for (int i = 0; i < 40 && nm != null && (nm.IsListening || nm.ShutdownInProgress); i++) await Task.Delay(50);
        }

        /// <summary>Numara jucatorii din toate camerele jocului (pentru lista de servere din Trade Plaza).</summary>
        public static async void RefreshCounts()
        {
            CountsTime = Time.unscaledTime;
            try
            {
                var svc = MultiplayerService.Instance;
                if (svc == null) return;
                var res = await svc.QuerySessionsAsync(new QuerySessionsOptions
                {
                    Count = 100,
                    FilterOptions = new List<FilterOption> { new FilterOption(FilterField.StringIndex2, Tag, FilterOperation.Equal) }
                });
                Counts.Clear();
                if (res == null || res.Sessions == null) return;
                foreach (var info in res.Sessions)
                {
                    SessionProperty p;
                    if (info.Properties == null || !info.Properties.TryGetValue("loc", out p) || p == null) continue;
                    int n = Mathf.Max(0, info.MaxPlayers - info.AvailableSlots);
                    int old; Counts.TryGetValue(p.Value, out old);
                    Counts[p.Value] = old + n;
                }
            }
            catch (Exception e) { LastError = "count: " + e.Message; }
        }
#else
        public static bool Connected { get { return false; } }
        public static bool IsHost { get { return false; } }
        public static bool InSession { get { return false; } }
        public static ulong LocalId { get { return 0; } }
        public static int PlayerCount { get { return 0; } }
        public static int MaxPlayers { get { return 0; } }
        public static void SendAll(byte[] payload, bool reliable) { }
        public static void SendTo(ulong target, byte[] payload, bool reliable) { }
        public static void SendHost(byte[] payload, bool reliable) { }
        public static void Go(string loc, int max) { }
        public static void Leave() { }
        public static void RefreshCounts() { }
#endif
    }
}
