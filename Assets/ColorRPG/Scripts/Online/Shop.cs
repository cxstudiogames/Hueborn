// =====================================================================
//  Shop.cs - ce se poate cumpara cu bani reali (prin Google Play Billing).
//  Fiecare produs are un id (acelasi trebuie creat in Play Console >
//  Monetize > Products) si ce primesti cand plata e confirmata.
//  In editor / pentru admin, fara magazin conectat: obiectul se da GRATIS
//  ca test. Pentru jucatori, fara magazin: nu se da nimic (mesaj).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using ColorRPG.Net;

namespace ColorRPG
{
    public static class Shop
    {
        // ---- id-urile produselor (Play Console) + pretul afisat cand Google nu e disponibil
        public const string PassMagnet = "pass_magnet", PassAuto = "pass_auto", PassTeleport = "pass_teleport",
                            PassPets = "pass_pets", PassPrivate = "pass_private", PassAll = "pass_all";
        public static readonly string[] Tokens = { "tokens_1", "tokens_2", "tokens_3" };
        public static readonly long[] TokenAmounts = { 100, 550, 1200 };
        public static readonly string[] Bags = { "bag_20", "bag_40", "bag_100" };
        public static readonly string[] Coins = { "coins_1", "coins_2", "coins_3" };
        public static readonly string[] Shards = { "shards_1", "shards_2", "shards_3" };
        public static readonly long[] ShardAmounts = { 10, 50, 250 };

        static readonly Dictionary<string, string> defaultPrice = new Dictionary<string, string>
        {
            { PassMagnet, "$1.49" }, { PassAuto, "$1.99" }, { PassTeleport, "$0.99" }, { PassPets, "$1.99" }, { PassPrivate, "$2.49" }, { PassAll, "$4.99" },
            { "tokens_1", "$0.49" }, { "tokens_2", "$1.99" }, { "tokens_3", "$3.99" },
            { "bag_20", "$0.49" }, { "bag_40", "$0.99" }, { "bag_100", "$1.99" },
            { "coins_1", "$0.49" }, { "coins_2", "$0.99" }, { "coins_3", "$1.99" },
            { "shards_1", "$0.49" }, { "shards_2", "$0.99" }, { "shards_3", "$1.99" },
        };

        static bool inited;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { inited = false; }

        public static void Init()
        {
            if (inited) return;
            inited = true;
            Iap.OnDeliver = Deliver;
            Iap.OnRestore = Restore;
            Iap.OnFailed = (id, why) =>
            {
                if (Game.I != null && Game.I.hud != null && why != "UserCancelled")
                    Game.I.hud.Toast("PURCHASE FAILED\n" + why, new Color(1f, 0.5f, 0.45f));
            };
            var defs = new List<Iap.Def>();
            foreach (var kv in defaultPrice)
            {
                string id = kv.Key;
                bool consumable = id.StartsWith("tokens_") || id.StartsWith("coins_") || id.StartsWith("shards_");
                defs.Add(new Iap.Def(id, consumable));
            }
            Iap.Init(defs);
        }

        public static string Price(string id)
        {
            string fb; defaultPrice.TryGetValue(id, out fb);
            return Iap.Price(id, fb ?? "BUY");
        }

        /// <summary>Apasat BUY: plata prin Google Play (sau test gratis in editor / admin).</summary>
        public static void Buy(string id)
        {
            var hud = Game.I != null ? Game.I.hud : null;
            if (Iap.Buy(id)) return;   // Google deschide fereastra de plata; obiectul vine in Deliver()
            if (Application.isEditor || AdminAccess.IsAdmin)
            {
                Deliver(id);
                if (hud != null) hud.Toast("TEST PURCHASE\nGiven for free (editor / admin only). Players pay with Google Play.", new Color(0.6f, 0.85f, 1f));
                return;
            }
            if (hud != null) hud.Toast("STORE NOT AVAILABLE\nCheck your internet connection and that you're signed in to Google Play.", new Color(1f, 0.55f, 0.45f));
            Sfx.Play("Error", 0.5f);
        }

