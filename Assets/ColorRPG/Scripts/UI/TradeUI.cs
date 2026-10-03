// =====================================================================
//  TradeUI.cs - interfata booth-urilor din Trade Plaza: booth-ul tau,
//  booth-urile altor jucatori (cumperi de la ei). Fara negocieri: pret fix.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class TradeUI : MonoBehaviour
    {
        Game game;
        RectTransform dim, panel, content;
        Text title, subtitle;
        Image header;
        int boothIdx = -1;   // -1 = booth-ul tau, altfel booth-ul altui jucator
        int listTab;         // 0 = iteme, 1 = pets, 2 = materiale (ce pui la vanzare)
        Text timerText;


        public bool IsOpen { get { return dim != null && dim.gameObject.activeSelf; } }

        static readonly Vector2 C = new Vector2(0.5f, 0.5f), TL = new Vector2(0, 1), TR = new Vector2(1, 1),
                                BL = new Vector2(0, 0), BR = new Vector2(1, 0), TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0);

        public static TradeUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("TradeUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<TradeUI>();
            ui.game = g;
            ui.Build(parent);
            return ui;
        }

        void Build(RectTransform parent)
        {
            dim = UIKit.Stretch(parent, "TradeDim");
            var di = dim.gameObject.AddComponent<Image>();
            di.color = new Color(0, 0, 0, 0.45f);
            di.raycastTarget = true;
            var db = dim.gameObject.AddComponent<Button>();
            db.transition = Selectable.Transition.None;
            db.onClick.AddListener(Close);

            // acelasi stil ca restul ferestrelor: panou gri-inchis, titlu in "eticheta" deasupra, X rosu
            panel = UIKit.Rect(dim, "Panel", C, C, new Vector2(0, -20), new Vector2(1400, 820));
            UIKit.Img(panel, "panel_dark", Color.white, 26f, 1f).raycastTarget = true;
            var hr = UIKit.Rect(panel, "Header", TC, C, new Vector2(0, 6), new Vector2(900, 84));
            header = UIKit.Img(hr, "tab_title", Color.white, 22f, 1f);
            title = UIKit.Text(hr, "Title", "", 40, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(860, 64));
            title.resizeTextForBestFit = true; title.resizeTextMinSize = 18; title.resizeTextMaxSize = 44;
            subtitle = UIKit.Text(panel, "Sub", "", 19, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, TC, TC, new Vector2(0, -50), new Vector2(1200, 28), true, true);
            var close = UIKit.Button(panel, "Close", "X", "Red", new Vector2(1, 1), C, new Vector2(-14, -2), new Vector2(84, 84), Close, 46);
            close.transform.localRotation = Quaternion.Euler(0, 0, -4f);
            content = UIKit.Rect(panel, "Content", TL, TL, new Vector2(0, -86), new Vector2(1400, 734));
            dim.gameObject.SetActive(false);

        }

        // ---------------------------------------------------------------
        /// <summary>Deschide booth-ul tau (booth = -1) sau al altui jucator (0..9).</summary>
        public void Open(int booth)
        {
            boothIdx = booth;
            dim.gameObject.SetActive(true);
            Sfx.Play("Open", 0.6f);
            Rebuild();
        }

        public void OpenMyBooth() { Open(-1); }

        public void Close()
        {
            if (!IsOpen) return;
            dim.gameObject.SetActive(false);
            ItemTooltip.Hide();
            Sfx.Play("Close", 0.6f);
            game.player.SaveNow();
        }

        void Rebuild()
        {
            if (!IsOpen) return;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            timerText = null;
            if (boothIdx < 0) BuildPlayerBooth();
            else BuildSimBooth();
        }

        // ---------------------------------------------------------------
        //  Elemente comune
        // ---------------------------------------------------------------
        RectTransform Card(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color c)
        {
            var rt = UIKit.Rect(parent, "Card", anchor, pivot, pos, size);
            UIKit.Img(rt, "card_dark", UIKit.Theme(c), 18f, 1f);
            return rt;
        }

        Image Icon(Transform parent, Item it, Vector2 anchor, Vector2 pivot, Vector2 pos, float size)
        {
            var bg = UIKit.Rect(parent, "IconBg", anchor, pivot, pos, new Vector2(size, size));
            UIKit.Img(bg, "sq_Grey", Color.Lerp(new Color(0.3f, 0.32f, 0.45f), ItemDB.Col(it.rarity), 0.55f), 16f, 1.2f);
            var ir = UIKit.Rect(bg, "Icon", C, C, new Vector2(0, 3), new Vector2(size * 0.72f, size * 0.72f));
            var im = ir.gameObject.AddComponent<Image>();
            im.sprite = ItemIcons.For(it); ItemIcons.Art(im);
            im.color = ItemDB.Col(it.rarity);
            im.raycastTarget = false;
            if (it.plus > 0)
                UIKit.Text(bg, "Plus", "+" + it.plus, 16, Color.white, TextAnchor.LowerRight, BR, BR, new Vector2(-5, 3), new Vector2(60, 24), true, true);
            bg.GetComponent<Image>().raycastTarget = true;
            var item = it;
            ItemHover.Attach(bg.gameObject, () => item);   // statisticile apar la hover
            return im;
        }

        /// <summary>Iconita pentru orice listare (item / pet / material).</summary>
        void ListingIcon(Transform parent, int kind, Item it, PetData pet, int mat, Vector2 anchor, Vector2 pivot, Vector2 pos, float size)
        {
            if (kind == 0 && it != null) { Icon(parent, it, anchor, pivot, pos, size); return; }
            Color c = TradeDB.ListingCol(kind, it, pet, mat);
            var bg = UIKit.Rect(parent, "IconBg", anchor, pivot, pos, new Vector2(size, size));
            UIKit.Img(bg, "sq_Grey", Color.Lerp(new Color(0.3f, 0.32f, 0.45f), c, 0.55f), 16f, 1.2f);
            var ir = UIKit.Rect(bg, "Icon", C, C, new Vector2(0, 3), new Vector2(size * 0.72f, size * 0.72f));
            var im = ir.gameObject.AddComponent<Image>();
            im.sprite = ItemIcons.Get(kind == 1 ? 6 : TradeDB.MatIcon[Mathf.Clamp(mat, 0, TradeDB.MatIcon.Length - 1)]); ItemIcons.Art(im);
            im.color = c;
            im.raycastTarget = false;
        }

        static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }

        string CompareText(Item it)
        {
            var cur = game.player.Equipped(it.S);
            if (cur == null) return "<color=#66ff77>Empty slot - upgrade!</color>";
            float a = Player.Score(it), b = Player.Score(cur);
            float pct = b > 0 ? (a / b - 1f) * 100f : 100f;
            return pct >= 0 ? "<color=#66ff77>+" + Mathf.RoundToInt(pct) + "% vs equipped</color>"
                            : "<color=#ff7777>" + Mathf.RoundToInt(pct) + "% vs equipped</color>";
        }

        /// <summary>Grila cu itemele din inventar (neechipate). onClick primeste itemul.</summary>
        void BagGrid(Transform parent, Vector2 pos, int cols, int rows, float cell, System.Func<Item, bool> filter,
                     System.Action<Item> onClick, System.Func<Item, bool> highlight, int skip = 0)
        {
            var p = game.player;
            var list = new List<Item>();
            foreach (var it in p.D.items) if (!p.IsEquipped(it) && (filter == null || filter(it))) list.Add(it);
            list.Sort((a, b) => b.rarity != a.rarity ? b.rarity.CompareTo(a.rarity) : Player.Score(b).CompareTo(Player.Score(a)));
            if (skip > 0) list.RemoveRange(0, Mathf.Min(skip, list.Count));
            float gap = 8f;
            for (int i = 0; i < cols * rows; i++)
            {
                int r = i / cols, c = i % cols;
                var rt = UIKit.Rect(parent, "Cell", TL, TL, pos + new Vector2(c * (cell + gap), -r * (cell + gap)), new Vector2(cell, cell));
                if (i < list.Count)
                {
                    var it = list[i];
                    bool hl = highlight != null && highlight(it);
                    var fr = UIKit.Img(rt, "sq_Grey", hl ? Color.white : Color.Lerp(new Color(0.3f, 0.32f, 0.45f), ItemDB.Col(it.rarity), 0.55f), 16f, 1.2f);
                    fr.raycastTarget = true;
                    var b = rt.gameObject.AddComponent<Button>();
                    b.targetGraphic = fr;
                    b.onClick.AddListener(() => onClick(it));
                    ItemHover.Attach(rt.gameObject, () => it);
                    var ir = UIKit.Rect(rt, "Icon", C, C, new Vector2(0, 3), new Vector2(cell * 0.7f, cell * 0.7f));
                    var im = ir.gameObject.AddComponent<Image>();
                    im.sprite = ItemIcons.For(it); ItemIcons.Art(im);
                    im.color = ItemDB.Col(it.rarity);
                    im.raycastTarget = false;
                    if (it.plus > 0) UIKit.Text(rt, "P", "+" + it.plus, 15, Color.white, TextAnchor.LowerRight, BR, BR, new Vector2(-5, 3), new Vector2(60, 22), true, true);
                    if (hl) UIKit.Text(rt, "Sel", "X", 20, new Color(0.3f, 1f, 0.4f), TextAnchor.UpperLeft, TL, TL, new Vector2(6, -2), new Vector2(30, 26), true);
                }
                else UIKit.Img(rt, "sq_Grey", new Color(0.3f, 0.32f, 0.45f, 0.6f), 16f, 1.2f);
            }
            if (list.Count == 0)
                UIKit.Text(parent, "Empty", "No matching items in your inventory.", 20, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, TL, TL, pos + new Vector2(0, -cell), new Vector2(cols * (cell + gap), 40), true, true);
        }

        // ---------------------------------------------------------------
        //  Booth-ul jucatorului: 18 locuri (iteme, pets, materiale), pretul il alegi tu
        // ---------------------------------------------------------------
        // ce pregatesti sa pui pe booth (dreapta jos)
        Item newItem; PetData newPet; int newMat = -1, newAmount = 1;
        long newPrice; bool newPriceSet;
        BoothListing editL;          // listarea selectata (stanga jos: schimbi pretul)
        int bagPage;

        void ClearNew() { newItem = null; newPet = null; newMat = -1; newAmount = 1; newPriceSet = false; }

        /// <summary>Banda colorata a tarabei (simpla, fara dungi) - doar decor.</summary>
        void Awning(Transform parent, Vector2 pos, float w, Color c)
        {
            var st = UIKit.Rect(parent, "Awning", TL, TL, pos, new Vector2(w, 10));
            UIKit.Img(st, "flat_Grey", new Color(c.r, c.g, c.b, 0.7f), 6f, 1f).raycastTarget = false;
        }

        void BuildPlayerBooth()
        {
            var ts = TradeSystem.I; var D = game.player.D;
            bool here = ts.MyBoothHere;
            Color bc = here ? TradeDB.BoothCols[Mathf.Clamp(D.boothIndex, 0, TradeDB.BoothCols.Length - 1)] : new Color(0.6f, 0.6f, 0.65f);
            title.text = "YOUR BOOTH";
            subtitle.text = here ? TradeSystem.ServerName(ts.Server.id) + "  -  booth " + (D.boothIndex + 1) + "/" + TradeDB.BoothCount + "   |   sells only while you stay in this market"
                                 : "NO BOOTH  -  walk to a FREE booth in the Trade Plaza and " + (Device.Mobile ? "tap CLAIM" : "press E") + " to claim it";
            if (editL != null && !D.booth.Contains(editL)) editL = null;

            // ---- firma booth-ului: copertina + castiguri
            var top = Card(content, TL, TL, new Vector2(24, -10), new Vector2(1352, 96), new Color(0.22f, 0.17f, 0.1f, 1f));
            Awning(top, new Vector2(0, 0), 1352, bc);
            var sign = UIKit.Rect(top, "Open", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -14), new Vector2(150, 44));
            UIKit.Img(sign, "flat_Grey", here ? new Color(0.25f, 0.75f, 0.35f) : new Color(0.65f, 0.25f, 0.25f), 12f, 1f).raycastTarget = false;
            UIKit.Text(sign, "T", here ? "OPEN" : "CLOSED", 22, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 1), new Vector2(150, 44));
            UIKit.Text(top, "E", "EARNINGS: " + Fmt.Num(D.boothEarnings) + " tokens", 28, new Color(0.55f, 0.95f, 0.75f), TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(186, -14), new Vector2(520, 50));
            UIKit.Text(top, "S", "Sold: " + D.boothSales + "  (" + Fmt.Num(D.boothTotal) + " tokens)   Trades: " + D.trades, 17, new Color(1, 1, 1, 0.8f), TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(720, -14), new Vector2(380, 50), true, true);
            UIKit.Button(top, "Collect", "COLLECT", D.boothEarnings > 0 ? "Green" : "Grey", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, -14), new Vector2(210, 56),
                         () => { ts.Collect(); Rebuild(); }, 22);

            // ---- stanga: cele 18 locuri de pe booth (6 x 3)
            UIKit.Text(content, "LT", "ON YOUR BOOTH  " + D.booth.Count + " / " + TradeDB.MaxListings, 22, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleLeft, TL, TL, new Vector2(30, -118), new Vector2(400, 32));
            if (!here && D.booth.Count > 0)
                UIKit.Text(content, "LW", "not selling - no booth", 16, new Color(1f, 0.55f, 0.5f), TextAnchor.MiddleRight, TL, TL, new Vector2(784 - 300, -118), new Vector2(300, 32), true, true).rectTransform.pivot = TL;
            const float cw = 118, ch = 146, gap = 10;
            for (int i = 0; i < TradeDB.MaxListings; i++)
            {
                int c = i % 6, r = i / 6;
                var cell = Card(content, TL, TL, new Vector2(24 + c * (cw + gap), -154 - r * (ch + gap)), new Vector2(cw, ch), new Color(0.17f, 0.19f, 0.29f, 1f));
                if (i >= D.booth.Count)
                {
                    UIKit.Text(cell, "E", "EMPTY", 15, new Color(1, 1, 1, 0.25f), TextAnchor.MiddleCenter, C, C, Vector2.zero, new Vector2(cw, 30), true, true);
                    continue;
                }
                var l = D.booth[i];
                bool sel = l == editL;
                cell.GetComponent<Image>().color = sel ? Color.white : UIKit.Theme(Color.Lerp(new Color(0.17f, 0.19f, 0.29f), l.Col, 0.35f));
                ListingIcon(cell, l.kind, l.item, l.pet, l.mat, TC, TC, new Vector2(0, -8), 84);
                if (l.kind == 2) UIKit.Text(cell, "Am", "x" + l.amount, 16, Color.white, TextAnchor.UpperRight, TR, TR, new Vector2(-6, -6), new Vector2(60, 22), true, true);
                var tk = UIKit.Rect(cell, "Tk", BL, BL, new Vector2(8, 26), new Vector2(22, 22)).gameObject.AddComponent<Image>();
                tk.sprite = ItemIcons.Get(19); ItemIcons.Art(tk); tk.raycastTarget = false;
                UIKit.Text(cell, "P", Fmt.Num(l.price), 18, new Color(0.55f, 0.95f, 0.75f), TextAnchor.MiddleLeft, BL, BL, new Vector2(32, 26), new Vector2(82, 24), true, true);
                // viteza de vanzare: bulina colorata
                float ratio = l.price / (float)System.Math.Max(1L, l.Fair);
                Color rc = ratio < 0.8f ? new Color(0.4f, 1f, 0.45f) : ratio < 1.15f ? new Color(0.8f, 1f, 0.4f) : ratio < 1.5f ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.4f, 0.4f);
                var dot = UIKit.Rect(cell, "Rate", BC, BC, new Vector2(0, 6), new Vector2(cw - 20, 8));
                UIKit.Img(dot, "flat_Grey", rc, 4f, 1f).raycastTarget = false;
                var ci = cell.GetComponent<Image>(); ci.raycastTarget = true;
                var btn = cell.gameObject.AddComponent<Button>(); btn.targetGraphic = ci;
                var ll = l;
                btn.onClick.AddListener(() => { editL = editL == ll ? null : ll; Sfx.Play("Click", 0.4f); Rebuild(); });
            }

            // ---- stanga jos: listarea selectata (pret / scoate)
            var ed = Card(content, TL, TL, new Vector2(24, -626), new Vector2(760, 100), new Color(0.12f, 0.12f, 0.15f, 1f));
            if (editL == null)
                UIKit.Text(ed, "H", "Click something on your booth to change its price or take it back.\nThe colored bar shows how fast it sells (green = fast, red = too expensive).", 16, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter, C, C, Vector2.zero, new Vector2(740, 80), true, true);
            else
            {
                var l = editL;
                ListingIcon(ed, l.kind, l.item, l.pet, l.mat, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), 78);
                var nm = UIKit.Text(ed, "N", l.Name, 18, l.Col, TextAnchor.UpperLeft, TL, TL, new Vector2(100, -10), new Vector2(250, 26), true, true);
                UIKit.Fit(nm, 11);
                var rl = UIKit.Text(ed, "R", TradeDB.RateLabel(l), 14, Color.white, TextAnchor.UpperLeft, TL, TL, new Vector2(100, -40), new Vector2(250, 22), true, true); rl.supportRichText = true;
                UIKit.Text(ed, "F", "market value ~" + Fmt.Num(l.Fair), 13, new Color(1, 1, 1, 0.55f), TextAnchor.UpperLeft, TL, TL, new Vector2(100, -64), new Vector2(250, 22), true, true);
                PriceField(ed, new Vector2(360, 0), l.price, v => ts.SetPrice(l, v));
                UIKit.Button(ed, "Rem", "TAKE BACK", "Red", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(150, 56), () => { ts.Unlist(l); editL = null; Rebuild(); }, 17);
            }

            // ---- dreapta: ce pui la vanzare
            string[] tabs = { "ITEMS", "PETS", "MATERIALS" };
            for (int k = 0; k < 3; k++)
            {
                int kk = k;
                UIKit.Button(content, "Tab" + k, tabs[k], listTab == k ? "Yellow" : "Grey", TL, TL, new Vector2(806 + k * 190, -112), new Vector2(180, 44), () => { listTab = kk; bagPage = 0; ClearNew(); Rebuild(); }, 17);
            }
            var pos = new Vector2(806, -166);
            if (listTab == 0) ItemPicker(content, pos);
            else if (listTab == 1) PetGrid(content, pos);
            else MatList(content, pos);
            NewListingEditor(content, new Vector2(806, -556));
        }

        /// <summary>Camp de pret (tokens) cu butoane rapide.</summary>
        void PriceField(Transform parent, Vector2 pos, long price, System.Action<long> set)
        {
            var tk = UIKit.Rect(parent, "Tk", new Vector2(0, 0.5f), new Vector2(0, 0.5f), pos + new Vector2(0, 0), new Vector2(34, 34)).gameObject.AddComponent<Image>();
            tk.sprite = ItemIcons.Get(19); ItemIcons.Art(tk); tk.raycastTarget = false;
            var pf = UIKit.MakeInput(parent, "Price", new Vector2(0, 0.5f), new Vector2(0, 0.5f), pos + new Vector2(38, 0), new Vector2(150, 50), price.ToString(), 9, 22);
            pf.contentType = InputField.ContentType.IntegerNumber;
            pf.onEndEdit.AddListener(v => { long n; if (long.TryParse(v, out n) && n > 0) { set(n); Rebuild(); } });
            UIKit.Text(parent, "PH", "your price", 12, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(0, 0.5f), pos + new Vector2(38, -36), new Vector2(150, 16), true, true);
        }

        /// <summary>Itemele din inventar (pagini de 24).</summary>
        void ItemPicker(Transform parent, Vector2 pos)
        {
            var p = game.player;
            int total = 0;
            foreach (var it in p.D.items) if (!p.IsEquipped(it) && !it.locked) total++;
            int pages = Mathf.Max(1, (total + 23) / 24);
            bagPage = Mathf.Clamp(bagPage, 0, pages - 1);
            BagGrid(parent, pos, 6, 4, 86, it => !it.locked, it => { ClearNew(); newItem = it; Sfx.Play("Click", 0.4f); Rebuild(); }, it => it == newItem, bagPage * 24);
            if (pages > 1)
            {
                UIKit.Button(parent, "Pp", "<", "Grey", TL, TL, pos + new Vector2(0, -380), new Vector2(50, 34), () => { bagPage = (bagPage + pages - 1) % pages; Rebuild(); }, 20);
                UIKit.Text(parent, "Pg", (bagPage + 1) + " / " + pages, 16, Color.white, TextAnchor.MiddleCenter, TL, TL, pos + new Vector2(54, -380), new Vector2(80, 34), true, true);
                UIKit.Button(parent, "Pn", ">", "Grey", TL, TL, pos + new Vector2(138, -380), new Vector2(50, 34), () => { bagPage = (bagPage + 1) % pages; Rebuild(); }, 20);
            }
            UIKit.Text(parent, "Hint", "Favorite items are hidden here.", 13, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleRight, TL, TL, pos + new Vector2(570 - 260, -380), new Vector2(256, 34), true, true);
        }

        void PetGrid(Transform parent, Vector2 pos)
        {
            var D = game.player.D;
            float cell = 86, gap = 8; int i = 0;
            foreach (var pd in D.pets)
            {
                if (i >= 24) break;
                int r = i / 6, c = i % 6; i++;
                var rt = UIKit.Rect(parent, "Pet", TL, TL, pos + new Vector2(c * (cell + gap), -r * (cell + gap)), new Vector2(cell, cell));
                bool sel = pd == newPet;
                var fr = UIKit.Img(rt, "sq_Grey", sel ? Color.white : Color.Lerp(new Color(0.3f, 0.32f, 0.45f), ItemDB.Col(pd.rarity), pd.equipped ? 0.2f : 0.55f), 16f, 1.2f);
                fr.raycastTarget = true;
                var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = fr;
                var p = pd;
                b.onClick.AddListener(() =>
                {
                    if (p.equipped) { game.Say("Unequip the pet first (PETS window).", 2f); Sfx.Play("Error", 0.5f); return; }
                    ClearNew(); newPet = p; Sfx.Play("Click", 0.4f); Rebuild();
                });
                var ir = UIKit.Rect(rt, "Icon", C, C, new Vector2(0, 8), new Vector2(cell * 0.62f, cell * 0.62f));
                var im = ir.gameObject.AddComponent<Image>();
                var pic = PetStudio.Icon(pd);
                im.sprite = pic != null ? pic : ItemIcons.Get(6); ItemIcons.Art(im); im.color = pic != null ? Color.white : ItemDB.Col(pd.rarity); im.raycastTarget = false; im.preserveAspect = true;
                UIKit.Text(rt, "N", GameData.PetName(pd.model), 13, Color.white, TextAnchor.LowerCenter, BC, BC, new Vector2(0, 4), new Vector2(cell, 20), true, true);
                if (pd.equipped) UIKit.Text(rt, "E", "EQ", 14, new Color(1f, 0.85f, 0.3f), TextAnchor.UpperLeft, TL, TL, new Vector2(6, -2), new Vector2(30, 22), true, true);
            }
            if (D.pets.Count == 0)
                UIKit.Text(parent, "Empty", "You have no pets.", 20, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter, TL, TL, pos + new Vector2(0, -60), new Vector2(560, 40), true, true);
            else
                UIKit.Text(parent, "Hint", "Equipped pets (EQ) must be unequipped first.", 13, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleLeft, TL, TL, pos + new Vector2(0, -380), new Vector2(560, 34), true, true);
        }

        void MatList(Transform parent, Vector2 pos)
        {
            var ts = TradeSystem.I;
            for (int m = 0; m < TradeDB.MatNames.Length; m++)
            {
                int mm = m;
                int own = ts.MatOwned(m);
                bool on = newMat == m;
                var row = Card(parent, TL, TL, pos + new Vector2(0, -m * 74), new Vector2(570, 68), on ? new Color(0.9f, 0.7f, 0.25f, 1f) : new Color(0.2f, 0.22f, 0.34f, 1f));
                ListingIcon(row, 2, null, null, m, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), 56);
                UIKit.Text(row, "N", TradeDB.MatNames[m], 20, TradeDB.MatCols[m], TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(74, 10), new Vector2(330, 28), true, true);
                UIKit.Text(row, "O", "You have: " + own, 16, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(74, -14), new Vector2(330, 24), true, true);
                UIKit.Button(row, "L", on ? "SELECTED" : "SELL...", on ? "Yellow" : own > 0 ? "Green" : "Grey", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(130, 50), () =>
                {
                    if (ts.MatOwned(mm) <= 0) { game.Say("You don't have any " + TradeDB.MatNames[mm] + ".", 1.5f); Sfx.Play("Error", 0.4f); return; }
                    ClearNew(); newMat = mm; newAmount = Mathf.Min(ts.MatOwned(mm), mm == TradeDB.MatStones ? 10 : 1); Rebuild();
                }, 17);
            }
        }

        /// <summary>Dreapta jos: pui pe booth ce ai ales, la pretul TAU (in tokens).</summary>
        void NewListingEditor(Transform parent, Vector2 pos)
        {
            var ts = TradeSystem.I; var D = game.player.D;
            var ed = Card(parent, TL, TL, pos, new Vector2(570, 170), new Color(0.12f, 0.12f, 0.15f, 1f));
            int kind = newItem != null ? 0 : newPet != null ? 1 : newMat >= 0 ? 2 : -1;
            if (kind < 0)
            {
                UIKit.Text(ed, "H", "Pick something above, type YOUR PRICE in tokens\nand put it on your booth (" + D.booth.Count + "/" + TradeDB.MaxListings + ").\nNo haggling: whoever wants it, buys it.", 17, new Color(1, 1, 1, 0.65f), TextAnchor.MiddleCenter, C, C, Vector2.zero, new Vector2(550, 140), true, true);
                return;
            }
            if (kind == 2) newAmount = Mathf.Clamp(newAmount, 1, Mathf.Max(1, ts.MatOwned(newMat)));
            long fair = TradeDB.ListingValue(kind, newItem, newPet, newMat, newAmount);
            if (!newPriceSet) newPrice = (long)Mathf.Ceil(fair * 1.1f);
            string nm = TradeDB.ListingName(kind, newItem, newPet, newMat, newAmount);
            Color col = TradeDB.ListingCol(kind, newItem, newPet, newMat);
            ListingIcon(ed, kind, newItem, newPet, newMat, TL, TL, new Vector2(10, -10), 64);
            var nt = UIKit.Text(ed, "N", nm, 18, col, TextAnchor.MiddleLeft, TL, TL, new Vector2(84, -10), new Vector2(470, 28), true, true);
            UIKit.Fit(nt, 11);
            var probe = new BoothListing { kind = kind, item = newItem, pet = newPet, mat = newMat, amount = newAmount, price = newPrice };
            var rl = UIKit.Text(ed, "R", "market value ~" + Fmt.Num(fair) + "   " + TradeDB.RateLabel(probe), 14, Color.white, TextAnchor.MiddleLeft, TL, TL, new Vector2(84, -42), new Vector2(470, 24), true, true);
            rl.supportRichText = true;
            if (kind == 2)
            {
                UIKit.Button(ed, "M10", "-10", "Grey", TL, TL, new Vector2(84, -64), new Vector2(54, 36), () => { newAmount -= 10; newPriceSet = false; Rebuild(); }, 15);
                UIKit.Button(ed, "M1", "-1", "Grey", TL, TL, new Vector2(142, -64), new Vector2(44, 36), () => { newAmount -= 1; newPriceSet = false; Rebuild(); }, 15);
                UIKit.Text(ed, "A", "x" + newAmount, 18, Color.white, TextAnchor.MiddleCenter, TL, TL, new Vector2(190, -64), new Vector2(70, 36), true, true);
                UIKit.Button(ed, "P1", "+1", "Grey", TL, TL, new Vector2(264, -64), new Vector2(44, 36), () => { newAmount += 1; newPriceSet = false; Rebuild(); }, 15);
                UIKit.Button(ed, "P10", "+10", "Grey", TL, TL, new Vector2(312, -64), new Vector2(54, 36), () => { newAmount += 10; newPriceSet = false; Rebuild(); }, 15);
                UIKit.Button(ed, "Max", "MAX", "Blue", TL, TL, new Vector2(370, -64), new Vector2(64, 36), () => { newAmount = ts.MatOwned(newMat); newPriceSet = false; Rebuild(); }, 15);
            }
            PriceField(ed, new Vector2(14, -42), newPrice, v => { newPrice = v; newPriceSet = true; });
            bool full = D.booth.Count >= TradeDB.MaxListings;
            UIKit.Button(ed, "List", full ? "BOOTH FULL" : "PUT ON BOOTH", full ? "Grey" : "Yellow", BR, BR, new Vector2(-12, 12), new Vector2(230, 56), () =>
            {
                bool ok = kind == 0 ? ts.List(newItem, newPrice) : kind == 1 ? ts.ListPet(newPet, newPrice) : ts.ListMat(newMat, newAmount, newPrice);
                if (ok) ClearNew();
                Rebuild();
            }, 20);
        }

        static string MatDesc(int m)
        {
            switch (m)
            {
                case TradeDB.MatStones: return "Needed to forge weapons (+2 and up).";
                case TradeDB.MatCloak: return "A failed forge (+5 and up) won't drop your level.";
                case TradeDB.MatPotXp: return "XP x2 for 5 minutes.";
                case TradeDB.MatPotDmg: return "Damage x1.5 for 5 minutes.";
                default: return "Coins x1.5 for 5 minutes.";
            }
        }

        // ---------------------------------------------------------------
        //  Booth-ul altui jucator: cumperi iteme de la el
        // ---------------------------------------------------------------
        void BuildSimBooth()
        {
            var ts = TradeSystem.I; var D = game.player.D;
            var b = ts.Server != null ? ts.Server.booths[boothIdx] : null;
            if (b == null) { Close(); return; }
            title.text = b.owner.ToUpper() + "'S BOOTH";
            Awning(content, new Vector2(24, -2), 1352, b.col);
            subtitle.text = TradeSystem.ServerName(ts.Server.id) + "  -  " + b.owner;
            timerText = UIKit.Text(content, "Timer", "", 18, new Color(0.7f, 0.9f, 1f), TextAnchor.MiddleLeft, TL, TL, new Vector2(30, -50), new Vector2(700, 30), true, true);
            UIKit.Text(content, "Money", "You have: " + Fmt.Num(D.tokens) + " tokens   |   fixed prices - no haggling", 20, new Color(0.55f, 0.95f, 0.75f), TextAnchor.MiddleRight, TR, TR, new Vector2(-30, -50), new Vector2(700, 30), true, true);
            int shown = 0;
            for (int i = 0; i < b.stock.Count; i++)
            {
                var l = b.stock[i];
                int col = shown % 3, row = shown / 3;
                shown++;
                var card = Card(content, TL, TL, new Vector2(30 + col * 452, -86 - row * 318), new Vector2(436, 306), new Color(0.2f, 0.22f, 0.34f, 1f));
                var it = l.item;
                var band = UIKit.Rect(card, "Band", TC, TC, Vector2.zero, new Vector2(436, 40));
                UIKit.Img(band, "flat_Grey", l.Col, 16f, 1f);
                string kindTag = l.kind == 1 ? "PET" : l.kind == 2 ? "MATERIAL" : ItemDB.RarityNames[it.rarity].ToUpper();
                if (l.kind == 1) kindTag = ItemDB.RarityNames[l.pet.rarity].ToUpper() + " PET";
                UIKit.Text(band, "R", kindTag, 20, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(420, 36));
                ListingIcon(card, l.kind, it, l.pet, l.mat, TL, TL, new Vector2(18, -56), 120);
                var n = UIKit.Text(card, "N", l.Name, 22, l.Col, TextAnchor.UpperLeft, TL, TL, new Vector2(152, -58), new Vector2(270, 60), true);
                n.horizontalOverflow = HorizontalWrapMode.Wrap;
                string info = l.kind == 0 ? CompareText(it) : l.kind == 1 ? "+" + Mathf.RoundToInt(Player.PetPower(l.pet) * 100) + "% damage when equipped" : MatDesc(l.mat);
                var cmp = UIKit.Text(card, "C", info, 16, Color.white, TextAnchor.UpperLeft, TL, TL, new Vector2(152, -126), new Vector2(270, 44), true, true);
                cmp.supportRichText = true;
                cmp.horizontalOverflow = HorizontalWrapMode.Wrap;
                if (l.kind == 0)
                    UIKit.Text(card, "Where", "from " + GameData.Maps[Mathf.Clamp(it.tier, 0, GameData.Maps.Length - 1)].name, 15, new Color(1, 1, 1, 0.6f), TextAnchor.UpperLeft, TL, TL, new Vector2(152, -172), new Vector2(270, 24), true, true);
                if (l.sold)
                {
                    UIKit.Text(card, "Sold", "SOLD", 34, new Color(1f, 0.45f, 0.45f), TextAnchor.MiddleCenter, BC, BC, new Vector2(0, 40), new Vector2(400, 60));
                    continue;
                }
                bool afford = D.tokens >= l.price;
                UIKit.Text(card, "Price", Fmt.Num(l.price) + " tokens", 24, afford ? new Color(0.55f, 0.95f, 0.75f) : new Color(1f, 0.45f, 0.45f), TextAnchor.MiddleLeft, BL, BL, new Vector2(20, 26), new Vector2(220, 56));
                var ll = l; int bi = boothIdx;
                UIKit.Button(card, "Buy", "BUY", afford ? "Green" : "Grey", BR, BR, new Vector2(-16, 16), new Vector2(170, 64), () =>
                {
                    if (ts.BuySim(bi, ll)) Fx.Sparkle(game.player.transform.position + Vector3.up * 1.5f, ll.Col, 12, 0.8f);
                    Rebuild();
                }, 22);
            }
            if (shown == 0)
                UIKit.Text(content, "Empty", b.owner + " has nothing for sale right now.", 28, Color.white, TextAnchor.MiddleCenter, TC, TC, new Vector2(0, -300), new Vector2(1200, 60));
        }

        // ---------------------------------------------------------------
        void Update()
        {
            if (game == null || game.player == null) return;
            if (IsOpen && (Inp.EscapeDown())) Close();
            if (timerText != null)
            {
                float t = TradeDB.RestockSeconds - (TradeDB.Now() % (long)TradeDB.RestockSeconds);
                timerText.text = "Players change their booths in " + Mathf.FloorToInt(t / 60f) + ":" + Mathf.FloorToInt(t % 60f).ToString("00");
            }

        }
    }
}
