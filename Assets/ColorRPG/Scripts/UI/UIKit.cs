// =====================================================================
//  UIKit.cs - mici utilitare pentru a construi UI (Canvas) direct din cod,
//  folosind sprite-urile si fontul CC0 din Kenney UI Pack.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ColorRPG
{
    public static class UIKit
    {
        static Font font, fontNarrow;
        /// <summary>Pagella are litere mai mici decat Kenney Future la aceeasi marime -> le marim putin.</summary>
        public const float FontScale = 1.08f;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Font Font
        {
            get
            {
                // font gros, rotunjit, usor de citit (Poppins Bold, SIL Open Font License)
                if (font == null) font = Resources.Load<Font>("UI/GameFont");
                if (font == null) font = Resources.Load<Font>("UI/PagellaBold");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Font FontNarrow
        {
            get
            {
                if (fontNarrow == null) fontNarrow = Font;   // acelasi font clasic peste tot
                return fontNarrow;
            }
        }

        /// <summary>Sprite 9-slice dintr-un PNG din Resources/UI (creat la runtime).</summary>
        public static Sprite Sprite(string n, float border = 16f)
        {
            Sprite s;
            string key = n + "_" + border;
            if (sprites.TryGetValue(key, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("UI/" + n);
            if (tex == null) { sprites[key] = null; return null; }
            tex.wrapMode = TextureWrapMode.Clamp;
            float b = Mathf.Min(border, Mathf.Min(tex.width, tex.height) * 0.45f);
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                                          SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = n;
            sprites[key] = s;
            return s;
        }

        public static Sprite FromTexture(Texture2D tex)
        {
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------------------------------------------------------------
        //  RectTransform
        // ---------------------------------------------------------------
        public static RectTransform Rect(Transform parent, string n, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(n, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string n, float inset = 0f)
        {
            var go = new GameObject(n, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        // ---------------------------------------------------------------
        //  Elemente
        // ---------------------------------------------------------------
        public static Image Img(RectTransform rt, string sprite, Color c, float border = 16f, float ppuMul = 1f)
        {
            var img = rt.gameObject.AddComponent<Image>();
            var s = sprite == null ? null : Sprite(sprite, border);
            img.sprite = s;
            img.type = s != null ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = ppuMul;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(Transform parent, string n, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color c, string sprite = "flat_Grey")
        {
            var rt = Rect(parent, n, anchor, pivot, pos, size);
            return Img(rt, sprite, c, 16f, 1.2f);
        }

        public static Text Text(Transform parent, string n, string s, int size, Color c, TextAnchor align,
                                Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box, bool outline = true, bool narrow = false)
        {
            var rt = Rect(parent, n, anchor, pivot, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = narrow ? FontNarrow : Font;
            t.fontSize = Mathf.RoundToInt(size * FontScale);
            t.color = c;
            t.alignment = align;
            t.text = s;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                // contur gros, ca in jocurile tip "simulator" (text alb cu margine neagra)
                // un singur contur gros + umbra (2 contururi inmulteau vertexurile de 25x -> eroare 65000 la texte lungi)
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.55f);
                sh.effectDistance = new Vector2(0f, -3.5f);
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.04f, 0.04f, 0.06f, 0.95f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        /// <summary>Textul se micsoreaza singur ca sa incapa in caseta lui (nu iese peste alte elemente).</summary>
        public static void Fit(Text t, int minSize)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = minSize;
            t.resizeTextMaxSize = t.fontSize;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public static Button Button(Transform parent, string n, string label, string colorName, Vector2 anchor, Vector2 pivot,
                                    Vector2 pos, Vector2 size, UnityAction onClick, int fontSize = 26)
        {
            var rt = Rect(parent, n, anchor, pivot, pos, size);
            var img = Img(rt, "btn_" + colorName, Color.white, 18f, 1f);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            cb.normalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(onClick);
            b.onClick.AddListener(() => Sfx.Play("Click", 0.5f));
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(rt, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 3), size);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 10;
                t.resizeTextMaxSize = Mathf.RoundToInt(fontSize * FontScale);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            return b;
        }

        public static Button IconButton(Transform parent, string n, string icon, string colorName, Vector2 anchor, Vector2 pivot,
                                        Vector2 pos, float size, UnityAction onClick, out Image iconImg)
        {
            var rt = Rect(parent, n, anchor, pivot, pos, new Vector2(size, size));
            var img = Img(rt, "sq_" + colorName, Color.white, 18f, 1f);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(onClick);
            b.onClick.AddListener(() => Sfx.Play("Click", 0.5f));
            var ir = Rect(rt, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(size * 0.6f, size * 0.6f));
            iconImg = ir.gameObject.AddComponent<Image>();
            var tex = Resources.Load<Texture2D>("UI/Icons/" + icon);
            if (tex != null) iconImg.sprite = FromTexture(tex);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            return b;
        }


        /// <summary>Buton patrat cu o iconita generata din cod (ItemIcons).</summary>
        public static Button SpriteButton(Transform parent, string n, int iconKind, Color iconCol, string colorName, Vector2 anchor, Vector2 pivot,
                                          Vector2 pos, float size, UnityAction onClick)
        {
            Image ic;
            var b = IconButton(parent, n, "", colorName, anchor, pivot, pos, size, onClick, out ic);
            ic.sprite = ItemIcons.Get(iconKind); ItemIcons.Art(ic);
            if (ItemIcons.IsArt(ic.sprite)) ic.rectTransform.sizeDelta = new Vector2(size * 0.74f, size * 0.74f);
            ic.color = iconCol;
            return b;
        }

        /// <summary>Slider orizontal (0..1).</summary>
        public static Slider MakeSlider(Transform parent, string n, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float value, UnityAction<float> onChange, Color fillCol)
        {
            var rt = Rect(parent, n, anchor, pivot, pos, size);
            var bgRt = Stretch(rt, "Background");
            bgRt.offsetMin = new Vector2(0, size.y * 0.3f); bgRt.offsetMax = new Vector2(0, -size.y * 0.3f);
            Img(bgRt, "flat_Grey", new Color(0.08f, 0.08f, 0.12f, 0.9f), 8f, 3f);
            var fa = Stretch(rt, "Fill Area");
            fa.offsetMin = new Vector2(6, size.y * 0.3f); fa.offsetMax = new Vector2(-6, -size.y * 0.3f);
            var fill = Stretch(fa, "Fill");
            fill.offsetMin = new Vector2(-4, 2); fill.offsetMax = new Vector2(4, -2);
            Img(fill, "flat_Grey", fillCol, 8f, 3f);
            var ha = Stretch(rt, "Handle Area");
            ha.offsetMin = new Vector2(size.y * 0.5f, 0); ha.offsetMax = new Vector2(-size.y * 0.5f, 0);
            var handle = Rect(ha, "Handle", new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.y, size.y));
            var hi = Img(handle, "round_Grey", Color.white, 16f, 1f);
            hi.raycastTarget = true;
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = hi;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f;
            s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            return s;
        }

        /// <summary>Camp de text (ex. numele personajului).</summary>
        public static InputField MakeInput(Transform parent, string n, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, string value, int limit, int fontSize)
        {
            var rt = Rect(parent, n, anchor, pivot, pos, size);
            var bg = Img(rt, "flat_Grey", new Color(0.95f, 0.95f, 1f, 1f), 12f, 1.5f);
            bg.raycastTarget = true;
            var t = Text(rt, "Text", "", fontSize, new Color(0.1f, 0.1f, 0.18f), TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(28, 8), false);
            t.supportRichText = false;
            var ph = Text(rt, "Placeholder", "Enter a name...", fontSize, new Color(0.1f, 0.1f, 0.18f, 0.4f), TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(28, 8), false);
            var f = rt.gameObject.AddComponent<InputField>();
            f.targetGraphic = bg;
            f.textComponent = t;
            f.placeholder = ph;
            f.characterLimit = limit;
            f.lineType = InputField.LineType.SingleLine;
            f.text = value;
            return f;
        }

        /// <summary>Buton comutator ON / OFF.</summary>
        public static Button Toggle(Transform parent, string n, bool on, Vector2 anchor, Vector2 pivot, Vector2 pos, UnityAction onClick)
        {
            return Button(parent, n, on ? "ON" : "OFF", on ? "Green" : "Grey", anchor, pivot, pos, new Vector2(120, 50), onClick, 20);
        }

        /// <summary>Fereastra standard: fundal intunecat + panou + titlu + buton X. Intoarce zona de continut.</summary>
        public static RectTransform Window(Transform parent, string n, string title, Vector2 size, string headerColor, UnityAction onClose, out RectTransform dim, out Text titleText)
        {
            // stil: panou gri-inchis simplu (fara dungi), semi-transparent, chenar deschis, titlul intr-o "eticheta" deasupra, X rosu in colt
            Vector2 C = new Vector2(0.5f, 0.5f), TC = new Vector2(0.5f, 1);
            dim = Stretch(parent, n);
            var di = dim.gameObject.AddComponent<Image>();
            di.color = new Color(0, 0, 0, 0.5f);
            di.raycastTarget = true;
            // click in afara ferestrei = inchide fereastra
            var db = dim.gameObject.AddComponent<UnityEngine.UI.Button>();
            db.transition = Selectable.Transition.None;
            if (onClose != null) db.onClick.AddListener(onClose);
            var panel = Rect(dim, "Panel", C, C, new Vector2(0, -20), size);
            Img(panel, "panel_dark", Color.white, 26f, 1f).raycastTarget = true;
            float tw = Mathf.Clamp(title.Length * 26f + 90f, 300f, size.x - 220f);
            var hr = Rect(panel, "Header", TC, C, new Vector2(0, 6), new Vector2(tw, 84));
            Img(hr, "tab_title", Color.white, 22f, 1f);
            titleText = Text(hr, "Title", title, 44, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(tw - 30, 70));
            titleText.resizeTextForBestFit = true; titleText.resizeTextMinSize = 20; titleText.resizeTextMaxSize = Mathf.RoundToInt(44 * FontScale);
            var close = Button(panel, "Close", "X", "Red", new Vector2(1, 1), C, new Vector2(-14, -2), new Vector2(84, 84), onClose, 46);
            close.transform.localRotation = Quaternion.Euler(0, 0, -4f);
            var content = Rect(panel, "Content", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -60), new Vector2(size.x, size.y - 60));
            return content;
        }

        /// <summary>Culoarea unei carduri in tema gri: culorile vechi (albastru inchis) devin gri, cele "cu sens" (rosu boss, verde etc.) raman usor colorate.</summary>
        public static Color Theme(Color c)
        {
            float l = c.r * 0.3f + c.g * 0.5f + c.b * 0.2f;
            float sat = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            Color grey = new Color(1f, 1f, 1f, c.a);
            // cardurile tipice (0.2,0.22,0.34) au saturatie mica -> gri neutru; cele foarte colorate pastreaza nuanta
            Color tint = Color.Lerp(Color.white, new Color(c.r / Mathf.Max(0.01f, l), c.g / Mathf.Max(0.01f, l), c.b / Mathf.Max(0.01f, l)) * 0.85f, Mathf.Clamp01((sat - 0.12f) * 2.5f));
            tint.a = c.a;
            return tint;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>Bara (HP/XP) cu fundal, umplere si text.</summary>
        public class Bar
        {
            public RectTransform root;
            public Image fill, back, ghost;
            public Text label;
            float ghostPct = 1f;

            public void Set(float pct, string txt)
            {
                pct = Mathf.Clamp01(pct);
                fill.fillAmount = pct;
                if (ghost != null)
                {
                    if (pct > ghostPct) ghostPct = pct;
                    else ghostPct = Mathf.MoveTowards(ghostPct, pct, Time.unscaledDeltaTime * 0.6f);
                    ghost.fillAmount = ghostPct;
                }
                if (label != null && txt != null && label.text != txt) label.text = txt;
            }
        }

        public static Bar MakeBar(Transform parent, string n, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color fillCol, int fontSize, bool ghost = true)
        {
            var bar = new Bar();
            bar.root = Rect(parent, n, anchor, pivot, pos, size);
            bar.back = Img(bar.root, "flat_Grey", new Color(0.08f, 0.08f, 0.12f, 0.85f), 12f, 2f);
            if (ghost)
            {
                var gr = Stretch(bar.root, "Ghost", 4f);
                bar.ghost = gr.gameObject.AddComponent<Image>();
                bar.ghost.sprite = Sprite("flat_Grey", 12f);
                bar.ghost.type = Image.Type.Filled;
                bar.ghost.fillMethod = Image.FillMethod.Horizontal;
                bar.ghost.color = new Color(1f, 1f, 1f, 0.65f);
                bar.ghost.raycastTarget = false;
            }
            var fr = Stretch(bar.root, "Fill", 4f);
            bar.fill = fr.gameObject.AddComponent<Image>();
            bar.fill.sprite = Sprite("flat_Grey", 12f);
            bar.fill.type = Image.Type.Filled;
            bar.fill.fillMethod = Image.FillMethod.Horizontal;
            bar.fill.color = fillCol;
            bar.fill.raycastTarget = false;
            // luciu
            var shine = Rect(fr, "Shine", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -2), new Vector2(0, size.y * 0.3f));
            shine.anchorMin = new Vector2(0, 1); shine.anchorMax = new Vector2(1, 1);
            shine.sizeDelta = new Vector2(-8, size.y * 0.28f);
            var si = shine.gameObject.AddComponent<Image>();
            si.color = new Color(1f, 1f, 1f, 0.22f);
            si.raycastTarget = false;
            if (fontSize > 0)
                bar.label = Text(bar.root, "Label", "", fontSize, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 1), size, true, true);
            return bar;
        }
    }

    /// <summary>Iconite de item desenate procedural (sabie, coif, armura, inel, moneda, minereu).</summary>
    public static class ItemIcons
    {
        static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
        const int N = 64;

        /// <summary>Iconita potrivita pentru item (armele au iconite dupa tip).</summary>
        public static Sprite For(Item it)
        {
            if (it == null) return Get(0);
            // iconita randata din modelul 3D al itemului (arata exact ca itemul purtat)
            var g = GearIcons.Get(it);
            if (g != null) return g;
            if (it.S != Slot.Weapon) return Get(it.slot);
            var k = ItemDB.KindOf(it);
            switch (k)
            {
                case ItemDB.WeaponKind.Axe: return Get(11);
                case ItemDB.WeaponKind.Hammer: return Get(12);
                case ItemDB.WeaponKind.Spear: return Get(13);
                case ItemDB.WeaponKind.Saber: return Get(14);
                case ItemDB.WeaponKind.Greatsword: return Get(15);
                default: return Get(0);
            }
        }

        // ---- iconite colorate (Noto Emoji, Apache 2.0) din Resources/UI/IconArt/<kind>.png ----
        static Material artMat;
        /// <summary>Materialul pentru iconitele colorate: tint-ul Image-ului nu le mai recoloreaza (doar alpha / intunecare).</summary>
        public static Material ArtMaterial
        {
            get
            {
                if (artMat == null)
                {
                    var sh = Resources.Load<Shader>("Shaders/UIIconArt");
                    if (sh != null) artMat = new Material(sh) { name = "UIIconArt" };
                }
                return artMat;
            }
        }

        public static bool IsArt(Sprite s) { return s != null && s.name.StartsWith("art_"); }

        /// <summary>Pune materialul potrivit pe Image dupa ce i-ai dat o iconita (colorata sau procedurala).</summary>
        public static void Art(Image img)
        {
            if (img == null) return;
            img.material = IsArt(img.sprite) ? ArtMaterial : null;
            if (IsArt(img.sprite)) img.preserveAspect = true;
        }

        public static Sprite Get(int kind)
        {
            Sprite s;
            if (cache.TryGetValue(kind, out s) && s != null) return s;
            var art = Resources.Load<Texture2D>("UI/IconArt/" + kind);
            if (art != null)
            {
                art.wrapMode = TextureWrapMode.Clamp;
                s = UnityEngine.Sprite.Create(art, new Rect(0, 0, art.width, art.height), new Vector2(0.5f, 0.5f), 100f);
                s.name = "art_" + kind;
                cache[kind] = s;
                return s;
            }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float a = Shape(kind, u, v);
                    // contur inchis
                    float o = Mathf.Max(Shape(kind, u + 0.06f, v), Mathf.Max(Shape(kind, u - 0.06f, v), Mathf.Max(Shape(kind, u, v + 0.06f), Shape(kind, u, v - 0.06f))));
                    float shade = 0.75f + 0.25f * Mathf.Clamp01(v * 0.5f + 0.5f);
                    Color c = Color.Lerp(new Color(0.1f, 0.08f, 0.15f, o), new Color(shade, shade, shade, 1f), a);
                    px[y * N + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            cache[kind] = s;
            return s;
        }

        static float Box(float u, float v, float cx, float cy, float hw, float hh)
        {
            return (Mathf.Abs(u - cx) <= hw && Mathf.Abs(v - cy) <= hh) ? 1f : 0f;
        }

        static float Circle(float u, float v, float cx, float cy, float r)
        {
            float d = Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy));
            return Mathf.Clamp01((r - d) * 30f);
        }

        /// <summary>0=sabie 1=coif 2=armura 3=inel 4=moneda 5=minereu 6=pet 7=cadou 8=portal 9=shard 10=punga (shop)</summary>
        static float Shape(int k, float u, float v)
        {
            switch (k)
            {
                case 0:
                {
                    // sabie diagonala
                    float c = 0.7071f;
                    float a = (u + v) * c, b = (v - u) * c;   // a de-a lungul lamei
                    float blade = (a > -0.35f && a < 0.8f && Mathf.Abs(b) < 0.1f - Mathf.Max(0, a - 0.62f) * 0.5f) ? 1f : 0f;
                    float guard = (Mathf.Abs(a + 0.38f) < 0.06f && Mathf.Abs(b) < 0.3f) ? 1f : 0f;
                    float grip = (a < -0.38f && a > -0.72f && Mathf.Abs(b) < 0.06f) ? 1f : 0f;
                    float pommel = Circle(u, v, -0.58f, -0.58f, 0.1f);
                    return Mathf.Max(Mathf.Max(blade, guard), Mathf.Max(grip, pommel));
                }
                case 1:
                {
                    float dome = (v > -0.2f && u * u / 0.55f + (v + 0.2f) * (v + 0.2f) / 0.5f < 1f) ? 1f : 0f;
                    float rim = Box(u, v, 0f, -0.28f, 0.8f, 0.1f);
                    float cheek = Box(u, v, -0.5f, -0.5f, 0.12f, 0.2f) + Box(u, v, 0.5f, -0.5f, 0.12f, 0.2f);
                    float slit = Box(u, v, 0f, -0.05f, 0.45f, 0.05f);
                    return Mathf.Clamp01(Mathf.Max(dome, Mathf.Max(rim, cheek)) - slit);
                }
                case 2:
                {
                    float body = Box(u, v, 0f, -0.1f, 0.45f, 0.6f);
                    float sh = Box(u, v, 0f, 0.4f, 0.75f, 0.15f);
                    float neck = Circle(u, v, 0f, 0.62f, 0.2f);
                    float belt = Box(u, v, 0f, -0.35f, 0.46f, 0.05f);
                    return Mathf.Clamp01(Mathf.Max(body, sh) - neck - belt * 0.4f);
                }
                case 3:
                {
                    float ring = Circle(u, v, 0f, -0.15f, 0.55f) - Circle(u, v, 0f, -0.15f, 0.36f);
                    float gem = Circle(u, v, 0f, 0.48f, 0.22f);
                    return Mathf.Clamp01(Mathf.Max(ring, gem));
                }
                case 4:
                    return Mathf.Clamp01(Circle(u, v, 0, 0, 0.75f) - 0.35f * Box(u, v, 0, 0, 0.12f, 0.4f));
                case 5:
                {
                    float d = Mathf.Abs(u) * 1.3f + Mathf.Abs(v);
                    return d < 0.8f ? 1f : 0f;
                }
                case 6:
                {
                    float head = Circle(u, v, 0f, -0.1f, 0.6f);
                    float earL = Circle(u, v, -0.45f, 0.5f, 0.2f), earR = Circle(u, v, 0.45f, 0.5f, 0.2f);
                    float eyes = Circle(u, v, -0.22f, 0f, 0.09f) + Circle(u, v, 0.22f, 0f, 0.09f);
                    return Mathf.Clamp01(Mathf.Max(head, Mathf.Max(earL, earR)) - eyes);
                }
                case 16:
                {
                    // piatra de forja: bolovan fatetat cu o nervura stralucitoare
                    float d = Mathf.Abs(u * 0.9f) + Mathf.Abs(v + 0.05f) * 1.2f + Mathf.Abs(u + v) * 0.25f;
                    float vein = Mathf.Abs(v - u * 0.5f) < 0.05f && d < 0.6f ? 0.35f : 0f;
                    return Mathf.Clamp01((d < 0.78f ? 1f : 0f) - vein);
                }
                case 17:
                {
                    // mantie (cloak): gluga + pelerina evazata
                    float hood = Circle(u, v, 0f, 0.45f, 0.3f);
                    float capeW = 0.25f + (0.45f - v) * 0.45f;
                    float cape = (v < 0.45f && v > -0.85f && Mathf.Abs(u) < capeW) ? 1f : 0f;
                    float face = Circle(u, v, 0f, 0.4f, 0.15f);
                    float fold = Mathf.Abs(u) < 0.03f && v < 0.1f ? 0.4f : 0f;
                    return Mathf.Clamp01(Mathf.Max(hood, cape) - face - fold);
                }
                case 18:
                {
                    // potiune: flacon rotund + gat + dop
                    float body = Circle(u, v, 0f, -0.25f, 0.55f);
                    float neck = Box(u, v, 0f, 0.4f, 0.14f, 0.2f);
                    float cork = Box(u, v, 0f, 0.68f, 0.2f, 0.09f);
                    float shine = Circle(u, v, -0.2f, -0.05f, 0.1f) * 0.5f;
                    return Mathf.Clamp01(Mathf.Max(body, Mathf.Max(neck, cork)) - shine);
                }
                case 19:
                {
                    // token: moneda hexagonala cu stea
                    float ang = Mathf.Atan2(v, u), r = Mathf.Sqrt(u * u + v * v);
                    float hex = 0.78f / Mathf.Cos(((ang + Mathf.PI / 6f) % (Mathf.PI / 3f) + Mathf.PI / 3f) % (Mathf.PI / 3f) - Mathf.PI / 6f);
                    float coin = r < hex ? 1f : 0f;
                    float star = r < 0.42f * (0.6f + 0.4f * Mathf.Cos(ang * 5f)) ? 0.45f : 0f;
                    float ringc = (r < hex * 0.82f && r > hex * 0.74f) ? 0.3f : 0f;
                    return Mathf.Clamp01(coin - star - ringc);
                }
                case 20:
                {
                    // quest: pergament cu semn de exclamare
                    float paper = Box(u, v, 0f, 0f, 0.5f, 0.62f);
                    float rolls = Circle(u, v, 0f, 0.64f, 0.14f) * 0f + Box(u, v, 0f, 0.66f, 0.6f, 0.09f) + Box(u, v, 0f, -0.66f, 0.6f, 0.09f);
                    float mark = Box(u, v, 0f, 0.12f, 0.08f, 0.28f) + Circle(u, v, 0f, -0.34f, 0.09f);
                    return Mathf.Clamp01(Mathf.Max(paper, rolls) - mark * 0.7f);
                }
                case 21:
                {
                    // strangere de mana: doua palme care se intalnesc
                    float c = 0.7071f;
                    float a = (u - v) * c, b = (u + v) * c;
                    float left = Box(a, b, -0.25f, -0.05f, 0.42f, 0.2f);
                    float right = Box(a, b, 0.25f, 0.12f, 0.42f, 0.2f);
                    float cuffL = Box(u, v, -0.68f, -0.05f, 0.14f, 0.3f), cuffR = Box(u, v, 0.68f, -0.05f, 0.14f, 0.3f);
                    float thumb = Circle(u, v, 0.05f, 0.3f, 0.13f);
                    return Mathf.Clamp01(Mathf.Max(Mathf.Max(left, right), Mathf.Max(Mathf.Max(cuffL, cuffR), thumb)));
                }
                case 22:
                {
                    // flacara (POWER): picatura intoarsa cu varf ondulat + flacara interioara
                    float vv = v + 0.2f;
                    float w = vv < 0f ? Mathf.Sqrt(Mathf.Max(0f, 0.36f - vv * vv)) : 0.6f * Mathf.Pow(Mathf.Max(0f, 1f - vv / 0.95f), 1.4f);
                    float outer = (Mathf.Abs(u + 0.1f * vv * vv) < w && vv > -0.6f) ? 1f : 0f;
                    float iv = v + 0.4f;
                    float iw = iv < 0f ? Mathf.Sqrt(Mathf.Max(0f, 0.06f - iv * iv)) : 0.25f * Mathf.Max(0f, 1f - iv / 0.5f);
                    float inner = (Mathf.Abs(u) < iw && iv > -0.25f) ? 0.35f : 0f;
                    return Mathf.Clamp01(outer - inner);
                }
                case 23:
                {
                    // ou cu pete
                    float ey = v < 0f ? v / 0.62f : v / 0.85f;
                    float egg = (u * u / 0.36f + ey * ey) < 1f ? 1f : 0f;
                    float spots = Circle(u, v, -0.2f, 0.2f, 0.12f) + Circle(u, v, 0.22f, -0.15f, 0.14f) + Circle(u, v, 0.05f, 0.55f, 0.08f);
                    return Mathf.Clamp01(egg - Mathf.Min(1f, spots) * 0.35f);
                }
                case 24:
                {
                    // magnet (potcoava)
                    float r = Mathf.Sqrt(u * u + (v + 0.05f) * (v + 0.05f));
                    float arc = (r < 0.72f && r > 0.34f && v < -0.05f) ? 1f : 0f;
                    float legs = Box(u, v, -0.53f, 0.3f, 0.19f, 0.36f) + Box(u, v, 0.53f, 0.3f, 0.19f, 0.36f);
                    float tips = Box(u, v, -0.53f, 0.58f, 0.19f, 0.09f) + Box(u, v, 0.53f, 0.58f, 0.19f, 0.09f);
                    return Mathf.Clamp01(Mathf.Max(arc, Mathf.Min(1f, legs)) - Mathf.Min(1f, tips) * 0.45f);
                }
                case 25:
                {
                    // sabie + sageata circulara (AUTO SWING)
                    float r = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u);
                    float ring = (r < 0.88f && r > 0.72f && Mathf.Repeat(ang + 0.4f, Mathf.PI) > 0.55f) ? 1f : 0f;
                    float c = 0.7071f;
                    float a = (u + v) * c, bb = (v - u) * c;
                    float blade = (a > -0.3f && a < 0.55f && Mathf.Abs(bb) < 0.08f) ? 1f : 0f;
                    float guard = (Mathf.Abs(a + 0.3f) < 0.05f && Mathf.Abs(bb) < 0.2f) ? 1f : 0f;
                    float grip = (a > -0.55f && a < -0.3f && Mathf.Abs(bb) < 0.05f) ? 1f : 0f;
                    return Mathf.Max(ring, Mathf.Max(blade, Mathf.Max(guard, grip)));
                }
                case 26:
                {
                    // stea (favorite)
                    float ang = Mathf.Atan2(u, v), r = Mathf.Sqrt(u * u + v * v);
                    float seg = Mathf.Repeat(ang, 2f * Mathf.PI / 5f) - Mathf.PI / 5f;   // -36..36 grade
                    float t = Mathf.Abs(seg) / (Mathf.PI / 5f);                           // 0 = varf, 1 = intre varfuri
                    float lim = Mathf.Lerp(0.85f, 0.36f, t);
                    return r < lim ? 1f : 0f;
                }
                case 11: case 12: case 13: case 14: case 15:
                {
                    float c = 0.7071f;
                    float a = (u + v) * c, bb = (v - u) * c;   // a = de-a lungul armei (jos-stanga -> sus-dreapta)
                    if (k == 13)
                    {
                        float shaft = (a > -0.95f && a < 0.5f && Mathf.Abs(bb) < 0.05f) ? 1f : 0f;
                        float head = (a >= 0.45f && a < 0.95f && Mathf.Abs(bb) < 0.16f * (0.95f - a) / 0.5f + 0.02f) ? 1f : 0f;
                        float ring = (Mathf.Abs(a - 0.42f) < 0.04f && Mathf.Abs(bb) < 0.1f) ? 1f : 0f;
                        return Mathf.Max(shaft, Mathf.Max(head, ring));
                    }
                    if (k == 11 || k == 12)
                    {
                        float shaft = (a > -0.85f && a < 0.7f && Mathf.Abs(bb) < 0.06f) ? 1f : 0f;
                        float headT;
                        if (k == 11) headT = (a > 0.25f && a < 0.72f && bb > 0f && bb < 0.5f - Mathf.Abs(a - 0.48f) * 0.6f) ? 1f : 0f;
                        else headT = (a > 0.35f && a < 0.75f && Mathf.Abs(bb) < 0.38f) ? 1f : 0f;
                        return Mathf.Max(shaft, headT);
                    }
                    // 14 saber (curbata), 15 sabie mare
                    float w = k == 15 ? 0.15f : 0.08f;
                    float bend = k == 14 ? (a + 0.3f) * (a + 0.3f) * 0.28f : 0f;
                    float blade = (a > -0.3f && a < 0.85f && Mathf.Abs(bb - bend) < w - Mathf.Max(0, a - 0.65f) * 0.5f) ? 1f : 0f;
                    float guard = (Mathf.Abs(a + 0.35f) < 0.06f && Mathf.Abs(bb) < (k == 15 ? 0.38f : 0.26f)) ? 1f : 0f;
                    float grip = (a < -0.35f && a > -0.72f && Mathf.Abs(bb) < 0.06f) ? 1f : 0f;
                    float pommel = Circle(u, v, -0.56f, -0.56f, 0.1f);
                    return Mathf.Max(Mathf.Max(blade, guard), Mathf.Max(grip, pommel));
                }
                case 8:
                {
                    // portal: arcada + vartej
                    float arch = Mathf.Clamp01(Circle(u, v, 0f, 0.05f, 0.78f) - Circle(u, v, 0f, 0.05f, 0.56f));
                    float legs = Box(u, v, -0.67f, -0.45f, 0.11f, 0.4f) + Box(u, v, 0.67f, -0.45f, 0.11f, 0.4f);
                    float r = Mathf.Sqrt(u * u + (v - 0.05f) * (v - 0.05f)), ang = Mathf.Atan2(v - 0.05f, u);
                    float swirl = r < 0.45f && Mathf.Sin(ang * 2f + r * 14f) > 0.2f ? 0.8f : 0f;
                    float cut = v < -0.8f ? 1f : 0f;
                    return Mathf.Clamp01(Mathf.Max(Mathf.Max(arch, legs), swirl) - cut);
                }
                case 9:
                {
                    // shard: cristal ascutit
                    float d = Mathf.Abs(u) * 2.1f + Mathf.Abs(v - 0.05f) * 1.05f;
                    float facet = Mathf.Abs(u) < 0.04f && v > -0.6f ? 0.35f : 0f;
                    return Mathf.Clamp01((d < 0.95f ? 1f : 0f) - facet);
                }
                case 10:
                {
                    // punga de cumparaturi
                    float bag = Box(u, v, 0f, -0.2f, 0.62f, 0.55f);
                    float handle = Mathf.Clamp01(Circle(u, v, 0f, 0.38f, 0.36f) - Circle(u, v, 0f, 0.38f, 0.24f)) * (v > 0.38f ? 1f : 0f);
                    float coin = Circle(u, v, 0f, -0.22f, 0.22f);
                    return Mathf.Clamp01(Mathf.Max(bag, handle) - coin * 0.4f);
                }
                default:
                {
                    float box = Box(u, v, 0f, -0.2f, 0.6f, 0.5f);
                    float lid = Box(u, v, 0f, 0.38f, 0.7f, 0.14f);
                    float bow = Circle(u, v, -0.2f, 0.62f, 0.16f) + Circle(u, v, 0.2f, 0.62f, 0.16f);
                    float rib = Box(u, v, 0f, 0f, 0.1f, 0.8f);
                    return Mathf.Clamp01(Mathf.Max(Mathf.Max(box, lid), bow) - rib * 0.35f);
                }
            }
        }
    }
}
