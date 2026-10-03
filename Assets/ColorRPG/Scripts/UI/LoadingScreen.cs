// =====================================================================
//  LoadingScreen.cs - ecranele de incarcare (pe tema jocului):
//   * la intrarea in joc (logo HUEBORN, eroi + iconite care plutesc)
//   * cand pleci spre alta harta (culorile hartii + numele ei)
//   * cand pleci spre Trade Plaza (auriu, monede si tokens)
//  Are fundal in degrade, iconite care plutesc, un portal care se roteste
//  cu o iconita care sare in mijloc, bara de progres si un sfat (TIP).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class LoadingScreen : MonoBehaviour
    {
        static LoadingScreen I;
        /// <summary>true cat timp un ecran de incarcare e pe ecran.</summary>
        public static bool Active { get { return I != null && I.t < I.dur; } }

        static readonly string[] Tips =
        {
            "Use your hero's 3 skills (keys 1 / 2 / 3 or tap them).",
            "Every hero has different skills - try them all in CHARACTER.",
            "Forge Stones drop from monsters and bosses.",
            "Turn on PROTECT in the FORGE to keep your item level on a fail.",
            "Eggs from harder maps hatch stronger pets.",
            "Potions stack up to 60 minutes - drink them from INVENTORY.",
            "Claim a booth in the Trade Plaza, set your prices and earn TOKENS!",
            "Pick your Awakening class carefully - changing it later costs a lot!",
            "STATS: you can reset your skill points for FREE once every 24 hours.",
            "Titles unlock as you play - pick yours from the INVENTORY.",
            "Rebirth near the boss arena gives a permanent damage bonus.",
            "Click faster to attack faster!",
            "Quests give potions, Forge Stones and Guardian Cloaks.",
            "Check the GUIDE (?) if you're stuck.",
        };

        Canvas canvas;
        CanvasGroup cg;
        RectTransform ring, ring2, center, barFill;
        Text title, sub, tip, pct;
        readonly List<RectTransform> floaters = new List<RectTransform>();
        readonly List<Vector2> floatVel = new List<Vector2>();
        float t, dur;
        int[] icons;

        // ---------------------------------------------------------------
        /// <summary>Ecranul de la pornirea jocului.</summary>
        public static void ShowStart()
        {
            Show("HUEBORN", "LOOT LEGENDS  -  SLAY  -  FORGE  -  HATCH", new Color(0.35f, 0.3f, 0.75f), 4.1f, new[] { 0, 2, 23, 30, 9, 15, 6, 4 }, 26);
        }

        /// <summary>Ecranul cand pleci spre o harta.</summary>
        public static void ShowMap(int map)
        {
            var m = GameData.Maps[Mathf.Clamp(map, 0, GameData.Maps.Length - 1)];
            string s = map == 0 ? "MAIN HUB" : "LEVEL " + m.req + "+";
            Show(m.name.ToUpper(), s, Color.Lerp(m.ground, m.border, 0.35f), 2.2f, new[] { 0, 5, 23, 4, 2, 9 }, 8);
        }

        /// <summary>Ecranul cand pleci spre Trade Plaza.</summary>
        public static void ShowPlaza(string market)
        {
            Show("TRADE PLAZA", market.ToUpper(), new Color(0.85f, 0.6f, 0.2f), 2.2f, new[] { 19, 4, 21, 27, 9, 6 }, 21);
        }

        /// <summary>Fara ecrane de incarcare (filmarea pentru reclama).</summary>
        public static bool Suppress;

        static void Show(string title, string sub, Color theme, float dur, int[] icons, int centerIcon)
        {
            if (Suppress) return;
            if (I == null)
            {
                var go = new GameObject("LoadingScreen");
                go.transform.SetParent(SceneGroups.Get("Systems"), false);
                I = go.AddComponent<LoadingScreen>();
                I.Build();
            }
            I.Begin(title, sub, theme, dur, icons, centerIcon);
        }

        // ---------------------------------------------------------------
        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();   // blocheaza click-urile cat timp se incarca
            cg = gameObject.AddComponent<CanvasGroup>();
        }

        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            floaters.Clear(); floatVel.Clear();
        }

        void Begin(string titleS, string subS, Color theme, float d, int[] ic, int centerIcon)
        {
            Clear();
            gameObject.SetActive(true);
            t = 0f; dur = d; icons = ic;
            var root = (RectTransform)transform;
            Vector2 C = new Vector2(0.5f, 0.5f);

            // fundal: degrade vertical pe culoarea temei + vigneta
            var bg = UIKit.Stretch(root, "BG").gameObject.AddComponent<RawImage>();
            bg.texture = Gradient(Color.Lerp(theme, Color.white, 0.12f), Color.Lerp(theme, Color.black, 0.72f));
            bg.raycastTarget = true;
            var vig = UIKit.Stretch(root, "Vignette").gameObject.AddComponent<RawImage>();
            vig.texture = Vignette();
            vig.raycastTarget = false;

            // iconite care plutesc in fundal
            for (int i = 0; i < 26; i++)
            {
                var r = UIKit.Rect(root, "F" + i, C, C, new Vector2(Random.Range(-1000f, 1000f), Random.Range(-600f, 600f)), Vector2.one * Random.Range(60f, 130f));
                var im = r.gameObject.AddComponent<Image>();
                im.sprite = ItemIcons.Get(icons[i % icons.Length]); ItemIcons.Art(im);
                im.color = new Color(1f, 1f, 1f, Random.Range(0.08f, 0.2f));
                im.raycastTarget = false;
                r.localRotation = Quaternion.Euler(0, 0, Random.Range(-25f, 25f));
                floaters.Add(r);
                floatVel.Add(new Vector2(Random.Range(-25f, 25f), Random.Range(30f, 80f)));
            }

            // portalul din mijloc: doua inele care se rotesc + iconita care sare
            var halo = UIKit.Rect(root, "Halo", C, C, new Vector2(0, 90), new Vector2(520, 520)).gameObject.AddComponent<RawImage>();
            halo.texture = Glow(); halo.color = Art.A(Color.Lerp(theme, Color.white, 0.5f), 0.55f); halo.raycastTarget = false;
            ring = UIKit.Rect(root, "Ring", C, C, new Vector2(0, 90), new Vector2(330, 330));
            var ri = ring.gameObject.AddComponent<RawImage>(); ri.texture = RingTex(8, 0.82f); ri.color = Color.Lerp(theme, Color.white, 0.6f); ri.raycastTarget = false;
            ring2 = UIKit.Rect(root, "Ring2", C, C, new Vector2(0, 90), new Vector2(250, 250));
            var ri2 = ring2.gameObject.AddComponent<RawImage>(); ri2.texture = RingTex(5, 0.78f); ri2.color = new Color(1f, 0.95f, 0.75f, 0.9f); ri2.raycastTarget = false;
            center = UIKit.Rect(root, "Center", C, C, new Vector2(0, 90), new Vector2(150, 150));
            var ci = center.gameObject.AddComponent<Image>();
            ci.sprite = ItemIcons.Get(centerIcon); ItemIcons.Art(ci); ci.raycastTarget = false;

            // titlu + subtitlu
            title = UIKit.Text(root, "Title", titleS, 96, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, -150), new Vector2(1600, 120));
            var sh = title.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(4, -6);
            sub = UIKit.Text(root, "Sub", subS, 30, Color.Lerp(theme, Color.white, 0.75f), TextAnchor.MiddleCenter, C, C, new Vector2(0, -228), new Vector2(1600, 44));

            // bara de progres + TIP
            var bar = UIKit.Rect(root, "Bar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(900, 30));
            UIKit.Img(bar, "flat_Grey", new Color(0.05f, 0.05f, 0.1f, 0.8f), 12f, 1.5f);
            barFill = UIKit.Rect(bar, "Fill", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(0, 22));
            UIKit.Img(barFill, "flat_Grey", Color.Lerp(theme, new Color(1f, 0.9f, 0.4f), 0.6f), 10f, 1.5f);
            pct = UIKit.Text(bar, "P", "LOADING", 20, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 1), new Vector2(900, 30));
            tip = UIKit.Text(root, "Tip", "TIP:  " + Tips[Random.Range(0, Tips.Length)], 24, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter,
                             new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(1500, 40));
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
        }

        void Update()
        {
            if (dur <= 0f) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            float k = Mathf.Clamp01(t / dur);
            // intra instant (acopera teleportul), iese cu fade
            const float fadeOut = 0.4f;
            cg.alpha = t > dur - fadeOut ? Mathf.Clamp01((dur - t) / fadeOut) : 1f;
            if (ring != null) ring.localRotation = Quaternion.Euler(0, 0, -t * 70f);
            if (ring2 != null) ring2.localRotation = Quaternion.Euler(0, 0, t * 120f);
            if (center != null)
            {
                float b = Mathf.Abs(Mathf.Sin(t * 5.2f));
                center.anchoredPosition = new Vector2(0, 90 + b * 34f);
                center.localScale = new Vector3(1f + (1f - b) * 0.12f, 1f - (1f - b) * 0.1f, 1f);
            }
            if (title != null) title.rectTransform.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(t * 3f)) * Mathf.Lerp(1.25f, 1f, Mathf.Clamp01(t / 0.3f));
            for (int i = 0; i < floaters.Count; i++)
            {
                var r = floaters[i];
                Vector2 p = r.anchoredPosition + floatVel[i] * dt;
                if (p.y > 640f) p = new Vector2(Random.Range(-1000f, 1000f), -640f);
                r.anchoredPosition = p;
                r.Rotate(0, 0, floatVel[i].x * 0.4f * dt);
            }
            // progres: rapid la inceput, incetineste, se umple inainte de fade
            float prog = Mathf.Clamp01(1f - Mathf.Pow(1f - Mathf.Clamp01(t / Mathf.Max(0.1f, dur - fadeOut)), 2.2f));
            if (barFill != null) barFill.sizeDelta = new Vector2(892f * prog, 22);
            if (pct != null) pct.text = prog >= 1f ? "READY!" : "LOADING  " + Mathf.RoundToInt(prog * 100) + "%";
            if (t >= dur)
            {
                dur = 0f;
                cg.blocksRaycasts = false;
                Clear();
                gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------
        //  Texturi generate (o singura data)
        // ---------------------------------------------------------------
        static Texture2D vigTex, glowTex;
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        public static Texture2D Gradient(Color top, Color bottom)
        {
            var tex = new Texture2D(2, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++)
            {
                Color c = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / 127f));
                tex.SetPixel(0, y, c); tex.SetPixel(1, y, c);
            }
            tex.Apply();
            return tex;
        }

        public static Texture2D Vignette()
        {
            if (vigTex != null) return vigTex;
            const int N = 128;
            vigTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (N - 1f) * 2f - 1f, v = y / (N - 1f) * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v) / 1.414f;
                    vigTex.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0.25f, 1f, d) * 0.7f));
                }
            vigTex.Apply();
            return vigTex;
        }

        static Texture2D Glow()
        {
            if (glowTex != null) return glowTex;
            const int N = 128;
            glowTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (N - 1f) * 2f - 1f, v = y / (N - 1f) * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    glowTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
                }
            glowTex.Apply();
            return glowTex;
        }

        /// <summary>Inel impartit in "segs" segmente (ca un portal rotativ).</summary>
        static Texture2D RingTex(int segs, float inner)
        {
            string key = segs + "_" + inner;
            Texture2D tex;
            if (texCache.TryGetValue(key, out tex) && tex != null) return tex;
            const int N = 256;
            tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Clamp01((1f - d) * 60f) * Mathf.Clamp01((d - inner) * 60f);
                    float ang = Mathf.Atan2(v, u) / (Mathf.PI * 2f) + 0.5f;
                    float seg = Mathf.Repeat(ang * segs, 1f);
                    a *= Mathf.Clamp01((0.78f - Mathf.Abs(seg - 0.5f) * 2f) * 12f);   // goluri intre segmente
                    float shade = 0.75f + 0.25f * Mathf.Clamp01((d - inner) / (1f - inner));
                    tex.SetPixel(x, y, new Color(shade, shade, shade, a));
                }
            tex.Apply();
            texCache[key] = tex;
            return tex;
        }
    }
}
