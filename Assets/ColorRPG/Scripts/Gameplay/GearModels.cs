// =====================================================================
//  GearModels.cs - armele 3D reale (CC0: KayKit Fantasy Weapons Bits +
//  Quaternius Modular Weapons) din Resources/Models/Weapons.
//   * fiecare tip de arma are alt model pe fiecare harta (6 tipuri x 4 harti)
//   * modelul e orientat automat: maner la y=0, varful in sus (+Y), lat pe X
//   * metalul se nuanteaza dupa harta, seturile primesc culoarea setului
//   * raritatea adauga piese: panglica (Uncommon), pietre (Rare), stralucire
//     (Epic), scantei in jurul armei (Legendary), culori care curg (Mythic)
//  Daca un model lipseste, GearVisuals construieste arma veche din cod.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class GearModels
    {
        const string P = "Models/Weapons/";

        //                                Meadow        Desert            Frozen        Lava
        static readonly string[][] Table =
        {
            new[] { "KK_sword_A",  "Q_Sword_2",      "KK_sword_C",  "Q_Sword_Golden" },   // Sword
            new[] { "KK_axe_A",    "Q_Axe_Small",    "KK_axe_B",    "Q_Axe_Double" },     // Axe
            new[] { "KK_hammer_A", "Q_Hammer_Small", "KK_hammer_B", "Q_Hammer_Double" },  // Hammer
            new[] { "KK_spear_A",  "Q_Spear",        "KK_halberd",  "Q_Scythe" },         // Spear
            new[] { "KK_dagger_A", "Q_Dagger",       "KK_sword_E",  "Q_Dagger_2" },       // Saber
            new[] { "KK_sword_B",  "Q_Claymore",     "KK_sword_D",  "Q_Sword_Big" },      // Greatsword
        };
        // lungimea totala a armei (in L), ca la armele vechi
        static readonly float[] KindLen = { 0.98f, 1.02f, 1.02f, 1.36f, 0.95f, 1.16f };
        // corectii manuale: modelul iese invers (varful jos) / rotit pe Y
        static readonly Dictionary<string, bool> Flip = new Dictionary<string, bool>();
        static readonly Dictionary<string, float> Yaw = new Dictionary<string, float>();

        static readonly Color[] TierTint = { new Color(0.85f, 0.88f, 0.92f), new Color(1f, 0.78f, 0.45f), new Color(0.7f, 0.88f, 1f), new Color(0.45f, 0.36f, 0.5f) };
        static readonly Dictionary<string, Material> tinted = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { tinted.Clear(); }

        public static string ModelName(Item it)
        {
            ItemCatalog.Def d;
            if (ItemCatalog.TryGet(it, out d) && !string.IsNullOrEmpty(d.model)) return d.model;
            int k = Mathf.Clamp((int)ItemDB.KindOf(it), 0, Table.Length - 1);
            return Table[k][Mathf.Clamp(it.tier, 0, 3)];
        }

        /// <summary>Construieste arma din modelul 3D. false = modelul nu exista (se foloseste cea veche).</summary>
        public static bool TryBuildWeapon(Item it, Transform root, float L)
        {
            string name = ModelName(it);
            var prefab = ModelLib.Prefab(P + name);
            if (prefab == null) return false;

            var holder = new GameObject("Model").transform;
            holder.SetParent(root, false);
            var inst = Object.Instantiate(prefab, holder, false);
            inst.name = name;
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Art.Kill(c);
            foreach (var an in inst.GetComponentsInChildren<Animator>(true)) Art.Kill(an);
            Art.FixMaterials(inst);

            // ---- puncte in spatiul lui holder (pentru orientare)
            var pts = new List<Vector3>(512);
            Matrix4x4 toHolder = holder.worldToLocalMatrix;
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                Matrix4x4 m = toHolder * mf.transform.localToWorldMatrix;
                Vector3[] v = null;
                try { if (mesh.isReadable) v = mesh.vertices; } catch (System.Exception) { v = null; }
                if (v != null && v.Length > 0)
                {
                    int step = Mathf.Max(1, v.Length / 600);
                    for (int i = 0; i < v.Length; i += step) pts.Add(m.MultiplyPoint3x4(v[i]));
                }
                else
                {
                    var b = mesh.bounds;   // fara varfuri: colturile cutiei
                    for (int i = 0; i < 8; i++)
                        pts.Add(m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                }
            }
            if (pts.Count < 2) { Object.Destroy(holder.gameObject); return false; }

            var kind = ItemDB.KindOf(it);
            Quaternion rot = Orientation(pts, kind, name);
            for (int i = 0; i < pts.Count; i++) pts[i] = rot * pts[i];
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            float len = Mathf.Max(0.0001f, maxY - minY);
            // manerul (ultimii 12% de jos) pe axa Y
            Vector3 hc = Vector3.zero; int hn = 0;
            foreach (var p in pts) if (p.y < minY + len * 0.12f) { hc += p; hn++; }
            if (hn > 0) hc /= hn;
            float s = KindLen[(int)kind] * L / len;
            inst.transform.localRotation = rot * inst.transform.localRotation;
            inst.transform.localPosition = rot * inst.transform.localPosition;
            holder.localScale = Vector3.one * s;
            holder.localPosition = new Vector3(-hc.x * s, -minY * s, -hc.z * s);

            Tint(it, inst);
            Extras(it, root, KindLen[(int)kind] * L, kind);
            return true;
        }

        /// <summary>Axa lunga -> +Y (capul armei sus), latimea -> X, grosimea -> Z.</summary>
        static Quaternion Orientation(List<Vector3> pts, ItemDB.WeaponKind kind, string name)
        {
            Vector3 mn = pts[0], mx = pts[0];
            foreach (var p in pts) { mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
            Vector3 ext = mx - mn;
            int ax = ext.x >= ext.y && ext.x >= ext.z ? 0 : ext.y >= ext.z ? 1 : 2;
            Vector3 axis = ax == 0 ? Vector3.right : ax == 1 ? Vector3.up : Vector3.forward;
            float lo = mn[ax], hi = mx[ax], span = Mathf.Max(0.0001f, hi - lo);
            Vector3 center = (mn + mx) * 0.5f;
            // cat de "lat" e fiecare capat (30%)
            float wLo = 0f, wHi = 0f;
            foreach (var p in pts)
            {
                Vector3 d = p - center; d[ax] = 0f;
                float w = d.magnitude;
                float t = (p[ax] - lo) / span;
                if (t < 0.3f) wLo = Mathf.Max(wLo, w);
                else if (t > 0.7f) wHi = Mathf.Max(wHi, w);
            }
            bool blade = kind == ItemDB.WeaponKind.Sword || kind == ItemDB.WeaponKind.Saber || kind == ItemDB.WeaponKind.Greatsword;
            // lamele: garda (lata) e langa maner; topor / ciocan / sulita: capul lat e sus
            bool headHigh = blade ? wLo > wHi : wHi >= wLo;
            bool f;
            if (Flip.TryGetValue(name, out f) && f) headHigh = !headHigh;
            Quaternion q = Quaternion.FromToRotation(headHigh ? axis : -axis, Vector3.up);
            // latimea pe X: rotim in jurul lui Y ca partea cea mai lata sa stea pe X
            float bestW = -1f, bestA = 0f;
            for (int a = 0; a < 180; a += 15)
            {
                Quaternion r = Quaternion.Euler(0, a, 0) * q;
                float wx = 0f, wz = 0f;
                foreach (var p in pts) { Vector3 v = r * (p - center); wx = Mathf.Max(wx, Mathf.Abs(v.x)); wz = Mathf.Max(wz, Mathf.Abs(v.z)); }
                float score = wx - wz;
                if (score > bestW) { bestW = score; bestA = a; }
            }
            float y;
            if (Yaw.TryGetValue(name, out y)) bestA += y;
            return Quaternion.Euler(0, bestA, 0) * q;
        }

        /// <summary>Metalul nuantat dupa harta (seturile: culoarea setului), stralucire de la Epic in sus.</summary>
        static void Tint(Item it, GameObject inst)
        {
            Color rc = ItemDB.Col(it.rarity);
            // harta da metalul de baza; raritatea coloreaza tot mai mult arma (Legendary aurie, Mythic rosie)
            Color metal = TierTint[Mathf.Clamp(it.tier, 0, 3)];
            Color mapTint = Color.Lerp(Color.white, metal, 0.35f);
            mapTint = Color.Lerp(mapTint, Color.Lerp(Color.white, rc, 0.85f), Mathf.Max(0, it.rarity - 1) * 0.11f);
            Color emit = it.rarity >= 3 ? rc * (0.1f + 0.08f * (it.rarity - 3) + 0.004f * it.plus) : Color.black;
            string key = ColorUtility.ToHtmlStringRGB(mapTint) + "|" + ColorUtility.ToHtmlStringRGB(emit);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    var src = ms[i];
                    if (src == null) continue;
                    string k = src.GetHashCode() + "|" + src.name + "|" + key;
                    Material m;
                    if (!tinted.TryGetValue(k, out m) || m == null)
                    {
                        m = new Material(src) { name = src.name + "_T" };
                        Art.SetColor(m, Art.GetColor(src) * mapTint);
                        if (emit.maxColorComponent > 0.01f) Art.SetEmission(m, emit);
                        m.enableInstancing = true;
                        tinted[k] = m;
                    }
                    ms[i] = m;
                }
                r.sharedMaterials = ms;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        /// <summary>Piesele de raritate puse peste model (len = lungimea armei).</summary>
        static void Extras(Item it, Transform root, float len, ItemDB.WeaponKind kind)
        {
            if (it.rarity <= 0) return;
            Color acc = GearVisuals.AccentOf(it), rc = ItemDB.Col(it.rarity);
            bool blade = kind == ItemDB.WeaponKind.Sword || kind == ItemDB.WeaponKind.Saber || kind == ItemDB.WeaponKind.Greatsword;
            // Uncommon+: panglica colorata pe maner
            var ribbon = GearVisuals.Mat(acc, 0.1f, 0.3f, Color.black);
            var t = GearVisuals.Torus(root, new Vector3(0, len * 0.09f, 0), Vector3.one * len * 0.07f, 0.28f, ribbon, new Vector3(0, 0, 0));
            if (it.rarity >= 2)
            {
                // Rare+: piatra pretioasa (la garda pentru lame, la cap pentru restul), pe ambele fete
                var gem = GearVisuals.Mat(rc, 0.2f, 0.9f, rc * (0.25f + 0.12f * it.rarity));
                float gy = blade ? 0.22f : kind == ItemDB.WeaponKind.Spear ? 0.7f : 0.8f;
                float gs = len * (0.045f + 0.008f * it.rarity);
                for (int sd = -1; sd <= 1; sd += 2)
                    GearVisuals.GemAt(root, new Vector3(0, len * gy, sd * len * 0.03f), gs, gem, true);
                if (it.rarity >= 3)   // Epic+: inca doua pietre mai mici
                    for (int sd = -1; sd <= 1; sd += 2)
                        GearVisuals.GemAt(root, new Vector3(sd * len * 0.05f, len * (blade ? 0.24f : 0.74f), len * 0.03f), gs * 0.6f, gem, false);
            }
            if (it.rarity >= 4)
            {
                var fx = root.gameObject.AddComponent<RarityAura>();
                fx.col = rc; fx.len = len; fx.mythic = it.rarity >= 5;
            }
        }
    }

    /// <summary>Scantei in jurul armelor Legendary / Mythic (doar in lume, nu in iconite). Mythic: culorile curg.</summary>
    public class RarityAura : MonoBehaviour
    {
        public Color col = Color.white;
        public float len = 1f;
        public bool mythic;
        float acc;

        void Update()
        {
            if (gameObject.layer == GearIcons.Layer || Game.I == null) return;
            acc += Time.deltaTime * (mythic ? 9f : 5f);
            while (acc >= 1f)
            {
                acc -= 1f;
                Vector3 p = transform.TransformPoint(new Vector3(Random.Range(-0.06f, 0.06f) * len, Random.Range(0.2f, 1f) * len, 0));
                Color c = mythic ? Color.HSVToRGB(Mathf.Repeat(Time.time * 0.25f + Random.value * 0.2f, 1f), 0.7f, 1f) : Color.Lerp(col, Color.white, Random.value * 0.4f);
                Fx.Sparkle(p, c, 1, 0.06f);
            }
        }
    }
}
