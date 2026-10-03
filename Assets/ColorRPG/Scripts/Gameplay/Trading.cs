// =====================================================================
//  Trading.cs - Trade Plaza pe servere:
//   * fiecare server are pana la 18 jucatori si 10 booth-uri (tarabe)
//   * ceilalti jucatori (simulati) ocupa booth-uri si vand iteme din drop-uri
//   * tu revendici (claim) un booth liber; daca toate sunt ocupate, schimbi serverul
//   * booth-ul tau vinde DOAR cat timp esti in Trade Plaza pe serverul lui;
//     cand pleci (alta harta / alt server / iesi din joc) booth-ul devine
//     UNCLAIMED - listarile raman la tine si urca din nou pe urmatorul booth
//   * pretul il alegi tu (in tokens) - nu se negociaza: cine vrea cumpara
//   * NPC-urile NU mai vand iteme: itemele pica doar de pe harti
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ColorRPG
{
    public class TradeSystem : MonoBehaviour
    {
        public static TradeSystem I;
        Player P { get { return Game.I.player; } }
        SaveData D { get { return Game.I.player.D; } }

        float tick, offerT = 60f;
        long bucket = -1;
        public Vector3 playerBoothPos;                     // setat de BoothSlot (booth-ul tau)
        public event Action<BoothListing> OnSold;
        public PlazaServer Server { get; private set; }
        readonly HashSet<string> soldKeys = new HashSet<string>();

        // oferta curenta de la un cumparator
        public class Offer { public string who; public BoothListing listing; public long amount; public float timeLeft; }
        [NonSerialized] public Offer pending;

        public static TradeSystem Create()
        {
            var go = new GameObject("TradeSystem");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            I = go.AddComponent<TradeSystem>();
            return I;
        }

        // ---------------------------------------------------------------
        //  Pornire: vanzari offline
        // ---------------------------------------------------------------
        public void Init()
        {
            long now = TradeDB.Now();
            // booth-ul nu ramane al tau cand nu esti pe server (nici offline)
            if (HasBooth) { D.boothIndex = -1; D.boothServer = 0; D.boothClaimed = false; }
            D.lastSeen = now;
            D.npcStock.Clear();   // magazinele NPC nu mai exista
            if (D.serverId <= 0) D.serverId = Random.Range(1, TradeDB.ServerCount + 1);
            JoinServer(D.serverId, false);
        }

        /// <summary>Booth-ul devine UNCLAIMED (ai plecat din piata / de pe server). Listarile raman la tine.</summary>
        public void Unclaim(bool toast)
        {
            if (!HasBooth) return;
            D.boothIndex = -1; D.boothServer = 0; D.boothClaimed = false; pending = null;
            if (toast && Game.I != null && Game.I.hud != null)
                Game.I.hud.Toast("Your booth is now unclaimed." + (D.booth.Count > 0 ? " Your " + D.booth.Count + " listings are kept - claim a booth to sell again." : ""), new Color(1f, 0.85f, 0.4f), ItemIcons.Get(21));
            P.SaveNow();
        }

        /// <summary>Booth-ul tau vinde acum (esti in piata, pe serverul lui).</summary>
        public bool Selling { get { return MyBoothHere && Game.I != null && Game.I.InPlaza; } }

        public int MaxTier
        {
            get
            {
                int t = 0;
                foreach (var m in GameData.Maps) if (P.Level >= m.req) t = Mathf.Max(t, m.tier);
                return t;
            }
        }

        // ---------------------------------------------------------------
        //  Servere
        // ---------------------------------------------------------------
        public bool HasBooth { get { return D.boothIndex >= 0 && D.boothServer > 0; } }
        /// <summary>Booth-ul tau e pe serverul pe care esti acum.</summary>
        public bool MyBoothHere { get { return HasBooth && Server != null && D.boothServer == Server.id; } }
        public bool IsMine(int booth) { return MyBoothHere && D.boothIndex == booth; }
        public int FreeBooths { get { int n = 0; for (int i = 0; i < TradeDB.BoothCount; i++) if (Server.booths[i] == null && !IsMine(i)) n++; return n; } }

        static long Bucket() { return TradeDB.Now() / (long)TradeDB.RestockSeconds; }

        /// <summary>Genereaza serverul "id" (acelasi rezultat pentru aceeasi perioada de 15 minute).</summary>
        public void JoinServer(int id, bool announce)
        {
            if (id == PrivateId && !D.passPrivate) id = 1;
            if (id != PrivateId) id = Mathf.Clamp(id, 1, TradeDB.ServerCount);
            bucket = Bucket();
            var srv = new PlazaServer { id = id };
            if (id == PrivateId)
            {
                // serverul tau privat: doar tu + prietenii invitati (max 18), fara booth-uri straine
                srv.players = Mathf.Min(TradeDB.MaxPlayers, 1 + D.privateInvites.Count);
                Server = srv;
                D.serverId = id;
                if (announce && Game.I.InPlaza) Game.I.hud.BigText("PRIVATE SERVER", new Color(1f, 0.85f, 0.35f));
                return;
            }
            if (Mp.Online)
            {
                // multiplayer: pe server sunt doar jucatori reali (booth-urile lor vin din Mp)
                srv.players = Mp.InPlazaRoom ? Mp.Players : 1;
                Server = srv;
                D.serverId = id;
                if (announce && Game.I.InPlaza) Game.I.hud.BigText(ServerName(id).ToUpper(), new Color(0.6f, 0.9f, 1f));
                return;
            }
            var state = Random.state;
            Random.InitState(id * 7919 + (int)(bucket % 100000) * 31);
            srv.players = Random.Range(8, TradeDB.MaxPlayers + 1);
            int occupied = Mathf.Min(srv.players - 1, Random.Range(4, TradeDB.BoothCount + 2));   // unele servere sunt pline
            var free = new List<int>();
            for (int i = 0; i < TradeDB.BoothCount; i++)
                if (!(HasBooth && D.boothServer == id && D.boothIndex == i)) free.Add(i);
            var names = new List<string>(TradeDB.ShopperNames);
            int tier = MaxTier;
            for (int k = 0; k < occupied && free.Count > 0; k++)
            {
                int bi = free[Random.Range(0, free.Count)];
                free.Remove(bi);
                var b = new SimBooth();
                int ni = Random.Range(0, names.Count);
                b.owner = names[ni]; names.RemoveAt(ni);
                b.hero = GameData.Heroes[Random.Range(0, GameData.Heroes.Length)];
                b.col = TradeDB.BoothCols[bi];
                int n = Random.Range(3, 7);
                for (int j = 0; j < n; j++)
                {
                    int t = Mathf.Clamp(tier - (Random.value < 0.4f ? 1 : 0), 0, ItemDB.MaxTier);
                    var sl = new SimListing { sold = soldKeys.Contains(Key(id, bi, j)) };
                    float r = Random.value;
                    if (r < 0.6f || j == 0)
                    {
                        sl.kind = 0; sl.item = ItemDB.RandomDrop(t, 0.8f, j == 0 ? 2 : 0, 0.45f);
                        // id fix pentru aceeasi oferta -> acelasi aspect cand revii pe server (cand o cumperi primeste id nou, acelasi aspect)
                        sl.item.id = -(StableHash(Key(id, bi, j)) & 0x3fffffff) - 1;
                    }
                    else if (r < 0.8f)
                    {
                        int pr = ItemDB.FixRarity(Random.Range(0, 4) + (Random.value < 0.15f ? 1 : 0));
                        sl.kind = 1; sl.pet = new PetData { rarity = pr, map = t, model = GameData.MapPets[t][pr] };
                    }
                    else
                    {
                        sl.kind = 2; sl.mat = Random.Range(0, 5);
                        sl.amount = sl.mat == TradeDB.MatStones ? Random.Range(1, 4) * 10 : sl.mat == TradeDB.MatCloak ? 1 : Random.Range(1, 4);
                    }
                    sl.price = System.Math.Max(1L, (long)Mathf.Round(TradeDB.ListingValue(sl.kind, sl.item, sl.pet, sl.mat, sl.amount) * Random.Range(0.85f, 1.5f)));
                    b.stock.Add(sl);
                }
                srv.booths[bi] = b;
            }
            Random.state = state;
            Server = srv;
            D.serverId = id;
            if (announce && Game.I.InPlaza)
                Game.I.hud.BigText(ServerName(id).ToUpper(), new Color(0.6f, 0.9f, 1f));
        }

        /// <summary>Cati jucatori are serverul "id" acum (fara sa intri pe el) - pentru lista de servere.</summary>
        public int PeekPlayers(int id)
        {
            if (Mp.Online)
            {
                if (Time.unscaledTime - Net.MpLink.CountsTime > 15f) Net.MpLink.RefreshCounts();
                int c; Net.MpLink.Counts.TryGetValue("p" + id, out c);
                if (Server != null && Server.id == id && Mp.InPlazaRoom) c = Mp.Players;
                return c;
            }
            var state = Random.state;
            Random.InitState(id * 7919 + (int)(Bucket() % 100000) * 31);
            int n = Random.Range(8, TradeDB.MaxPlayers + 1);
            Random.state = state;
            if (Server != null && Server.id == id) n = Server.players;
            return n;
        }

        // ---------------------------------------------------------------
        //  Server privat + prieteni
        // ---------------------------------------------------------------
        /// <summary>Id-ul serverului tau privat (in afara listei publice).</summary>
        public const int PrivateId = 999;
        public const int MaxFriends = 100;
        public static bool IsPrivate(int id) { return id == PrivateId; }
        // fiecare piata (server) are numele ei: 8 x 5 = 40 de nume unice
        static readonly string[] MarketAdj = { "Amber", "Cobalt", "Jade", "Ruby", "Sunny", "Misty", "Copper", "Velvet" };
        static readonly string[] MarketNoun = { "Bazaar", "Market", "Square", "Harbor", "Fair" };
        public static string ServerName(int id)
        {
            if (id == PrivateId) return "Your Private Market";
            int k = Mathf.Max(0, id - 1);
            return MarketAdj[k % MarketAdj.Length] + " " + MarketNoun[(k / MarketAdj.Length) % MarketNoun.Length];
        }

        public bool AddFriend(string name)
        {
            name = name == null ? "" : name.Trim();
            if (name.Length < 2) { Game.I.Say("Type your friend's username first.", 2f); Sfx.Play("Error", 0.4f); return false; }
            if (name == D.playerName) { Game.I.Say("You can't add yourself!", 2f); Sfx.Play("Error", 0.4f); return false; }
            if (D.friends.Exists(f => f.ToLowerInvariant() == name.ToLowerInvariant())) { Game.I.Say(name + " is already your friend.", 2f); Sfx.Play("Error", 0.4f); return false; }
            if (D.friends.Count >= MaxFriends) { Game.I.Say("You can have at most " + MaxFriends + " friends.", 2.5f); Sfx.Play("Error", 0.4f); return false; }
            D.friends.Add(name);
            Game.I.hud.Toast("Friend request sent to " + name + " (accepted when the game is online)", new Color(0.6f, 0.9f, 1f), ItemIcons.Get(21));
            Sfx.Play("Equip", 0.5f, 1.2f);
            P.SaveNow();
            return true;
        }

        public void RemoveFriend(string name)
        {
            D.friends.Remove(name);
            D.privateInvites.Remove(name);
            Sfx.Play("Close", 0.5f);
            P.SaveNow();
        }

        /// <summary>Invita / scoate un prieten de pe serverul privat (max 17 invitati + tu).</summary>
        public void ToggleInvite(string name)
        {
            if (!D.passPrivate) return;
            if (D.privateInvites.Remove(name)) { Sfx.Play("Close", 0.4f); }
            else
            {
                if (D.privateInvites.Count >= TradeDB.MaxPlayers - 1) { Game.I.Say("Your private server is full (" + TradeDB.MaxPlayers + " players).", 2.5f); Sfx.Play("Error", 0.4f); return; }
                D.privateInvites.Add(name);
                Game.I.hud.Toast("Invited " + name + " to your private server", new Color(1f, 0.85f, 0.35f), ItemIcons.Get(21));
                Sfx.Play("Equip", 0.5f, 1.2f);
            }
            if (Server != null && Server.id == PrivateId) Server.players = Mathf.Min(TradeDB.MaxPlayers, 1 + D.privateInvites.Count);
            P.SaveNow();
        }

        public static readonly string[] Regions = { "EU", "EU", "EU", "EU", "EU", "EU", "NA", "NA", "SA", "ASIA", "ASIA", "OCE" };
        public static string Region(int id) { return Regions[(id * 7) % Regions.Length]; }

        /// <summary>Intra pe serverul "id" din Trade Plaza (te duce in piata daca nu esti acolo).</summary>
        public void JoinFromHub(int id)
        {
            if (Game.I.TradeLocked) { Game.I.Say("The TRADE PLAZA unlocks at level " + GameData.TradeLevel + "!", 3f); Sfx.Play("Error", 0.6f); return; }
            if (HasBooth && D.boothServer != id) Unclaim(true);
            JoinServer(id, true);
            if (!Game.I.InPlaza) Game.I.GoPlaza();
            else { Sfx.Play("Portal", 0.6f); Game.I.hud.DeathFade(0.5f); }
            P.SaveNow();
        }

        static int StableHash(string s) { unchecked { int h = 23; foreach (char c in s) h = h * 31 + c; return h; } }

        string Key(int srv, int booth, int j) { return srv + "_" + bucket + "_" + booth + "_" + j; }

        /// <summary>Schimba pe alt server (pierzi booth-ul revendicat, itemele raman la tine).</summary>
        public void ChangeServer()
        {
            Unclaim(true);
            int id = Server != null ? Server.id : 1;
            for (int t = 0; t < 10; t++)
            {
                int n = Random.Range(1, TradeDB.ServerCount + 1);
                if (n != id) { id = n; break; }
            }
            JoinServer(id, true);
            Sfx.Play("Portal", 0.6f);
            Game.I.hud.DeathFade(0.5f);
            P.SaveNow();
        }

        /// <summary>Revendica booth-ul "booth" pe serverul curent.</summary>
        public bool Claim(int booth)
        {
            if (Server == null || booth < 0 || booth >= TradeDB.BoothCount) return false;
            if (Server.booths[booth] != null) { Game.I.Say("This booth belongs to " + Server.booths[booth].owner + "!", 2f); Sfx.Play("Error", 0.5f); return false; }
            if (MyBoothHere) { Game.I.Say("You already have a booth on this server!", 2f); Sfx.Play("Error", 0.5f); return false; }
            if (HasBooth) Unclaim(false);
            D.boothServer = Server.id;
            D.boothIndex = booth;
            D.boothClaimed = true;
            Game.I.hud.BigText("BOOTH CLAIMED!", new Color(1f, 0.8f, 0.2f));
            if (D.booth.Count > 0) Game.I.hud.Toast("Your " + D.booth.Count + " listings are on sale again!", new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(19));
            Sfx.Play("JingleHatch", 0.6f, 1.1f, 0f);
            P.SaveNow();
            return true;
        }

        public bool BuySim(int booth, SimListing l)
        {
            if (Server == null || l == null || l.sold) return false;
            var bb = booth >= 0 && booth < Server.booths.Length ? Server.booths[booth] : null;
            if (bb == null || !bb.stock.Contains(l)) { Game.I.Say("This booth just changed its stock.", 2f); Sfx.Play("Error", 0.5f); return false; }
            if (D.tokens < l.price) { Game.I.Say("Not enough tokens! Get more in the SHOP (TOKENS).", 2.5f); Sfx.Play("Error", 0.6f); return false; }
            if (l.kind == 0 && P.BagCount >= P.BagSize) { Game.I.Say("Your inventory is full! Sell something first.", 2f); Sfx.Play("Error", 0.6f); return false; }
            if (l.kind == 1 && D.pets.Count >= GameData.PetStorage) { Game.I.Say("Your pet storage is full!", 2f); Sfx.Play("Error", 0.6f); return false; }
            if (bb.netOwner != 0)
            {
                // booth-ul unui jucator real: cererea pleaca la el, primesti itemul cand confirma
                if (!Mp.RequestBuy(bb, l)) { Game.I.Say(bb.owner + " is not reachable right now.", 2f); Sfx.Play("Error", 0.5f); return false; }
                Game.I.Say("Buying from " + bb.owner + "...", 1.5f);
                return true;
            }
            D.tokens -= l.price;
            l.sold = true;
            var b = Server.booths[booth];
            if (b != null) soldKeys.Add(Key(Server.id, booth, b.stock.IndexOf(l)));
            D.trades++;
            Receive(l.kind, l.item, l.pet, l.mat, l.amount);
            Sfx.Play("Sell", 0.6f);
            P.SaveNow();
            return true;
        }

        /// <summary>Multiplayer: ai cumparat de la un jucator real (tokenii au fost deja platiti).</summary>
        public void ReceiveNet(BoothListing l, string seller)
        {
            D.trades++;
            if (l.kind == 0 && l.item != null) GearVisuals.FreshIdKeepLook(l.item);   // id nou la tine, acelasi aspect
            Receive(l.kind, l.item, l.pet, l.mat, l.amount);
            Game.I.hud.Toast("Bought " + l.Name + " from " + seller, l.Col, ItemIcons.Get(19));
            Sfx.Play("Sell", 0.6f);
            P.SaveNow();
        }

        /// <summary>Multiplayer: un jucator real cumpara listarea "idx" de pe booth-ul tau. null = nu mai e (vanduta / schimbata).</summary>
        public BoothListing NetSell(int idx, string sig, long price, string buyer)
        {
            if (!Selling || idx < 0 || idx >= D.booth.Count) return null;
            var l = D.booth[idx];
            if (Mp.Sig(l.kind, l.Name, l.price, idx) != sig || l.price != price) return null;
            Sold(l, l.price, buyer);
            D.trades++;
            return l;
        }

        /// <summary>Primesti ce ai cumparat (item / pet / material).</summary>
        void Receive(int kind, Item item, PetData pet, int mat, int amount)
        {
            if (kind == 0)
            {
                if (item.id <= 0) GearVisuals.FreshIdKeepLook(item);   // ofertele simulate au id temporar
                P.AddItem(item);
                Game.I.hud.ItemToast(item);
                Sfx.Play(item.rarity >= 3 ? "ItemRare" : "ItemDrop", 0.7f);
            }
            else if (kind == 1)
            {
                pet.equipped = false;
                D.pets.Add(pet);
                Game.I.hud.Toast("New pet: " + ItemDB.RarityNames[pet.rarity] + " " + GameData.PetName(pet.model) + " (open PETS to equip)", ItemDB.Col(pet.rarity), ItemIcons.Get(6));
                Sfx.Play("JingleHatch", 0.6f, 1.1f, 0f);
            }
            else GiveMat(mat, amount);
        }

        public void GiveMat(int mat, int amount)
        {
            if (mat == TradeDB.MatStones) D.forgeStones += amount;
            else if (mat == TradeDB.MatCloak) D.cloaks += amount;
            else P.AddPotion(mat - TradeDB.MatPotXp, amount);
            Game.I.hud.Toast("+" + amount + " " + TradeDB.MatNames[mat], TradeDB.MatCols[mat], ItemIcons.Get(TradeDB.MatIcon[mat]));
        }

        public int MatOwned(int mat)
        {
            if (mat == TradeDB.MatStones) return D.forgeStones;
            if (mat == TradeDB.MatCloak) return D.cloaks;
            return D.potions[mat - TradeDB.MatPotXp];
        }

        void TakeMat(int mat, int amount)
        {
            if (mat == TradeDB.MatStones) D.forgeStones -= amount;
            else if (mat == TradeDB.MatCloak) D.cloaks -= amount;
            else D.potions[mat - TradeDB.MatPotXp] -= amount;
        }

        /// <summary>Cat se pune la vanzare dintr-un material (pietrele se vand in pachete de 10).</summary>
        public static int MatBundle(int mat) { return mat == TradeDB.MatStones ? 10 : 1; }

        // ---------------------------------------------------------------
        //  Update: vanzari live + oferte + booth-urile celorlalti se schimba
        // ---------------------------------------------------------------
        void Update()
        {
            if (Game.I == null || Game.I.player == null) return;
            float dt = Time.deltaTime;
            tick += dt;
            if (tick >= 1f)
            {
                tick -= 1f;
                D.lastSeen = TradeDB.Now();
                if (Server != null && Bucket() != bucket && !Mp.Online)
                {
                    JoinServer(Server.id, false);
                    if (Game.I.InPlaza) Game.I.hud.Toast("Players on this server changed their booths!", new Color(0.6f, 0.9f, 1f));
                }
                // vanzari doar cat timp esti in piata, pe serverul booth-ului (fara negocieri: pretul e al tau)
                if (Selling && !Mp.Online)   // online: cumpara doar jucatorii reali
                    for (int i = D.booth.Count - 1; i >= 0; i--)
                    {
                        var l = D.booth[i];
                        if (Random.value < TradeDB.SaleRate(l)) Sold(l, l.price, TradeDB.ShopperNames[Random.Range(0, TradeDB.ShopperNames.Length)]);
                    }
            }
            pending = null;
        }

        void Sold(BoothListing l, long amount, string who)
        {
            D.booth.Remove(l);
            D.boothEarnings += amount;
            D.boothSales++;
            D.boothTotal += amount;
            Game.I.hud.Toast(who + " bought " + l.Name + " from your booth: +" + Fmt.Num(amount) + " tokens", l.Col, ItemIcons.Get(19));
            Sfx.Play("Sell", 0.7f);
            if (Game.I.InPlaza && MyBoothHere)
            {
                Fx.Sparkle(playerBoothPos + Vector3.up * 2f, new Color(1f, 0.85f, 0.3f), 14, 1f);
                Game.I.hud.Pop(playerBoothPos + Vector3.up * 3.5f, "+" + Fmt.Num(amount), new Color(1f, 0.9f, 0.3f), 1.3f);
            }
            if (OnSold != null) OnSold(l);
            P.SaveNow();
        }

        public void AcceptOffer()
        {
            if (pending == null) return;
            if (D.booth.Contains(pending.listing))
            {
                Sold(pending.listing, pending.amount, pending.who);
                D.trades++;
            }
            pending = null;
        }

        public void DeclineOffer()
        {
            if (pending == null) return;
            Game.I.hud.Toast(pending.who + ": \"Okay, maybe next time!\"", new Color(0.8f, 0.8f, 0.9f));
            pending = null;
            Sfx.Play("Close", 0.5f);
        }

        // ---------------------------------------------------------------
        //  Booth-ul jucatorului
        // ---------------------------------------------------------------
        bool BoothFull()
        {
            if (D.booth.Count >= TradeDB.MaxListings) { Game.I.Say("Your booth is full (" + TradeDB.MaxListings + " listings)!", 2f); Sfx.Play("Error", 0.5f); return true; }
            return false;
        }

        public const long MaxPrice = 999999999L;
        static long ClampPrice(long p) { return Math.Max(1L, Math.Min(p, MaxPrice)); }

        public bool List(Item it, long price = 0)
        {
            if (it == null || P.IsEquipped(it)) { Sfx.Play("Error", 0.5f); return false; }
            if (BoothFull()) return false;
            D.items.Remove(it);
            it.locked = false;
            D.booth.Add(new BoothListing { kind = 0, item = it, price = price > 0 ? ClampPrice(price) : (long)Mathf.Ceil(TradeDB.TokenValue(it) * 1.1f), listedAt = D.playTime });
            Sfx.Play("Equip", 0.6f);
            P.SaveNow();
            return true;
        }

        public bool ListPet(PetData pd, long price = 0)
        {
            if (pd == null || !D.pets.Contains(pd)) return false;
            if (pd.equipped) { Game.I.Say("Unequip the pet first (PETS window).", 2f); Sfx.Play("Error", 0.5f); return false; }
            if (BoothFull()) return false;
            D.pets.Remove(pd);
            D.booth.Add(new BoothListing { kind = 1, pet = pd, price = price > 0 ? ClampPrice(price) : (long)Mathf.Ceil(TradeDB.PetValue(pd) * 1.1f), listedAt = D.playTime });
            Sfx.Play("Equip", 0.6f);
            P.SaveNow();
            return true;
        }

        /// <summary>Pune pe booth "n" bucati dintr-un material la pretul ales (in tokens). price &lt;= 0 = pret corect.</summary>
        public bool ListMat(int mat, int n = 0, long price = 0)
        {
            if (n <= 0) n = MatBundle(mat);
            if (MatOwned(mat) < n) { Game.I.Say("You only have " + MatOwned(mat) + "x " + TradeDB.MatNames[mat] + ".", 2f); Sfx.Play("Error", 0.5f); return false; }
            if (BoothFull()) return false;
            price = price <= 0 ? (long)Mathf.Ceil(TradeDB.MatValue(mat, n) * 1.1f) : ClampPrice(price);
            TakeMat(mat, n);
            D.booth.Add(new BoothListing { kind = 2, mat = mat, amount = n, price = price, listedAt = D.playTime });
            Game.I.hud.Toast("Listed " + n + "x " + TradeDB.MatNames[mat] + " for " + Fmt.Num(price) + " tokens", TradeDB.MatCols[mat], ItemIcons.Get(19));
            Sfx.Play("Equip", 0.6f);
            P.SaveNow();
            return true;
        }

        public void Unlist(BoothListing l)
        {
            if (l == null || !D.booth.Contains(l)) return;
            if (l.kind == 0 && P.BagCount >= P.BagSize) { Game.I.Say("Your inventory is full!", 2f); Sfx.Play("Error", 0.5f); return; }
            if (l.kind == 1 && D.pets.Count >= GameData.PetStorage) { Game.I.Say("Your pet storage is full!", 2f); Sfx.Play("Error", 0.5f); return; }
            D.booth.Remove(l);
            if (l.kind == 0) D.items.Add(l.item);
            else if (l.kind == 1) { l.pet.equipped = false; D.pets.Add(l.pet); }
            else GiveMat(l.mat, l.amount);
            if (pending != null && pending.listing == l) pending = null;
            Sfx.Play("Close", 0.5f);
            P.SaveNow();
        }

        /// <summary>Pretul il alegi tu (in tokens).</summary>
        public void SetPrice(BoothListing l, long price)
        {
            if (l == null || !D.booth.Contains(l)) return;
            l.price = ClampPrice(price);
            Sfx.Play("Click", 0.4f, 1.1f);
            P.SaveNow();
        }

        public void Collect()
        {
            if (D.boothEarnings <= 0) { Sfx.Play("Error", 0.4f); return; }
            long e = D.boothEarnings;
            D.boothEarnings = 0;
            P.AddTokens(e);
            Game.I.hud.BigText("+" + Fmt.Num(e) + " TOKENS", new Color(0.55f, 0.95f, 0.75f));
            Sfx.Play("Coin", 0.8f);
            Sfx.Play("Sell", 0.8f);
            if (Game.I.InPlaza && MyBoothHere) Fx.Fountain(playerBoothPos + Vector3.up * 1.5f, new Color(0.55f, 0.95f, 0.75f), 36);
            P.SaveNow();
        }
    }
}
