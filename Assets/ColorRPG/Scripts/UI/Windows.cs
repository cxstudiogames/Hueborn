// =====================================================================
//  Windows.cs - ferestrele CHARACTER (nume, erou, awakening),
//  STATS (skill points HP / DMG / DEF + toate statisticile) si SETTINGS.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public abstract class Window : MonoBehaviour
    {
        protected Game game;
        protected RectTransform dim, content;
        protected Text title;
        public bool IsOpen { get { return dim != null && dim.gameObject.activeSelf; } }

        protected static readonly Vector2 C = new Vector2(0.5f, 0.5f), TL = new Vector2(0, 1), TR = new Vector2(1, 1),
                                          BL = new Vector2(0, 0), BR = new Vector2(1, 0), TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0);

        protected void Init(RectTransform parent, Game g, string n, string t, Vector2 size, string color)
        {
            game = g;
            content = UIKit.Window(parent, n, t, size, color, Close, out dim, out title);
            dim.gameObject.SetActive(false);
        }

        public void Open()
        {
            dim.gameObject.SetActive(true);
            dim.SetAsLastSibling();
            Sfx.Play("Open", 0.6f);
            Rebuild();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public virtual void Close()
        {
            if (!IsOpen) return;
            dim.gameObject.SetActive(false);
            Sfx.Play("Close", 0.6f);
            if (game != null && game.player != null) game.player.SaveNow();
        }

        public void Rebuild()
        {
            if (!IsOpen) return;
            UIKit.Clear(content);
            Build();
            FitTitle();
        }

        /// <summary>Eticheta titlului se lateste dupa text (unele ferestre isi schimba titlul).</summary>
        protected void FitTitle()
        {
            if (title == null) return;
            var hr = title.transform.parent as RectTransform;
            var panel = hr != null ? hr.parent as RectTransform : null;
            if (hr == null || panel == null) return;
            float tw = Mathf.Clamp(title.text.Length * 26f + 90f, 300f, panel.sizeDelta.x - 220f);
            hr.sizeDelta = new Vector2(tw, hr.sizeDelta.y);
            title.rectTransform.sizeDelta = new Vector2(tw - 30f, title.rectTransform.sizeDelta.y);
        }

        protected abstract void Build();

        protected virtual void Update()
        {
            if (IsOpen && Inp.EscapeDown() && !Inp.Typing) Close();
        }

        protected Text Label(Transform p, string s, int size, Color c, Vector2 pos, Vector2 box, TextAnchor a = TextAnchor.MiddleLeft, Vector2? anchor = null)
        {
            Vector2 an = anchor ?? TL;
            var t = UIKit.Text(p, "L", s, size, c, a, an, an, pos, box, true, true);
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        protected RectTransform Card(Transform p, Vector2 pos, Vector2 size, Color c, Vector2? anchor = null)
        {
            Vector2 an = anchor ?? TL;
            var rt = UIKit.Rect(p, "Card", an, an, pos, size);
            UIKit.Img(rt, "card_dark", UIKit.Theme(c), 18f, 1f);
            return rt;
        }
    }

    // ==================================================================
    //  CHARACTER - portret, nume, erou, awakening
    // ==================================================================
    public class CharacterUI : Window
    {
        InputField nameField;
        float resetT, pickT;   // confirmari (CHANGE CLASS / CHOOSE)
        int pickCls;

        protected override void Update()
        {
            base.Update();
            if (pickT > 0f) pickT -= Time.unscaledDeltaTime;
            if (resetT > 0f) { resetT -= Time.unscaledDeltaTime; if (resetT <= 0f && IsOpen) Rebuild(); }
        }

        public static CharacterUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("CharacterUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<CharacterUI>();
            w.Init(parent, g, "CharacterDim", "CHARACTER", new Vector2(1100, 700), "Blue");
            return w;
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            // portret mare
            var pc = Card(content, new Vector2(30, -24), new Vector2(340, 440), new Color(0.2f, 0.24f, 0.4f, 1f));
            var raw = UIKit.Rect(pc, "Portrait", TC, TC, new Vector2(0, -10), new Vector2(320, 320)).gameObject.AddComponent<RawImage>();
            raw.texture = game.portrait != null ? game.portrait.Texture : null;
            raw.raycastTarget = false;
            Label(pc, SkillDB.HeroTitle(D.hero).ToUpper(), 22, new Color(1f, 0.85f, 0.4f), new Vector2(0, -330), new Vector2(340, 32), TextAnchor.MiddleCenter, TC);
            // skill-urile eroului (1 / 2 / 3) - se schimba odata cu eroul
            var sc = Card(content, new Vector2(30, -476), new Vector2(340, 120), new Color(0.16f, 0.18f, 0.3f, 1f));
            var sk = SkillDB.ForHero(D.hero);
            string[] keys = { Device.Key("1"), Device.Key("2"), Device.Key("3") };
            for (int i = 0; i < 3; i++)
            {
                var d = SkillDB.Get(sk[i]);
                var ic = UIKit.Rect(sc, "Sk" + i, TL, TL, new Vector2(28 + i * 110, -8), new Vector2(58, 58)).gameObject.AddComponent<Image>();
                ic.sprite = ItemIcons.Get(d.icon); ItemIcons.Art(ic);
                var kind = sk[i]; string key = keys[i];
                TextHover.Attach(ic.gameObject, () => SkillDB.Tooltip(kind, key), d.col);
                Label(sc, keys[i], 18, new Color(1f, 0.9f, 0.5f), new Vector2(88 + i * 110, -6), new Vector2(30, 24));
                var nm = Label(sc, d.name, 15, Color.Lerp(d.col, Color.white, 0.35f), new Vector2(4 + i * 110, -70), new Vector2(106, 40), TextAnchor.UpperCenter);
                nm.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            UIKit.Button(pc, "Prev", "<", "Grey", BL, BL, new Vector2(16, 14), new Vector2(90, 56), () => { p.ChangeHero(-1); Rebuild(); }, 28);
            Label(pc, "HERO " + (D.hero + 1) + "/" + GameData.Heroes.Length, 18, Color.white, new Vector2(0, 42), new Vector2(160, 30), TextAnchor.MiddleCenter, BC);
            UIKit.Button(pc, "Next", ">", "Grey", BR, BR, new Vector2(-16, 14), new Vector2(90, 56), () => { p.ChangeHero(1); Rebuild(); }, 28);

            // nume
            Label(content, "NAME", 22, new Color(1f, 0.9f, 0.6f), new Vector2(400, -24), new Vector2(300, 34));
            nameField = UIKit.MakeInput(content, "Name", TL, TL, new Vector2(400, -62), new Vector2(430, 60), D.playerName, 16, 26);
            UIKit.Button(content, "Save", "SAVE", "Green", TL, TL, new Vector2(846, -62), new Vector2(220, 60), SaveName, 22);
            var cls = p.Class;
            string tag = GameData.ClassTagRich(D.awakenClass, D.awakenTier, 22);
            Label(content, tag + "<color=#b8c4d6>Lv " + D.level + "</color>  " + D.playerName + "   -   " + cls.name, 22, Color.white, new Vector2(400, -140), new Vector2(660, 34));

            // STATISTICI (in dreapta avatarului, cu loc suficient)
            var st = Card(content, new Vector2(400, -186), new Vector2(670, 410), new Color(0.2f, 0.22f, 0.34f, 1f));
            Label(st, "STATISTICS", 28, new Color(1f, 0.8f, 0.3f), new Vector2(0, -14), new Vector2(670, 40), TextAnchor.MiddleCenter, TC);
            string[] sl = { "MONSTERS KILLED", "BOSSES KILLED", "QUESTS DONE", "DEATHS", "TIME PLAYED", "REBIRTHS" };
            string[] sv = { Fmt.Num(D.kills), Fmt.Num(D.bossKills), Fmt.Num(D.questsDone), Fmt.Num(D.deaths), Mathf.FloorToInt(D.playTime / 3600f) + "h " + Mathf.FloorToInt(D.playTime % 3600f / 60f) + "m", Fmt.Num(D.rebirth) };
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2, row = i / 2;
                var box = Card(st, new Vector2(24 + col * 316, -70 - row * 112), new Vector2(304, 100), new Color(0.14f, 0.16f, 0.26f, 1f));
                Label(box, sv[i], 36, Color.white, new Vector2(0, -8), new Vector2(304, 50), TextAnchor.MiddleCenter, TC);
                Label(box, sl[i], 15, new Color(1f, 0.85f, 0.45f), new Vector2(0, -62), new Vector2(304, 26), TextAnchor.MiddleCenter, TC);
            }
        }

        void SaveName()
        {
            string n = nameField != null ? nameField.text.Trim() : "";
            if (n.Length < 2) { game.Say("The name must have at least 2 characters!", 2f); Sfx.Play("Error", 0.5f); return; }
            game.player.D.playerName = n;
            game.player.SaveNow();
            ColorRPG.Net.Ugs.Rename(n);   // numele si in clasamentele online
            game.Say("Name saved: " + n, 2f);
            Sfx.Play("Equip", 0.6f);
            Rebuild();
        }
    }

    // ==================================================================
    //  AWAKENING - fereastra altarului din ultima harta (alegi clasa, I / II / III)
    // ==================================================================
    public class AwakeningUI : Window
    {
        float resetT, pickT;
        int pickCls;
        /// <summary>Harta cu altarul (ultima harta).</summary>
        public static int ShrineMap { get { return GameData.Maps.Length - 1; } }
        public static string ShrineMapName { get { return GameData.Maps[ShrineMap].name; } }
        public static Vector3 ShrinePos { get { return GameData.Maps[ShrineMap].Spawn + new Vector3(-27f, 0f, 2f); } }

        /// <summary>Te duce la altar (de la Lv 20, chiar daca harta are nivel mai mare - altarul e langa spawn, in siguranta).</summary>
        public static void GoToShrine()
        {
            var g = Game.I;
            if (g == null) return;
            g.Travel(ShrineMap, true);
            g.player.Teleport(ShrinePos + new Vector3(7f, 0.1f, 0f));
            g.Say("AWAKENING SHRINE - walk to the altar and press " + (Device.Mobile ? "the button" : "E") + "!", 4f);
        }

        protected override void Update()
        {
            base.Update();
            if (pickT > 0f) pickT -= Time.unscaledDeltaTime;
            if (resetT > 0f) { resetT -= Time.unscaledDeltaTime; if (resetT <= 0f && IsOpen) Rebuild(); }
        }

        public static AwakeningUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("AwakeningUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<AwakeningUI>();
            w.Init(parent, g, "AwakeningDim", "AWAKENING", new Vector2(760, 560), "Orange");
            return w;
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            var aw = Card(content, new Vector2(30, -24), new Vector2(700, 470), new Color(0.2f, 0.22f, 0.34f, 1f));
            int tier = D.awakenTier;
            Label(aw, "AWAKENING  " + tier + " / " + GameData.MaxAwaken, 30, new Color(1f, 0.8f, 0.3f), new Vector2(0, -12), new Vector2(700, 40), TextAnchor.MiddleCenter, TC);
            for (int k = 0; k < GameData.MaxAwaken; k++)
            {
                var st = UIKit.Rect(aw, "Star" + k, TL, C, new Vector2(40 + k * 40, -32), new Vector2(34, 34)).gameObject.AddComponent<Image>();
                st.sprite = ItemIcons.Get(26); ItemIcons.Art(st); st.color = k < tier ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 1f, 1f, 0.2f); st.raycastTarget = false;
            }
            if (D.awakenClass == 0)
            {
                bool changing = p.ChoosingNewClass;
                bool can = changing || D.level >= GameData.AwakenLevels[0];
                Label(aw, changing ? "Choose your NEW class - it keeps your Awakening " + GameData.TierRoman[D.awakenTier] + "." :
                          can ? "Choose your class carefully - <color=#ff8a8a>changing it later costs a lot!</color>  It grows stronger at Awakening II and III."
                              : "Reach level " + GameData.AwakenLevels[0] + " for Awakening I (pick a class). Awakening II at Lv " + GameData.AwakenLevels[1] + ", III at Lv " + GameData.AwakenLevels[2] + ".",
                      17, new Color(1, 1, 1, 0.85f), new Vector2(0, -56), new Vector2(660, 44), TextAnchor.MiddleCenter, TC);
                for (int i = 1; i < GameData.Classes.Length; i++)
                {
                    var c = GameData.Classes[i];
                    int ci = i;
                    int col = (i - 1) % 2, row = (i - 1) / 2;
                    var card = Card(aw, new Vector2(24 + col * 330, -106 - row * 150), new Vector2(318, 142), new Color(0.14f, 0.16f, 0.26f, 1f));
                    var badge = UIKit.Rect(card, "Tag", TL, TL, new Vector2(12, -12), new Vector2(60, 60));
                    UIKit.Img(badge, "round_Grey", c.col, 16f);
                    UIKit.Text(badge, "T", c.tag, 30, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(60, 60));
                    Label(card, c.name.ToUpper(), 24, Color.white, new Vector2(84, -12), new Vector2(220, 32));
                    Label(card, c.desc + "\n<color=#8a90a0>> " + GameData.ClassTierNames[i][1] + " > " + GameData.ClassTierNames[i][2] + "</color>", 14, new Color(1, 1, 1, 0.85f), new Vector2(84, -44), new Vector2(226, 46), TextAnchor.UpperLeft);
                    UIKit.Button(card, "Pick", can ? "CHOOSE" : "LV " + GameData.AwakenLevels[0], can ? "Yellow" : "Grey", BC, BC, new Vector2(0, 8), new Vector2(200, 46), () =>
                    {
                        if (pickT <= 0f || pickCls != ci) { pickT = 3f; pickCls = ci; game.Say("Click CHOOSE again to become a " + GameData.Classes[ci].name + "!", 2.5f); Sfx.Play("Click", 0.5f); return; }
                        pickT = 0f;
                        if (p.Awaken(ci)) Rebuild();
                    }, 18).interactable = can;
                }
                return;
            }
            for (int t = 1; t <= GameData.MaxAwaken; t++)
            {
                var c = GameData.ClassAt(D.awakenClass, t);
                bool done = t <= tier;
                var card = Card(aw, new Vector2(24 + (t - 1) * 222, -66), new Vector2(210, 260), done ? Color.Lerp(c.col, Color.white, 0.25f) : new Color(0.14f, 0.16f, 0.26f, 1f));
                var badge = UIKit.Rect(card, "Tag", TC, C, new Vector2(0, -44), new Vector2(64, 64));
                UIKit.Img(badge, "round_Grey", done ? c.col : new Color(0.4f, 0.4f, 0.45f), 16f);
                var bt = UIKit.Text(badge, "T", c.tag, 30, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(64, 64));
                bt.font = t >= 3 ? TitleStyle.Fancy : t == 2 ? TitleStyle.Serif : UIKit.Font;
                if (t >= 3) bt.fontSize = Mathf.RoundToInt(38 * UIKit.FontScale);
                var nmL = Label(card, GameData.TierRoman[t] + ".  " + c.name.ToUpper(), 20, Color.white, new Vector2(0, -80), new Vector2(200, 28), TextAnchor.MiddleCenter, TC);
                UIKit.Fit(nmL, 12);
                var dsL = Label(card, c.desc, 14, new Color(1, 1, 1, 0.85f), new Vector2(0, -112), new Vector2(196, 80), TextAnchor.UpperCenter, TC);
                UIKit.Fit(dsL, 10);
                var lvL = Label(card, done ? "<color=#7dff8a>UNLOCKED</color>" : "Lv " + GameData.AwakenLevels[t - 1] + (GameData.AwakenCoins[t - 1] > 0 ? "\n" + Fmt.Num(GameData.AwakenCoins[t - 1]) + " coins + " + GameData.AwakenShards[t - 1] + " shards" : ""),
                      14, new Color(1f, 0.9f, 0.6f), new Vector2(0, 8), new Vector2(200, 44), TextAnchor.LowerCenter, BC);
                UIKit.Fit(lvL, 10);
            }
            if (tier < GameData.MaxAwaken)
            {
                int nt = tier + 1;
                bool can = D.level >= GameData.AwakenLevels[nt - 1];
                var ab = UIKit.Button(aw, "Awaken", can ? "AWAKEN " + GameData.TierRoman[nt] + "!" : "AWAKENING " + GameData.TierRoman[nt] + " AT LV " + GameData.AwakenLevels[nt - 1],
                             can ? "Yellow" : "Grey", BL, BL, new Vector2(24, 18), new Vector2(420, 70), () => { if (p.Awaken(0)) Rebuild(); }, 24);
                ab.interactable = can;
                UIKit.Fit(ab.GetComponentInChildren<Text>(), 14);
            }
            else Label(aw, "FINAL AWAKENING!", 26, new Color(1f, 0.8f, 0.3f), new Vector2(24, 32), new Vector2(420, 40), TextAnchor.MiddleCenter, BL);
            var rc = UIKit.Button(aw, "ResetCls", "", resetT > 0f ? "Red" : "Grey", BR, BR, new Vector2(-24, 18), new Vector2(220, 70), () =>
            {
                if (resetT <= 0f) { resetT = 3f; Sfx.Play("Error", 0.4f, 1.3f); Rebuild(); return; }
                resetT = 0f;
                if (p.ResetClass()) Rebuild();
            }, 14);
            var rct = UIKit.Text(rc.transform, "T", resetT > 0f ? "CLICK AGAIN\nTO CHANGE CLASS" : "CHANGE CLASS\n<size=14>" + Fmt.Num(p.ClassResetCoins) + " coins + " + Fmt.Num(p.ClassResetShards) + " shards</size>",
                                 17, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(210, 64));
            rct.supportRichText = true; UIKit.Fit(rct, 10);
        }
    }

    // ==================================================================
    //  STATS - skill points (HP / DMG / DEF) + toate statisticile
    // ==================================================================
    public class StatsUI : Window
    {
        public static StatsUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("StatsUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<StatsUI>();
            w.Init(parent, g, "StatsDim", "STATS", new Vector2(1100, 720), "Green");
            return w;
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            int free = p.PointsFree;
            var top = Card(content, new Vector2(24, -20), new Vector2(1052, 70), free > 0 ? new Color(0.3f, 0.25f, 0.08f, 1f) : new Color(0.2f, 0.22f, 0.34f, 1f));
            Label(top, "LEVEL " + D.level + "      SKILL POINTS: <color=#ffd84d>" + free + "</color>  (+" + Player.PointsPerLevel + " every level)", 26, Color.white, new Vector2(24, -16), new Vector2(760, 40));
            // resetare GRATIS o data la 24 de ore
            bool canReset = p.CanResetStats;
            var lft = p.StatResetLeft;
            var rb = UIKit.Button(top, "Reset", canReset ? "FREE RESET" : "RESET IN " + (int)lft.TotalHours + "h " + lft.Minutes + "m", canReset ? "Red" : "Grey", TR, TR, new Vector2(-12, -8), new Vector2(230, 54),
                                  () => { if (p.FreeResetPoints()) Rebuild(); }, 18);
            UIKit.Fit(rb.GetComponentInChildren<Text>(), 12);

            string[] names = { "HEALTH", "DAMAGE", "DEFENSE" };
            string[] per = { "+12 max HP per point", "+3% damage per point", "+0.8% damage reduction per point" };
            Color[] cols = { new Color(1f, 0.35f, 0.4f), new Color(1f, 0.65f, 0.25f), new Color(0.4f, 0.7f, 1f) };
            int[] vals = { D.statHp, D.statDmg, D.statDef };
            for (int i = 0; i < 3; i++)
            {
                int si = i;
                var row = Card(content, new Vector2(24, -106 - i * 110), new Vector2(520, 98), new Color(0.2f, 0.22f, 0.34f, 1f));
                var ic = UIKit.Rect(row, "Col", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(12, 98));
                ic.gameObject.AddComponent<Image>().color = cols[i];
                Label(row, names[i], 26, cols[i], new Vector2(28, -12), new Vector2(260, 34));
                Label(row, per[i], 15, new Color(1, 1, 1, 0.75f), new Vector2(28, -52), new Vector2(280, 30));
                Label(row, vals[i].ToString(), 38, Color.white, new Vector2(-150, 0), new Vector2(90, 60), TextAnchor.MiddleCenter, new Vector2(1, 0.5f));
                UIKit.Button(row, "P1", "+1", free > 0 ? "Green" : "Grey", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-80, 0), new Vector2(64, 60), () => { if (p.AddPoint(si, 1)) Rebuild(); }, 22);
                UIKit.Button(row, "P5", "+5", free > 0 ? "Green" : "Grey", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(64, 60), () => { if (p.AddPoint(si, 5)) Rebuild(); }, 22);
            }

            // toate statisticile
            // INDEX: cate iteme din catalog ai descoperit
            var idx = Card(content, new Vector2(24, -440), new Vector2(520, 184), new Color(0.2f, 0.22f, 0.34f, 1f));
            Label(idx, "ITEM INDEX", 22, new Color(1f, 0.9f, 0.6f), new Vector2(18, -10), new Vector2(480, 30));
            int seen = D.seenCat != null ? D.seenCat.Count : 0;
            Label(idx, "Discovered <color=#7dff8a>" + seen + "</color> / " + ItemCatalog.Count + " items.\nEvery map has 6 unique weapons, helmets, armors and rings.\nVisit the INDEX in Meadow Fields to see them all!", 16, Color.white, new Vector2(18, -46), new Vector2(490, 130)).alignment = TextAnchor.UpperLeft;

            var st = Card(content, new Vector2(566, -106), new Vector2(510, 518), new Color(0.2f, 0.22f, 0.34f, 1f));
            Label(st, "ALL STATS", 24, new Color(1f, 0.9f, 0.6f), new Vector2(20, -12), new Vector2(460, 34));
            var cls = p.Class;
            string[,] rows =
            {
                { "Class", D.awakenClass > 0 ? cls.name + " (" + cls.tag + ")" : "Not awakened" },
                { "Damage", Fmt.Num(p.Damage) },
                { "Max HP", Fmt.Num(p.MaxHp) },
                { "Defense", Mathf.RoundToInt(p.Defense * 100) + "%" },
                { "Crit chance", Mathf.RoundToInt(p.CritChance * 100) + "%  (x2.2 dmg)" },
                { "Attack speed", (1f / p.AttackCooldown).ToString("0.0") + " hits/s" },
                { "Coin bonus", "x" + p.CoinMul.ToString("0.00") },
                { "Pet bonus", "+" + Mathf.RoundToInt(p.PetBonus * 100) + "% dmg  (" + p.PetsEquippedCount + "/" + p.MaxPets + " equipped)" },
                { "Shards", Fmt.Num(D.shards) },
                { "Rebirths", D.rebirth + " / " + Player.MaxRebirth + "  (+" + (D.rebirth * 25) + "% dmg)" },
                { "Monsters slain", Fmt.Num(D.kills) },
                { "Bosses slain", Fmt.Num(D.bossKills) },
                { "Trades", D.trades.ToString() },
                { "Next rebirth", p.RebirthMaxed ? "MAX" : "level " + p.RebirthReq },
                { "Play time", Mathf.FloorToInt(D.playTime / 3600f) + "h " + Mathf.FloorToInt(D.playTime % 3600f / 60f) + "m" },
            };
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Label(st, rows[i, 0], 17, new Color(1, 1, 1, 0.75f), new Vector2(20, -52 - i * 29), new Vector2(200, 28));
                Label(st, rows[i, 1], 17, Color.white, new Vector2(-20, -52 - i * 29), new Vector2(300, 28), TextAnchor.MiddleRight, TR);
            }
        }
    }

    // ==================================================================
    //  SETTINGS - performanta, grafica, sunet
    // ==================================================================
    public class SettingsUI : Window
    {
        Text fpsText;
        float fpsAcc; int fpsFrames;

        public static SettingsUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("SettingsUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<SettingsUI>();
            w.Init(parent, g, "SettingsDim", "SETTINGS", new Vector2(1100, 600), "Grey");
            return w;
        }

        void Row(Transform p, string name, float y)
        {
            Label(p, name, 20, Color.white, new Vector2(24, y), new Vector2(300, 40));
        }

        void Section(Transform p, string name, float y, Color c)
        {
            Label(p, name, 24, c, new Vector2(20, y), new Vector2(480, 36));
        }

        protected override void Build()
        {
            // ---- AUDIO
            var a = Card(content, new Vector2(24, -20), new Vector2(520, 170), new Color(0.2f, 0.22f, 0.34f, 1f));
            Section(a, "AUDIO", -12, new Color(1f, 0.85f, 0.4f));
            Row(a, "Ambient music", -56);
            UIKit.MakeSlider(a, "Music", TL, TL, new Vector2(250, -58), new Vector2(240, 36), GameSettings.MusicVolume,
                             v => { GameSettings.MusicVolume = v; GameSettings.Apply(); }, new Color(0.45f, 0.8f, 1f));
            Row(a, "Sound effects", -110);
            UIKit.MakeSlider(a, "Sfx", TL, TL, new Vector2(250, -112), new Vector2(240, 36), GameSettings.SfxVolume,
                             v => { GameSettings.SfxVolume = v; GameSettings.Apply(); }, new Color(1f, 0.7f, 0.3f));

            // ---- GAME: FPS in timp real, particule, joc online pe harti, confidentialitate
            var pf = Card(content, new Vector2(24, -204), new Vector2(520, 290), new Color(0.2f, 0.22f, 0.34f, 1f));
            Section(pf, "GAME", -12, new Color(0.5f, 1f, 0.6f));
            Row(pf, "FPS", -56);
            fpsText = Label(pf, "...", 26, new Color(0.5f, 1f, 0.6f), new Vector2(250, -56), new Vector2(240, 40), TextAnchor.MiddleLeft);
            Row(pf, "Ambient particles", -110);
            UIKit.MakeSlider(pf, "Parts", TL, TL, new Vector2(250, -112), new Vector2(240, 36), GameSettings.Particles,
                             v => { GameSettings.Particles = v; GameSettings.Apply(); }, new Color(0.9f, 0.6f, 1f));
            ToggleRow(pf, "Play with others on maps", GameSettings.OnlineMaps, -164, () => { GameSettings.OnlineMaps = !GameSettings.OnlineMaps; GameSettings.Save(); Rebuild(); });

            // ---- confidentialitate (cerinta Google Play): politica + stergerea contului din joc
            UIKit.Button(pf, "Privacy", "PRIVACY POLICY", "Grey", TL, TL, new Vector2(24, -228), new Vector2(230, 46), () => Application.OpenURL(PrivacyUrl), 15);
            UIKit.Button(pf, "DelData", deleteT > 0f ? "TAP AGAIN TO DELETE" : "DELETE MY DATA", "Red", TL, TL, new Vector2(266, -228), new Vector2(230, 46), DeleteMyData, 15);

            // ---- GRAPHICS
            var gr = Card(content, new Vector2(560, -20), new Vector2(516, 470), new Color(0.2f, 0.22f, 0.34f, 1f));
            Section(gr, "GRAPHICS", -12, new Color(0.6f, 0.8f, 1f));
            Row(gr, "Quality", -62);
            for (int i = 0; i < GameSettings.QualityNames.Length; i++)
            {
                int qi = i;
                UIKit.Button(gr, "Q" + i, GameSettings.QualityNames[i], GameSettings.Quality == i ? "Blue" : "Grey", TL, TL,
                             new Vector2(150 + i * 176, -58), new Vector2(170, 48), () => { GameSettings.ApplyPreset(qi); Rebuild(); }, 13);
            }
            ToggleRow(gr, "Shadows", GameSettings.Shadows, -122, () => { GameSettings.Shadows = !GameSettings.Shadows; GameSettings.Apply(); Rebuild(); });
            ToggleRow(gr, "Bloom & colors (post FX)", GameSettings.PostFx, -178, () => { GameSettings.PostFx = !GameSettings.PostFx; GameSettings.Apply(); Rebuild(); });
            ToggleRow(gr, "3D grass", GameSettings.Grass, -234, () => { GameSettings.Grass = !GameSettings.Grass; GameSettings.Save(); Rebuild(); });
            ToggleRow(gr, "Damage numbers", GameSettings.ShowDamageNumbers, -290, () => { GameSettings.ShowDamageNumbers = !GameSettings.ShowDamageNumbers; GameSettings.Apply(); Rebuild(); });
            Row(gr, "Camera sensitivity", -350);
            UIKit.MakeSlider(gr, "CamSens", TL, TL, new Vector2(250, -352), new Vector2(240, 36), GameSettings.CamSens,
                             v => { GameSettings.CamSens = v; GameSettings.Save(); }, new Color(0.6f, 0.8f, 1f));

            // (CONTROLS sunt acum in GUIDE)

            // ---- iesire din joc (salveaza inainte)
            UIKit.Button(gr, "Exit", "EXIT GAME", "Red", BC, BC, new Vector2(0, 16), new Vector2(300, 60), ExitGame, 22);

            // ---- versiunea jocului (apasa de 5 ori = deblocare ADMIN cu cod)
            var ver = Label(content, "HUEBORN: LOOT LEGENDS  v" + Application.version + (adminPrompt || string.IsNullOrEmpty(ColorRPG.Net.Ugs.PlayerId) ? "" : "   ID " + ColorRPG.Net.Ugs.PlayerId), 15, new Color(1, 1, 1, 0.4f), new Vector2(24, 12), new Vector2(760, 30), TextAnchor.MiddleLeft, BL);
            ver.horizontalOverflow = HorizontalWrapMode.Overflow;   // pe un singur rand, in chenar
            if (AdminAccess.IsAdmin && !adminPrompt)
                Label(content, "ADMIN", 15, new Color(1f, 0.55f, 0.48f, 0.85f), new Vector2(-24, 12), new Vector2(120, 30), TextAnchor.MiddleRight, BR);
            ver.raycastTarget = true;
            var vb = ver.gameObject.AddComponent<Button>(); vb.transition = Selectable.Transition.None;
            vb.onClick.AddListener(() => { if (++verTaps >= 5) { verTaps = 0; adminPrompt = true; Rebuild(); } });
            if (adminPrompt)
            {
                if (AdminAccess.Unlocked)
                {
                    Label(content, "ADMIN is unlocked on this device.", 16, new Color(1f, 0.6f, 0.5f), new Vector2(360, 8), new Vector2(360, 34), TextAnchor.MiddleLeft, BL);
                    UIKit.Button(content, "Lock", "LOCK ADMIN", "Grey", BL, BL, new Vector2(730, 4), new Vector2(200, 42), () => { AdminAccess.Lock(); adminPrompt = false; Rebuild(); }, 15);
                }
                else
                {
                    var code = UIKit.MakeInput(content, "Code", BL, BL, new Vector2(360, 4), new Vector2(300, 42), "", 24, 18);
                    code.contentType = InputField.ContentType.Password;
                    var ph = code.placeholder as Text; if (ph != null) ph.text = "Admin code";
                    UIKit.Button(content, "Unlock", "UNLOCK", "Red", BL, BL, new Vector2(670, 4), new Vector2(160, 42), () =>
                    {
                        if (AdminAccess.TryUnlock(code.text)) { game.Say("ADMIN unlocked on this device.", 2.5f); Sfx.Play("JingleLevel", 0.5f); adminPrompt = false; }
                        else { game.Say("Wrong code.", 1.5f); Sfx.Play("Error", 0.5f); }
                        Rebuild();
                    }, 16);
                }
            }
        }

        /// <summary>Adresa paginii cu politica de confidentialitate (aceeasi ca in Google Play Console).</summary>
        public const string PrivacyUrl = "https://claude.ai/artifact/JiaFeDSddyN2uEi3mQbXx5";
        float deleteT;
        bool deleting;
        void DeleteMyData()
        {
            if (deleting) return;
            if (deleteT <= 0f)
            {
                deleteT = 5f;
                game.Say("This deletes your online account, cloud save, leaderboard entries AND your progress on this device. Tap again to confirm.", 4.5f);
                Sfx.Play("Error", 0.5f, 0.9f);
                Rebuild();
                return;
            }
            deleting = true;
            game.Say("Deleting your data...", 3f);
            ColorRPG.Net.Ugs.DeleteAccount(ok =>
            {
                deleting = false; deleteT = 0f;
                if (!ok) { game.Say("Could not reach the server. Connect to the internet and try again, or email cxstudiodev@gmail.com.", 5f); Rebuild(); return; }
                AdminTools.BlockSave = true;   // nimic nu mai salveaza pana la repornirea scenei
                OnlineAccount.ForgetCloud();
                SaveSystem.Wipe();
                PlayerPrefs.DeleteKey("crpg_admin_ok");
                PlayerPrefs.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
        }

        float exitT;
        int verTaps;
        bool adminPrompt;
        void ExitGame()
        {
            if (exitT <= 0f) { exitT = 3f; game.Say("Press EXIT GAME again to quit (your progress is saved).", 2.5f); Sfx.Play("Error", 0.4f, 1.3f); return; }
            game.player.SaveNow();
            PlayerPrefs.Save();
            if (Application.isEditor)
            {
                // in editor: oprim Play mode
                var ea = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
                var prop = ea != null ? ea.GetProperty("isPlaying") : null;
                if (prop != null) prop.SetValue(null, false);
                return;
            }
            Application.Quit();
        }

        void ToggleRow(Transform p, string name, bool on, float y, UnityEngine.Events.UnityAction a)
        {
            Row(p, name, y);
            UIKit.Toggle(p, name, on, TR, TR, new Vector2(-20, y + 4), a);
        }

        protected override void Update()
        {
            base.Update();
            if (exitT > 0f) exitT -= Time.unscaledDeltaTime;
            if (deleteT > 0f && (deleteT -= Time.unscaledDeltaTime) <= 0f && !deleting) Rebuild();
            if (!IsOpen || fpsText == null) return;
            fpsAcc += Time.unscaledDeltaTime; fpsFrames++;
            if (fpsAcc >= 0.5f)
            {
                int f = Mathf.RoundToInt(fpsFrames / fpsAcc);
                fpsText.text = f.ToString();
                fpsText.color = f >= 50 ? new Color(0.5f, 1f, 0.6f) : f >= 28 ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.45f, 0.4f);
                fpsAcc = 0; fpsFrames = 0;
            }
        }
    }
}
