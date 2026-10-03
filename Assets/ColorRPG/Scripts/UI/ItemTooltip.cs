// =====================================================================
//  ItemTooltip.cs - chenarul care apare cand tii mouse-ul ~1 secunda pe un item
//  (inventar, booth, magazine). Arata raritatea si TOATE statisticile itemului.
// =====================================================================
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorRPG
{
    public class ItemTooltip : MonoBehaviour
    {
        public const float Delay = 0.6f;     // secunde pana apare chenarul
        const float Width = 460f;

        static ItemTooltip I;
        RectTransform canvas, panel;
        Text text;
        Image edge, band;
        Item pending;
        string pendingText; Color pendingCol;   // chenar simplu cu text (skill-uri)
        float hoverT;
        bool shown;

        public static void Create(RectTransform hudRoot)
        {
            var go = new GameObject("ItemTooltip", typeof(RectTransform));
            go.transform.SetParent(hudRoot, false);
            I = go.AddComponent<ItemTooltip>();
            I.canvas = hudRoot;
            I.Build();
        }

        void Build()
        {
            panel = UIKit.Rect(transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0, 1), Vector2.zero, new Vector2(Width, 200));
            var bg = UIKit.Img(panel, "flat_Grey", new Color(0.07f, 0.08f, 0.14f, 0.96f), 16f, 1f);
            bg.raycastTarget = false;
            band = UIKit.Rect(panel, "Band", new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(Width, 8)).gameObject.AddComponent<Image>();
            band.raycastTarget = false;
            edge = UIKit.Rect(panel, "Edge", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(6, 10)).gameObject.AddComponent<Image>();
            edge.raycastTarget = false;
            // fara contur dublu: textul lung cu 2 contururi + umbra trecea de 65000 de vertexuri (chenar gol + eroare)
            text = UIKit.Text(panel, "T", "", 20, Color.white, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -18), new Vector2(Width - 40, 100), false, false);
            var tsh = text.gameObject.AddComponent<Shadow>(); tsh.effectColor = new Color(0, 0, 0, 0.85f); tsh.effectDistance = new Vector2(1.5f, -1.5f);
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.05f;
            panel.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------
        //  API folosit de ItemHover
        // ---------------------------------------------------------------
        public static void Enter(Item it)
        {
            if (I == null) return;
            I.pending = it; I.hoverT = 0f;
            I.HideNow();
        }

        public static void EnterText(string txt, Color c)
        {
            if (I == null) return;
            I.pending = null; I.pendingText = txt; I.pendingCol = c; I.hoverT = 0f;
            I.HideNow();
        }

        public static void ExitText()
        {
            if (I == null || I.pendingText == null) return;
            I.pendingText = null; I.HideNow();
        }

        public static void Exit(Item it)
        {
            if (I == null) return;
            if (it == null || I.pending == it) { I.pending = null; I.HideNow(); }
        }

        public static void Hide() { if (I != null) { I.pending = null; I.pendingText = null; I.HideNow(); } }

        void HideNow()
        {
            shown = false;
            if (panel != null && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
        }

        void Update()
        {
            if (pending == null && pendingText == null) return;
            hoverT += Time.unscaledDeltaTime;
            if (!shown && hoverT >= Delay) Show(pending);
            if (shown) Follow();
        }

        void Show(Item it)
        {
            var p = Game.I != null ? Game.I.player : null;
            text.text = it != null ? Describe(it, p) : pendingText;
            Color rc = it != null ? ItemDB.Col(it.rarity) : pendingCol;
            band.color = rc; edge.color = rc;
            panel.gameObject.SetActive(true);   // textul trebuie sa fie activ ca sa-i putem masura inaltimea
            text.rectTransform.sizeDelta = new Vector2(Width - 40, 100);
            Canvas.ForceUpdateCanvases();
            float h = LayoutUtility.GetPreferredHeight(text.rectTransform) + 40f;
            panel.sizeDelta = new Vector2(Width, h);
            text.rectTransform.sizeDelta = new Vector2(Width - 40, h - 30);
            edge.rectTransform.sizeDelta = new Vector2(6, h - 16);
            transform.SetAsLastSibling();
            shown = true;
            Follow();
        }

        void Follow()
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, Inp.MousePos(), null, out local)) return;
            Rect r = canvas.rect;
            Vector2 size = panel.sizeDelta;
            Vector2 pos = local + new Vector2(26, -26);
            if (pos.x + size.x > r.xMax - 10) pos.x = local.x - 26 - size.x;     // nu iesim din ecran (dreapta)
            if (pos.y - size.y < r.yMin + 10) pos.y = r.yMin + 10 + size.y;       // jos
            if (pos.y > r.yMax - 10) pos.y = r.yMax - 10;
            panel.anchoredPosition = pos;
        }

        // ---------------------------------------------------------------
        //  Textul cu toate statisticile
        // ---------------------------------------------------------------
        static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }

        public static string Describe(Item it, Player p)
        {
            string rc = Hex(ItemDB.Col(it.rarity));
            var sb = new System.Text.StringBuilder();
            sb.Append("<size=28><color=#").Append(rc).Append(">").Append(ItemDB.FullName(it)).Append("</color></size>\n");
            sb.Append("<color=#").Append(rc).Append(">").Append(ItemDB.RarityNames[it.rarity].ToUpper()).Append("</color>")
              .Append("  <color=#9aa6c0>").Append(it.S == Slot.Weapon ? ItemDB.KindText(it) : ItemDB.SlotNames[it.slot]).Append("</color>\n\n");

            // statistici
            string g = "<color=#7dff8a>", e = "</color>";
            switch (it.S)
            {
                case Slot.Weapon:
                    sb.Append(g).Append("+").Append(Fmt.Num(ItemDB.Power(it))).Append(e).Append(" Damage\n");
                    break;
                case Slot.Helmet:
                    sb.Append(g).Append("+").Append(Fmt.Num(ItemDB.Power(it))).Append(e).Append(" Max HP\n");
                    break;
                case Slot.Armor:
                    sb.Append(g).Append("+").Append(Fmt.Num(ItemDB.Power(it))).Append(e).Append(" Max HP\n");
                    sb.Append(g).Append(Mathf.RoundToInt(ItemDB.Defense(it) * 100)).Append("%").Append(e).Append(" Defense (damage reduction)\n");
                    break;
                default:
                    sb.Append(g).Append("+").Append(Mathf.RoundToInt(ItemDB.CritBonus(it) * 100)).Append("%").Append(e).Append(" Crit chance\n");
                    sb.Append(g).Append("+").Append(Mathf.RoundToInt(ItemDB.CoinBonus(it) * 100)).Append("%").Append(e).Append(" Coins from monsters\n");
                    break;
            }

            // detalii
            int quality = Mathf.RoundToInt(Mathf.InverseLerp(0.92f, 1.08f, it.roll) * 100f);
            sb.Append("\n<color=#b8c4d6>Quality:</color> ").Append(quality).Append("%");
            sb.Append("\n<color=#b8c4d6>Forge level:</color> +").Append(it.plus).Append(" / ").Append(ItemDB.MaxPlus);
            int t = Mathf.Clamp(it.tier, 0, GameData.Maps.Length - 1);
            sb.Append("\n<color=#b8c4d6>Found in:</color> ").Append(GameData.Maps[t].name);
            sb.Append("\n<color=#b8c4d6>Sell value:</color> <color=#ffd84d>").Append(Fmt.Num(ItemDB.SellValue(it))).Append(" coins</color>");

            // comparatie cu itemul echipat
            if (p != null)
            {
                if (p.IsEquipped(it)) sb.Append("\n\n<color=#99ccff>EQUIPPED</color>");
                else
                {
                    var cur = p.Equipped(it.S);
                    if (cur == null) sb.Append("\n\n<color=#66ff77>Empty slot - equip it!</color>");
                    else
                    {
                        float a = Player.Score(it), b = Player.Score(cur);
                        float pct = b > 0 ? (a / b - 1f) * 100f : 100f;
                        sb.Append(pct >= 0 ? "\n\n<color=#66ff77>BETTER: +" : "\n\n<color=#ff6666>WORSE: ")
                          .Append(Mathf.RoundToInt(pct)).Append("% vs equipped ").Append(ItemDB.FullName(cur)).Append("</color>");
                    }
                }
            }
            if (it.locked) sb.Append("\n<color=#ffcc55>LOCKED (can't be sold)</color>");
            return sb.ToString();
        }
    }

    /// <summary>Pune pe orice element de UI care arata un item: la hover apare ItemTooltip.</summary>
    public class ItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public System.Func<Item> get;
        Item current;

        public static ItemHover Attach(GameObject go, System.Func<Item> getter)
        {
            var h = go.GetComponent<ItemHover>();
            if (h == null) h = go.AddComponent<ItemHover>();
            h.get = getter;
            return h;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            current = get != null ? get() : null;
            if (current != null) ItemTooltip.Enter(current);
        }

        public void OnPointerExit(PointerEventData e) { ItemTooltip.Exit(current); current = null; }

        // click: ascundem chenarul si reluam numaratoarea (ca sa nu stea in cale)
        public void OnPointerDown(PointerEventData e)
        {
            ItemTooltip.Hide();
            current = get != null ? get() : null;
            if (current != null) ItemTooltip.Enter(current);
        }

        void OnDisable() { if (current != null) { ItemTooltip.Exit(current); current = null; } }
    }

    /// <summary>Chenar cu text la hover (ex. descrierea unui skill).</summary>
    public class TextHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public System.Func<string> get;
        public Color col = Color.white;

        public static TextHover Attach(GameObject go, System.Func<string> getter, Color c)
        {
            var h = go.GetComponent<TextHover>();
            if (h == null) h = go.AddComponent<TextHover>();
            h.get = getter; h.col = c;
            return h;
        }

        public void OnPointerEnter(PointerEventData e) { var s = get != null ? get() : null; if (!string.IsNullOrEmpty(s)) ItemTooltip.EnterText(s, col); }
        public void OnPointerExit(PointerEventData e) { ItemTooltip.ExitText(); }
        public void OnPointerDown(PointerEventData e) { ItemTooltip.ExitText(); }
        void OnDisable() { ItemTooltip.ExitText(); }
    }
}
