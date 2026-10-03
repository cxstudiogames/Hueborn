// =====================================================================
//  ProcNature.cs - copaci, tufe si stanci "realiste", generate din cod:
//   * trunchi + crengi (plasa conica, usor curbata) cu shaderul ColorRPG/Surface
//   * frunzis din carduri cu textura de frunze (ColorRPG/Foliage): normale sferice,
//     lumina prin frunze, vant, zapada pe brazi
//   * stanci din sfere deformate cu zgomot
//  Plasele sunt create o singura data (cateva variante) si refolosite;
//  editorul le salveaza ca asset-uri in Generated/Meshes cand construieste scena.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public enum TreeKind { Oak, OakAutumn, Birch, Pine, SnowPine, Palm, Dead, Bush, Cactus }

    public static class ProcNature
    {
        const int Variants = 4;
        static readonly Dictionary<string, Mesh[]> meshCache = new Dictionary<string, Mesh[]>();
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        static Texture2D leafTex, needleTex, frondTex;

        public static void Reset()
        {
            meshCache.Clear();
            matCache.Clear();
            leafTex = needleTex = frondTex = null;
        }

        public static bool Available { get { var s = Shader.Find("ColorRPG/Foliage"); return s != null && s.isSupported; } }

        // ---------------------------------------------------------------
        //  Plasare
        // ---------------------------------------------------------------
        /// <summary>Planteaza un copac / tufa. size = inaltimea aproximativa (m).</summary>
        public static Transform Place(TreeKind kind, Vector3 pos, float size, float yaw, Transform parent, int seed)
        {
            var root = new GameObject(kind.ToString()).transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0, yaw, 0);
            root.localScale = Vector3.one * (size / BaseHeight(kind));
            int v = Mathf.Abs(seed) % Variants;
            var meshes = Get(kind, v);
            if (meshes[0] != null) AddPart(root, "Trunk", meshes[0], BarkMat(kind), true);
            if (meshes.Length > 1 && meshes[1] != null) AddPart(root, "Leaves", meshes[1], LeafMat(kind), true);
            return root;
        }

        /// <summary>Stanca neregulata (sfera deformata). tint = culoarea pietrei.</summary>
        public static Transform Rock(Vector3 pos, float size, float yaw, Color tint, Transform parent, int seed, bool snowy = false)
        {
            var root = new GameObject("Rock").transform;
            root.SetParent(parent, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0, yaw, 0);
            var r = new System.Random(seed);
            root.localScale = new Vector3(size * (0.9f + (float)r.NextDouble() * 0.5f), size * (0.55f + (float)r.NextDouble() * 0.45f), size * (0.8f + (float)r.NextDouble() * 0.5f));
            int v = Mathf.Abs(seed) % Variants;
            string key = "rock" + v;
            Mesh[] m;
            if (!meshCache.TryGetValue(key, out m)) { m = new[] { RockMesh(v) }; meshCache[key] = m; }
            string mk = "rockmat" + ColorUtility.ToHtmlStringRGB(tint) + snowy;
            Material mat;
            if (!matCache.TryGetValue(mk, out mat) || mat == null)
            {
                mat = Art.NewLit(tint, snowy ? 0.35f : 0.18f);
                mat.name = snowy ? "SnowRock" : "Rock";
                if (mat.HasProperty("_BumpStrength")) mat.SetFloat("_BumpStrength", 1.6f);
                if (mat.HasProperty("_DetailScale")) mat.SetFloat("_DetailScale", 1.3f);
                matCache[mk] = mat;
            }
            AddPart(root, "Mesh", m[0], mat, true);
            if (snowy)
            {
                // "caciula" de zapada peste stanca
                var cap = AddPart(root, "Snow", m[0], SnowMat(), false);
                cap.localPosition = new Vector3(0, 0.12f, 0);
                cap.localScale = new Vector3(0.96f, 0.8f, 0.96f);
            }
            return root;
        }

        static Transform AddPart(Transform root, string n, Mesh mesh, Material mat, bool shadows)
        {
            var go = new GameObject(n);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static float BaseHeight(TreeKind k)
        {
            switch (k)
            {
                case TreeKind.Bush: return 1.4f;
                case TreeKind.Cactus: return 3f;
                case TreeKind.Palm: return 8f;
                case TreeKind.Pine: case TreeKind.SnowPine: return 9f;
                default: return 7f;
            }
        }

        // ---------------------------------------------------------------
        //  Materiale
        // ---------------------------------------------------------------
        static Material BarkMat(TreeKind k)
        {
            string key = "bark" + k;
            Material m;
            if (matCache.TryGetValue(key, out m) && m != null) return m;
            Color c;
            switch (k)
            {
                case TreeKind.Birch: c = new Color(0.86f, 0.84f, 0.78f); break;
                case TreeKind.Palm: c = new Color(0.55f, 0.43f, 0.3f); break;
                case TreeKind.Dead: c = new Color(0.16f, 0.12f, 0.11f); break;
                case TreeKind.Cactus: c = new Color(0.27f, 0.52f, 0.25f); break;
                case TreeKind.Pine: case TreeKind.SnowPine: c = new Color(0.36f, 0.25f, 0.18f); break;
                default: c = new Color(0.4f, 0.29f, 0.19f); break;
            }
            m = Art.NewLit(c, k == TreeKind.Cactus ? 0.35f : 0.12f);
            m.name = "Bark_" + k;
            if (m.HasProperty("_BumpStrength")) m.SetFloat("_BumpStrength", k == TreeKind.Cactus ? 0.6f : 1.8f);
            if (k == TreeKind.Dead) Art.SetEmission(m, new Color(0.35f, 0.08f, 0.02f) * 0.25f);
            matCache[key] = m;
            return m;
        }

        static Material SnowMat()
        {
            Material m;
            if (matCache.TryGetValue("snow", out m) && m != null) return m;
            m = Art.NewLit(new Color(0.93f, 0.96f, 1f), 0.45f);
            m.name = "SnowCap";
            matCache["snow"] = m;
            return m;
        }

        static Material LeafMat(TreeKind k)
        {
            string key = "leaf" + k;
            Material m;
            if (matCache.TryGetValue(key, out m) && m != null) return m;
            var sh = Shader.Find("ColorRPG/Foliage");
            m = new Material(sh) { name = "Leaves_" + k };
            Color a, b; Texture2D tex = LeafTex(); float snow = 0f, wind = 0.12f, trans = 0.55f;
            switch (k)
            {
                case TreeKind.OakAutumn: a = new Color(0.85f, 0.42f, 0.12f); b = new Color(0.95f, 0.72f, 0.2f); break;
                case TreeKind.Birch: a = new Color(0.45f, 0.68f, 0.22f); b = new Color(0.68f, 0.8f, 0.3f); break;
                case TreeKind.Pine: a = new Color(0.14f, 0.32f, 0.16f); b = new Color(0.22f, 0.42f, 0.2f); tex = NeedleTex(); wind = 0.06f; trans = 0.3f; break;
                case TreeKind.SnowPine: a = new Color(0.12f, 0.28f, 0.2f); b = new Color(0.2f, 0.36f, 0.26f); tex = NeedleTex(); snow = 1f; wind = 0.05f; trans = 0.25f; break;
                case TreeKind.Palm: a = new Color(0.3f, 0.55f, 0.18f); b = new Color(0.55f, 0.66f, 0.22f); tex = FrondTex(); wind = 0.18f; break;
                case TreeKind.Bush: a = new Color(0.22f, 0.48f, 0.16f); b = new Color(0.4f, 0.6f, 0.2f); wind = 0.06f; break;
                default: a = new Color(0.25f, 0.5f, 0.16f); b = new Color(0.45f, 0.64f, 0.2f); break;
            }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", a);
            m.SetColor("_Color2", b);
            m.SetFloat("_SnowAmount", snow);
            m.SetFloat("_WindStrength", wind);
            m.SetFloat("_Translucency", trans);
            m.enableInstancing = true;
            matCache[key] = m;
            return m;
        }

        // ---------------------------------------------------------------
        //  Texturi procedurale (alb + alpha; culoarea vine din material)
        // ---------------------------------------------------------------
        static Texture2D NewTex(string n, int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = n, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            return t;
        }

        /// <summary>Un smoc de frunze ovale cu nervura, pe fundal transparent.</summary>
        static Texture2D LeafTex()
        {
            if (leafTex != null) return leafTex;
            const int S = 256;
            var px = new Color[S * S];
            var r = new System.Random(5);
            for (int k = 0; k < 26; k++)
            {
                float cx = 0.2f + (float)r.NextDouble() * 0.6f, cy = 0.2f + (float)r.NextDouble() * 0.6f;
                float ang = (float)r.NextDouble() * Mathf.PI * 2f;
                float len = 0.13f + (float)r.NextDouble() * 0.08f, wid = len * 0.42f;
                float shade = 0.7f + (float)r.NextDouble() * 0.3f;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                int x0 = Mathf.Max(0, (int)((cx - len) * S)), x1 = Mathf.Min(S - 1, (int)((cx + len) * S));
                int y0 = Mathf.Max(0, (int)((cy - len) * S)), y1 = Mathf.Min(S - 1, (int)((cy + len) * S));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = x / (float)S - cx, dy = y / (float)S - cy;
                        float u = (dx * ca + dy * sa) / len, v = (-dx * sa + dy * ca) / wid;
                        float edge = 1f - (u * u + v * v * (1f + 0.6f * u));
                        if (edge <= 0f) continue;
                        float vein = Mathf.Abs(v) < 0.08f ? 0.82f : 1f;
                        float side = Mathf.Abs(Mathf.Sin(u * 12f + Mathf.Abs(v) * 6f)) < 0.12f ? 0.9f : 1f;
                        float lum = shade * vein * side * (0.85f + 0.15f * v);
                        int i = y * S + x;
                        px[i] = new Color(lum, lum, lum, Mathf.Clamp01(edge * 6f));
                    }
            }
            leafTex = NewTex("LeafCluster", S);
            leafTex.SetPixels(px); leafTex.Apply(true);
            return leafTex;
        }

        /// <summary>Crenguta de brad: ax central + ace.</summary>
        static Texture2D NeedleTex()
        {
            if (needleTex != null) return needleTex;
            const int S = 256;
            var px = new Color[S * S];
            var r = new System.Random(9);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float width = 0.45f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(v * 1.05f)) * (1f - v * 0.4f);
                    float dx = Mathf.Abs(u - 0.5f);
                    if (dx > width) continue;
                    float needle = Mathf.Abs(Mathf.Sin((v * 60f) - dx * 30f));
                    float a = needle > 0.35f ? 1f : 0f;
                    if (dx < 0.02f) a = 1f;
                    if (a <= 0f) continue;
                    float lum = 0.75f + 0.25f * (1f - dx / Mathf.Max(0.01f, width));
                    px[y * S + x] = new Color(lum, lum, lum, a);
                }
            needleTex = NewTex("PineNeedles", S);
            needleTex.SetPixels(px); needleTex.Apply(true);
            return needleTex;
        }

        /// <summary>Frunza de palmier: nervura + foliole.</summary>
        static Texture2D FrondTex()
        {
            if (frondTex != null) return frondTex;
            const int S = 256;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S - 0.5f, v = y / (float)S;
                    float w = 0.48f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(v)) ;
                    float dx = Mathf.Abs(u);
                    if (dx > w) continue;
                    float leaflet = Mathf.Abs(Mathf.Sin((v * 34f) - dx * 9f));
                    float a = leaflet > 0.22f ? 1f : 0f;
                    if (dx < 0.018f) a = 1f;
                    if (a <= 0f) continue;
                    float lum = 0.7f + 0.3f * (1f - dx / Mathf.Max(0.01f, w));
                    px[y * S + x] = new Color(lum, lum, lum, 1f);
                }
            frondTex = NewTex("PalmFrond", S);
            frondTex.SetPixels(px); frondTex.Apply(true);
            return frondTex;
        }

        // ---------------------------------------------------------------
        //  Plase
        // ---------------------------------------------------------------
        static Mesh[] Get(TreeKind k, int v)
        {
            string key = k + "_" + v;
            Mesh[] m;
            if (meshCache.TryGetValue(key, out m) && m != null) return m;
            var r = new System.Random(1000 + (int)k * 97 + v * 13);
            switch (k)
            {
                case TreeKind.Pine: case TreeKind.SnowPine: m = Pine(r, k == TreeKind.SnowPine); break;
                case TreeKind.Palm: m = Palm(r); break;
                case TreeKind.Dead: m = Dead(r); break;
                case TreeKind.Bush: m = Bush(r); break;
                case TreeKind.Cactus: m = Cactus(r); break;
                default: m = Broadleaf(r, k == TreeKind.Birch); break;
            }
            for (int i = 0; i < m.Length; i++) if (m[i] != null) m[i].name = "Proc_" + key + "_" + i;
            meshCache[key] = m;
            return m;
        }

        static float Rf(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        class MB
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<Color> c = new List<Color>();
            public readonly List<int> t = new List<int>();

            public Mesh Build(bool recalcNormals)
            {
                var m = new Mesh();
                if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(c); m.SetTriangles(t, 0);
                if (recalcNormals || n.Count != v.Count) m.RecalculateNormals(); else m.SetNormals(n);
                m.RecalculateBounds();
                return m;
            }
        }

        /// <summary>Tub conic de-a lungul unei curbe (trunchi / creanga).</summary>
        static void Tube(MB mb, Vector3[] path, float r0, float r1, int sides, System.Random rnd, float gnarl = 0.12f)
        {
            int baseI = mb.v.Count;
            int rings = path.Length;
            for (int i = 0; i < rings; i++)
            {
                float t = i / (float)(rings - 1);
                Vector3 dir = i < rings - 1 ? (path[i + 1] - path[i]).normalized : (path[i] - path[i - 1]).normalized;
                Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Vector3 up = Vector3.Cross(side, dir).normalized;
                float rad = Mathf.Lerp(r0, r1, t) * (i == 0 ? 1.35f : 1f);
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    float bump = 1f + gnarl * Mathf.Sin(a * 3f + i * 1.7f) * (float)rnd.NextDouble();
                    Vector3 off = (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rad * bump;
                    mb.v.Add(path[i] + off);
                    mb.uv.Add(new Vector2(s / (float)sides, t));
                    mb.c.Add(Color.white);
                }
            }
            for (int i = 0; i < rings - 1; i++)
                for (int s = 0; s < sides; s++)
                {
                    int a = baseI + i * (sides + 1) + s, b = a + sides + 1;
                    mb.t.Add(a); mb.t.Add(b); mb.t.Add(a + 1);
                    mb.t.Add(a + 1); mb.t.Add(b); mb.t.Add(b + 1);
                }
        }

        static Vector3[] Curve(Vector3 a, Vector3 dir, float len, int seg, float bend, System.Random r)
        {
            var p = new Vector3[seg + 1];
            Vector3 d = dir.normalized;
            Vector3 cur = a;
            Vector3 drift = new Vector3(Rf(r, -1, 1), 0, Rf(r, -1, 1)) * bend;
            for (int i = 0; i <= seg; i++)
            {
                p[i] = cur;
                cur += (d + drift * (i / (float)seg)) .normalized * (len / seg);
            }
            return p;
        }

        /// <summary>Carduri de frunze pe o sfera (normale spre exterior = lumina moale).</summary>
        static void Cluster(MB mb, Vector3 center, float radius, int cards, float cardSize, float treeH, System.Random r, float snowTop = 0f)
        {
            for (int i = 0; i < cards; i++)
            {
                Vector3 dirv = Random3(r);
                dirv.y = dirv.y * 0.8f + 0.12f;
                Vector3 pos = center + dirv.normalized * radius * Mathf.Pow((float)r.NextDouble(), 0.35f);
                Vector3 nrm = (pos - center + Vector3.up * radius * 0.35f).normalized;
                Quaternion q = Quaternion.LookRotation(Random3(r), Random3(r));
                float s = cardSize * Rf(r, 0.75f, 1.25f);
                Vector3 ax = q * Vector3.right * s * 0.5f, ay = q * Vector3.up * s * 0.5f;
                float h = Mathf.Clamp01(pos.y / treeH);
                float inner = Mathf.Clamp01((pos - center).magnitude / radius);
                var col = new Color(h, (float)r.NextDouble(), 0.35f + 0.65f * inner, snowTop);
                Quad(mb, pos, ax, ay, nrm, col);
            }
        }

        static void Quad(MB mb, Vector3 c, Vector3 ax, Vector3 ay, Vector3 nrm, Color col)
        {
            int b = mb.v.Count;
            mb.v.Add(c - ax - ay); mb.v.Add(c + ax - ay); mb.v.Add(c + ax + ay); mb.v.Add(c - ax + ay);
            mb.uv.Add(new Vector2(0, 0)); mb.uv.Add(new Vector2(1, 0)); mb.uv.Add(new Vector2(1, 1)); mb.uv.Add(new Vector2(0, 1));
            for (int k = 0; k < 4; k++) { mb.n.Add(nrm); mb.c.Add(col); }
            mb.t.Add(b); mb.t.Add(b + 2); mb.t.Add(b + 1);
            mb.t.Add(b); mb.t.Add(b + 3); mb.t.Add(b + 2);
        }

        static Vector3 Random3(System.Random r)
        {
            Vector3 v;
            do { v = new Vector3(Rf(r, -1, 1), Rf(r, -1, 1), Rf(r, -1, 1)); } while (v.sqrMagnitude > 1f || v.sqrMagnitude < 0.01f);
            return v.normalized;
        }

        // ---- foioase (stejar / mesteacan)
        static Mesh[] Broadleaf(System.Random r, bool birch)
        {
            var bark = new MB(); var leaves = new MB();
            float H = 7f;
            float trunkH = birch ? Rf(r, 4.6f, 5.4f) : Rf(r, 3.2f, 3.9f);
            var trunk = Curve(Vector3.zero, new Vector3(Rf(r, -0.08f, 0.08f), 1, Rf(r, -0.08f, 0.08f)), trunkH, 6, 0.15f, r);
            Tube(bark, trunk, birch ? 0.2f : 0.34f, birch ? 0.11f : 0.2f, 9, r);
            Vector3 top = trunk[trunk.Length - 1];
            var centers = new List<Vector3>();
            centers.Add(top + Vector3.up * (birch ? 1.1f : 1.4f));
            int branches = 4 + r.Next(0, 2);
            for (int b = 0; b < branches; b++)
            {
                float a = b / (float)branches * Mathf.PI * 2f + Rf(r, -0.4f, 0.4f);
                float startT = Rf(r, 0.55f, 0.95f);
                int si = Mathf.Clamp(Mathf.RoundToInt(startT * (trunk.Length - 1)), 1, trunk.Length - 1);
                Vector3 dir = new Vector3(Mathf.Cos(a), Rf(r, 0.7f, 1.2f), Mathf.Sin(a));
                float len = Rf(r, 1.6f, 2.6f) * (birch ? 0.7f : 1f);
                var br = Curve(trunk[si], dir, len, 3, 0.25f, r);
                Tube(bark, br, birch ? 0.08f : 0.14f, 0.04f, 6, r, 0.05f);
                centers.Add(br[br.Length - 1] + Vector3.up * 0.3f);
            }
            float cr = birch ? 1.25f : 1.75f;
            foreach (var c in centers) Cluster(leaves, c, cr * Rf(r, 0.85f, 1.15f), birch ? 36 : 46, birch ? 1.25f : 1.55f, H, r);
            return new[] { bark.Build(true), leaves.Build(false) };
        }

        // ---- brad (optional cu zapada)
        static Mesh[] Pine(System.Random r, bool snowy)
        {
            var bark = new MB(); var leaves = new MB();
            float H = 9f;
            var trunk = Curve(Vector3.zero, Vector3.up, H * 0.92f, 6, 0.04f, r);
            Tube(bark, trunk, 0.3f, 0.05f, 8, r, 0.06f);
            int layers = 9;
            for (int l = 0; l < layers; l++)
            {
                float t = l / (float)(layers - 1);
                float y = Mathf.Lerp(1.3f, H * 0.95f, t);
                float rad = Mathf.Lerp(2.6f, 0.35f, t) * Rf(r, 0.9f, 1.1f);
                int n = Mathf.Max(5, Mathf.RoundToInt(Mathf.Lerp(16, 6, t)));
                for (int i = 0; i < n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f + l * 0.7f + Rf(r, -0.2f, 0.2f);
                    Vector3 outDir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    // crenguta: card orientat in afara si usor in jos
                    Vector3 along = (outDir - Vector3.up * 0.38f).normalized;
                    Vector3 across = Vector3.Cross(Vector3.up, outDir).normalized;
                    float len = rad * Rf(0.95f, 1.15f, r);
                    Vector3 c = new Vector3(0, y, 0) + along * len * 0.5f;
                    Vector3 nrm = (outDir + Vector3.up * 0.55f).normalized;
                    var col = new Color(y / H, (float)r.NextDouble(), 0.45f + 0.55f * t, snowy ? 1f : 0f);
                    // doua carduri incrucisate pentru volum
                    Quad(leaves, c, across * len * 0.42f, along * len * 0.5f, nrm, col);
                    Quad(leaves, c + Vector3.up * 0.05f, Vector3.Cross(along, across).normalized * len * 0.3f, along * len * 0.5f, nrm, col);
                }
            }
            return new[] { bark.Build(true), leaves.Build(false) };
        }

        static float Rf(float a, float b, System.Random r) { return Rf(r, a, b); }

        // ---- palmier
        static Mesh[] Palm(System.Random r)
        {
            var bark = new MB(); var leaves = new MB();
            float H = 8f;
            var trunk = new Vector3[9];
            float lean = Rf(r, 0.8f, 1.8f); float la = Rf(r, 0, Mathf.PI * 2f);
            for (int i = 0; i < trunk.Length; i++)
            {
                float t = i / (float)(trunk.Length - 1);
                trunk[i] = new Vector3(Mathf.Cos(la) * lean * t * t, t * H * 0.9f, Mathf.Sin(la) * lean * t * t);
            }
            Tube(bark, trunk, 0.32f, 0.2f, 9, r, 0.2f);
            Vector3 top = trunk[trunk.Length - 1];
            int fronds = 9;
            for (int f = 0; f < fronds; f++)
            {
                float a = f / (float)fronds * Mathf.PI * 2f + Rf(r, -0.2f, 0.2f);
                Vector3 outDir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 across = Vector3.Cross(Vector3.up, outDir).normalized;
                float len = Rf(r, 3f, 3.8f);
                int segs = 5;
                int b0 = leaves.v.Count;
                for (int s = 0; s <= segs; s++)
                {
                    float t = s / (float)segs;
                    // arc: urca putin, apoi cade
                    Vector3 p = top + outDir * len * t + Vector3.up * (0.9f * t - 2.3f * t * t);
                    float w = 0.75f * Mathf.Sin(Mathf.PI * Mathf.Clamp(t, 0.05f, 0.95f)) + 0.1f;
                    Vector3 nrm = (Vector3.up + outDir * 0.3f).normalized;
                    var col = new Color(0.7f + 0.3f * t, (float)r.NextDouble(), 0.6f + 0.4f * t, 0f);
                    leaves.v.Add(p - across * w); leaves.v.Add(p + across * w);
                    leaves.uv.Add(new Vector2(0, t)); leaves.uv.Add(new Vector2(1, t));
                    leaves.n.Add(nrm); leaves.n.Add(nrm); leaves.c.Add(col); leaves.c.Add(col);
                }
                for (int s = 0; s < segs; s++)
                {
                    int i0 = b0 + s * 2;
                    leaves.t.Add(i0); leaves.t.Add(i0 + 2); leaves.t.Add(i0 + 1);
                    leaves.t.Add(i0 + 1); leaves.t.Add(i0 + 2); leaves.t.Add(i0 + 3);
                }
            }
            // nuci de cocos
            Cluster(leaves, top - Vector3.up * 0.3f, 0.35f, 6, 0.5f, H, r);
            return new[] { bark.Build(true), leaves.Build(false) };
        }

        // ---- copac uscat / ars (vulcan)
        static Mesh[] Dead(System.Random r)
        {
            var bark = new MB();
            var trunk = Curve(Vector3.zero, new Vector3(Rf(r, -0.15f, 0.15f), 1, Rf(r, -0.15f, 0.15f)), Rf(r, 4.5f, 5.8f), 6, 0.35f, r);
            Tube(bark, trunk, 0.32f, 0.07f, 8, r, 0.25f);
            int branches = 5 + r.Next(0, 3);
            for (int b = 0; b < branches; b++)
            {
                float a = Rf(r, 0, Mathf.PI * 2f);
                int si = r.Next(2, trunk.Length - 1);
                var br = Curve(trunk[si], new Vector3(Mathf.Cos(a), Rf(r, 0.3f, 1f), Mathf.Sin(a)), Rf(r, 1.2f, 2.6f), 3, 0.6f, r);
                Tube(bark, br, 0.1f, 0.02f, 5, r, 0.1f);
                if (r.NextDouble() < 0.6)
                {
                    var tw = Curve(br[2], new Vector3(Rf(r, -1, 1), 1, Rf(r, -1, 1)), Rf(r, 0.6f, 1.2f), 2, 0.5f, r);
                    Tube(bark, tw, 0.04f, 0.01f, 4, r, 0.05f);
                }
            }
            return new[] { bark.Build(true) };
        }

        // ---- tufa
        static Mesh[] Bush(System.Random r)
        {
            var leaves = new MB();
            int n = 3 + r.Next(0, 2);
            for (int i = 0; i < n; i++)
            {
                Vector3 c = new Vector3(Rf(r, -0.45f, 0.45f), Rf(r, 0.45f, 0.75f), Rf(r, -0.45f, 0.45f));
                Cluster(leaves, c, Rf(r, 0.5f, 0.7f), 22, 0.75f, 1.4f, r);
            }
            return new[] { (Mesh)null, leaves.Build(false) };
        }

        // ---- cactus cu nervuri (saguaro)
        static Mesh[] Cactus(System.Random r)
        {
            var mb = new MB();
            float H = Rf(r, 2.4f, 3f);
            Ribbed(mb, Curve(Vector3.zero, Vector3.up, H, 6, 0f, r), 0.3f, 0.26f);
            int arms = 1 + r.Next(0, 2);
            for (int a = 0; a < arms; a++)
            {
                float ang = a * Mathf.PI + Rf(r, -0.4f, 0.4f);
                Vector3 o = new Vector3(0, Rf(r, 0.9f, 1.5f), 0);
                Vector3 outDir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                var p = new Vector3[6];
                for (int i = 0; i < 6; i++)
                {
                    float t = i / 5f;
                    p[i] = o + outDir * (0.65f * Mathf.Sin(Mathf.Min(1f, t * 1.6f) * Mathf.PI * 0.5f)) + Vector3.up * (t * t * 1.1f);
                }
                Ribbed(mb, p, 0.18f, 0.15f);
            }
            return new[] { mb.Build(true) };
        }

        static void Ribbed(MB mb, Vector3[] path, float r0, float r1)
        {
            int sides = 16;
            int baseI = mb.v.Count;
            for (int i = 0; i < path.Length; i++)
            {
                float t = i / (float)(path.Length - 1);
                Vector3 dir = i < path.Length - 1 ? (path[i + 1] - path[i]).normalized : (path[i] - path[i - 1]).normalized;
                Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Vector3 up = Vector3.Cross(side, dir).normalized;
                float rad = Mathf.Lerp(r0, r1, t) * (i == path.Length - 1 ? 0.35f : 1f);
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    float rib = 1f + 0.12f * Mathf.Cos(a * 8f);
                    mb.v.Add(path[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rad * rib);
                    mb.uv.Add(new Vector2(s / (float)sides, t));
                    mb.c.Add(Color.white);
                }
            }
            for (int i = 0; i < path.Length - 1; i++)
                for (int s = 0; s < sides; s++)
                {
                    int a = baseI + i * (sides + 1) + s, b = a + sides + 1;
                    mb.t.Add(a); mb.t.Add(b); mb.t.Add(a + 1);
                    mb.t.Add(a + 1); mb.t.Add(b); mb.t.Add(b + 1);
                }
        }

        // ---- stanca
        static Mesh RockMesh(int v)
        {
            // icosfera subdivizata + zgomot
            var r = new System.Random(77 + v * 31);
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            var tris = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                                       3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            for (int sub = 0; sub < 3; sub++)
            {
                var mid = new Dictionary<long, int>();
                var nt = new List<int>();
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(verts, mid, a, b), bc = Mid(verts, mid, b, c), ca = Mid(verts, mid, c, a);
                    nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = nt;
            }
            float ox = Rf(r, 0, 100), oz = Rf(r, 0, 100);
            // plane de "clivaj": fete plate ca la stancile reale
            var planes = new List<Vector4>();
            for (int k = 0; k < 5; k++) { var d = Random3(r); planes.Add(new Vector4(d.x, d.y, d.z, Rf(r, 0.6f, 0.85f))); }
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 p = verts[i];
                float n = Fbm3(p * 1.8f + new Vector3(ox, 0, oz));
                float rad = 0.78f + 0.45f * n;
                p *= rad;
                foreach (var pl in planes)
                {
                    Vector3 d = new Vector3(pl.x, pl.y, pl.z);
                    float h = Vector3.Dot(p, d);
                    if (h > pl.w) p -= d * (h - pl.w);
                }
                if (p.y < -0.25f) p.y = -0.25f + (p.y + 0.25f) * 0.15f;   // baza turtita (sta pe sol)
                verts[i] = new Vector3(p.x * 0.5f, (p.y + 0.25f) * 0.5f, p.z * 0.5f);
            }
            var m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            var cols = new List<Color>(); var uvs = new List<Vector2>();
            foreach (var p in verts) { cols.Add(Color.white); uvs.Add(new Vector2(p.x, p.z)); }
            m.SetColors(cols); m.SetUVs(0, uvs);
            m.RecalculateNormals();
            m.RecalculateBounds();
            m.name = "Proc_rock_" + v;
            return m;
        }

        static int Mid(List<Vector3> v, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            int i;
            if (cache.TryGetValue(key, out i)) return i;
            v.Add(((v[a] + v[b]) * 0.5f).normalized);
            i = v.Count - 1;
            cache[key] = i;
            return i;
        }

        static float Noise3(Vector3 p)
        {
            return (Mathf.PerlinNoise(p.x, p.y) + Mathf.PerlinNoise(p.y + 31.7f, p.z) + Mathf.PerlinNoise(p.z + 71.3f, p.x)) / 3f;
        }
        static float Fbm3(Vector3 p) { return Noise3(p) * 0.65f + Noise3(p * 2.3f + Vector3.one * 5f) * 0.35f; }
    }
}