        /// <summary>Reda in liniste pass-urile / spatiul din inventar deja platite (fara mesaje, doar daca lipsesc).</summary>
        static void Restore(string id)
        {
            var g = Game.I;
            if (g == null || g.player == null || g.player.D == null) return;
            var p = g.player; var D = p.D; bool changed = false;
            switch (id)
            {
                case PassMagnet: if (!D.passMagnet) { D.passMagnet = true; changed = true; } break;
                case PassAuto: if (!D.passAuto) { D.passAuto = true; changed = true; } break;
                case PassTeleport: if (!D.passTeleport) { D.passTeleport = true; changed = true; } break;
                case PassPets: if (!D.passGame) { D.passGame = true; p.RebuildPets(); changed = true; } break;
                case PassPrivate: if (!D.passPrivate) { D.passPrivate = true; changed = true; } break;
                case PassAll:
                    if (!(D.passMagnet && D.passAuto && D.passTeleport && D.passGame && D.passPrivate))
                    { D.passMagnet = D.passAuto = D.passTeleport = D.passGame = D.passPrivate = true; p.RebuildPets(); changed = true; }
                    break;
                case "bag_20": if (!D.bag20) { D.bag20 = true; changed = true; } break;
                case "bag_40": if (!D.bag40) { D.bag40 = true; changed = true; } break;
                case "bag_100": if (!D.bag100) { D.bag100 = true; changed = true; } break;
            }
            if (changed) { p.SaveNow(); if (g.hud != null) g.hud.Toast("PURCHASES RESTORED\nYour Google Play purchases are back.", new Color(0.55f, 0.95f, 0.75f)); }
        }

        /// <summary>Da jucatorului ce a cumparat. True = dat (plata poate fi confirmata).</summary>
        static bool Deliver(string id)
        {
            var g = Game.I;
            if (g == null || g.player == null || g.player.D == null) return false;   // jocul inca se incarca: Google o retrimite data viitoare
            var p = g.player; var D = p.D; var hud = g.hud;
            string big = null;
            switch (id)
            {
                case PassMagnet: D.passMagnet = true; big = "MAGNET PASS!"; break;
                case PassAuto: D.passAuto = true; D.autoAttack = true; big = "AUTO SWING PASS!"; break;
                case PassTeleport: D.passTeleport = true; big = "TELEPORT PASS!"; break;
                case PassPets: D.passGame = true; p.RebuildPets(); big = "+2 PET SLOTS PASS!"; break;
                case PassPrivate: D.passPrivate = true; big = "PRIVATE SERVER PASS!"; break;
                case PassAll:
                    D.passMagnet = D.passAuto = D.passTeleport = D.passGame = D.passPrivate = true;
                    D.autoAttack = true; p.RebuildPets(); big = "ALL PASSES!"; break;
                case "bag_20": D.bag20 = true; big = "+20 INVENTORY SLOTS!"; break;
                case "bag_40": D.bag40 = true; big = "+40 INVENTORY SLOTS!"; break;
                case "bag_100": D.bag100 = true; big = "+100 INVENTORY SLOTS!"; break;
                default:
                    int k = System.Array.IndexOf(Tokens, id);
                    if (k >= 0) { p.AddTokens(TokenAmounts[k]); big = "+" + TokenAmounts[k] + " TOKENS"; Sfx.Play("Coin", 0.8f, 1.2f); break; }
                    k = System.Array.IndexOf(Coins, id);
                    if (k >= 0) { long c = GameData.CoinPack(D.level, k); p.AddCoins(c); if (hud != null) hud.PulseCoins(); big = "+" + Fmt.Num(c) + " COINS"; Sfx.Play("Coin", 0.8f); break; }
                    k = System.Array.IndexOf(Shards, id);
                    if (k >= 0) { p.AddShards(ShardAmounts[k], "shop"); big = "+" + ShardAmounts[k] + " SHARDS"; break; }
                    Debug.LogWarning("[ColorRPG] Unknown product: " + id);
                    return true;   // necunoscut: il confirmam ca sa nu ramana blocat
            }
            if (hud != null && big != null) hud.BigText(big, new Color(1f, 0.9f, 0.4f));
            Sfx.Play("JingleHatch", 0.6f, 1.1f, 0f);
            p.SaveNow();
            if (NetSync.I != null) NetSync.I.PushSave(true);   // cumparatura ajunge imediat si pe server
            if (hud != null && hud.shop != null && hud.shop.IsOpen) hud.shop.Rebuild();
            return true;
        }
    }
}
