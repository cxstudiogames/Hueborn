// =====================================================================
//  Rewards.cs - REWARDS: cadouri pentru timpul jucat in sesiunea curenta (10 praguri,
//  de la 5 minute la 2 ore, se reseteaza cand iesi din joc) + cadouri pe 7 zile
//  (cate unul pe zi; daca sari o zi continui de unde ai ramas, dupa ziua 7
//  o ia de la capat). Valorile sunt calculate ca sa nu strice economia:
//  ~70 tokens pe zi online (100 tokens = cel mai mic pachet din shop).
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class Rewards
    {
        // tipuri de cadou
        public const int Tokens = 0, Coins = 1, RandPotion = 2, Stones = 3, Shards = 4, Cloaks = 5, AllPotions = 6, EpicItem = 7, LegendPet = 8, GiftBox = 9;

        public struct R
        {
            public int kind; public long amt;
            public R(int k, long a) { kind = k; amt = a; }
        }

        public static readonly int[] OnlineMin = { 5, 10, 15, 20, 30, 45, 60, 75, 90, 120 };
        public static readonly R[][] Online =
        {
            new[] { new R(Tokens, 5) },
            new[] { new R(Coins, 5) },                       // coins = cat ai farma in 5 minute pe cea mai buna harta
            new[] { new R(RandPotion, 1) },
            new[] { new R(Tokens, 10) },
            new[] { new R(Stones, 3) },
            new[] { new R(Shards, 15) },
            new[] { new R(Tokens, 15), new R(RandPotion, 1) },
            new[] { new R(Cloaks, 1) },
            new[] { new R(Tokens, 20) },
            new[] { new R(GiftBox, 1) },
        };
        public static readonly R[][] Week =
        {
            new[] { new R(Coins, 30), new R(Tokens, 10) },
            new[] { new R(AllPotions, 1) },
            new[] { new R(Tokens, 25), new R(Stones, 5) },
            new[] { new R(EpicItem, 1) },
            new[] { new R(Cloaks, 2), new R(Shards, 30) },
            new[] { new R(Tokens, 50) },
            new[] { new R(LegendPet, 1), new R(Cloaks, 3) },
        };

        static Player P { get { return Game.I != null ? Game.I.player : null; } }
        static string Today { get { return System.DateTime.Now.ToString("yyyy-MM-dd"); } }

        /// <summary>Timpul online de azi (apelat din HUD in fiecare frame).</summary>
        // cadourile ONLINE sunt pe sesiune: la fiecare intrare in joc timpul si cadourile luate pornesc de la 0
        // (cadoul zilnic din 7 DAYS nu e afectat)
        static bool sessionReset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { sessionReset = false; }

        public static void Tick(float dt)
        {
            var p = P;
            if (p == null || p.D == null) return;
            var D = p.D;
            if (!sessionReset) { sessionReset = true; D.rwOnline = 0f; D.rwClaimed = 0; D.rwDay = Today; }
            D.rwOnline += Mathf.Min(dt, 1f);
        }

        public static float OnlineMinutes { get { var p = P; return p == null || p.D == null ? 0f : p.D.rwOnline / 60f; } }
        public static bool OnlineClaimed(int i) { var p = P; return p != null && (p.D.rwClaimed & (1 << i)) != 0; }
        public static bool OnlineReady(int i) { return !OnlineClaimed(i) && OnlineMinutes >= OnlineMin[i]; }
        public static bool WeekReady { get { var p = P; return p != null && p.D.rwWeekDate != Today; } }
        public static int WeekIndex { get { var p = P; return p == null ? 0 : Mathf.Clamp(p.D.rwWeekNext, 0, 6); } }

        public static bool AnyReady
        {
            get
            {
                if (P == null || P.D == null) return false;
                if (WeekReady) return true;
                for (int i = 0; i < OnlineMin.Length; i++) if (OnlineReady(i)) return true;
                return false;
            }
        }

        public static void ClaimOnline(int i)
        {
            var p = P;
            if (p == null || !OnlineReady(i)) return;
            p.D.rwClaimed |= 1 << i;
            foreach (var r in Online[i]) Give(r);
            Done(OnlineMin[i] >= 60 ? (OnlineMin[i] / 60) + " HOUR" + (OnlineMin[i] >= 120 ? "S" : "") + " REWARD!" : OnlineMin[i] + " MIN REWARD!");
        }

        public static void ClaimWeek()
        {
            var p = P;
            if (p == null || !WeekReady) return;
            int d = WeekIndex;
            p.D.rwWeekDate = Today;
            p.D.rwWeekNext = (d + 1) % 7;
            foreach (var r in Week[d]) Give(r);
            Done("DAY " + (d + 1) + " REWARD!");
        }

        static void Done(string big)
        {
            var p = P;
            Game.I.hud.BigText(big, new Color(1f, 0.85f, 0.35f));
            Fx.Ring(p.transform.position, new Color(1f, 0.85f, 0.35f), 4f, 50);
            Fx.Sparkle(p.transform.position + Vector3.up * 1.5f, new Color(1f, 0.9f, 0.5f), 20, 1f);
            Sfx.Play("JingleHatch", 0.6f, 1.15f, 0f);
            p.SaveNow();
        }

        /// <summary>Cea mai buna harta deblocata (pentru coins si nivelul itemelor / pets).</summary>
        public static int BestMap
        {
            get
            {
                var p = P; int b = 0;
                if (p == null) return 0;
                for (int i = 0; i < GameData.Maps.Length; i++) if (p.D.level >= GameData.Maps[i].req) b = i;
                return b;
            }
        }

        public static long CoinAmount(long minutes) { return (long)Mathf.Max(50f, GameData.CoinsPerMinute(BestMap) * minutes); }

        static void Give(R r)
        {
            var p = P; var D = p.D;
            var hud = Game.I.hud;
            Color gold = new Color(1f, 0.85f, 0.35f);
            switch (r.kind)
            {
                case Tokens: p.AddTokens(r.amt); hud.Toast("+" + r.amt + " Tokens", new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(19)); break;
                case Coins: { long c = CoinAmount(r.amt); p.AddCoins(c); hud.Toast("+" + Fmt.Num(c) + " Coins", gold, ItemIcons.Get(4)); break; }
                case RandPotion:
                    for (int k = 0; k < r.amt; k++) { int i = Random.Range(0, 3); p.AddPotion(i, 1); hud.Toast("+1 " + Player.PotionNames[i], Player.PotionCols[i], ItemIcons.Get(Player.PotionIcons[i])); }
                    break;
                case AllPotions:
                    for (int i = 0; i < 3; i++) p.AddPotion(i, (int)r.amt);
                    hud.Toast("+" + r.amt + " of every Potion (XP / DMG / COINS)", Player.PotionCols[0], ItemIcons.Get(Player.PotionIcons[0]));
                    break;
                case Stones: p.AddStones((int)r.amt); break;
                case Shards: p.AddShards(r.amt, "Reward"); break;
                case Cloaks: p.AddCloaks((int)r.amt); break;
                case EpicItem: GiveItem(4); break;
                case LegendPet: GivePet(4); break;
                case GiftBox:
                    {
                        int roll = Random.Range(0, 4);
                        hud.Toast("GIFT BOX opened!", gold, ItemIcons.Get(7));
                        if (roll == 0) GivePet(2);
                        else if (roll == 1) GiveItem(2);
                        else if (roll == 2) p.AddCloaks(2);
                        else { p.AddTokens(40); hud.Toast("+40 Tokens", new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(19)); }
                        break;
                    }
            }
        }

        static void GiveItem(int rarity)
        {
            var p = P;
            var it = ItemDB.Make((Slot)Random.Range(0, 4), rarity, BestMap);
            if (p.BagCount >= p.BagSize)
            {
                long v = TradeDB.TokenValue(it) * 3;
                p.AddTokens(v);
                Game.I.hud.Toast("Inventory full - got " + v + " Tokens instead of the item", new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(19));
                return;
            }
            p.AddItem(it);
            Game.I.hud.Toast("New item: " + ItemDB.FullName(it), ItemDB.Col(it.rarity), ItemIcons.Get(26));
        }

        static void GivePet(int rarity)
        {
            var p = P; var D = p.D;
            int m = BestMap;
            var pd = new PetData { rarity = rarity, model = GameData.MapPets[m][rarity], map = m };
            if (D.pets.Count >= GameData.PetStorage)
            {
                long v = TradeDB.PetValue(pd) * 3;
                p.AddTokens(v);
                Game.I.hud.Toast("Pet storage full - got " + v + " Tokens instead of the pet", new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(19));
                return;
            }
            D.pets.Add(pd);
            p.RebuildPets();
            Game.I.hud.Toast(ItemDB.RarityNames[rarity] + " pet: " + GameData.PetName(pd.model) + "!", ItemDB.Col(rarity), ItemIcons.Get(6));
        }

        // ---- pentru fereastra: text + iconita + culoare
        public static string Text(R r)
        {
            switch (r.kind)
            {
                case Tokens: return r.amt + " Tokens";
                case Coins: return Fmt.Num(CoinAmount(r.amt)) + " Coins";
                case RandPotion: return r.amt + " Random Potion";
                case AllPotions: return "All 3 Potions";
                case Stones: return r.amt + " Forge Stones";
                case Shards: return r.amt + " Shards";
                case Cloaks: return r.amt + " Guardian Cloak" + (r.amt > 1 ? "s" : "");
                case EpicItem: return "Legendary Item";
                case LegendPet: return "LEGENDARY Pet";
                default: return "GIFT BOX";
            }
        }
        public static int Icon(R r)
        {
            switch (r.kind)
            {
                case Tokens: return 19;
                case Coins: return 4;
                case RandPotion: case AllPotions: return 36;
                case Stones: return 16;
                case Shards: return 9;
                case Cloaks: return 17;
                case EpicItem: return 26;
                case LegendPet: return 6;
                default: return 7;
            }
        }
        public static Color Col(R r)
        {
            switch (r.kind)
            {
                case Tokens: return new Color(0.55f, 0.95f, 0.75f);
                case Coins: return new Color(1f, 0.85f, 0.25f);
                case RandPotion: case AllPotions: return new Color(0.4f, 0.9f, 1f);
                case Stones: return TradeDB.MatCols[TradeDB.MatStones];
                case Shards: return new Color(0.6f, 0.85f, 1f);
                case Cloaks: return TradeDB.MatCols[TradeDB.MatCloak];
                case EpicItem: return ItemDB.Col(4);
                case LegendPet: return ItemDB.Col(4);
                default: return new Color(1f, 0.55f, 0.85f);
            }
        }
    }
}
