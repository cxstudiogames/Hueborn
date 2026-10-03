// =====================================================================
//  TitleData.cs - TITLURILE (apar deasupra numelui). Se deblocheaza in timp,
//  pe categorii: POWER, TOP, LEVEL, CLASS, REBIRTH, FORGE, PETS, QUESTS,
//  HUNTER, BOSS, TIME, TRADER, WEALTH. O data deblocat, titlul ramane al tau.
//  Il alegi din INVENTORY (click pe titlu) -> fereastra TITLES.
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class TitleDef
    {
        public string id, name, cat, desc;
        public Color col;
        public int tier;   // 0 COMMON .. 4 MYTHIC (cat de greu e de obtinut - vezi TitleStyle)
        public Func<Player, bool> check;
    }

    public static class TitleDB
    {
        public static readonly string[] Categories = { "POWER", "TOP", "LEVEL", "CLASS", "REBIRTH", "FORGE", "PETS", "QUESTS", "HUNTER", "BOSS", "TIME", "TRADER", "WEALTH" };
        static List<TitleDef> all;
        static Dictionary<string, TitleDef> byId;

        public static List<TitleDef> All { get { if (all == null) Build(); return all; } }
        public static TitleDef Get(string id) { if (all == null) Build(); TitleDef d; return id != null && byId.TryGetValue(id, out d) ? d : null; }

        static void Add(string cat, string name, string desc, Color c, Func<Player, bool> check)
        {
            var d = new TitleDef { id = cat + ":" + name, name = name, cat = cat, desc = desc, col = c, check = check };
            all.Add(d);
            byId[d.id] = d;
        }

        static int MaxPlus(Player p) { int m = 0; foreach (var it in p.D.items) m = Mathf.Max(m, it.plus); return Mathf.Max(m, p.D.bestPlus); }
        static int MaxPetRarity(Player p) { int m = -1; foreach (var pd in p.D.pets) m = Mathf.Max(m, pd.rarity); return m; }

        static void Build()
        {
            all = new List<TitleDef>();
            byId = new Dictionary<string, TitleDef>();
            Color gold = new Color(1f, 0.82f, 0.3f), cyan = new Color(0.4f, 0.9f, 1f), green = new Color(0.5f, 1f, 0.55f), pink = new Color(1f, 0.55f, 0.9f),
                  red = new Color(1f, 0.42f, 0.38f), purple = new Color(0.78f, 0.55f, 1f), white = new Color(0.9f, 0.92f, 0.96f);

            // POWER - aceleasi trepte ca titlul automat
            for (int i = 0; i < Player.PowerTitles.Length; i++)
            {
                long need = Player.PowerSteps[i];
                Add("POWER", Player.PowerTitles[i], need == 0 ? "Start the game" : "Reach " + Fmt.Num(need) + " POWER", Player.PowerTitleCols[i], p => p.Power >= need);
            }
            // TOP - clasamentele din hub
            Add("TOP", "TOP 10 POWER", "Be in the TOP 10 POWER leaderboard", gold, p => Leaderboard.PlayerRank(0, p) <= 10);
            Add("TOP", "TOP 3 POWER", "Be in the TOP 3 POWER leaderboard", gold, p => Leaderboard.PlayerRank(0, p) <= 3);
            Add("TOP", "#1 POWER", "Be #1 in the POWER leaderboard", new Color(1f, 0.9f, 0.4f), p => Leaderboard.PlayerRank(0, p) <= 1);
            Add("TOP", "TOP 10 PLAYTIME", "Be in the TOP 10 TIME PLAYED leaderboard", cyan, p => Leaderboard.PlayerRank(1, p) <= 10);
            Add("TOP", "#1 PLAYTIME", "Be #1 in the TIME PLAYED leaderboard", cyan, p => Leaderboard.PlayerRank(1, p) <= 1);
            // LEVEL
            int[] lv = { 10, 25, 40, 60, 80, 100 };
            string[] ln = { "APPRENTICE", "ADEPT", "EXPERT", "MASTER", "GRANDMASTER", "ASCENDED" };
            for (int i = 0; i < lv.Length; i++) { int l = lv[i]; Add("LEVEL", ln[i], "Reach level " + l, Color.Lerp(white, cyan, i / 5f), p => p.D.level >= l); }
            // CLASS - fiecare clasa, pe fiecare treapta de awakening
            for (int c = 1; c < GameData.Classes.Length; c++)
                for (int t = 1; t <= GameData.MaxAwaken; t++)
                {
                    int cc = c, tt = t;
                    var cd = GameData.ClassAt(c, t);
                    Add("CLASS", cd.name.ToUpper(), "Awakening " + GameData.TierRoman[t] + " as " + cd.name, cd.col, p => p.D.awakenClass == cc && p.D.awakenTier >= tt);
                }
            // REBIRTH
            Add("REBIRTH", "REBORN", "Do your first Rebirth", pink, p => p.D.rebirth >= 1);
            Add("REBIRTH", "PHOENIX", "Reach Rebirth 3", new Color(1f, 0.55f, 0.25f), p => p.D.rebirth >= 3);
            Add("REBIRTH", "ETERNAL", "Reach the max Rebirth (5)", purple, p => p.D.rebirth >= 5);
            // FORGE
            Add("FORGE", "BLACKSMITH", "Forge an item to +5", new Color(1f, 0.65f, 0.3f), p => MaxPlus(p) >= 5);
            Add("FORGE", "MASTER SMITH", "Forge an item to +10", new Color(1f, 0.5f, 0.2f), p => MaxPlus(p) >= 10);
            Add("FORGE", "FORGE GOD", "Forge an item to +15 (max)", red, p => MaxPlus(p) >= ItemDB.MaxPlus);
            // PETS
            Add("PETS", "PET LOVER", "Own 5 pets", pink, p => p.D.pets.Count >= 5);
            Add("PETS", "ZOOKEEPER", "Own 25 pets", pink, p => p.D.pets.Count >= 25);
            Add("PETS", "RARE TAMER", "Own a Rare pet", ItemDB.Col(2), p => MaxPetRarity(p) >= 2);
            Add("PETS", "LEGEND TAMER", "Own a Legendary pet", ItemDB.Col(4), p => MaxPetRarity(p) >= 4);
            Add("PETS", "MYTHIC TAMER", "Own a Mythic pet", ItemDB.Col(5), p => MaxPetRarity(p) >= 5);
            // QUESTS
            Add("QUESTS", "HELPER", "Complete 10 quests", gold, p => p.D.questsDone >= 10);
            Add("QUESTS", "QUESTER", "Complete 50 quests", gold, p => p.D.questsDone >= 50);
            Add("QUESTS", "HERO OF THE REALM", "Complete 150 quests", gold, p => p.D.questsDone >= 150);
            // HUNTER
            Add("HUNTER", "HUNTER", "Defeat 500 monsters", green, p => p.D.kills >= 500);
            Add("HUNTER", "SLAYER", "Defeat 5,000 monsters", green, p => p.D.kills >= 5000);
            Add("HUNTER", "EXTERMINATOR", "Defeat 50,000 monsters", red, p => p.D.kills >= 50000);
            // BOSS
            Add("BOSS", "BOSS HUNTER", "Defeat 10 bosses", red, p => p.D.bossKills >= 10);
            Add("BOSS", "BOSS SLAYER", "Defeat 100 bosses", red, p => p.D.bossKills >= 100);
            Add("BOSS", "TITAN KILLER", "Defeat 500 bosses", purple, p => p.D.bossKills >= 500);
            // TIME
            Add("TIME", "REGULAR", "Play for 1 hour", cyan, p => p.D.playTime >= 3600f);
            Add("TIME", "DEDICATED", "Play for 10 hours", cyan, p => p.D.playTime >= 36000f);
            Add("TIME", "NO-LIFER", "Play for 50 hours", purple, p => p.D.playTime >= 180000f);
            Add("TIME", "OLD SOUL", "Play for 100 hours", gold, p => p.D.playTime >= 360000f);
            // TRADER
            Add("TRADER", "MERCHANT", "Complete 10 trades", new Color(0.55f, 0.95f, 0.75f), p => p.D.trades >= 10);
            Add("TRADER", "TYCOON", "Complete 100 trades", new Color(0.55f, 0.95f, 0.75f), p => p.D.trades >= 100);
            // WEALTH
            Add("WEALTH", "RICH", "Have 1M coins", gold, p => p.D.coins >= 1000000);
            Add("WEALTH", "MILLIONAIRE", "Have 100M coins", gold, p => p.D.coins >= 100000000);
            Add("WEALTH", "BILLIONAIRE", "Have 1B coins", new Color(1f, 0.95f, 0.5f), p => p.D.coins >= 1000000000L);
            Add("WEALTH", "EXPLORER", "Unlock every map", green, p => p.D.level >= GameData.Maps[GameData.Maps.Length - 1].req);
            AssignTiers();
        }

        /// <summary>Treapta de "raritate" a fiecarui titlu: in fiecare categorie, cu cat e mai greu, cu atat mai sus.</summary>
        static void AssignTiers()
        {
            foreach (var cat in Categories)
            {
                var l = all.FindAll(d => d.cat == cat);
                for (int i = 0; i < l.Count; i++)
                {
                    int t;
                    if (cat == "POWER") t = Mathf.Clamp(i * 5 / Mathf.Max(1, l.Count), 0, 4);
                    else if (cat == "CLASS") t = 1 + i % GameData.MaxAwaken;               // I / II / III
                    else if (l.Count == 1) t = 2;
                    else t = 1 + Mathf.RoundToInt(i * 3f / (l.Count - 1));
                    l[i].tier = Mathf.Clamp(t, 0, 4);
                }
            }
            // cele mai grele titluri din joc
            foreach (var id in new[] { "TOP:#1 POWER", "REBIRTH:ETERNAL", "FORGE:FORGE GOD", "PETS:MYTHIC TAMER", "WEALTH:BILLIONAIRE", "TIME:OLD SOUL" })
            { var d = Get(id); if (d != null) d.tier = 4; }
            { var d = Get("WEALTH:EXPLORER"); if (d != null) d.tier = 2; }
        }

        /// <summary>Verifica si deblocheaza titlurile noi. Intoarce numarul de titluri noi.</summary>
        public static int CheckUnlocks(Player p)
        {
            int n = 0;
            foreach (var d in All)
            {
                if (p.D.titles.Contains(d.id)) continue;
                bool ok;
                try { ok = d.check(p); } catch (Exception) { ok = false; }
                if (!ok) continue;
                p.D.titles.Add(d.id);
                n++;
                // titlurile POWER se primesc automat, fara notificare (sunt titlul implicit)
                if (d.cat != "POWER" && Game.I != null && Game.I.hud != null)
                    Game.I.hud.Toast("NEW TITLE UNLOCKED: " + d.name + "  (Inventory > click your title)", d.col, ItemIcons.Get(26));
            }
            return n;
        }

        /// <summary>Titlul afisat deasupra jucatorului (ales de el sau, implicit, cel dupa putere).</summary>
        public static void Current(Player p, out string name, out Color col)
        {
            int tier;
            Current(p, out name, out col, out tier);
        }

        public static void Current(Player p, out string name, out Color col, out int tier)
        {
            var d = Get(p.D.title);
            if (d != null && p.D.titles.Contains(d.id)) { name = d.name; col = d.col; tier = d.tier; return; }
            long pw = p.Power;
            name = Player.PowerTitle(pw); col = Player.PowerTitleCol(pw);
            var pd = Get("POWER:" + name);
            tier = pd != null ? pd.tier : 0;
        }
    }
}
