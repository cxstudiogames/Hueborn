// =====================================================================
//  AdminUI.cs - panoul ADMIN (doar in editor / build de test; F1), pe pagini:
//   PLAYER      - monede, nivel, materiale, potiuni, trucuri
//   ITEMS       - catalogul cu TOATE itemele (tip x stil), le dai cu un click
//   PETS        - toti pets din joc (4 harti x 6), le dai cu un click
//   MODERATION  - cauti un jucator: KICK, BAN, MUTE, TRADE BAN, WARN, cadouri
//   GIVEAWAYS   - giveaway-uri, anunturi, cadou pentru toti
//   LOG         - tot ce s-a facut din ADMIN
//  Pana cand jocul e online, actiunile pe alti jucatori sunt salvate local
//  (Social.cs) si se aplica pe server cand va exista.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColorRPG
{
    public static class AdminTools
    {
        public static bool God;        // jucatorul nu primeste damage
        public static bool BlockSave;  // folosit la resetarea salvarii

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { God = false; BlockSave = false; }
    }

    public class AdminUI : Window
    {
        static readonly string[] Pages = { "PLAYER", "ITEMS", "PETS", "MODERATION", "GIVEAWAYS", "LOG" };
        static readonly string[] PageCols = { "Blue", "Green", "Purple", "Red", "Yellow", "Grey" };
        int page;

        // ITEMS
        int map, rarity = 4, slot, forge;
        float lbClickT = -10f;
        static bool autoEquip = true;
        // MODERATION
        string modQuery = "", modTarget;
        int durPick = 1;
        static readonly string[] DurNames = { "1 HOUR", "1 DAY", "7 DAYS", "30 DAYS", "PERMANENT" };
        static readonly float[] DurHours = { 1f, 24f, 168f, 720f, 0f };
        InputField reasonField, giftField, modSearch;
        // GIVEAWAYS
        int gPrize, gWinners = 1, gAudience, gDelay = 1;
        static readonly string[] PrizeNames = { "COINS", "SHARDS", "TOKENS", "PET", "ITEM", "FORGE STONES", "GUARDIAN CLOAKS", "POTIONS (ALL 3)", "XP", "ORE" };
        static readonly string[] PrizeCols = { "Yellow", "Blue", "Green", "Purple", "Orange", "Grey", "Red", "Purple", "Blue", "Orange" };
        static bool AmountPrize(int p) { return p <= 2 || p >= 5; }
        // GIFT A PLAYER
        // (GIFT A PLAYER a fost scos din GIVEAWAYS - cadourile individuale sunt in MODERATION)
        static readonly int[] WinnerCounts = { 1, 3, 5, 10 };
        static readonly string[] AudienceNames = { "EVERYONE", "THIS SERVER", "FRIENDS" };
        static readonly float[] DelayS = { 0f, 30f, 60f, 300f };
        static readonly string[] DelayNames = { "INSTANT", "30 SEC", "1 MIN", "5 MIN" };
        InputField gAmountField, announceField;
        float gEndT = -1f; string gText = ""; int gCount; int gAud;

        float resetT;
        InputField amountField, levelField, forgeField;

        public static AdminUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("AdminUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<AdminUI>();
            w.Init(parent, g, "AdminDim", "ADMIN", new Vector2(1400, 940), "Red");
            return w;
        }

        Player P { get { return game.player; } }
        static readonly Color CardCol = new Color(0.2f, 0.22f, 0.34f, 1f);

        void Btn(Transform p, string label, Vector2 pos, Vector2 size, string col, System.Action a, int fs = 17)
        {
            UIKit.Button(p, label, label, col, TL, TL, pos, size, () => { a(); P.SaveNow(); Rebuild(); }, fs);
        }

        static long Parse(InputField f, long def)
        {
            long v;
            if (f == null || !long.TryParse(f.text.Trim(), out v)) return def;
            return System.Math.Max(0, v);
        }

        void Cycler(Transform p, Vector2 pos, string title, string value, System.Action next, float w = 290f)
        {
            Label(p, title, 15, new Color(1, 1, 1, 0.65f), pos, new Vector2(w, 24));
            var b = UIKit.Button(p, "Cyc" + title, "", "Grey", TL, TL, pos + new Vector2(0, -24), new Vector2(w, 46), () => { next(); Rebuild(); }, 17);
            var t = UIKit.Text(b.transform, "V", value + "   >", 18, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(w - 10, 40));
            t.supportRichText = true;
        }

        protected override void Build()
        {
            for (int i = 0; i < Pages.Length; i++)
            {
                int k = i;
                UIKit.Button(content, "P" + i, Pages[i], page == i ? PageCols[i] : "Grey", TL, TL, new Vector2(20 + i * 226, -10), new Vector2(218, 54), () => { page = k; Rebuild(); }, 19);
            }
            switch (page)
            {
                case 1: BuildItems(); break;
                case 2: BuildPets(); break;
                case 3: BuildModeration(); break;
                case 4: BuildGiveaways(); break;
                case 5: BuildLog(); break;
                default: BuildPlayer(); break;
            }
        }

        // ================================================================
        //  PLAYER
        // ================================================================
        void BuildPlayer()
        {
            var D = P.D;
            var cur = Card(content, new Vector2(20, -80), new Vector2(660, 250), CardCol);
            Label(cur, "CURRENCIES", 22, new Color(1f, 0.85f, 0.3f), new Vector2(16, -10), new Vector2(400, 30));
            Label(cur, "Coins: <color=#ffd84d>" + Fmt.Num(D.coins) + "</color>    Shards: <color=#8fd8ff>" + Fmt.Num(D.shards) + "</color>    Ore: <color=#ffae66>" + Fmt.Num(D.ore) + "</color>    Tokens: <color=#8cf5bf>" + Fmt.Num(D.tokens) + "</color>",
                  16, Color.white, new Vector2(16, -44), new Vector2(630, 26));
            Label(cur, "Amount:", 17, Color.white, new Vector2(16, -86), new Vector2(100, 30));
            amountField = UIKit.MakeInput(cur, "Amount", TL, TL, new Vector2(110, -80), new Vector2(220, 46), amountField != null ? amountField.text : "100000", 15, 20);
            Btn(cur, "+ COINS", new Vector2(16, -140), new Vector2(150, 48), "Yellow", () => { P.AddCoins(Parse(amountField, 0)); game.hud.PulseCoins(); });
            Btn(cur, "+ SHARDS", new Vector2(174, -140), new Vector2(150, 48), "Blue", () => P.AddShards(Parse(amountField, 0), "admin"));
            Btn(cur, "+ TOKENS", new Vector2(332, -140), new Vector2(150, 48), "Green", () => P.AddTokens(Parse(amountField, 0)));
            Btn(cur, "+ ORE", new Vector2(490, -140), new Vector2(150, 48), "Grey", () => D.ore += (int)System.Math.Min(int.MaxValue - D.ore, Parse(amountField, 0)));
            Btn(cur, "SET COINS", new Vector2(16, -194), new Vector2(200, 44), "Grey", () => D.coins = Parse(amountField, 0), 15);
            Btn(cur, "SET SHARDS", new Vector2(226, -194), new Vector2(200, 44), "Grey", () => D.shards = Parse(amountField, 0), 15);
            Btn(cur, "RESET ALL TO 0", new Vector2(436, -194), new Vector2(204, 44), "Red", () => { D.coins = 0; D.shards = 0; D.ore = 0; D.tokens = 0; }, 14);

            var lv = Card(content, new Vector2(696, -80), new Vector2(684, 250), CardCol);
            Label(lv, "LEVEL", 22, new Color(0.5f, 0.9f, 1f), new Vector2(16, -10), new Vector2(300, 30));
            Label(lv, "Level " + D.level + "   XP " + Fmt.Num(D.xp) + " / " + Fmt.Num(P.XpNeed) + "   Rebirths " + D.rebirth + "/" + Player.MaxRebirth, 17, Color.white, new Vector2(16, -44), new Vector2(650, 26));
            levelField = UIKit.MakeInput(lv, "Level", TL, TL, new Vector2(16, -80), new Vector2(160, 46), D.level.ToString(), 4, 20);
            Btn(lv, "SET LEVEL", new Vector2(186, -80), new Vector2(170, 46), "Green", () => { int l; if (int.TryParse(levelField.text.Trim(), out l)) SetLevel(l); });
            Btn(lv, "+1 LEVEL", new Vector2(16, -140), new Vector2(170, 48), "Blue", () => SetLevel(D.level + 1));
            Btn(lv, "+10 LEVELS", new Vector2(196, -140), new Vector2(170, 48), "Blue", () => SetLevel(D.level + 10));
            Btn(lv, "FILL XP", new Vector2(376, -140), new Vector2(170, 48), "Grey", () => P.GainXp(P.XpNeed - D.xp));
            Btn(lv, "UNLOCK ALL MAPS (Lv 60)", new Vector2(16, -194), new Vector2(330, 44), "Yellow", () => { if (D.level < 60) SetLevel(60); }, 15);
            Btn(lv, "+1 REBIRTH", new Vector2(356, -194), new Vector2(150, 44), "Grey", () => D.rebirth = Mathf.Min(Player.MaxRebirth, D.rebirth + 1), 15);
            Btn(lv, "RESET REBIRTH", new Vector2(514, -194), new Vector2(150, 44), "Grey", () => D.rebirth = 0, 14);

            var ch = Card(content, new Vector2(20, -346), new Vector2(1360, 200), CardCol);
            Label(ch, "CHEATS", 22, new Color(1f, 0.5f, 0.45f), new Vector2(16, -10), new Vector2(400, 30));
            Btn(ch, AdminTools.God ? "GOD MODE: ON" : "GOD MODE: OFF", new Vector2(16, -50), new Vector2(250, 50), AdminTools.God ? "Green" : "Grey", () => AdminTools.God = !AdminTools.God);
            Btn(ch, Time.timeScale > 1.5f ? "SPEED x3: ON" : "SPEED x3: OFF", new Vector2(276, -50), new Vector2(250, 50), Time.timeScale > 1.5f ? "Green" : "Grey", () => Time.timeScale = Time.timeScale > 1.5f ? 1f : 3f);
            Btn(ch, "HEAL", new Vector2(536, -50), new Vector2(180, 50), "Green", () => P.hp = P.MaxHp);
            Btn(ch, "KILL NEARBY ENEMIES", new Vector2(726, -50), new Vector2(300, 50), "Red", KillNearby, 15);
            Btn(ch, "SKILL POINTS RESET", new Vector2(1036, -50), new Vector2(300, 50), "Grey", () => P.ResetPoints(), 15);
            Btn(ch, D.passTeleport ? "TELEPORT PASS: ON" : "TELEPORT PASS: OFF", new Vector2(16, -112), new Vector2(300, 50), D.passTeleport ? "Green" : "Grey", () => D.passTeleport = !D.passTeleport, 15);
            Btn(ch, D.passGame ? "PET PASS: ON" : "PET PASS: OFF", new Vector2(326, -112), new Vector2(240, 50), D.passGame ? "Green" : "Grey", () => { D.passGame = !D.passGame; P.RebuildPets(); }, 15);
            Btn(ch, "GIFT READY", new Vector2(576, -112), new Vector2(220, 50), "Yellow", () =>
            {
                D.giftTimer = Game.GiftInterval;
                game.hud.Toast("GIFT READY\nGo to the GIFT box near the start of any map and open it.", new Color(1f, 0.85f, 0.3f));
                Sfx.Play("JingleHatch", 0.5f, 1.2f, 0f);
            }, 15);
            UIKit.Button(ch, "Wipe", resetT > 0f ? "CLICK AGAIN TO WIPE!" : "RESET SAVE (new game)", "Red", TL, TL, new Vector2(806, -112), new Vector2(260, 50), WipeSave, 14);
            bool lbArm = Time.unscaledTime - lbClickT < 4f;
            UIKit.Button(ch, "LbReset", lbArm ? "CLICK AGAIN TO RESET!" : "RESET LEADERBOARDS", "Orange", TL, TL, new Vector2(1076, -112), new Vector2(260, 50), () =>
            {
                if (Time.unscaledTime - lbClickT < 4f)
                {
                    lbClickT = -10f;
                    NetSync.AdminResetLeaderboards();
                    Social.Log("RESET LEADERBOARDS");
                    game.hud.Toast(NetSync.Online ? "Leaderboards reset for everyone (live)." : "Leaderboards reset (offline - only here).", new Color(1f, 0.7f, 0.3f));
                }
                else lbClickT = Time.unscaledTime;
                Rebuild();
            }, 14);

            var mt = Card(content, new Vector2(20, -562), new Vector2(1360, 130), CardCol);
            Label(mt, "MATERIALS / POTIONS", 20, new Color(0.55f, 0.95f, 0.75f), new Vector2(16, -8), new Vector2(600, 28));
            Label(mt, "Stones " + D.forgeStones + "   Cloaks " + D.cloaks + "   Potions " + D.potions[0] + "/" + D.potions[1] + "/" + D.potions[2],
                  16, Color.white, new Vector2(700, -8), new Vector2(640, 28));
            Btn(mt, "+10 STONES", new Vector2(16, -52), new Vector2(200, 50), "Grey", () => D.forgeStones += 10, 15);
            Btn(mt, "+1 CLOAK", new Vector2(226, -52), new Vector2(180, 50), "Blue", () => D.cloaks++, 15);
            Btn(mt, "+1 XP POT", new Vector2(416, -52), new Vector2(190, 50), "Blue", () => P.AddPotion(0, 1), 15);
            Btn(mt, "+1 DMG POT", new Vector2(616, -52), new Vector2(190, 50), "Red", () => P.AddPotion(1, 1), 15);
            Btn(mt, "+1 COIN POT", new Vector2(816, -52), new Vector2(190, 50), "Yellow", () => P.AddPotion(2, 1), 15);
            Btn(mt, "CLEAR BUFFS", new Vector2(1016, -52), new Vector2(320, 50), "Grey", () => { for (int k = 0; k < 3; k++) D.buffs[k] = 0f; }, 15);

            // ---- unelte de test (mobil, harti, questuri, pass-uri, cloud)
            var tl = Card(content, new Vector2(20, -706), new Vector2(1360, 168), CardCol);
            Label(tl, "TOOLS", 20, new Color(0.75f, 0.85f, 1f), new Vector2(16, -8), new Vector2(300, 28));
            Label(tl, "Device: " + (Device.Mobile ? "<color=#7dff8a>MOBILE</color>" : "PC") + (Device.Simulated ? " (simulated)" : "") + "   Online: " + OnlineAccount.Status +
                      "   Server: " + (ColorRPG.Net.Ugs.Ready ? "<color=#7dff8a>ONLINE</color> " + ColorRPG.Net.Ugs.PlayerName + "  id " + ColorRPG.Net.Ugs.PlayerId : ColorRPG.Net.Ugs.Installed ? "<color=#ff9a7a>offline</color>" : "not installed") +
                      "   Power " + Fmt.Num(P.Power) + " / cap " + Fmt.Num(GameData.PowerCap) + "\n" + Mp.DebugInfo, 13, Color.white, new Vector2(330, -2), new Vector2(1010, 40));
            Btn(tl, Device.Simulated ? "SIMULATE MOBILE: ON" : "SIMULATE MOBILE: OFF", new Vector2(16, -44), new Vector2(300, 50), Device.Simulated ? "Green" : "Grey", () =>
            {
                Device.SetSimulated(!Device.Simulated);
                game.Say("Mobile mode " + (Device.Simulated ? "ON" : "OFF") + " - restarting the scene...", 2f);
                P.SaveNow();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }, 15);
            for (int m = 0; m < GameData.Maps.Length; m++)
            {
                int mm = m;
                Btn(tl, "GO " + GameData.Maps[m].name.Split(' ')[0].ToUpper(), new Vector2(326 + m * 172, -44), new Vector2(164, 50), "Blue", () => { Close(); game.Travel(mm, true); }, 14);
            }
            Btn(tl, "GO PLAZA", new Vector2(1014, -44), new Vector2(160, 50), "Orange", () => { Close(); if (game.player.D.level < GameData.TradeLevel) SetLevel(GameData.TradeLevel); game.GoPlaza(); }, 14);
            Btn(tl, "MAX LEVEL", new Vector2(1184, -44), new Vector2(160, 50), "Yellow", () => SetLevel(GameData.MaxLevel), 14);
            Btn(tl, "COMPLETE QUESTS", new Vector2(16, -104), new Vector2(240, 50), "Yellow", () => P.AdminCompleteQuests(), 14);
            Btn(tl, "RESET EGG PRICES", new Vector2(266, -104), new Vector2(240, 50), "Purple", () => { for (int k = 0; k < D.eggsHatched.Length; k++) D.eggsHatched[k] = 0; }, 14);
            bool allP = D.passMagnet && D.passAuto && D.passTeleport && D.passGame && D.passPrivate;
            Btn(tl, allP ? "ALL PASSES: ON" : "ALL PASSES: OFF", new Vector2(516, -104), new Vector2(230, 50), allP ? "Green" : "Grey", () =>
            {
                bool v = !allP; D.passMagnet = D.passAuto = D.passTeleport = D.passGame = D.passPrivate = v; P.RebuildPets();
            }, 14);
            Btn(tl, "UNLOCK ALL TITLES", new Vector2(756, -104), new Vector2(240, 50), "Grey", () => { foreach (var t in TitleDB.All) if (!D.titles.Contains(t.id)) D.titles.Add(t.id); }, 14);
            Btn(tl, "RECORD PROMO", new Vector2(1006, -104), new Vector2(160, 50), "Red", () => { Close(); PromoDirector.Begin(); }, 14);
            Btn(tl, "CLOUD SAVE", new Vector2(1176, -104), new Vector2(168, 50), "Blue", () => { P.SaveNow(); OnlineAccount.Flush(); game.Say("Cloud: " + OnlineAccount.Status, 2f); }, 14);
        }

        void SetLevel(int l)
        {
            var D = P.D;
            D.level = Mathf.Clamp(l, 1, GameData.MaxLevel);
            D.xp = 0;
            if (D.lastShardLevel > D.level) D.lastShardLevel = D.level;
            P.hp = P.MaxHp;
            game.hud.BigText("LEVEL " + D.level, new Color(0.4f, 0.95f, 1f));
        }

        // ================================================================
        //  ITEMS - catalogul complet
        // ================================================================
        // mostrele din catalog (iconitele) raman aceleasi intre redesenari
        readonly Dictionary<int, Item> samples = new Dictionary<int, Item>();
        Item Sample(int idx)
        {
            Item it;
            if (!samples.TryGetValue(idx, out it)) { it = ItemDB.FromCatalog(idx); it.roll = 1f; samples[idx] = it; }
            return it;
        }

        void BuildItems()
        {
            var top = Card(content, new Vector2(20, -80), new Vector2(1360, 110), CardCol);
            string[] slotNames = { "WEAPONS", "HELMETS", "ARMOR", "RINGS" };
            for (int i = 0; i < 4; i++)
            {
                int k = i;
                UIKit.Button(top, "S" + i, slotNames[i], slot == i ? "Green" : "Grey", TL, TL, new Vector2(16 + i * 160, -12), new Vector2(152, 46), () => { slot = k; Rebuild(); }, 18);
            }
            Btn(top, autoEquip ? "AUTO-EQUIP: ON" : "AUTO-EQUIP: OFF", new Vector2(670, -12), new Vector2(240, 46), autoEquip ? "Green" : "Grey", () => autoEquip = !autoEquip, 15);
            Btn(top, "CLEAR INVENTORY", new Vector2(920, -12), new Vector2(240, 46), "Red", () => P.D.items.RemoveAll(x => !P.IsEquipped(x)), 15);
            Label(top, "Bag " + P.BagCount + "/" + P.BagSize, 18, Color.white, new Vector2(1170, -20), new Vector2(180, 30));
            Label(top, "FORGE +", 15, new Color(1, 1, 1, 0.65f), new Vector2(16, -70), new Vector2(100, 24));
            forgeField = UIKit.MakeInput(top, "Forge", TL, TL, new Vector2(110, -60), new Vector2(110, 44), forge.ToString(), 2, 20);
            Label(top, "Click an item to get it. Each map: 6 unique items per category (2 Common, 2 Rare, 1 Legendary, 1 Mythic).", 15, new Color(1, 1, 1, 0.6f), new Vector2(240, -70), new Vector2(1100, 24));

            // grila: un rand pe harta, 6 iteme pe rand
            float cw = 214f, ch = 140f;
            for (int m = 0; m < GameData.Maps.Length && m < 4; m++)
            {
                Label(content, GameData.Maps[m].name.ToUpper(), 15, Color.Lerp(GameData.Maps[m].ground, Color.white, 0.45f), new Vector2(20, -202 - m * (ch + 12)), new Vector2(60, ch)).alignment = TextAnchor.MiddleLeft;
                for (int pos = 0; pos < ItemCatalog.PerMap; pos++)
                {
                    int idx = ItemCatalog.IndexOf(slot, m, pos);
                    if (idx < 0) continue;
                    var d = ItemCatalog.All[idx];
                    var sample = Sample(idx);
                    var cell = Card(content, new Vector2(84 + pos * (cw + 8), -202 - m * (ch + 12)), new Vector2(cw, ch), Color.Lerp(new Color(0.2f, 0.22f, 0.3f), ItemDB.Col(d.rarity), 0.3f));
                    var ic = UIKit.Rect(cell, "Ic", TC, TC, new Vector2(0, -4), new Vector2(86, 86)).gameObject.AddComponent<Image>();
                    ic.sprite = ItemIcons.For(sample); ItemIcons.Art(ic); ic.preserveAspect = true; ic.raycastTarget = false;
                    var nm = Label(cell, d.name + "\n<color=#" + ColorUtility.ToHtmlStringRGB(ItemDB.Col(d.rarity)) + ">" + ItemDB.RarityNames[d.rarity] + "</color>", 14, Color.white, new Vector2(0, -92), new Vector2(cw - 8, 46), TextAnchor.UpperCenter, TC);
                    UIKit.Fit(nm, 10);
                    var img = cell.GetComponent<Image>(); img.raycastTarget = true;
                    var btn = cell.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
                    int ci = idx;
                    btn.onClick.AddListener(() =>
                    {
                        int f2 = 0; if (forgeField != null) int.TryParse(forgeField.text.Trim(), out f2);
                        forge = Mathf.Clamp(f2, 0, ItemDB.MaxPlus);
                        GiveItem(ItemDB.FromCatalog(ci, forge));
                    });
                }
            }
        }

        void GiveItem(Item item)
        {
            if (P.BagCount >= P.BagSize) { game.Say("Inventory full!", 2f); Sfx.Play("Error", 0.5f); return; }
            P.AddItem(item);
            if (autoEquip) P.Equip(item, true);
            game.hud.ItemToast(item);
            Sfx.Play("ItemRare", 0.6f);
            Social.Log("gave item " + item.name + (item.plus > 0 ? " +" + item.plus : "") + " to self");
            P.SaveNow();
            Rebuild();
        }

        // ================================================================
        //  PETS - toti pets din joc
        // ================================================================
        void BuildPets()
        {
            var D = P.D;
            Label(content, "Click a pet to get it (rarity = column).   Storage " + D.pets.Count + "/" + GameData.PetStorage, 20, Color.white, new Vector2(24, -80), new Vector2(900, 30));
            Btn(content, "RELEASE UNEQUIPPED PETS", new Vector2(1000, -74), new Vector2(380, 46), "Red", () => { D.pets.RemoveAll(x => !x.equipped); P.RebuildPets(); }, 15);
            for (int m = 0; m < GameData.MapPets.Length; m++)
            {
                Label(content, GameData.Maps[m].name.ToUpper(), 20, Color.Lerp(GameData.Maps[m].ground, Color.white, 0.4f), new Vector2(24, -122 - m * 180), new Vector2(400, 28));
                for (int ri = 0; ri < ItemDB.UsedRarities.Length; ri++)
                {
                    int r = ItemDB.UsedRarities[ri];
                    var pd = new PetData { rarity = r, model = GameData.MapPets[m][r], map = m };
                    var cell = Card(content, new Vector2(24 + ri * 226, -150 - m * 180), new Vector2(216, 148), Color.Lerp(new Color(0.18f, 0.2f, 0.28f), ItemDB.Col(r), 0.35f));
                    var ic = UIKit.Rect(cell, "Ic", TC, TC, new Vector2(0, -4), new Vector2(92, 92)).gameObject.AddComponent<Image>();
                    var sp = PetStudio.Icon(pd);
                    ic.sprite = sp != null ? sp : ItemIcons.Get(6); ItemIcons.Art(ic); ic.preserveAspect = true; ic.raycastTarget = false;
                    Label(cell, GameData.PetName(pd.model) + "  <color=#" + ColorUtility.ToHtmlStringRGB(ItemDB.Col(r)) + ">" + ItemDB.RarityNames[r] + "</color>", 16, Color.white,
                          new Vector2(0, -98), new Vector2(210, 24), TextAnchor.MiddleCenter, TC);
                    Label(cell, "+" + Mathf.RoundToInt(Player.PetPower(pd) * 100) + "% dmg", 14, new Color(0.5f, 1f, 0.55f), new Vector2(0, -122), new Vector2(210, 22), TextAnchor.MiddleCenter, TC);
                    var img = cell.GetComponent<Image>(); img.raycastTarget = true;
                    var btn = cell.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
                    int mm = m, rr = r;
                    btn.onClick.AddListener(() => GivePet(mm, rr));
                }
            }
        }

        void GivePet(int m, int r)
        {
            var D = P.D;
            if (D.pets.Count >= GameData.PetStorage) { game.Say("Pet storage full!", 2f); Sfx.Play("Error", 0.5f); return; }
            var pd = new PetData { rarity = r, model = GameData.MapPets[m][r], map = m, equipped = P.PetsEquippedCount < P.MaxPets };
            D.pets.Add(pd);
            P.RebuildPets();
            game.hud.Toast("Pet: " + ItemDB.RarityNames[r] + " " + GameData.PetName(pd.model), ItemDB.Col(r), ItemIcons.Get(6));
            Social.Log("gave pet " + ItemDB.RarityNames[r] + " " + GameData.PetName(pd.model) + " to self");
            P.SaveNow();
            Rebuild();
        }

        // ================================================================
        //  MODERATION
        // ================================================================
        void BuildModeration()
        {
            var left = Card(content, new Vector2(20, -80), new Vector2(440, 780), CardCol);
            Label(left, "PLAYERS", 22, new Color(1f, 0.85f, 0.45f), new Vector2(16, -10), new Vector2(300, 30));
            modSearch = UIKit.MakeInput(left, "Find", TL, TL, new Vector2(16, -48), new Vector2(300, 50), modQuery, 20, 20);
            var ph = modSearch.placeholder as Text; if (ph != null) ph.text = "Search name...";
            UIKit.Button(left, "Go", "FIND", "Blue", TL, TL, new Vector2(324, -48), new Vector2(100, 50), () => { modQuery = modSearch.text; Rebuild(); }, 18);
            var names = new List<string>(Social.Search(modQuery, 11));
            string typed = (modQuery ?? "").Trim();
            if (typed.Length >= 2 && !names.Exists(n => n.ToLowerInvariant() == typed.ToLowerInvariant())) names.Insert(0, typed);
            for (int i = 0; i < names.Count && i < 11; i++)
            {
                string n = names[i];
                var s = Social.ActiveFor(n);
                string tag = s.Count > 0 ? "  <color=#ff7a6b>" + string.Join(", ", s.ConvertAll(x => x.kind).ToArray()) + "</color>" : "";
                bool cur = n == modTarget;
                var b = UIKit.Button(left, "N" + i, "", cur ? "Yellow" : "Grey", TL, TL, new Vector2(16, -110 - i * 58), new Vector2(408, 52), () => { modTarget = n; Rebuild(); }, 18);
                var t = UIKit.Text(b.transform, "T", n + tag, 18, Color.white, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 2), new Vector2(390, 44));
                t.supportRichText = true;
            }

            var right = Card(content, new Vector2(476, -80), new Vector2(904, 780), CardCol);
            if (string.IsNullOrEmpty(modTarget))
            {
                Label(right, "Select a player on the left.", 24, new Color(1, 1, 1, 0.7f), new Vector2(0, -330), new Vector2(904, 40), TextAnchor.MiddleCenter);
                return;
            }
            string tgt = modTarget;
            bool self = tgt == P.D.playerName;
            Label(right, tgt + (self ? "  <color=#8cf5bf>(YOU)</color>" : ""), 34, Color.white, new Vector2(20, -12), new Vector2(600, 46));
            Label(right, (Social.IsOnline(tgt) || self ? "<color=#5dff73>● ONLINE</color>" : "<color=#8890aa>● offline</color>") + "    Lv " + (self ? P.D.level : Social.FakeLevel(tgt)), 18, Color.white, new Vector2(20, -60), new Vector2(600, 28));

            // sanctiuni active
            var act = Social.ActiveFor(tgt);
            Label(right, "ACTIVE: " + (act.Count == 0 ? "<color=#5dff73>none</color>" : ""), 18, new Color(1f, 0.85f, 0.45f), new Vector2(20, -96), new Vector2(200, 28));
            for (int i = 0; i < act.Count && i < 4; i++)
            {
                var s = act[i];
                Label(right, "<color=#ff7a6b>" + s.kind + "</color>  " + Social.LeftText(s) + (s.reason.Length > 0 ? "  - " + s.reason : ""), 16, Color.white, new Vector2(120, -96 - i * 30), new Vector2(560, 28));
                string k = s.kind;
                Btn(right, "LIFT " + k, new Vector2(700, -94 - i * 30), new Vector2(180, 28), "Green", () => Social.Lift(tgt, k), 13);
            }

            // actiuni
            float y = -230;
            Label(right, "REASON", 15, new Color(1, 1, 1, 0.65f), new Vector2(20, y), new Vector2(200, 24));
            reasonField = UIKit.MakeInput(right, "Reason", TL, TL, new Vector2(20, y - 24), new Vector2(560, 46), reasonField != null ? reasonField.text : "", 60, 18);
            Cycler(right, new Vector2(600, y), "DURATION", DurNames[durPick], () => durPick = (durPick + 1) % DurNames.Length, 284);
            y -= 90;
            System.Func<string> R = () => reasonField != null ? reasonField.text.Trim() : "";
            Btn(right, "KICK", new Vector2(20, y), new Vector2(206, 56), "Orange", () => { Social.Log("KICK " + tgt + (R().Length > 0 ? " - " + R() : "")); game.Say(tgt + " was kicked from the server.", 2f); }, 20);
            Btn(right, "WARN", new Vector2(236, y), new Vector2(206, 56), "Yellow", () => { Social.Log("WARN " + tgt + (R().Length > 0 ? " - " + R() : "")); game.Say("Warning sent to " + tgt + ".", 2f); }, 20);
            Btn(right, "MUTE", new Vector2(452, y), new Vector2(206, 56), "Purple", () => Social.Punish(tgt, "MUTE", DurHours[durPick], R()), 20);
            Btn(right, "BAN", new Vector2(668, y), new Vector2(212, 56), "Red", () => { Social.Punish(tgt, "BAN", DurHours[durPick], R()); game.Say(tgt + " is banned (" + DurNames[durPick].ToLower() + ").", 2.5f); }, 20);
            y -= 66;
            Btn(right, "TRADE BAN", new Vector2(20, y), new Vector2(206, 56), "Red", () => Social.Punish(tgt, "TRADE BAN", DurHours[durPick], R()), 17);
            Btn(right, "CHAT RESTRICT", new Vector2(236, y), new Vector2(206, 56), "Purple", () => Social.Punish(tgt, "CHAT RESTRICT", DurHours[durPick], R()), 15);
            Btn(right, "BOOTH BAN", new Vector2(452, y), new Vector2(206, 56), "Orange", () => Social.Punish(tgt, "BOOTH BAN", DurHours[durPick], R()), 17);
            Btn(right, "LIFT ALL", new Vector2(668, y), new Vector2(212, 56), "Green", () => { foreach (var s in Social.ActiveFor(tgt).ToArray()) Social.Lift(tgt, s.kind); }, 17);

            // cadouri (pentru un jucator anume): orice premiu - monede, shards, tokens, pet, item, pietre, cloaks, potiuni, XP, minereu
            y -= 86;
            Label(right, "GIFT TO " + tgt.ToUpper(), 22, new Color(0.55f, 0.95f, 0.75f), new Vector2(20, y), new Vector2(420, 30));
            Label(right, "AMOUNT", 15, new Color(1, 1, 1, 0.65f), new Vector2(450, y + 4), new Vector2(120, 24));
            giftField = UIKit.MakeInput(right, "GiftAmt", TL, TL, new Vector2(560, y + 6), new Vector2(320, 44), giftField != null ? giftField.text : "10000", 12, 20);
            y -= 52;
            for (int k = 0; k < PrizeNames.Length; k++)
            {
                int pk = k;
                string lbl = k == 3 ? ItemDB.RarityNames[rarity].ToUpper() + " PET" : k == 4 ? ItemDB.RarityNames[rarity].ToUpper() + " " + ItemDB.SlotNames[slot].ToUpper() : PrizeNames[k];
                Btn(right, lbl, new Vector2(20 + (k % 5) * 174, y - (k / 5) * 58), new Vector2(166, 50), PrizeCols[k], () =>
                {
                    long amt = Parse(giftField, 0);
                    string what = PrizeText(pk, amt);
                    if (self) { GiveSelf(pk, amt); game.hud.Toast("Gift received: " + what, new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(7)); Social.Log("GIFT " + what + " to " + tgt + " (delivered)"); }
                    else { Social.Log("GIFT " + what + " to " + tgt + " (pending)"); game.Say("Gift for " + tgt + ": " + what + " - delivered when they log in.", 2.5f); }
                    Sfx.Play("JingleHatch", 0.5f, 1.2f, 0f);
                }, 14);
            }
            y -= 124;
            Label(right, (self ? "Gifts to yourself are given right away." : "Gifts to other players are delivered when they log in.") + "  (PET / ITEM prizes: Legendary.)",
                  15, new Color(1, 1, 1, 0.6f), new Vector2(20, y), new Vector2(860, 40));
        }

        /// <summary>kind: 0 coins, 1 shards, 2 tokens, 3 pet (amount = raritate).</summary>
        void Gift(string who, int kind, long amount)
        {
            string what = kind == 0 ? Fmt.Num(amount) + " coins" : kind == 1 ? Fmt.Num(amount) + " shards" : kind == 2 ? Fmt.Num(amount) + " tokens" : ItemDB.RarityNames[Mathf.Clamp((int)amount, 0, 5)] + " pet";
            if (who == P.D.playerName)
            {
                if (kind == 0) P.AddCoins(amount);
                else if (kind == 1) P.AddShards(amount, "gift");
                else if (kind == 2) P.AddTokens(amount);
                else
                {
                    int m = Random.Range(0, 4), r = Mathf.Clamp((int)amount, 0, 5);
                    if (P.D.pets.Count < GameData.PetStorage) { P.D.pets.Add(new PetData { rarity = r, model = GameData.MapPets[m][r], map = m }); P.RebuildPets(); }
                }
                game.hud.Toast("Gift received: " + what, new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(7));
                Social.Log("GIFT " + what + " to " + who + " (delivered)");
            }
            else
            {
                Social.Log("GIFT " + what + " to " + who + " (pending)");
                game.Say("Gift for " + who + ": " + what + " - delivered when they log in.", 2.5f);
            }
            Sfx.Play("JingleHatch", 0.5f, 1.2f, 0f);
        }

        // ================================================================
        //  GIVEAWAYS + anunturi
        // ================================================================
        void BuildGiveaways()
        {
            var g = Card(content, new Vector2(20, -80), new Vector2(1360, 360), CardCol);
            Label(g, "GIVEAWAY", 26, new Color(1f, 0.85f, 0.3f), new Vector2(16, -10), new Vector2(400, 34));
            Cycler(g, new Vector2(16, -56), "PRIZE", PrizeNames[gPrize], () => gPrize = (gPrize + 1) % PrizeNames.Length, 250);
            Label(g, gPrize == 3 ? "PET RARITY (from ITEMS)" : gPrize == 4 ? "ITEM (ITEMS page settings)" : "AMOUNT (each)", 15, new Color(1, 1, 1, 0.65f), new Vector2(280, -56), new Vector2(260, 24));
            if (AmountPrize(gPrize)) gAmountField = UIKit.MakeInput(g, "GAmt", TL, TL, new Vector2(280, -80), new Vector2(250, 46), gAmountField != null ? gAmountField.text : "50000", 12, 20);
            else Label(g, gPrize == 3 ? ItemDB.RarityNames[rarity] + " pet" : ItemDB.RarityNames[rarity] + " " + ItemDB.SlotNames[slot], 20, ItemDB.Col(rarity), new Vector2(280, -86), new Vector2(250, 36));
            Cycler(g, new Vector2(544, -56), "WINNERS", WinnerCounts[gWinners].ToString(), () => gWinners = (gWinners + 1) % WinnerCounts.Length, 250);
            Cycler(g, new Vector2(808, -56), "WHO CAN WIN", AudienceNames[gAudience], () => gAudience = (gAudience + 1) % AudienceNames.Length, 260);
            Cycler(g, new Vector2(1082, -56), "DRAW AFTER", DelayNames[gDelay], () => gDelay = (gDelay + 1) % DelayNames.Length, 262);
            bool running = gEndT > 0f;
            if (running)
                Label(g, "RUNNING: " + gText + "  -  winners in " + Mathf.CeilToInt(gEndT - Time.unscaledTime) + "s", 22, new Color(0.55f, 1f, 0.55f), new Vector2(16, -150), new Vector2(1330, 36));
            Btn(g, running ? "DRAW WINNERS NOW" : "START GIVEAWAY", new Vector2(16, -200), new Vector2(420, 70), running ? "Orange" : "Green", () => { if (gEndT > 0f) Draw(); else StartGiveaway(); }, 24);
            if (running) Btn(g, "CANCEL", new Vector2(446, -200), new Vector2(200, 70), "Red", () => { gEndT = -1f; Announce("The giveaway was cancelled.", false); }, 20);
            Btn(g, "GIFT PRIZE TO EVERYONE", new Vector2(950, -200), new Vector2(394, 70), "Yellow", () =>
            {
                string what = PrizeText();
                Announce("GIFT FOR EVERYONE: " + what + "! Check your inventory.", true);
                Social.Log("GIFT TO EVERYONE: " + what);
                GiveSelf(); // tu primesti si tu (ceilalti cand jocul e online)
            }, 20);
            Label(g, "The giveaway is announced in the chat, winners are drawn at random and announced too.",
                  15, new Color(1, 1, 1, 0.6f), new Vector2(16, -296), new Vector2(1330, 40));

            var a = Card(content, new Vector2(20, -456), new Vector2(1360, 180), CardCol);
            Label(a, "ANNOUNCEMENT", 26, new Color(0.6f, 0.85f, 1f), new Vector2(16, -10), new Vector2(400, 34));
            announceField = UIKit.MakeInput(a, "Ann", TL, TL, new Vector2(16, -56), new Vector2(1000, 56), announceField != null ? announceField.text : "", 120, 20);
            var ph = announceField.placeholder as Text; if (ph != null) ph.text = "Message for all players (shown big on screen + in GLOBAL chat)";
            Btn(a, "SEND TO ALL", new Vector2(1030, -56), new Vector2(314, 56), "Blue", () =>
            {
                string t = announceField.text.Trim();
                if (t.Length == 0) return;
                Announce(t, true);
                Social.Log("ANNOUNCE: " + t);
                announceField.text = "";
            }, 20);

        }

        string PrizeText() { return PrizeText(gPrize, Parse(gAmountField, 0)); }

        string PrizeText(int prize, long amt)
        {
            switch (prize)
            {
                case 0: return Fmt.Num(amt) + " COINS";
                case 1: return Fmt.Num(amt) + " SHARDS";
                case 2: return Fmt.Num(amt) + " TOKENS";
                case 3: return ItemDB.RarityNames[rarity].ToUpper() + " PET";
                case 4: return ItemDB.RarityNames[rarity].ToUpper() + " " + ItemDB.SlotNames[slot].ToUpper();
                case 5: return Fmt.Num(amt) + " FORGE STONES";
                case 6: return Fmt.Num(amt) + " GUARDIAN CLOAKS";
                case 8: return Fmt.Num(amt) + " XP";
                case 9: return Fmt.Num(amt) + " ORE";
                default: return Fmt.Num(amt) + "x EACH POTION";
            }
        }

        void GiveSelf() { GiveSelf(gPrize, Parse(gAmountField, 0)); }

        void GiveSelf(int prize, long amt)
        {
            switch (prize)
            {
                case 0: P.AddCoins(amt); break;
                case 1: P.AddShards(amt, "gift"); break;
                case 2: P.AddTokens(amt); break;
                case 3: Gift(P.D.playerName, 3, rarity); break;
                case 4: if (P.BagCount < P.BagSize) { var gi = ItemDB.Make((Slot)slot, rarity, map); gi.plus = forge; P.AddItem(gi); } break;
                case 5: P.D.forgeStones += (int)System.Math.Min(100000, amt); break;
                case 6: P.D.cloaks += (int)System.Math.Min(10000, amt); break;
                case 8: P.GainXp((int)System.Math.Min(int.MaxValue / 2, amt)); break;
                case 9: P.D.ore += (int)System.Math.Min(100000000, amt); break;
                default: for (int k = 0; k < 3; k++) P.AddPotion(k, (int)System.Math.Min(1000, amt)); break;
            }
        }

        void Announce(string text, bool big)
        {
            if (ChatUI.I != null) ChatUI.I.Post(0, "[ADMIN] " + text);
            if (big) game.hud.BigText(text.Length > 40 ? text.Substring(0, 40) + "..." : text, new Color(1f, 0.85f, 0.3f));
            Sfx.Play("JingleLevel", 0.5f, 1.1f, 0f);
        }

        void StartGiveaway()
        {
            gText = PrizeText() + " x" + WinnerCounts[gWinners];
            gCount = WinnerCounts[gWinners];
            gAud = gAudience;
            Announce("GIVEAWAY: " + gText + " winner" + (gCount > 1 ? "s" : "") + " (" + AudienceNames[gAud].ToLower() + ")" +
                     (DelayS[gDelay] > 0f ? " - drawing in " + DelayNames[gDelay].ToLower() + "!" : "!"), true);
            Social.Log("GIVEAWAY started: " + gText + " (" + AudienceNames[gAud] + ")");
            gEndT = Time.unscaledTime + DelayS[gDelay];
            if (DelayS[gDelay] <= 0f) Draw();
        }

        void Draw()
        {
            gEndT = -1f;
            var pool = new List<string>();
            if (gAud == 2) pool.AddRange(P.D.friends);
            else
            {
                var dir = Social.Directory;
                int take = gAud == 1 ? Mathf.Min(dir.Length, TradeDB.MaxPlayers - 1) : dir.Length;
                for (int i = 0; i < take; i++) if (Social.Active(dir[i], "BAN") == null) pool.Add(dir[i]);
            }
            pool.Add(P.D.playerName);
            var winners = new List<string>();
            for (int i = 0; i < gCount && pool.Count > 0; i++)
            {
                int k = Random.Range(0, pool.Count);
                winners.Add(pool[k]); pool.RemoveAt(k);
            }
            Announce("GIVEAWAY WINNERS (" + gText + "): " + string.Join(", ", winners.ToArray()), true);
            Social.Log("GIVEAWAY winners: " + string.Join(", ", winners.ToArray()));
            if (winners.Contains(P.D.playerName)) GiveSelf();
            Rebuild();
        }

        // ================================================================
        //  LOG
        // ================================================================
        void BuildLog()
        {
            var c = Card(content, new Vector2(20, -80), new Vector2(1360, 780), CardCol);
            var log = Social.AdminLog;
            Label(c, "ADMIN LOG  (" + log.Count + ")", 24, new Color(1f, 0.85f, 0.45f), new Vector2(16, -10), new Vector2(600, 34));
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < log.Count && i < 22; i++)
            {
                var t = new System.DateTime(log[i].time, System.DateTimeKind.Utc).ToLocalTime();
                sb.Append("<color=#9aa6c0>").Append(t.ToString("dd.MM HH:mm")).Append("</color>   ").Append(log[i].text.Replace("<", "‹")).Append('\n');
            }
            var txt = Label(c, log.Count == 0 ? "Nothing yet." : sb.ToString(), 18, Color.white, new Vector2(16, -56), new Vector2(1320, 690), TextAnchor.UpperLeft);
            txt.lineSpacing = 1.05f;
        }

        // ================================================================
        void KillNearby()
        {
            var pos = P.transform.position;
            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                if (i >= Enemy.All.Count) continue;
                var e = Enemy.All[i];
                if (e == null || e.Dying) continue;
                if ((e.transform.position - pos).sqrMagnitude < 40f * 40f) e.AdminKill();
            }
        }

        void WipeSave()
        {
            if (resetT <= 0f) { resetT = 3f; Sfx.Play("Error", 0.5f, 1.3f); Rebuild(); return; }
            AdminTools.BlockSave = true;
            SaveSystem.Wipe();
            Time.timeScale = 1f;
            AdminTools.God = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            AdminTools.BlockSave = false;
        }

        float refreshT;
        protected override void Update()
        {
            base.Update();
            if (resetT > 0f)
            {
                resetT -= Time.unscaledDeltaTime;
                if (resetT <= 0f) Rebuild();
            }
            // giveaway-ul merge si cu fereastra inchisa
            if (gEndT > 0f && Time.unscaledTime >= gEndT) Draw();
            if (IsOpen && page == 4 && gEndT > 0f && (refreshT -= Time.unscaledDeltaTime) <= 0f) { refreshT = 1f; Rebuild(); }
        }
    }
}
