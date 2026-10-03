// =====================================================================
//  NatureKit.cs - modelele "Stylized Nature MegaKit" (Quaternius, CC0)
//  din Resources/Models/Megakit, aceleasi pe toate hartile (acelasi stil),
//  doar nuantate dupa tema: pajiste verde, desert uscat, munte inzapezit,
//  vulcan ars. Folosit de WorldBuilder si PlazaBuilder.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class NatureKit
    {
        public const string P = "Models/Megakit/";
        public static bool Available { get { return ModelLib.Exists(P + "CommonTree_1"); } }

        public static readonly string[] Common = { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5" };
        public static readonly string[] Twisted = { "TwistedTree_1", "TwistedTree_2", "TwistedTree_3", "TwistedTree_4", "TwistedTree_5" };
        public static readonly string[] Pines = { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5" };
        public static readonly string[] Dead = { "DeadTree_1", "DeadTree_2", "DeadTree_3", "DeadTree_4", "DeadTree_5" };
        public static readonly string[] Rocks = { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3" };
        public static readonly string[] Pebbles = { "Pebble_Round_1", "Pebble_Round_2", "Pebble_Round_3", "Pebble_Round_4", "Pebble_Round_5", "Pebble_Square_1", "Pebble_Square_2", "Pebble_Square_3" };
        public static readonly string[] Bushes = { "Bush_Common", "Bush_Common_Flowers" };
        public static readonly string[] Flowers = { "Flower_3_Group", "Flower_4_Group", "Flower_3_Single", "Flower_4_Single", "Plant_1", "Plant_7", "Fern_1", "Clover_1", "Clover_2" };
        public static readonly string[] Grass = { "Grass_Common_Short", "Grass_Common_Tall", "Grass_Wispy_Short", "Grass_Wispy_Tall" };
        public static readonly string[] DryGrass = { "Grass_Wispy_Short", "Grass_Wispy_Tall" };
        public static readonly string[] Mushrooms = { "Mushroom_Common", "Mushroom_Laetiporus" };
        public static readonly string[] PathStones = { "RockPath_Round_Small_1", "RockPath_Round_Small_2", "RockPath_Round_Small_3", "RockPath_Square_Small_1", "RockPath_Square_Small_2", "RockPath_Square_Small_3" };

        /// <summary>Nuante pe harta: frunze, scoarta, roci (inmultite cu textura).</summary>
        public struct Theme { public Color leaves, bark, rock, grass, twisted; public string rockTex; }

        public static Theme For(int map)
        {
            switch (map)
            {
                case 1: return new Theme { leaves = new Color(0.95f, 0.85f, 0.55f), bark = new Color(1f, 0.92f, 0.82f), rock = Color.white, rockTex = "Rocks_Desert_Diffuse",
                                           grass = new Color(0.86f, 0.74f, 0.42f), twisted = new Color(0.7f, 0.62f, 0.32f) };
                case 2: return new Theme { leaves = new Color(0.8f, 0.92f, 0.96f), bark = new Color(0.9f, 0.92f, 1f), rock = new Color(0.9f, 0.94f, 1f), rockTex = null,
                                           grass = new Color(0.72f, 0.84f, 0.82f), twisted = new Color(0.8f, 0.9f, 0.95f) };
                case 3: return new Theme { leaves = new Color(0.55f, 0.35f, 0.25f), bark = new Color(0.42f, 0.32f, 0.3f), rock = new Color(0.45f, 0.36f, 0.34f), rockTex = null,
                                           grass = new Color(0.45f, 0.34f, 0.24f), twisted = new Color(0.42f, 0.2f, 0.12f) };
                default: return new Theme { leaves = new Color(0.92f, 1f, 0.86f), bark = Color.white, rock = Color.white, rockTex = null,
                                            grass = new Color(0.5f, 0.78f, 0.28f), twisted = new Color(0.42f, 0.7f, 0.26f) };
            }
        }

        static readonly Dictionary<string, Material> variants = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        static Texture2D Tex(string n)
        {
            Texture2D t;
            if (texCache.TryGetValue(n, out t)) return t;
            t = Resources.Load<Texture2D>(P + "Textures/" + n);
            texCache[n] = t;
            return t;
        }

        /// <summary>Pune modelul si il nuanteaza dupa tema hartii (materialele variante sunt partajate = putine draw calls).</summary>
        public static Transform Place(string name, Vector3 pos, float size, float yaw, Transform parent, Theme th, bool byWidth = false)
        {
            var t = ModelLib.Place(P + name, pos, size, yaw, parent, byWidth);
            if (t == null) return null;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    string mn = m.name.ToLowerInvariant();
                    Color tint = Color.white; Texture2D swap = null;
                    if (mn.Contains("rock") && !mn.Contains("path")) { tint = th.rock; if (!string.IsNullOrEmpty(th.rockTex)) swap = Tex(th.rockTex); }
                    else if (mn.Contains("bark")) tint = th.bark;
                    else if (mn.Contains("twistedtree") && mn.Contains("lea")) { tint = th.twisted; swap = Tex("Leaves_TwistedTree"); }   // frunzele "albe" se coloreaza (in loc de rosu)
                    else if (mn.Contains("grass")) tint = th.grass;
                    else if (mn.Contains("lea") || mn.Contains("pine")) tint = th.leaves;
                    else if (mn.Contains("path")) tint = Color.Lerp(Color.white, th.rock, 0.6f);
                    bool cut = mn.Contains("lea") || mn.Contains("pine") || mn.Contains("grass") || mn.Contains("flower");
                    if (tint == Color.white && swap == null && !cut) continue;   // frunzele: mereu varianta cu transparenta setata explicit
                    string key = m.name + "|" + ColorUtility.ToHtmlStringRGB(tint) + "|" + (swap != null ? swap.name : "");
                    Material v;
                    if (!variants.TryGetValue(key, out v) || v == null)
                    {
                        v = new Material(m) { name = m.name + "_" + ColorUtility.ToHtmlStringRGB(tint) };
                        if (v.HasProperty("_BaseColor")) v.SetColor("_BaseColor", tint);
                        if (swap != null && v.HasProperty("_BaseMap")) v.SetTexture("_BaseMap", swap);
                        if (cut) Cutout(v);
                        v.enableInstancing = true;
                        variants[key] = v;
                    }
                    mats[i] = v; changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
            return t;
        }

        /// <summary>Transparenta taiata + doua fete, setata explicit (altfel unele frunze apar ca patrate albe).</summary>
        static void Cutout(Material v)
        {
            v.SetFloat("_AlphaClip", 1f);
            v.SetFloat("_Cutoff", 0.2f);
            v.EnableKeyword("_ALPHATEST_ON");
            v.SetFloat("_Surface", 0f);
            v.SetFloat("_Cull", 0f);
            if (v.HasProperty("_Smoothness")) v.SetFloat("_Smoothness", 0.05f);
            v.SetOverrideTag("RenderType", "TransparentCutout");
            v.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }

        public static void Reset() { variants.Clear(); texCache.Clear(); }
    }
}
