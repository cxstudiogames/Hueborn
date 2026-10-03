// =====================================================================
//  GearIcons.cs - iconitele itemelor (arme, coifuri, armuri, inele) sunt
//  randate din modelele 3D reale (GearVisuals), intr-un "studio" ascuns sub
//  lume. Asa iconita din inventar arata exact ca itemul purtat pe erou.
//  Iconitele se tin in cache dupa cheia vizuala a itemului.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class GearIcons
    {
        const int Size = 128;
        public const int Layer = 31;                                  // acelasi layer ascuns ca portretul (alt loc)
        static readonly Vector3 Stage = new Vector3(0f, -900f, 0f);
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Camera cam;
        static RenderTexture rt;
        static Transform studio;
        static bool failed;

        /// <summary>Dimensiunile capului / trunchiului eroului curent (le seteaza Player), ca iconitele sa aiba proportiile reale.</summary>
        public static Vector3 HeadSize = new Vector3(0.78f, 0.78f, 0.74f);
        public static Vector3 TorsoSize = new Vector3(0.56f, 0.56f, 0.4f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); cam = null; rt = null; studio = null; failed = false; }

        /// <summary>Iconita randata a itemului (null daca randarea nu e posibila).</summary>
        public static Sprite Get(Item it)
        {
            if (it == null || failed) return null;
            string key = GearVisuals.VisualKey(it);
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            try { s = Render(it); }
            catch (System.Exception e) { Debug.LogWarning("GearIcons: " + e.Message); failed = true; return null; }
            if (s != null) { s.name = "art_gear_" + key; cache[key] = s; }
            return s;
        }

        static void EnsureStudio()
        {
            if (studio != null && cam != null) return;
            var go = new GameObject("GearIconStudio");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            studio = go.transform;
            studio.position = Stage;
            rt = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32) { name = "GearIconRT", antiAliasing = 4 };
            var cgo = new GameObject("GearIconCam");
            cgo.transform.SetParent(studio, false);
            cam = cgo.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.orthographic = true;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;
            cam.targetTexture = rt;
            try
            {
                var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
                if (data != null) { data.renderPostProcessing = false; data.renderShadows = false; }
            }
            catch (System.Exception) { }
            // doua lumini proprii (cheie + umplere), doar pentru layer-ul studioului
            AddLight(new Vector3(2.5f, 4f, 4f), 3.2f, new Color(1f, 0.96f, 0.9f));
            AddLight(new Vector3(-4f, 1f, 2f), 1.4f, new Color(0.75f, 0.85f, 1f));
            AddLight(new Vector3(0f, 3f, -4f), 1.2f, Color.white);
        }

        static void AddLight(Vector3 local, float intensity, Color c)
        {
            var lg = new GameObject("Light");
            lg.transform.SetParent(studio, false);
            lg.transform.localPosition = local;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 14f;
            l.intensity = intensity;
            l.color = c;
            l.cullingMask = 1 << Layer;
            l.shadows = LightShadows.None;
        }

        static Sprite Render(Item it)
        {
            EnsureStudio();
            var holder = new GameObject("Holder").transform;
            holder.SetParent(studio, false);
            holder.localPosition = Vector3.zero;
            Vector3 viewDir;
            switch (it.S)
            {
                case Slot.Weapon:
                    var w = GearVisuals.BuildWeapon(it, holder, 1f);
                    holder.localRotation = Quaternion.Euler(0, 0, -45f);   // pe diagonala, varful sus-dreapta
                    viewDir = new Vector3(0.25f, 0.2f, 1f);
                    break;
                case Slot.Helmet:
                    GearVisuals.BuildHelmet(it, holder, HeadSize);
                    viewDir = new Vector3(0.45f, 0.2f, 1f);
                    break;
                case Slot.Armor:
                    GearVisuals.BuildArmor(it, holder, TorsoSize);
                    viewDir = new Vector3(0.4f, 0.12f, 1f);
                    break;
                default:
                    GearVisuals.BuildRing(it, holder);
                    holder.localRotation = Quaternion.Euler(-10f, 25f, 0);
                    viewDir = new Vector3(0.2f, 0.35f, 1f);
                    break;
            }
            SetLayer(holder, Layer);

            // incadrare: bounds-ul modelului proiectat pe axele camerei
            Bounds b = new Bounds(holder.position, Vector3.zero);
            bool any = false;
            foreach (var r in holder.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            viewDir.Normalize();
            cam.transform.position = b.center + viewDir * 10f;
            cam.transform.LookAt(b.center);
            Vector3 right = cam.transform.right, up = cam.transform.up;
            float hx = 0f, hy = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)) - b.center;
                hx = Mathf.Max(hx, Mathf.Abs(Vector3.Dot(c, right)));
                hy = Mathf.Max(hy, Mathf.Abs(Vector3.Dot(c, up)));
            }
            cam.orthographicSize = Mathf.Max(0.05f, Mathf.Max(hx, hy) * 0.98f);

            cam.Render();
            var raw = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            ReadBack(rt, raw);
            Object.DestroyImmediate(holder.gameObject);

            var tex = Stylize(raw);
            Object.Destroy(raw);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Citeste pixelii unui RenderTexture (si cu MSAA): intai il "rezolvam" intr-o textura fara MSAA.
        /// Pe unele telefoane (Vulkan / Mali), ReadPixels direct dintr-un RT cu MSAA da pixeli amestecati
        /// (iconitele aratau ca un "cod" / zgomot).</summary>
        public static void ReadBack(RenderTexture src, Texture2D dst)
        {
            var tmp = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1);
            Graphics.Blit(src, tmp);
            var prev = RenderTexture.active;
            RenderTexture.active = tmp;
            dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            dst.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        /// <summary>Acelasi stil ca restul iconitelor: imaginea micsorata cu margine, contur inchis + umbra.</summary>
        static Texture2D Stylize(Texture2D raw)
        {
            const int pad = 6, outline = 3;
            int inner = Size - pad * 2;
            var src = raw.GetPixels32();
            var dst = new Color32[Size * Size];
            // 1) imaginea micsorata in mijloc (bilinear)
            var img = new Color[Size * Size];
            for (int y = 0; y < inner; y++)
                for (int x = 0; x < inner; x++)
                {
                    float u = (x + 0.5f) / inner * Size - 0.5f, v = (y + 0.5f) / inner * Size - 0.5f;
                    img[(y + pad) * Size + x + pad] = Sample(src, u, v);
                }
            // 2) contur: alpha dilatat
            var alpha = new float[Size * Size];
            for (int i = 0; i < alpha.Length; i++) alpha[i] = img[i].a > 0.3f ? 1f : img[i].a / 0.3f;
            var ol = new float[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float m = 0f;
                    for (int dy = -outline; dy <= outline && m < 1f; dy++)
                    {
                        int yy = y + dy; if (yy < 0 || yy >= Size) continue;
                        for (int dx = -outline; dx <= outline; dx++)
                        {
                            int xx = x + dx; if (xx < 0 || xx >= Size) continue;
                            if (dx * dx + dy * dy > outline * outline + 1) continue;
                            float a = alpha[yy * Size + xx];
                            if (a > m) m = a;
                        }
                    }
                    ol[y * Size + x] = m;
                }
            Color outCol = new Color(22 / 255f, 18 / 255f, 32 / 255f, 1f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int i = y * Size + x;
                    // umbra (contur deplasat in jos)
                    float sh = y + 3 < Size ? ol[(y + 3) * Size + x] * 0.4f : 0f;
                    Color c = new Color(0, 0, 0, sh);
                    c = Over(c, new Color(outCol.r, outCol.g, outCol.b, ol[i]));
                    Color p = img[i];
                    // un pic mai luminos si mai saturat (iconitele "pop")
                    if (p.a > 0f)
                    {
                        Color q = new Color(p.r / Mathf.Max(p.a, 0.001f), p.g / Mathf.Max(p.a, 0.001f), p.b / Mathf.Max(p.a, 0.001f), 1f);
                        q = Color.Lerp(q, q * 1.15f, 0.6f);
                        p = new Color(Mathf.Clamp01(q.r), Mathf.Clamp01(q.g), Mathf.Clamp01(q.b), Mathf.Clamp01(p.a * 1.4f));
                    }
                    c = Over(c, p);
                    dst[i] = c;
                }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(dst);
            tex.Apply(false, true);
            return tex;
        }

        static Color Over(Color under, Color over)
        {
            float a = over.a + under.a * (1f - over.a);
            if (a <= 0.0001f) return new Color(0, 0, 0, 0);
            Color c = (over * over.a + under * under.a * (1f - over.a)) / a;
            c.a = a;
            return c;
        }

        static Color Sample(Color32[] src, float u, float v)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt(u), 0, Size - 1), y0 = Mathf.Clamp(Mathf.FloorToInt(v), 0, Size - 1);
            int x1 = Mathf.Min(x0 + 1, Size - 1), y1 = Mathf.Min(y0 + 1, Size - 1);
            float fx = Mathf.Clamp01(u - x0), fy = Mathf.Clamp01(v - y0);
            Color a = src[y0 * Size + x0], b = src[y0 * Size + x1], c = src[y1 * Size + x0], d = src[y1 * Size + x1];
            return Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
        }
    }
}
