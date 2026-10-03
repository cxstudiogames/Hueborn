// =====================================================================
//  InventoryUI.cs - INVENTORY (tasta I / Tab / butonul de pe HUD)
//   * stanga: avatarul 3D cu tot echipamentul pe el, cele 4 sloturi
//     (arma, coif, armura, inel), POWER-ul, statisticile si materialele
//   * dreapta: grila cu iteme + EQUIP BEST / EQUIP / FAVORITE / SELL
//   * statisticile unui item apar cand tii mouse-ul pe el (ItemTooltip)
//   * FAVORITE (stea) = itemul nu se vinde din greseala
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class InventoryUI : MonoBehaviour
    {
        // grila: 6 iteme pe rand, se deruleaza (rotita, tragi cu degetul / mouse-ul, bara din dreapta, sagetile)
        const int Cols = 6, MinRows = 4;
        const float Cell = 140f, Gap = 10f;
        RectTransform gridContent;
        ScrollRect gridScroll;
        Game game;
        RectTransform dim;
        Text title, powerText, powerTitle, statsL, statsR, bagCount, summary;
        RawImage avatar;
        readonly Image[] eqIcons = new Image[4];
        readonly Image[] eqFrames = new Image[4];
        readonly Text[] eqPlus = new Text[4];
        int matSel = -1;   // materialul selectat in sectiunea MATERIALS
        const int PageOff = 0;   // (fara pagini - grila se deruleaza)
        readonly List<Image> cellFrames = new List<Image>();
        readonly List<Image> cellIcons = new List<Image>();
        readonly List<Text> cellPlus = new List<Text>();
        readonly List<GameObject> cellFavs = new List<GameObject>();
        readonly List<GameObject> cellChecks = new List<GameObject>();
        readonly List<Image> filterBtns = new List<Image>();
        Button btnSell, btnEquip, btnFav, btnMode;
        Text btnSellText, btnEquipText, btnFavText, btnModeText;

        // click normal = UN singur item (cur). Multi-select doar in modul SELL.
        readonly HashSet<Item> sel = new HashSet<Item>();
        Item cur;
        bool sellMode;
        Item eqSelected;
        readonly List<Item> view = new List<Item>();
        int filter = -1;
        float confirmT;

        static readonly Vector2 C = new Vector2(0.5f, 0.5f), TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0), TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0);

        public bool IsOpen { get { return dim != null && dim.gameObject.activeSelf; } }

        public static InventoryUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("InventoryUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<InventoryUI>();
            ui.game = g;
            ui.Build(parent);
            return ui;
        }

        RectTransform Card(Transform p, Vector2 pos, Vector2 size, Color c)
        {
            var rt = UIKit.Rect(p, "Card", TL, TL, pos, size);
            UIKit.Img(rt, "card_dark", UIKit.Theme(c), 18f, 1f);
            return rt;
        }

        Text T(Transform p, string s, int size, Color c, TextAnchor a, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var t = UIKit.Text(p, "T", s, size, c, a, anchor, anchor, pos, box, true, true);
            t.supportRichText = true;
            return t;
        }

        void Build(RectTransform parent)
        {
            RectTransform d;
            var content = UIKit.Window(parent, "InvDim", "INVENTORY", new Vector2(1640, 900), "Blue", Close, out d, out title);
            dim = d;

            // ================= stanga: avatar + echipament + statistici =================
            var left = Card(content, new Vector2(24, -14), new Vector2(580, 812), new Color(0.2f, 0.2f, 0.22f));
            // avatarul live (mai mic, pe un "podium" ca la PETS)
            var avBg = UIKit.Rect(left, "AvatarBg", TC, TC, new Vector2(0, -40), new Vector2(240, 270));
            UIKit.Img(avBg, "flat_Grey", new Color(0f, 0f, 0f, 0.28f), 16f, 1f).raycastTarget = false;
            var av = UIKit.Rect(left, "Avatar", TC, TC, new Vector2(0, -44), new Vector2(262, 262));
            avatar = av.gameObject.AddComponent<RawImage>();
            avatar.raycastTarget = false;
            // sloturile in jurul avatarului: arma + inel in stanga, coif + armura in dreapta
            Vector2[] slotPos = { new Vector2(22, -40), new Vector2(-22, -40), new Vector2(-22, -190), new Vector2(22, -190) };
            for (int s = 0; s < 4; s++)
            {
                int slot = s;
                bool rightSide = s == 1 || s == 2;
                var an = rightSide ? TR : TL;
                var rt = UIKit.Rect(left, "Eq" + s, an, an, slotPos[s], new Vector2(118, 118));
                eqFrames[s] = UIKit.Img(rt, "sq_Grey", Color.white, 16f, 1.2f);
                eqFrames[s].raycastTarget = true;
                var b = rt.gameObject.AddComponent<Button>();
                b.targetGraphic = eqFrames[s];
                b.onClick.AddListener(() => SelectEquipped((Slot)slot));
                ItemHover.Attach(rt.gameObject, () => game.player.Equipped((Slot)slot));
                var ir = UIKit.Rect(rt, "Icon", C, C, new Vector2(0, 6), new Vector2(80, 80));
                eqIcons[s] = ir.gameObject.AddComponent<Image>();
                eqIcons[s].raycastTarget = false;
                eqPlus[s] = UIKit.Text(rt, "P", "", 24, Color.white, TextAnchor.LowerRight, BR, BR, new Vector2(-6, 2), new Vector2(90, 32), true, true);
                UIKit.Text(rt, "S", ItemDB.SlotNames[s].ToUpper(), 15, new Color(1, 1, 1, 0.7f), TextAnchor.UpperCenter, TC, TC, new Vector2(0, 16), new Vector2(118, 20), true, true);
            }

            // POWER
            var pw = UIKit.Rect(left, "Power", TC, TC, new Vector2(0, -350), new Vector2(540, 70));
            UIKit.Img(pw, "flat_Grey", new Color(0f, 0f, 0f, 0.35f), 16f, 1f);
            var fl = UIKit.Rect(pw, "Flame", new Vector2(0, 0.5f), C, new Vector2(46, 2), new Vector2(56, 56)).gameObject.AddComponent<Image>();
            fl.sprite = ItemIcons.Get(22); ItemIcons.Art(fl); fl.color = new Color(1f, 0.55f, 0.15f); fl.raycastTarget = false;
            powerText = T(pw, "", 46, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(84, 2), new Vector2(260, 66));
            powerText.rectTransform.pivot = new Vector2(0, 0.5f);
            // titlul: buton -> fereastra TITLES (titluri blocate / deblocate pe categorii)
            var tb = UIKit.Rect(pw, "TitleBtn", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(250, 58));
            var tbi = UIKit.Img(tb, "btn_Grey", Color.white, 18f, 1f);
            tbi.raycastTarget = true;
            var tbb = tb.gameObject.AddComponent<Button>();
            tbb.targetGraphic = tbi;
            tbb.onClick.AddListener(() => { Sfx.Play("Click", 0.5f); game.hud.titles.Open(); });
            powerTitle = UIKit.Text(tb, "T", "", 24, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-12, 3), new Vector2(210, 50), true, true);
            UIKit.Fit(powerTitle, 12);
            UIKit.Text(tb, "Arr", "v", 20, new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 3), new Vector2(24, 40), true, true);
            

            // statistici (doua coloane)
            statsL = T(left, "", 22, Color.white, TextAnchor.UpperLeft, TL, new Vector2(30, -436), new Vector2(260, 170));
            statsL.rectTransform.pivot = TL;
            statsR = T(left, "", 22, Color.white, TextAnchor.UpperLeft, TL, new Vector2(300, -436), new Vector2(260, 170));
            statsR.rectTransform.pivot = TL;

            UIKit.Button(left, "Best", "EQUIP BEST", "Blue", BC, BC, new Vector2(0, 16), new Vector2(532, 62), () => { game.player.EquipBest(); ClearSel(); Refresh(); }, 28);

            // ================= dreapta: filtre + grila + actiuni =================
            var right = Card(content, new Vector2(620, -14), new Vector2(996, 812), new Color(0.2f, 0.2f, 0.22f));
            string[] fn = { "ALL", "WEAPONS", "HELMETS", "ARMOR", "RINGS", "MATERIALS" };
            for (int i = 0; i < fn.Length; i++)
            {
                int f = i - 1;
                var fb = UIKit.Button(right, "F" + i, fn[i], "Grey", TL, TL, new Vector2(14 + i * 122, -14), new Vector2(118, 56), () => { filter = f; ClearSel(); Refresh(); if (gridScroll != null) gridScroll.verticalNormalizedPosition = 1f; }, 19);
                filterBtns.Add(fb.GetComponent<Image>());
            }
            bagCount = T(right, "", 22, Color.white, TextAnchor.MiddleCenter, TR, new Vector2(-150, -42), new Vector2(140, 50));
            bagCount.supportRichText = true;
            // ---- grila derulanta
            var vp = UIKit.Rect(right, "Grid", TL, TL, new Vector2(16, -84), new Vector2(Cols * (Cell + Gap) - Gap, 4 * (Cell + Gap) - Gap));
            var vpi = vp.gameObject.AddComponent<Image>(); vpi.color = new Color(0, 0, 0, 0.001f);
            vp.gameObject.AddComponent<RectMask2D>();
            gridContent = UIKit.Rect(vp, "Content", TL, TL, Vector2.zero, new Vector2(vp.sizeDelta.x, 10f));
            gridScroll = vp.gameObject.AddComponent<ScrollRect>();
            gridScroll.content = gridContent; gridScroll.viewport = vp;
            gridScroll.horizontal = false; gridScroll.vertical = true;
            gridScroll.movementType = ScrollRect.MovementType.Elastic; gridScroll.elasticity = 0.08f;
            gridScroll.inertia = true; gridScroll.decelerationRate = 0.12f;
            gridScroll.scrollSensitivity = 45f;
            // bara din dreapta (se poate trage) + sageti sus / jos
            float bx = 16 + Cols * (Cell + Gap) + 6;
            UIKit.Button(right, "Up", "^", "Grey", TL, TL, new Vector2(bx, -84), new Vector2(56, 56), () => ScrollRows(-1), 26);
            UIKit.Button(right, "Down", "v", "Grey", TL, TL, new Vector2(bx, -84 - 4 * (Cell + Gap) + Gap + 56), new Vector2(56, 56), () => ScrollRows(1), 22);
            var track = UIKit.Rect(right, "Track", TL, TL, new Vector2(bx + 12, -146), new Vector2(32, 4 * (Cell + Gap) - Gap - 124));
            UIKit.Img(track, "flat_Grey", new Color(0.08f, 0.09f, 0.12f, 0.6f), 8f, 2f);
            var sb = track.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            var area = UIKit.Stretch(track, "Area", 2f);
            var handle = UIKit.Rect(area, "Handle", C, C, Vector2.zero, Vector2.zero);
            handle.anchorMin = new Vector2(0, 0); handle.anchorMax = new Vector2(1, 1); handle.offsetMin = handle.offsetMax = Vector2.zero;
            var hImg = UIKit.Img(handle, "flat_Grey", new Color(1f, 0.85f, 0.4f, 0.9f), 8f, 2f);
            sb.handleRect = handle; sb.targetGraphic = hImg;
            gridScroll.verticalScrollbar = sb;
            gridScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            EnsureCells(MinRows * Cols);

            summary = T(right, "", 20, Color.white, TextAnchor.MiddleLeft, TL, new Vector2(20, -690), new Vector2(956, 36));
            summary.rectTransform.pivot = TL;
            UIKit.Fit(summary, 12);
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            // bara de actiuni (compacta). Normal: EQUIP / FAVORITE / SELL...
            // Mod SELL: CANCEL / SELECT ALL / SELECT JUNK / SELL N
            btnMode = UIKit.Button(right, "Mode", "CANCEL", "Grey", BL, BL, new Vector2(16, 14), new Vector2(210, 64), () => { sellMode = false; sel.Clear(); confirmT = 0f; Sfx.Play("Click", 0.4f); Refresh(); }, 22);
            btnModeText = btnMode.GetComponentInChildren<Text>();
            btnEquip = UIKit.Button(right, "Equip", "EQUIP", "Green", BL, BL, new Vector2(236, 14), new Vector2(220, 64), EquipSelected, 26);
            btnEquipText = btnEquip.GetComponentInChildren<Text>();
            btnFav = UIKit.Button(right, "Fav", "FAVORITE", "Yellow", BL, BL, new Vector2(466, 14), new Vector2(220, 64), FavSelected, 24);
            btnFavText = btnFav.GetComponentInChildren<Text>();
            btnSell = UIKit.Button(right, "Sell", "SELL", "Red", BL, BL, new Vector2(696, 14), new Vector2(284, 64), SellSelected, 24);
            btnSellText = btnSell.GetComponentInChildren<Text>();

            dim.gameObject.SetActive(false);
        }

        /// <summary>Sare cu un rand in sus (-1) sau in jos (+1).</summary>
        void ScrollRows(int dir)
        {
            if (gridScroll == null) return;
            float range = gridContent.rect.height - ((RectTransform)gridScroll.viewport).rect.height;
            if (range <= 1f) return;
            float step = (Cell + Gap) / range;
            gridScroll.StopMovement();
            gridScroll.verticalNormalizedPosition = Mathf.Clamp01(gridScroll.verticalNormalizedPosition - dir * step);
            Sfx.Play("Click", 0.3f);
        }

        /// <summary>Destule celule in grila (randuri intregi, minim 4 randuri) - se creeaza doar cand ai nevoie.</summary>
        void EnsureCells(int n)
        {
            int rows = Mathf.Max(MinRows, (n + Cols - 1) / Cols);
            int need = rows * Cols;
            while (cellFrames.Count < need) MakeCell(cellFrames.Count);
            for (int i = 0; i < cellFrames.Count; i++) cellFrames[i].transform.gameObject.SetActive(i < need);
            gridContent.sizeDelta = new Vector2(gridContent.sizeDelta.x, rows * (Cell + Gap) - Gap);
        }

        void MakeCell(int idx)
        {
            int r = idx / Cols, c = idx % Cols;
            var rt = UIKit.Rect(gridContent, "C" + idx, TL, TL, new Vector2(c * (Cell + Gap), -r * (Cell + Gap)), new Vector2(Cell, Cell));
            var fr = UIKit.Img(rt, "sq_Grey", new Color(0.35f, 0.38f, 0.5f, 1f), 16f, 1.2f);
            fr.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = fr;
            b.onClick.AddListener(() => ClickCell(idx));
            ItemHover.Attach(rt.gameObject, () => filter != MatFilter && PageOff + idx < view.Count ? view[PageOff + idx] : null);
            TextHover.Attach(rt.gameObject, () => filter == MatFilter && idx < Mats.Length ? MatTip(idx) : null, new Color(1f, 0.85f, 0.45f));
            var ir = UIKit.Rect(rt, "Icon", C, C, new Vector2(0, 5), new Vector2(Cell * 0.68f, Cell * 0.68f));
            var im = ir.gameObject.AddComponent<Image>();
            im.raycastTarget = false;
            var pl = UIKit.Text(rt, "Plus", "", 24, Color.white, TextAnchor.LowerRight, BR, BR, new Vector2(-6, 2), new Vector2(90, 32), true, true);
            // stea = favorit (nu se vinde)
            var fv = UIKit.Rect(rt, "Fav", TL, C, new Vector2(20, -20), new Vector2(32, 32));
            var fvi = fv.gameObject.AddComponent<Image>();
            fvi.sprite = ItemIcons.Get(26); ItemIcons.Art(fvi); fvi.color = new Color(1f, 0.85f, 0.25f); fvi.raycastTarget = false;
            // bifa (selectat)
            var ck = UIKit.Rect(rt, "Check", TR, C, new Vector2(-16, -16), new Vector2(34, 34));
            UIKit.Img(ck, "round_Green", Color.white, 12f, 1f).raycastTarget = false;
            cellFrames.Add(fr); cellIcons.Add(im); cellPlus.Add(pl); cellFavs.Add(fv.gameObject); cellChecks.Add(ck.gameObject);
        }

        // ---------------------------------------------------------------
        public void Open(bool shop)
        {
            dim.gameObject.SetActive(true);
            dim.SetAsLastSibling();
            title.text = "INVENTORY";
            avatar.texture = game.portrait != null ? game.portrait.Texture : null;
            ClearSel();
            Refresh();
            Sfx.Play("Open", 0.6f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            dim.gameObject.SetActive(false);
            ItemTooltip.Hide();
            Sfx.Play("Close", 0.6f);
            game.player.SaveNow();
        }

        void ClearSel() { sel.Clear(); cur = null; sellMode = false; eqSelected = null; confirmT = 0f; matSel = -1; }

        void ClickCell(int idx)
        {
            eqSelected = null;
            confirmT = 0f;
            if (filter == MatFilter)
            {
                matSel = idx < Mats.Length && matSel != idx ? idx : -1;
                Sfx.Play("Click", 0.4f);
                Refresh();
                return;
            }
            if (PageOff + idx < view.Count)
            {
                var it = view[PageOff + idx];
                if (sellMode)
                {
                    if (it.locked) { game.Say("Favorite items can't be sold.", 1.2f); Sfx.Play("Error", 0.4f); return; }
                    if (!sel.Remove(it)) sel.Add(it);
                }
                else cur = cur == it ? null : it;
                Sfx.Play("Click", 0.4f);
            }
            Refresh();
        }

        void SelectEquipped(Slot s)
        {
            var it = game.player.Equipped(s);
            sel.Clear(); cur = null; sellMode = false;
            eqSelected = eqSelected == it ? null : it;
            confirmT = 0f;
            Sfx.Play("Click", 0.4f);
            Refresh();
        }

        void SelectAll()
        {
            eqSelected = null; confirmT = 0f;
            bool all = true;
            foreach (var it in view) if (!it.locked && !sel.Contains(it)) all = false;
            sel.Clear();
            if (!all) foreach (var it in view) if (!it.locked) sel.Add(it);
            Sfx.Play("Click", 0.4f);
            Refresh();
        }

        void SelectJunk()
        {
            eqSelected = null; confirmT = 0f;
            sel.Clear();
            foreach (var it in view) if (!it.locked && it.rarity <= 1) sel.Add(it);
            if (sel.Count == 0) { game.Say("No Common items to select.", 1.5f); Sfx.Play("Error", 0.4f); }
            Refresh();
        }

        List<Item> Sellable()
        {
            var p = game.player;
            var l = new List<Item>();
            foreach (var it in sel) if (!it.locked && !p.IsEquipped(it)) l.Add(it);
            return l;
        }

        void SellSelected()
        {
            if (!sellMode)
            {
                // intram in modul SELL (itemul selectat e deja bifat)
                sellMode = true; eqSelected = null; confirmT = 0f;
                sel.Clear();
                if (cur != null && !cur.locked) sel.Add(cur);
                cur = null;
                Sfx.Play("Click", 0.5f);
                Refresh();
                return;
            }
            var l = Sellable();
            if (l.Count == 0) { Sfx.Play("Error", 0.5f); return; }
            bool precious = false;
            foreach (var it in l) if (it.rarity >= 3) precious = true;
            if (precious && confirmT <= 0f)
            {
                confirmT = 3f;
                Sfx.Play("Error", 0.35f, 1.3f);
                Refresh();
                return;
            }
            game.player.SellMany(l);
            ClearSel();
            ItemTooltip.Hide();
            Refresh();
        }

        void EquipSelected()
        {
            var p = game.player;
            if (sellMode) { SelectAll(); return; }
            if (filter == MatFilter)
            {
                if (matSel >= 0 && Mats[matSel].potion >= 0) p.UsePotion(Mats[matSel].potion);
                Refresh();
                return;
            }
            if (eqSelected != null)
            {
                p.Unequip(eqSelected.S);
                eqSelected = null;
            }
            else
            {
                if (cur == null) return;
                p.Equip(cur);
                cur = null;
            }
            Refresh();
        }

        void FavSelected()
        {
            if (sellMode) { SelectJunk(); return; }
            var it = eqSelected != null ? eqSelected : cur;
            if (it == null) return;
            it.locked = !it.locked;      // apasa din nou = anuleaza
            Sfx.Play("Click", 0.5f);
            game.player.SaveNow();
            Refresh();
        }

        // ---------------------------------------------------------------
        public void Refresh()
        {
            var p = game.player; var D = p.D;
            for (int s = 0; s < 4; s++)
            {
                var it = p.Equipped((Slot)s);
                eqIcons[s].color = it == null ? new Color(1, 1, 1, 0.18f) : ItemDB.Col(it.rarity);
                eqIcons[s].sprite = it == null ? ItemIcons.Get(s) : ItemIcons.For(it); ItemIcons.Art(eqIcons[s]);
                eqFrames[s].color = it != null && it == eqSelected ? Color.white : it == null ? new Color(0.45f, 0.45f, 0.5f) : Color.Lerp(new Color(0.4f, 0.4f, 0.45f), ItemDB.Col(it.rarity), 0.55f);
                eqPlus[s].text = it != null && it.plus > 0 ? "+" + it.plus : "";
            }
            long pow = p.Power;
            powerText.text = Fmt.Num(pow);
            string tn; Color tc; int tt;
            TitleDB.Current(p, out tn, out tc, out tt);
            TitleStyle.Apply(powerTitle, tn, tc, tt, 24);
            powerTitle.resizeTextMaxSize = powerTitle.fontSize;
            statsL.text = "<color=#ff9a7a>DAMAGE</color>  " + Fmt.Num(p.Damage) +
                          "\n<color=#7dff8a>HEALTH</color>  " + Fmt.Num(p.MaxHp) +
                          "\n<color=#7fd8ff>DEFENSE</color>  " + Mathf.RoundToInt(p.Defense * 100) + "%" +
                          "\n<color=#ffd84d>CRIT</color>  " + Mathf.RoundToInt(p.CritChance * 100) + "%";
            statsR.text = "<color=#ffd84d>COINS</color>  x" + p.CoinMul.ToString("0.00") +
                          "\n<color=#ff9ae6>PETS</color>  +" + Mathf.RoundToInt(p.PetBonus * 100) + "%" +
                          "\n<color=#c8b0ff>CLASS</color>  " + p.Class.name +
                          "\n<color=#b8c4d6>LEVEL</color>  " + D.level + (D.rebirth > 0 ? "  <color=#ff8ae0>R" + D.rebirth + "</color>" : "");
            for (int i = 0; i < filterBtns.Count; i++)
                filterBtns[i].sprite = UIKit.Sprite(i - 1 == filter ? "btn_Yellow" : "btn_Grey", 18f);

            if (filter == MatFilter) { RefreshMaterials(p); return; }
            view.Clear();
            foreach (var it in D.items)
                if (!p.IsEquipped(it) && (filter < 0 || it.slot == filter)) view.Add(it);
            view.Sort((a, b) => b.rarity != a.rarity ? b.rarity.CompareTo(a.rarity) : Player.Score(b).CompareTo(Player.Score(a)));
            sel.RemoveWhere(it => !D.items.Contains(it) || p.IsEquipped(it) || it.locked);
            if (cur != null && (!D.items.Contains(cur) || p.IsEquipped(cur))) cur = null;
            EnsureCells(view.Count);
            bagCount.text = p.BagCount + " / " + p.BagSize;
            for (int i = 0; i < cellFrames.Count; i++)
            {
                if (PageOff + i < view.Count)
                {
                    var it = view[PageOff + i];
                    Color rc = ItemDB.Col(it.rarity);
                    bool on = sellMode ? sel.Contains(it) : it == cur;
                    cellFrames[i].color = on ? Color.white : sellMode && it.locked ? new Color(0.3f, 0.3f, 0.34f, 0.7f) : Color.Lerp(new Color(0.4f, 0.4f, 0.45f), rc, 0.6f);
                    cellIcons[i].enabled = true;
                    cellIcons[i].sprite = ItemIcons.For(it); ItemIcons.Art(cellIcons[i]);
                    cellIcons[i].color = rc;
                    cellPlus[i].text = it.plus > 0 ? "+" + it.plus : "";
                    cellFavs[i].SetActive(it.locked);
                    cellChecks[i].SetActive(sellMode && on);
                }
                else
                {
                    cellFrames[i].color = new Color(0.4f, 0.4f, 0.45f, 0.55f);
                    cellIcons[i].enabled = false;
                    cellPlus[i].text = "";
                    cellFavs[i].SetActive(false);
                    cellChecks[i].SetActive(false);
                }
            }

            var sellable = Sellable();
            long total = 0;
            foreach (var it in sellable) total += ItemDB.SellValue(it);
            btnMode.gameObject.SetActive(sellMode);
            if (sellMode)
            {
                bool all = view.Count > 0;
                foreach (var it in view) if (!it.locked && !sel.Contains(it)) all = false;
                btnEquipText.text = all ? "UNSELECT ALL" : "SELECT ALL";
                btnEquip.GetComponent<Image>().sprite = UIKit.Sprite("btn_Blue", 18f);
                btnEquip.interactable = view.Count > 0;
                btnFavText.text = "SELECT JUNK";
                btnFav.GetComponent<Image>().sprite = UIKit.Sprite("btn_Grey", 18f);
                btnFav.interactable = view.Count > 0;
                btnSellText.text = confirmT > 0f ? "CONFIRM?" : sellable.Count > 0 ? "SELL " + sellable.Count + " (+" + Fmt.Num(total) + ")" : "SELL 0";
                btnSell.GetComponent<Image>().sprite = UIKit.Sprite(sellable.Count > 0 ? "btn_Red" : "btn_Grey", 18f);
                btnSell.interactable = sellable.Count > 0;
                summary.text = "<color=#ff9a7a>SELL MODE</color>  -  click items to tick them   |   " + sel.Count + " selected   |   <color=#ffd84d>+" + Fmt.Num(total) + " coins</color>" +
                               (confirmT > 0f ? "   <color=#ff7777>Epic+ selected - click SELL again!</color>" : "");
                return;
            }
            btnEquip.GetComponent<Image>().sprite = UIKit.Sprite("btn_Green", 18f);
            btnFav.GetComponent<Image>().sprite = UIKit.Sprite("btn_Yellow", 18f);
            btnSell.GetComponent<Image>().sprite = UIKit.Sprite(view.Count > 0 ? "btn_Red" : "btn_Grey", 18f);
            btnSell.interactable = view.Count > 0 && eqSelected == null;
            btnSellText.text = "SELL...";
            var fit = eqSelected != null ? eqSelected : cur;
            btnFavText.text = fit != null && fit.locked ? "UNFAVORITE" : "FAVORITE";
            btnFav.interactable = fit != null;

            if (eqSelected != null)
            {
                btnEquip.interactable = true;
                btnEquipText.text = "UNEQUIP";
                summary.text = "<color=#" + ColorUtility.ToHtmlStringRGB(ItemDB.Col(eqSelected.rarity)) + ">" + ItemDB.FullName(eqSelected) + "</color>  is equipped.";
                return;
            }
            btnEquip.interactable = cur != null;
            btnEquipText.text = "EQUIP";
            if (view.Count == 0)
                summary.text = filter < 0 ? "Your inventory is empty - slay monsters for loot!" : "No items of this type.";
            else if (cur == null)
                summary.text = "<color=#8890aa>Click an item to select it. SELL... lets you pick many at once.</color>";
            else
                summary.text = "<color=#" + ColorUtility.ToHtmlStringRGB(ItemDB.Col(cur.rarity)) + ">" + ItemDB.FullName(cur) + "</color>" +
                               "   <color=#ffd84d>" + Fmt.Num(ItemDB.SellValue(cur)) + " coins</color>" + (cur.locked ? "   <color=#ffcc55>FAVORITE</color>" : "");
        }

        // ---------------------------------------------------------------
        //  Sectiunea MATERIALS: materiale, potiuni si monede, ca obiecte in grila
        // ---------------------------------------------------------------
        const int MatFilter = 4;

        struct MatDef { public string name, desc; public int icon, potion; public Color col; }

        static readonly MatDef[] Mats =
        {
            new MatDef { name = "Ore", icon = 5, potion = -1, col = new Color(0.65f, 0.75f, 0.95f), desc = "Used by the FORGE. Drops from monsters." },
            new MatDef { name = "Forge Stone", icon = 16, potion = -1, col = new Color(1f, 0.6f, 0.25f), desc = "Needed for forging from +2. Drops from monsters, bosses and quests." },
            new MatDef { name = "Guardian Cloak", icon = 17, potion = -1, col = new Color(0.55f, 0.75f, 1f), desc = "With PROTECT on, a failed forge uses a cloak instead of dropping the item level." },
            new MatDef { name = "XP Potion", icon = 36, potion = 0, col = new Color(0.4f, 0.9f, 1f), desc = "XP x2 for 5 minutes. Click USE to drink it." },
            new MatDef { name = "Damage Potion", icon = 37, potion = 1, col = new Color(1f, 0.42f, 0.36f), desc = "Damage x1.5 for 5 minutes. Click USE to drink it." },
            new MatDef { name = "Coin Potion", icon = 38, potion = 2, col = new Color(1f, 0.85f, 0.25f), desc = "Coins x1.5 for 5 minutes. Click USE to drink it." },
            new MatDef { name = "Shards", icon = 9, potion = -1, col = new Color(0.6f, 0.85f, 1f), desc = "Rare currency: forge from +10, eggs and awakening." },
            new MatDef { name = "Tokens", icon = 19, potion = -1, col = new Color(0.55f, 0.95f, 0.75f), desc = "Trading currency - used in the Trade Plaza." },
        };

        long MatCount(int i)
        {
            var D = game.player.D;
            switch (i)
            {
                case 0: return D.ore;
                case 1: return D.forgeStones;
                case 2: return D.cloaks;
                case 3: case 4: case 5: return D.potions[i - 3];
                case 6: return D.shards;
                default: return D.tokens;
            }
        }

        string MatTip(int i)
        {
            var m = Mats[i];
            string t = "<size=26><color=#" + ColorUtility.ToHtmlStringRGB(m.col) + ">" + m.name + "</color></size>\n" +
                       "<color=#9aa6c0>You have " + Fmt.Num(MatCount(i)) + "</color>\n\n" + m.desc;
            if (m.potion >= 0 && game.player.D.buffs[m.potion] > 0f)
                t += "\n<color=#7dff8a>Active: " + Mathf.CeilToInt(game.player.D.buffs[m.potion] / 60f) + " min left</color>";
            return t;
        }

        void RefreshMaterials(Player p)
        {
            view.Clear();
            sel.Clear();
            bagCount.text = "";
            EnsureCells(Mats.Length);
            for (int i = 0; i < cellFrames.Count; i++)
            {
                cellFavs[i].SetActive(false);
                cellChecks[i].SetActive(false);
                if (i < Mats.Length)
                {
                    var m = Mats[i];
                    long n = MatCount(i);
                    cellFrames[i].color = matSel == i ? Color.white : Color.Lerp(new Color(0.4f, 0.4f, 0.45f), m.col, n > 0 ? 0.45f : 0.12f);
                    cellIcons[i].enabled = true;
                    cellIcons[i].sprite = ItemIcons.Get(m.icon); ItemIcons.Art(cellIcons[i]);
                    cellIcons[i].color = n > 0 ? Color.white : new Color(0.5f, 0.5f, 0.55f, 0.6f);
                    cellPlus[i].text = "x" + Fmt.Num(n);
                }
                else
                {
                    cellFrames[i].color = new Color(0.4f, 0.4f, 0.45f, 0.55f);
                    cellIcons[i].enabled = false;
                    cellPlus[i].text = "";
                }
            }
            btnMode.gameObject.SetActive(false);
            btnSellText.text = "SELL...";
            btnSell.GetComponent<Image>().sprite = UIKit.Sprite("btn_Grey", 18f);
            btnSell.interactable = false;
            btnEquip.GetComponent<Image>().sprite = UIKit.Sprite("btn_Green", 18f);
            btnFav.GetComponent<Image>().sprite = UIKit.Sprite("btn_Grey", 18f);
            btnFav.interactable = false;
            btnFavText.text = "FAVORITE";
            bool potion = matSel >= 0 && Mats[matSel].potion >= 0;
            btnEquipText.text = potion ? "USE" : "EQUIP";
            btnEquip.interactable = potion && MatCount(matSel) > 0;
            summary.text = matSel >= 0 ? "<color=#" + ColorUtility.ToHtmlStringRGB(Mats[matSel].col) + ">" + Mats[matSel].name + "</color>  x" + Fmt.Num(MatCount(matSel)) + "   -   " + Mats[matSel].desc
                                       : "Materials, potions and currencies. Click a potion and press USE.";
        }

        void Update()
        {
            if (!IsOpen) return;
            if (Inp.EscapeDown() && !Inp.Typing) { Close(); return; }
            if (confirmT > 0f)
            {
                confirmT -= Time.unscaledDeltaTime;
                if (confirmT <= 0f) Refresh();
            }
            if (filter == MatFilter) return;
            for (int i = 0; PageOff + i < view.Count && i < cellIcons.Count; i++)
                if (view[PageOff + i].rarity >= 5) cellIcons[i].color = ItemDB.AnimCol(5);
        }
    }
}
