// =====================================================================
//  Art.cs - materiale, primitive colorate, texturi procedurale
//  Merge in URP (proiectul actual) si in Built-in (fallback pe "Standard").
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class Art
    {
        static readonly Dictionary<Color, Material> litCache = new Dictionary<Color, Material>();
        static readonly Dictionary<Material, Material> fixCache = new Dictionary<Material, Material>();
        static Material nullFix;
        static readonly Dictionary<string, Material> fxCache = new Dictionary<string, Material>();
        static Shader lit, particle;
        static Texture2D softDot, sparkTex;

        public static bool IsURP
        {
            get { return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null; }
        }

        /// <summary>Distruge un obiect si in Play, si in editor (cand construim scena).</summary>
        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        public static void Reset()
        {
            litCache.Clear();
            fixCache.Clear();
            surfCache.Clear();
            ProcNature.Reset();
            nullFix = null;
            fxCache.Clear();
        }

        // ---------------------------------------------------------------
        //  Shadere
        // ---------------------------------------------------------------
        public static Shader LitShader
        {
            get
            {
                if (lit == null)
                {
                    if (IsURP) lit = Shader.Find("ColorRPG/Surface");      // PBR + detalii procedurale (realist)
                    if (IsURP && (lit == null || !lit.isSupported)) lit = Shader.Find("Universal Render Pipeline/Lit");
                    if (lit == null) lit = Shader.Find("Standard");
                    if (lit == null) lit = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (lit == null) lit = Shader.Find("Sprites/Default");
                }
                return lit;
            }
        }

        static Shader ParticleShader
        {
            get
            {
                if (particle == null)
                {
                    if (IsURP) particle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (particle == null) particle = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                    if (particle == null) particle = Shader.Find("Sprites/Default");
                }
                return particle;
            }
        }

        // ---------------------------------------------------------------
        //  Materiale lit
        // ---------------------------------------------------------------
        public static Material NewLit(Color c, float smooth = 0.25f, Texture tex = null)
        {
            var m = new Material(LitShader);
            SetColor(m, c);
            if (tex != null) SetTex(m, tex);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        /// <summary>Material pentru iarba 3D (shader ColorRPG/Grass). null daca shaderul lipseste.</summary>
        public static Material GrassMaterial(Color root, Color tip, float wind = 0.22f)
        {
            var sh = Shader.Find("ColorRPG/Grass");
            if (sh == null) return null;
            var m = new Material(sh) { name = "Grass" };
            m.SetColor("_BaseColor", root);
            m.SetColor("_TipColor", tip);
            m.SetFloat("_WindStrength", wind);
            m.enableInstancing = true;
            return m;
        }

        /// <summary>Material pentru cerul procedural (shader ColorRPG/Sky).</summary>
        public static Material SkyMaterial()
        {
            var sh = Shader.Find("ColorRPG/Sky");
            if (sh == null) return null;
            return new Material(sh) { name = "Sky" };
        }

        public static Material SharedLit(Color c)
        {
            Material m;
            if (!litCache.TryGetValue(c, out m) || m == null)
            {
                m = NewLit(c);
                litCache[c] = m;
            }
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static Color GetColor(Material m)
        {
            if (m == null) return Color.white;
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return Color.white;
        }

        static void SetTex(Material m, Texture t)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        }

        static Texture GetTex(Material m)
        {
            if (m == null) return null;
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return m.GetTexture("_BaseMap");
            if (m.HasProperty("_MainTex")) return m.GetTexture("_MainTex");
            return null;
        }

        /// <summary>Emisie (stralucire) - folosit pentru flash la lovitura si loot rar.</summary>
        public static void SetEmission(Material m, Color c)
        {
            if (m == null || !m.HasProperty("_EmissionColor")) return;
            if (c.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            m.SetColor("_EmissionColor", c);
        }

        /// <summary>
        /// Garanteaza ca un model importat (FBX) arata corect in pipeline-ul curent:
        /// orice material cu shader nesuportat (roz) este inlocuit cu unul Lit cu aceeasi culoare/textura.
        /// </summary>
        public static void FixMaterials(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src != null && ShaderOk(src.shader)) continue;
                    Material rep;
                    if (src == null)
                    {
                        if (nullFix == null) nullFix = NewLit(Color.white, 0.2f);
                        rep = nullFix;
                    }
                    else if (!fixCache.TryGetValue(src, out rep) || rep == null)
                    {
                        Color c = GetColor(src);
                        c.a = 1f;
                        rep = NewLit(c, 0.2f, GetTex(src));
                        fixCache[src] = rep;
                    }
                    mats[i] = rep;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static readonly Dictionary<Material, Material> surfCache = new Dictionary<Material, Material>();

        /// <summary>
        /// Trece materialele opace URP/Lit (ale modelelor Kenney) pe shaderul realist ColorRPG/Surface:
        /// aceeasi culoare / textura, dar cu relief, variatie de culoare, ocluzie si lumina de margine.
        /// </summary>
        public static void Realistic(GameObject root)
        {
            var sh = LitShader;
            if (sh == null || sh.name != "ColorRPG/Surface") return;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null || src.shader == null || src.shader == sh) continue;
                    if (src.name.StartsWith("MK_")) continue;   // pachetul de natura (Quaternius) are materialele lui (frunze cu transparenta)
                    string n = src.shader.name;
                    if (n != "Universal Render Pipeline/Lit" && n != "Universal Render Pipeline/Simple Lit") continue;
                    if (src.HasProperty("_Surface") && src.GetFloat("_Surface") > 0.5f) continue;   // transparente raman cum sunt
                    Material rep;
                    if (!surfCache.TryGetValue(src, out rep) || rep == null)
                    {
                        rep = new Material(sh) { name = src.name };
                        var tex = GetTex(src);
                        if (tex != null)
                        {
                            rep.SetTexture("_BaseMap", tex);
                            if (src.HasProperty("_BaseMap"))
                            {
                                rep.SetTextureScale("_BaseMap", src.GetTextureScale("_BaseMap"));
                                rep.SetTextureOffset("_BaseMap", src.GetTextureOffset("_BaseMap"));
                            }
                        }
                        Color c = GetColor(src); c.a = 1f;
                        rep.SetColor("_BaseColor", c);
                        if (src.HasProperty("_Smoothness")) rep.SetFloat("_Smoothness", Mathf.Min(0.6f, src.GetFloat("_Smoothness")));
                        if (src.HasProperty("_Metallic")) rep.SetFloat("_Metallic", src.GetFloat("_Metallic"));
                        if (src.IsKeywordEnabled("_EMISSION") && src.HasProperty("_EmissionColor"))
                            rep.SetColor("_EmissionColor", src.GetColor("_EmissionColor"));
                        rep.enableInstancing = true;
                        surfCache[src] = rep;
                    }
                    mats[i] = rep;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static bool ShaderOk(Shader s)
        {
            if (s == null || !s.isSupported) return false;
            string n = s.name;
            if (n.Contains("InternalErrorShader")) return false;
            if (IsURP)
                return n.StartsWith("Universal Render Pipeline") || n.StartsWith("Shader Graphs") || n.StartsWith("Sprites") || n.StartsWith("ColorRPG/");
            return true;
        }

        // ---------------------------------------------------------------
        //  Primitive colorate
        // ---------------------------------------------------------------
        public static GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color c,
                                      Transform parent = null, bool collider = true, bool shared = true)
        {
            var go = GameObject.CreatePrimitive(t);
            // optimizare: sfera / capsula Unity au 768 / 832 de triunghiuri - folosim variante low poly (arata la fel)
            if (t == PrimitiveType.Sphere || t == PrimitiveType.Capsule)
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = t == PrimitiveType.Sphere ? MeshGen.LowSphere() : MeshGen.LowCapsule();
            }
            if (!collider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Kill(col);
            }
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = shared ? SharedLit(c) : NewLit(c);
            return go;
        }

        public static Color Dk(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f, 1f); }
        public static Color Lt(Color c, float f) { return Color.Lerp(c, Color.white, f); }
        public static Color A(Color c, float a) { c.a = a; return c; }

        // ---------------------------------------------------------------
        //  Materiale pentru efecte (particule, raze de loot)
        // ---------------------------------------------------------------
        /// <param name="additive">true = lumina (aditiv), false = alpha blend</param>
        public static Material FxMaterial(bool additive, Texture tex = null)
        {
            string key = (additive ? "add_" : "alpha_") + (tex == null ? "dot" : tex.name);
            Material m;
            if (fxCache.TryGetValue(key, out m) && m != null) return m;

            m = new Material(ParticleShader);
            if (tex == null) tex = SoftDot;
            SetTex(m, tex);
            SetColor(m, Color.white);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 2f : 0f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend"))
                m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One
                                                 : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) m.EnableKeyword("_BLENDMODE_ADD"); else m.EnableKeyword("_BLENDMODE_ALPHA");
            m.renderQueue = 3000;
            fxCache[key] = m;
            return m;
        }

        /// <summary>Material transparent colorat pentru mesh-uri (raze de loot, aure, portal). Instanta separata.</summary>
        public static Material GlowMaterial(Color c) { return MeshFx(c, true); }

        /// <summary>Umbra "blob" semi-transparenta sub personaje.</summary>
        public static Material ShadowBlob()
        {
            Material m;
            if (fxCache.TryGetValue("blob", out m) && m != null) return m;
            m = MeshFx(new Color(0f, 0f, 0f, 0.32f), false, SoftDot);
            fxCache["blob"] = m;
            return m;
        }

        static Shader unlit;
        /// <summary>Material Unlit transparent (nu depinde de culorile de vertex ale mesh-ului).</summary>
        public static Material MeshFx(Color c, bool additive, Texture tex = null)
        {
            if (unlit == null)
            {
                if (IsURP) unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null) unlit = Shader.Find("Unlit/Transparent");
                if (unlit == null) unlit = ParticleShader;
            }
            var m = new Material(unlit);
            SetTex(m, tex != null ? tex : WhiteTex);
            SetColor(m, c);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 2f : 0f);
            float src = (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
            float dst = additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", src);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", dst);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", dst);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            return m;
        }

        static Texture2D whiteTex;
        public static Texture2D WhiteTex
        {
            get
            {
                if (whiteTex == null)
                {
                    whiteTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    whiteTex.name = "white";
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    whiteTex.SetPixels32(px);
                    whiteTex.Apply();
                }
                return whiteTex;
            }
        }

        /// <summary>Punct moale (gradient radial) pentru particule.</summary>
        public static Texture2D SoftDot
        {
            get
            {
                if (softDot == null)
                {
                    const int N = 64;
                    softDot = new Texture2D(N, N, TextureFormat.RGBA32, false);
                    softDot.name = "softdot";
                    softDot.wrapMode = TextureWrapMode.Clamp;
                    var px = new Color32[N * N];
                    for (int y = 0; y < N; y++)
                        for (int x = 0; x < N; x++)
                        {
                            float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = Mathf.Clamp01(1f - d);
                            a = a * a * (3f - 2f * a);
                            px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                        }
                    softDot.SetPixels32(px);
                    softDot.Apply();
                }
                return softDot;
            }
        }

        /// <summary>Stea cu 4 colturi (sclipiri).</summary>
        public static Texture2D Spark
        {
            get
            {
                if (sparkTex == null)
                {
                    const int N = 64;
                    sparkTex = new Texture2D(N, N, TextureFormat.RGBA32, false);
                    sparkTex.name = "spark";
                    sparkTex.wrapMode = TextureWrapMode.Clamp;
                    var px = new Color32[N * N];
                    for (int y = 0; y < N; y++)
                        for (int x = 0; x < N; x++)
                        {
                            float dx = Mathf.Abs((x + 0.5f) / N * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / N * 2f - 1f);
                            float star = Mathf.Clamp01(1f - (dx * dy * 18f + (dx + dy) * 0.7f));
                            float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.2f);
                            float a = Mathf.Clamp01(star + core);
                            px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                        }
                    sparkTex.SetPixels32(px);
                    sparkTex.Apply();
                }
                return sparkTex;
            }
        }

        // ---------------------------------------------------------------
        //  Texturi procedurale de sol (seamless): 0 iarba, 1 nisip, 2 zapada, 3 pavaj
        // ---------------------------------------------------------------
        static readonly Texture2D[] groundTex = new Texture2D[4];

        /// <summary>Solul pictat si pavajul (texturi Ground*/Cobble): material URP Lit mat, fara luciu/reflexii,
        /// ca sa ramana aceeasi culoare din orice unghi (shader-ul "realist" le dadea luciu de iarba udă).</summary>
        public static void MatteGround(Transform root)
        {
            if (!IsURP || root == null) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;
            var done = new Dictionary<Material, Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool ch = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !m.HasProperty("_BaseMap") || m.shader == lit) continue;
                    var t = m.GetTexture("_BaseMap");
                    if (t == null || !(t.name.StartsWith("Ground") || t.name == "Cobble")) continue;
                    Material mm;
                    if (!done.TryGetValue(m, out mm))
                    {
                        mm = new Material(lit) { name = m.name + "_Matte" };
                        mm.SetTexture("_BaseMap", t);
                        mm.SetTextureScale("_BaseMap", m.GetTextureScale("_BaseMap"));
                        mm.SetTextureOffset("_BaseMap", m.GetTextureOffset("_BaseMap"));
                        mm.SetColor("_BaseColor", GetColor(m));
                        mm.SetFloat("_Smoothness", 0f);
                        mm.SetFloat("_Metallic", 0f);
                        mm.SetFloat("_SpecularHighlights", 0f); mm.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                        mm.SetFloat("_EnvironmentReflections", 0f); mm.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                        mm.enableInstancing = true;
                        done[m] = mm;
                    }
                    mats[i] = mm; ch = true;
                }
                if (ch) r.sharedMaterials = mats;
            }
        }

        public static void ApplyTexture(Material m, Texture t, float tiling)
        {
            if (m == null || t == null) return;
            SetTex(m, t);
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", new Vector2(tiling, tiling));
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        /// <summary>Value noise periodic (se repeta la "period" celule) => textura fara cusaturi.</summary>
        static float Noise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int ax = ((x0 % period) + period) % period, bx = (ax + 1) % period;
            int ay = ((y0 % period) + period) % period, by = (ay + 1) % period;
            float a = Hash(ax, ay, seed), b = Hash(bx, ay, seed), c = Hash(ax, by, seed), d = Hash(bx, by, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float u, float v, int seed)
        {
            float s = 0, amp = 0.5f; int per = 4;
            for (int o = 0; o < 4; o++)
            {
                s += Noise(u * per, v * per, per, seed + o * 17) * amp;
                amp *= 0.5f; per *= 2;
            }
            return s;
        }

        static readonly Dictionary<string, Texture2D> painted = new Dictionary<string, Texture2D>();

        /// <summary>Textura pictata din Resources/Textures (Ground0..3 = pajiste, nisip, zapada, roca vulcanica; Cobble = pavaj).</summary>
        public static Texture2D PaintedTex(string n)
        {
            Texture2D t;
            if (painted.TryGetValue(n, out t) && t != null) return t;
            t = Resources.Load<Texture2D>("Textures/" + n);
            if (t != null) { t.wrapMode = TextureWrapMode.Repeat; t.anisoLevel = 4; }
            painted[n] = t;
            return t;
        }

        public static Texture2D GroundTex(int kind)
        {
            kind = Mathf.Clamp(kind, 0, 3);
            if (groundTex[kind] != null) return groundTex[kind];
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true);
            tex.name = "ground" + kind;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 4;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float n = Fbm(u, v, 11 + kind * 7);
                    float val;
                    if (kind == 0)
                    {
                        // iarba: pete + "fire" fine
                        float blades = Noise(u * 64, v * 64, 64, 91);
                        val = 0.84f + n * 0.14f + (blades > 0.78f ? 0.06f : 0f) - (blades < 0.12f ? 0.07f : 0f);
                    }
                    else if (kind == 1)
                    {
                        // nisip: valuri de dune
                        float rip = Mathf.Sin((v * 10f + n * 1.6f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                        val = 0.86f + rip * 0.07f + n * 0.08f;
                    }
                    else if (kind == 2)
                    {
                        // zapada: moale + sclipiri
                        float sp = Hash(x, y, 5);
                        val = 0.9f + n * 0.08f + (sp > 0.996f ? 0.1f : 0f);
                    }
                    else
                    {
                        // pavaj: celule Voronoi (pietre) cu rosturi
                        float cells = 6f;
                        float gx = u * cells, gy = v * cells;
                        int cx = Mathf.FloorToInt(gx), cy = Mathf.FloorToInt(gy);
                        float d1 = 9f, d2 = 9f; float stoneId = 0;
                        for (int j = -1; j <= 1; j++)
                            for (int i = -1; i <= 1; i++)
                            {
                                int qx = cx + i, qy = cy + j;
                                int wx = ((qx % 6) + 6) % 6, wy = ((qy % 6) + 6) % 6;
                                float px2 = qx + 0.2f + Hash(wx, wy, 3) * 0.6f, py2 = qy + 0.2f + Hash(wx, wy, 4) * 0.6f;
                                float dd = (gx - px2) * (gx - px2) + (gy - py2) * (gy - py2);
                                if (dd < d1) { d2 = d1; d1 = dd; stoneId = Hash(wx, wy, 8); }
                                else if (dd < d2) d2 = dd;
                            }
                        float edge = Mathf.Sqrt(d2) - Mathf.Sqrt(d1);
                        val = edge < 0.06f ? 0.62f : 0.82f + stoneId * 0.12f + n * 0.06f;
                    }
                    byte b = (byte)(Mathf.Clamp01(val) * 255);
                    px[y * N + x] = new Color32(b, b, b, 255);
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            groundTex[kind] = tex;
            return tex;
        }

        public static void SetLayerShadows(GameObject go, bool cast)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
