// =====================================================================
//  GameWindows.cs - ferestrele TELEPORT (orice harta, cu Teleport Pass),
//  PETS (colectia de pets: 2 echipati, 4 cu Game Pass) si GENERAL SHOP
//  (coins, shards, pass-uri - deocamdata in mod de TEST, fara plata reala).
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    // ==================================================================
    //  TELEPORT - alegi harta (doar cu Teleport Pass)
    // ==================================================================
    public class TeleportUI : Window
    {
        public static TeleportUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("TeleportUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<TeleportUI>();
            w.Init(parent, g, "TeleportDim", "TELEPORT", new Vector2(1000, 700), "Blue");
            return w;
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            bool pass = D.passTeleport;
            var top = Card(content, new Vector2(24, -18), new Vector2(952, 84), pass ? new Color(0.14f, 0.3f, 0.2f, 1f) : new Color(0.32f, 0.2f, 0.1f, 1f));
            Label(top, pass ? "<color=#7dff8a>TELEPORT PASS</color>" : "<color=#ffcc55>TELEPORT PASS NEEDED</color>",
                  26, Color.white, new Vector2(20, -10), new Vector2(680, 66));
            if (!pass)
                UIKit.Button(top, "Get", "GET PASS", "Yellow", TR, TR, new Vector2(-14, -12), new Vector2(210, 60), () => { Close(); game.hud.shop.Open(); }, 20);

            // 4 harti + TRADE PLAZA, fiecare cu poza ei
            int maps = GameData.Maps.Length, n = maps + 1;
            for (int i = 0; i < n; i++)
            {
                bool plaza = i == maps, shrine = false;
                var m = i < maps ? GameData.Maps[i] : null;
                int col = i % 3, row = i / 3;
                Color c = plaza ? new Color(1f, 0.8f, 0.3f) : shrine ? new Color(1f, 0.6f, 0.2f) : m.ground;
                var card = Card(content, new Vector2(24 + col * 320, -116 - row * 262), new Vector2(304, 252), new Color(0.2f, 0.22f, 0.34f, 1f));
                // poza hartii (Resources/UI/Maps - ColorRPG > Capture Map Pictures)
                var picRt = UIKit.Rect(card, "Picture", TC, TC, new Vector2(0, -6), new Vector2(292, 150));
                var tex = Resources.Load<Texture2D>("UI/Maps/" + (plaza ? "plaza" : shrine ? "shrine" : "map" + i));
                if (tex != null) { var raw = picRt.gameObject.AddComponent<RawImage>(); raw.texture = tex; raw.raycastTarget = false; }
                else UIKit.Img(picRt, "flat_Grey", c, 12f, 1f);
                var strip = UIKit.Rect(picRt, "NameStrip", BC, BC, Vector2.zero, new Vector2(292, 40));
                UIKit.Img(strip, "flat_Grey", new Color(0.05f, 0.05f, 0.08f, 0.6f), 8f, 1f);
                Label(strip, plaza ? "TRADE PLAZA" : shrine ? "AWAKENING SHRINE" : m.name.ToUpper(), 22, Color.white, new Vector2(0, 0), new Vector2(292, 40), TextAnchor.MiddleCenter, C);
                bool here = plaza ? game.InPlaza : shrine ? false : (!game.InPlaza && game.mapIndex == i);
                bool unlocked = plaza ? !game.TradeLocked : shrine ? (p.Level >= GameData.AwakenLevels[0] || D.awakenClass != 0) : p.Level >= m.req;
                string info = plaza ? "Lv " + GameData.TradeLevel + "+  -  trade with players" : shrine ? "Lv " + GameData.AwakenLevels[0] + "+  -  " + AwakeningUI.ShrineMapName : "Lv " + m.req + "+";
                Label(card, info, 17, new Color(1, 1, 1, 0.85f), new Vector2(0, -160), new Vector2(292, 26), TextAnchor.MiddleCenter, TC);
                bool needPass = !shrine && !pass;   // altarul nu cere TELEPORT PASS
                string lockLv = plaza ? "Lv " + GameData.TradeLevel : shrine ? "Lv " + GameData.AwakenLevels[0] : "Lv " + (m != null ? m.req : 0);
                string btn = here ? "YOU ARE HERE" : !unlocked ? "LOCKED (" + lockLv + ")" : needPass ? "NEEDS PASS" : "GO";
                string colr = here ? "Grey" : !unlocked ? "Grey" : needPass ? "Yellow" : "Green";
                int ti = i;
                UIKit.Button(card, "Go", btn, colr, BC, BC, new Vector2(0, 10), new Vector2(260, 54), () =>
                {
                    if (here || !unlocked) { Sfx.Play("Error", 0.5f); return; }
                    if (!game.player.D.passTeleport) { Close(); game.hud.shop.Open(); return; }
                    Close();
                    if (ti == maps) game.GoPlaza(); else game.Travel(ti, false);
                }, 20);
            }
        }
    }

    // ==================================================================
    //  PETS - colectia ta de pets
    // ==================================================================
    public class PetsUI : Window
    {
        PetData selected;
        float releaseT;

        public static PetsUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("PetsUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<PetsUI>();
            w.Init(parent, g, "PetsDim", "PETS", new Vector2(1240, 760), "Blue");
            return w;
        }

        /// <summary>Selecteaza cel mai puternic pet (filmarea pentru reclama).</summary>
        public void SelectBest()
        {
            selected = null;
            foreach (var x in game.player.D.pets) if (selected == null || Player.PetPower(x) > Player.PetPower(selected)) selected = x;
            if (IsOpen) Rebuild();
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            if (selected != null && !D.pets.Contains(selected)) selected = null;
            var top = Card(content, new Vector2(24, -16), new Vector2(1192, 70), new Color(0.2f, 0.22f, 0.34f, 1f));
            Label(top, "EQUIPPED <color=#ffd84d>" + p.PetsEquippedCount + "/" + p.MaxPets + "</color>" + (D.passGame ? "  <color=#7dff8a>(GAME PASS)</color>" : "  <color=#8890aa>(4 with GAME PASS)</color>") +
                       "     PET BONUS: <color=#7dff8a>+" + Mathf.RoundToInt(p.PetBonus * 100) + "% damage</color>     STORAGE: " + D.pets.Count + "/" + GameData.PetStorage,
                  20, Color.white, new Vector2(20, -16), new Vector2(1150, 40));

            // grila 4 x 4 (16 locuri); salvarile vechi cu mai multi pets vad grila mare 6 x 5
            var list = new System.Collections.Generic.List<PetData>(D.pets);
            list.Sort((a, b) => a.equipped != b.equipped ? (a.equipped ? -1 : 1) : Player.PetPower(b).CompareTo(Player.PetPower(a)));
            int slots = Mathf.Max(GameData.PetStorage, list.Count);
            bool bigGrid = slots <= 16;
            int cols = bigGrid ? 4 : 6;
            float cw = bigGrid ? 206f : 134f, ch = bigGrid ? 114f : 92f, sx = bigGrid ? 214f : 142f, sy = bigGrid ? 122f : 98f, icS = bigGrid ? 76f : 62f;
            for (int i = 0; i < slots; i++)
            {
                int c = i % cols, r = i / cols;
                var cell = Card(content, new Vector2(24 + c * sx, -96 - r * sy), new Vector2(cw, ch), new Color(0.16f, 0.18f, 0.28f, 1f));
                if (i >= list.Count) continue;
                var pd = list[i];
                Color rc = ItemDB.Col(pd.rarity);
                bool sel = pd == selected;
                cell.GetComponent<Image>().color = sel ? Color.Lerp(rc, Color.white, 0.55f) : Color.Lerp(new Color(0.16f, 0.18f, 0.28f), rc, pd.equipped ? 0.45f : 0.22f);
                // banda de raritate jos
                var band = UIKit.Rect(cell, "Band", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(cw - 16f, 5)).gameObject.AddComponent<Image>();
                band.color = rc; band.raycastTarget = false;
                var ic = UIKit.Rect(cell, "Ic", TC, TC, new Vector2(0, -4), new Vector2(icS, icS)).gameObject.AddComponent<Image>();
                var petIc = PetStudio.Icon(pd);   // poza pet-ului (randata din modelul 3D)
                ic.sprite = petIc != null ? petIc : ItemIcons.Get(6); ItemIcons.Art(ic);
                ic.preserveAspect = true;
                ic.color = petIc != null ? Color.white : rc;
                ic.raycastTarget = false;
                Label(cell, GameData.PetName(pd.model), bigGrid ? 17 : 15, sel ? new Color(0.1f, 0.1f, 0.2f) : Color.white, new Vector2(0, -icS - 6f), new Vector2(cw - 6f, 22), TextAnchor.MiddleCenter, TC);
                if (pd.equipped)
                {
                    // echipat: contur verde + un punct verde mic in colt (curat si pe telefon)
                    var ol = cell.gameObject.AddComponent<Outline>();
                    ol.effectColor = new Color(0.45f, 1f, 0.5f, 0.95f); ol.effectDistance = new Vector2(3f, -3f);
                    var ol2 = cell.gameObject.AddComponent<Outline>();
                    ol2.effectColor = new Color(0.45f, 1f, 0.5f, 0.95f); ol2.effectDistance = new Vector2(-3f, 3f);
                    var eq = UIKit.Rect(cell, "Eq", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -8), new Vector2(16, 16));
                    UIKit.Img(eq, "round_Grey", new Color(0.45f, 1f, 0.5f), 8f, 1f).raycastTarget = false;
                }
                var cimg = cell.GetComponent<Image>();
                cimg.raycastTarget = true;
                var b = cell.gameObject.AddComponent<Button>();
                b.targetGraphic = cimg;
                var pp = pd;
                TextHover.Attach(cell.gameObject, () => PetInfo(pp), rc);
                b.onClick.AddListener(() => { selected = selected == pp ? null : pp; releaseT = 0f; Sfx.Play("Click", 0.4f); Rebuild(); });
            }

            // detalii + actiuni
            var side = Card(content, new Vector2(884, -96), new Vector2(332, 488), new Color(0.2f, 0.22f, 0.34f, 1f));
            if (selected == null)
            {
                PetStudio.StopPreview();
                Label(side, "SELECT A PET",
                      18, new Color(1, 1, 1, 0.85f), new Vector2(18, -18), new Vector2(296, 300));
            }
            else
            {
                var s = selected;
                Color rc = ItemDB.Col(s.rarity);
                // pet-ul animat (ca avatarul din INVENTORY) - mai mic
                var stage = UIKit.Rect(side, "Stage", TC, TC, new Vector2(0, -12), new Vector2(240, 170));
                UIKit.Img(stage, "flat_Grey", new Color(rc.r * 0.35f, rc.g * 0.35f, rc.b * 0.35f, 0.55f), 16f, 1f);
                var big = UIKit.Rect(side, "Big", TC, TC, new Vector2(0, -10), new Vector2(170, 170)).gameObject.AddComponent<RawImage>();
                big.texture = PetStudio.Preview(s);
                big.raycastTarget = true;
                TextHover.Attach(big.gameObject, () => PetInfo(s), rc);
                Label(side, GameData.PetName(s.model).ToUpper(), 28, rc, new Vector2(0, -192), new Vector2(300, 40), TextAnchor.MiddleCenter, TC);
                Label(side, ItemDB.RarityNames[s.rarity].ToUpper() + "   " + (s.equipped ? "<color=#ffd84d>EQUIPPED</color>" : "<color=#b8c4d6>IN STORAGE</color>"),
                      17, Color.white, new Vector2(0, -232), new Vector2(300, 26), TextAnchor.MiddleCenter, TC);
                Label(side, "<color=#8890aa>hover for details</color>", 14, Color.white, new Vector2(0, -260), new Vector2(300, 22), TextAnchor.MiddleCenter, TC);
                UIKit.Button(side, "Eq", s.equipped ? "UNEQUIP" : "EQUIP", s.equipped ? "Grey" : "Green", BC, BC, new Vector2(0, 84), new Vector2(280, 56), () => { p.TogglePet(s); Rebuild(); }, 22);
                UIKit.Button(side, "Rel", releaseT > 0f ? "CONFIRM RELEASE?" : "RELEASE", "Red", BC, BC, new Vector2(0, 18), new Vector2(280, 56), () =>
                {
                    if (releaseT <= 0f) { releaseT = 3f; Sfx.Play("Error", 0.4f, 1.3f); Rebuild(); return; }
                    p.ReleasePet(s); selected = null; releaseT = 0f; Rebuild();
                }, 20);
            }
            UIKit.Button(content, "Best", "EQUIP BEST", "Blue", BL, BL, new Vector2(24, 20), new Vector2(300, 60), () =>
            {
                var all = new System.Collections.Generic.List<PetData>(D.pets);
                all.Sort((a, b) => Player.PetPower(b).CompareTo(Player.PetPower(a)));
                foreach (var x in D.pets) x.equipped = false;
                for (int i = 0; i < all.Count && i < p.MaxPets; i++) all[i].equipped = true;
                p.RebuildPets();
                p.SaveNow();
                Sfx.Play("Equip", 0.6f);
                Rebuild();
            }, 20);
            if (!D.passGame)
                UIKit.Button(content, "GP", "GET GAME PASS (4 PETS)", "Yellow", BL, BL, new Vector2(340, 20), new Vector2(360, 60), () => { Close(); game.hud.shop.Open(); }, 18);
        }

        static string PetInfo(PetData pd)
        {
            if (pd == null) return null;
            string rc = ColorUtility.ToHtmlStringRGB(ItemDB.Col(pd.rarity));
            var m = GameData.Maps[Mathf.Clamp(pd.map, 0, GameData.Maps.Length - 1)];
            return "<size=26><color=#" + rc + ">" + GameData.PetName(pd.model) + "</color></size>\n<color=#" + rc + ">" + ItemDB.RarityNames[pd.rarity].ToUpper() + "</color>  <color=#9aa6c0>PET</color>\n\n" +
                   "<color=#7dff8a>+" + Mathf.RoundToInt(Player.PetPower(pd) * 100) + "%</color> damage (when equipped)\n" +
                   "<color=#b8c4d6>Home:</color> " + m.name + "\n" +
                   (pd.equipped ? "\n<color=#ffd84d>EQUIPPED</color>" : "\n<color=#b8c4d6>In storage - press EQUIP</color>");
        }

        public override void Close()
        {
            PetStudio.StopPreview();
            base.Close();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) PetStudio.StopPreview();
            if (releaseT > 0f)
            {
                releaseT -= Time.unscaledDeltaTime;
                if (releaseT <= 0f) Rebuild();
            }
        }
    }

    // ==================================================================
    //  GENERAL SHOP - tokens, coins, shards, pass-uri (mod de TEST)
    // ==================================================================
    public class ShopUI : Window
    {
        public static ShopUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("ShopUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<ShopUI>();
            w.Init(parent, g, "ShopDim", "SHOP", new Vector2(1560, 900), "Green");
            return w;
        }

        void Section(string t, float y, Color c, int icon)
        {
            var ic = UIKit.Rect(content, "SecI", TL, TL, new Vector2(30, y), new Vector2(44, 44)).gameObject.AddComponent<Image>();
            ic.sprite = ItemIcons.Get(icon); ItemIcons.Art(ic); ic.color = c; ic.raycastTarget = false;
            Label(content, t, 34, Color.white, new Vector2(84, y + 2), new Vector2(900, 46));
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            // butonul de test apare doar in editor / build de test (nu in jocul lansat)
            if (Application.isEditor || Debug.isDebugBuild)
                UIKit.Button(content, "Reset", "reset passes (test)", "Grey", TR, TR, new Vector2(-30, -8), new Vector2(220, 44), () =>
                {
                    D.passTeleport = D.passGame = D.passMagnet = D.passAuto = D.passPrivate = false; D.autoAttack = false;
                    p.RebuildPets(); p.SaveNow(); Rebuild();
                }, 15);

            // ---- GAME PASSES: carduri mari si colorate (+ pachetul cu TOATE pass-urile, la pret redus)
            //      plata reala prin Google Play (Shop.cs); in editor / admin = test gratis
            Section("GAME PASSES", -8, new Color(1f, 0.85f, 0.3f), 26);
            PassCard(0, 24, "Blue", "MAGNET", Shop.PassMagnet, D.passMagnet);
            PassCard(1, 25, "Red", "AUTO SWING", Shop.PassAuto, D.passAuto);
            PassCard(2, 8, "Purple", "TELEPORT", Shop.PassTeleport, D.passTeleport);
            PassCard(3, 6, "Green", "+2 PET SLOTS", Shop.PassPets, D.passGame);
            PassCard(4, 21, "Orange", "PRIVATE SERVER", Shop.PassPrivate, D.passPrivate);
            bool all = D.passMagnet && D.passAuto && D.passTeleport && D.passGame && D.passPrivate;
            PassCard(5, 31, "Yellow", "ALL PASSES", Shop.PassAll, all, "SAVE 44%");

            // ---- TOKENS (stanga) + SPATIU IN INVENTAR (dreapta)
            Section("TOKENS", -420, new Color(0.55f, 0.95f, 0.75f), 19);
            var sec2 = UIKit.Rect(content, "SecI2", TL, TL, new Vector2(786, -420), new Vector2(44, 44)).gameObject.AddComponent<Image>();
            sec2.sprite = ItemIcons.Get(10); ItemIcons.Art(sec2); sec2.raycastTarget = false;
            Label(content, "INVENTORY SPACE", 34, Color.white, new Vector2(840, -418), new Vector2(600, 46));
            for (int i = 0; i < 3; i++)
                Pack(new Vector2(26 + i * 252, -472), new Vector2(240, 150), 19, "Green", Fmt.Num(Shop.TokenAmounts[i]), Shop.Tokens[i], false);
            int[] bagAdd = { 20, 40, 100 };
            bool[] bagOwned = { D.bag20, D.bag40, D.bag100 };
            for (int i = 0; i < 3; i++)
                Pack(new Vector2(782 + i * 252, -472), new Vector2(240, 150), 10, "Blue", "+" + bagAdd[i] + " SLOTS", Shop.Bags[i], bagOwned[i]);

            // ---- COINS + SHARDS
            Section("COINS & SHARDS", -640, new Color(1f, 0.85f, 0.3f), 4);
            for (int i = 0; i < 3; i++)
            {
                Pack(new Vector2(26 + i * 252, -692), new Vector2(240, 140), 4, "Yellow", Fmt.Num(GameData.CoinPack(D.level, i)), Shop.Coins[i], false);   // cresc cu harta ta
                Pack(new Vector2(782 + i * 252, -692), new Vector2(240, 140), 9, "Blue", Shop.ShardAmounts[i].ToString(), Shop.Shards[i], false);
            }
        }

        /// <summary>Card mare de pass (stil "simulator"): fundal colorat, iconita mare, titlu, pret sau OWNED.</summary>
        void PassCard(int i, int icon, string colorName, string title, string productId, bool owned, string badge = "")
        {
            var card = UIKit.Rect(content, "Pass" + i, TL, TL, new Vector2(26 + i * 249, -62), new Vector2(240, 340));
            UIKit.Img(card, "sq_" + colorName, Color.white, 22f, 1f);
            var glow = UIKit.Rect(card, "Glow", TC, C, new Vector2(0, -100), new Vector2(150, 150));
            UIKit.Img(glow, "round_Grey", new Color(1f, 1f, 1f, 0.18f), 16f, 1f);
            var ic = UIKit.Rect(card, "Ic", TC, C, new Vector2(0, -100), new Vector2(120, 120)).gameObject.AddComponent<Image>();
            ic.sprite = ItemIcons.Get(icon); ItemIcons.Art(ic); ic.color = Color.white; ic.raycastTarget = false;
            var sh = ic.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.5f); sh.effectDistance = new Vector2(3, -5);
            // titlul pe un singur rand (se micsoreaza singur), descrierea in caseta ei - nimic nu se suprapune
            var tl = Label(card, title, 32, Color.white, new Vector2(0, -190), new Vector2(222, 48), TextAnchor.MiddleCenter, TC);
            if (badge.Length > 0 && !owned)
            {
                var bd = UIKit.Rect(card, "Badge", TR, C, new Vector2(-34, -26), new Vector2(120, 40));
                UIKit.Img(bd, "flat_Grey", new Color(0.9f, 0.15f, 0.2f, 1f), 12f, 1f);
                UIKit.Text(bd, "T", badge, 18, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 1), new Vector2(120, 40), true, true);
                bd.localRotation = Quaternion.Euler(0, 0, -12);
            }
            UIKit.Fit(tl, 18);
            // fara descrieri aici - ce face fiecare pass e explicat in GUIDE (butonul "?" de sus)
            if (owned)
            {
                var ob = UIKit.Rect(card, "Owned", BC, BC, new Vector2(0, 14), new Vector2(216, 58));
                UIKit.Img(ob, "flat_Grey", new Color(0f, 0f, 0f, 0.45f), 16f, 1f);
                UIKit.Text(ob, "T", "OWNED", 34, new Color(0.55f, 1f, 0.55f), TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(216, 58));
            }
            else UIKit.Button(card, "Buy", Shop.Price(productId), "Green", BC, BC, new Vector2(0, 12), new Vector2(216, 62), () => Shop.Buy(productId), 26);
        }

        /// <summary>Pachet mic: iconita, titlu, pret (sau OWNED pentru cele cumparate o singura data).</summary>
        void Pack(Vector2 pos, Vector2 size, int icon, string colorName, string title, string productId, bool owned)
        {
            var card = UIKit.Rect(content, "Pack", TL, TL, pos, size);
            UIKit.Img(card, "sq_" + colorName, Color.white, 20f, 1f);
            var ic = UIKit.Rect(card, "Ic", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 18), new Vector2(70, 70)).gameObject.AddComponent<Image>();
            ic.sprite = ItemIcons.Get(icon); ItemIcons.Art(ic); ic.color = Color.white; ic.raycastTarget = false;
            UIKit.Fit(Label(card, title, 26, Color.white, new Vector2(88, -18), new Vector2(size.x - 96, 40)), 14);
            if (owned)
            {
                var ob = UIKit.Rect(card, "Owned", BC, BC, new Vector2(0, 10), new Vector2(size.x - 24, 46));
                UIKit.Img(ob, "flat_Grey", new Color(0f, 0f, 0f, 0.45f), 16f, 1f);
                UIKit.Text(ob, "T", "OWNED", 24, new Color(0.55f, 1f, 0.55f), TextAnchor.MiddleCenter, C, C, new Vector2(0, 1), new Vector2(size.x - 24, 46));
                return;
            }
            UIKit.Button(card, "Buy", Shop.Price(productId), "Green", BC, BC, new Vector2(0, 10), new Vector2(size.x - 24, 46), () => Shop.Buy(productId), 22);
        }
    }
}
