// =====================================================================
//  GearVisuals.cs - modelele 3D pentru echipament, construite din cod.
//   * Fiecare item arata altfel: tipul (Sword/Axe/.., Helm/Cap/Hood/Crown,
//     Plate/Mail/Tunic/Vest, Ring/Band/..) x 4 stiluri x culoarea lui.
//     Stilul si culoarea vin din id-ul itemului (raman aceleasi mereu).
//   * Piesele aceluiasi SET arata la fel (stil + culoarea setului).
//   * Coifurile / armurile sunt cutii rotunjite (ca stilul eroilor Kenney),
//     potrivite dupa capul / trunchiul fiecarui erou.
//   * Iconitele din inventar sunt randate din ACESTE modele (GearIcons),
//     deci ce vezi in inventar e exact ce porti.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class GearVisuals
    {
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { mats.Clear(); }

        /// <summary>Metalul de baza pe harta: fier, bronz, argint-albastrui, obsidian.</summary>
        static readonly Color[] TierMetal =
        {
            new Color(0.70f, 0.72f, 0.76f), new Color(0.80f, 0.60f, 0.36f),
            new Color(0.72f, 0.84f, 0.95f), new Color(0.26f, 0.22f, 0.3f),
        };

        public static Material Mat(Color c, float metal, float smooth, Color emit)
        {
            string key = ColorUtility.ToHtmlStringRGB(c) + metal.ToString("0.0") + smooth.ToString("0.0") + ColorUtility.ToHtmlStringRGB(emit);
            Material m;
            if (mats.TryGetValue(key, out m) && m != null) return m;
            m = Art.NewLit(c, smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (emit.maxColorComponent > 0.01f) Art.SetEmission(m, emit);
            m.enableInstancing = true;
            mats[key] = m;
            return m;
        }

        // ---------------------------------------------------------------
        //  Identitatea vizuala a itemului
        // ---------------------------------------------------------------
        /// <summary>Arma afisata cand nu ai nimic echipat (aceeasi mereu - nu mai schimba culoarea la fiecare redesenare).</summary>
        static readonly Item FallbackWeapon = new Item { id = 1, slot = 0, rarity = 0, tier = 0, roll = 1f, name = "Wooden Sword" };

        static uint Hash(Item it)
        {
            uint h = it.cat > 0 ? (uint)(it.cat * 7919 + it.slot * 131 + 17) * 2654435761u
                                : (uint)it.id * 2654435761u ^ (uint)(it.slot * 40503);
            h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
            return h;
        }

        /// <summary>Stilul (0..3) in cadrul tipului. Piesele unui set au acelasi stil.</summary>
        public static int Style(Item it)
        {
            ItemCatalog.Def d;
            if (ItemCatalog.TryGet(it, out d)) return d.style;
            return it == null ? 0 : (int)(Hash(it) % 4u);
        }
        static float Hue(Item it) { return ((Hash(it) >> 4) % 18u) / 18f; }
        /// <summary>Indexul culorii (0..17) - doua iteme cu acelasi stil si aceeasi culoare arata la fel.</summary>
        public static int HueIndex(Item it) { return it == null ? 0 : (int)((Hash(it) >> 4) % 18u); }

        /// <summary>Da itemului un id nou (unic), pastrand acelasi stil si aceeasi culoare.</summary>
        public static void FreshIdKeepLook(Item it)
        {
            if (it == null) return;
            int style = Style(it), hue = HueIndex(it);
            it.id = ItemDB.NextIdTake();
            if (it.cat == 0)
                for (int k = 0; k < 4000 && (Style(it) != style || HueIndex(it) != hue); k++) it.id = ItemDB.NextIdTake();
        }

        /// <summary>Tipul de baza din nume (Helm, Cap, Hood, Crown / Plate, Mail, Tunic, Vest / Ring, Band, Loop, Signet).</summary>
        public static int BaseOf(Item it)
        {
            ItemCatalog.Def d;
            if (ItemCatalog.TryGet(it, out d)) return d.baseIdx;
            string n = it == null || it.name == null ? "" : it.name;
            switch (it == null ? Slot.Weapon : it.S)
            {
                case Slot.Weapon: return (int)ItemDB.KindOf(it);
                case Slot.Helmet: return n.Contains("Crown") ? 3 : n.Contains("Hood") ? 2 : n.Contains("Cap") ? 1 : 0;
                case Slot.Armor: return n.Contains("Vest") ? 3 : n.Contains("Tunic") ? 2 : n.Contains("Mail") ? 1 : 0;
                default: return n.Contains("Signet") ? 3 : n.Contains("Loop") ? 2 : n.Contains("Band") ? 1 : 0;
            }
        }

        /// <summary>Cheia vizuala (pentru cache-ul de iconite): doua iteme cu aceeasi cheie arata identic.</summary>
        public static string VisualKey(Item it)
        {
            if (it == null) return "none";
            return "c" + it.cat + "_" + it.slot + "_" + BaseOf(it) + "_" + Style(it) + "_" + ((Hash(it) >> 4) % 18u) + "_" + it.tier + "_" + it.rarity + "_" + it.set + "_" + (it.rarity >= 3 || it.plus >= 10 ? it.plus : 0);   // stralucirea creste cu forja la Epic+
        }

        public static Color MetalOf(Item it)
        {
            Color baseCol = TierMetal[Mathf.Clamp(it.tier, 0, TierMetal.Length - 1)];
            return Color.Lerp(baseCol, ItemDB.Col(it.rarity), 0.12f + 0.08f * it.rarity);
        }

        /// <summary>Culoarea proprie a itemului (setul are culoarea lui).</summary>
        public static Color AccentOf(Item it)
        {
            ItemCatalog.Def d;
            if (ItemCatalog.TryGet(it, out d)) return d.accent;
            return Color.HSVToRGB(Hue(it), 0.62f, 0.86f);
        }

        static Color Cloth(Item it) { return Color.Lerp(AccentOf(it), new Color(0.3f, 0.26f, 0.32f), 0.25f); }

        static Color Glow(Item it, Color c)
        {
            if (it.rarity < 3 && it.plus < 10) return Color.black;
            float k = 0.25f + 0.15f * Mathf.Max(0, it.rarity - 3) + 0.02f * it.plus;
            return c * k;
        }

        struct Mats { public Material metal, acc, gem, cloth, leather, dark, gold, bone, wood, grip; }

        static Mats MatsOf(Item it)
        {
            Color metal = MetalOf(it), acc = AccentOf(it), rc = ItemDB.Col(it.rarity);
            var m = new Mats();
            m.metal = Mat(metal, it.tier == 3 ? 0.4f : 0.8f, 0.65f, Glow(it, rc) * 0.3f);
            m.acc = Mat(acc, 0.35f, 0.55f, Glow(it, acc) * 0.5f);
            m.gem = Mat(rc, 0.2f, 0.9f, rc * (0.3f + 0.2f * it.rarity));
            m.cloth = Mat(Cloth(it), 0f, 0.15f, Color.black);
            m.leather = Mat(Color.Lerp(new Color(0.45f, 0.29f, 0.16f), acc, 0.12f), 0f, 0.3f, Color.black);
            m.dark = Mat(new Color(0.12f, 0.11f, 0.14f), 0.3f, 0.4f, Color.black);
            m.gold = Mat(Color.Lerp(new Color(1f, 0.8f, 0.3f), acc, 0.2f), 0.9f, 0.75f, Glow(it, acc) * 0.4f);
            m.bone = Mat(new Color(0.93f, 0.89f, 0.78f), 0f, 0.35f, Color.black);
            m.wood = Mat(new Color(0.45f, 0.3f, 0.17f), 0f, 0.2f, Color.black);
            m.grip = Mat(it.rarity >= 4 ? Color.Lerp(acc, Color.black, 0.6f) : new Color(0.33f, 0.2f, 0.11f), 0f, 0.25f, Color.black);
            return m;
        }

        // ---------------------------------------------------------------
        //  Piese
        // ---------------------------------------------------------------
        static GameObject Part(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default(Vector3))
        {
            var go = Art.Prim(t, pos, scale, Color.white, parent, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        static GameObject MeshPart(string n, Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 euler)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        /// <summary>Cutie rotunjita (round = cat de rotunde sunt colturile, 0..0.5).</summary>
        static GameObject RB(Transform p, Vector3 pos, Vector3 size, float round, Material m, Vector3 euler = default(Vector3))
        {
            return MeshPart("RB", MeshGen.RoundedBox(round, size), p, pos, size, m, euler);
        }

        /// <summary>Con cu baza in pos, inalt "h", raza "r", orientat dupa euler (implicit in sus).</summary>
        static GameObject Cone(Transform p, Vector3 pos, float r, float h, Material m, Vector3 euler = default(Vector3), float top = 0f)
        {
            return MeshPart("Cone", MeshGen.Cone(top), p, pos, new Vector3(r * 2f, h, r * 2f), m, euler);
        }

        /// <summary>Inel (tor) cu diametrul d si grosimea relativa tube (fata de d).</summary>
        static GameObject Ring(Transform p, Vector3 pos, Vector3 d, float tube, Material m, Vector3 euler = default(Vector3))
        {
            return MeshPart("Torus", MeshGen.Torus(tube), p, pos, d, m, euler);
        }

        public static GameObject Torus(Transform p, Vector3 pos, Vector3 d, float tube, Material m, Vector3 euler) { return Ring(p, pos, d, tube, m, euler); }
        public static void GemAt(Transform p, Vector3 pos, float s, Material m, bool diamond) { Gem(p, pos, s, m, diamond); }

        static void Gem(Transform p, Vector3 pos, float s, Material m, bool diamond)
        {
            if (diamond) Part(PrimitiveType.Cube, p, pos, new Vector3(s * 0.7f, s * 0.7f, s * 0.45f), m, new Vector3(0, 0, 45));
            else Part(PrimitiveType.Sphere, p, pos, Vector3.one * s, m);
        }

        // ---------------------------------------------------------------
        //  Arme: baza (pomul) la y=0, lama in sus pe +Y, lungime ~L
        // ---------------------------------------------------------------
        public static Transform BuildWeapon(Item it, Transform parent, float L)
        {
            var root = new GameObject("WeaponMesh").transform;
            root.SetParent(parent, false);
            if (it == null) it = FallbackWeapon;
            if (GearModels.TryBuildWeapon(it, root, L)) return root;   // model 3D real (CC0), cu piesele de raritate
            var M = MatsOf(it);
            var mBlade = Mat(MetalOf(it), it.tier == 3 ? 0.4f : 0.85f, 0.7f, Glow(it, ItemDB.Col(it.rarity)) * 0.4f);
            int st = Style(it);

            switch (ItemDB.KindOf(it))
            {
                case ItemDB.WeaponKind.Axe:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.46f * L, 0), new Vector3(0.055f * L, 0.46f * L, 0.055f * L), M.wood);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        if (s < 0 && st != 1) continue;                     // stil 1 = topor dublu
                        float big = st == 2 ? 1.25f : 1f;                    // stil 2 = lama "cu barba" (mai lunga in jos)
                        Part(PrimitiveType.Cube, root, new Vector3(s * 0.13f * L, (st == 2 ? 0.78f : 0.82f) * L, 0), new Vector3(0.24f * L, 0.2f * L * big, 0.03f * L), mBlade);
                        Part(PrimitiveType.Cylinder, root, new Vector3(s * 0.25f * L, (st == 2 ? 0.76f : 0.82f) * L, 0), new Vector3(0.28f * L, 0.018f * L, 0.28f * L * big), mBlade, new Vector3(90, 0, 0));
                    }
                    if (st != 1) Part(PrimitiveType.Cube, root, new Vector3(-0.08f * L, 0.84f * L, 0), new Vector3(0.12f * L, 0.06f * L, 0.05f * L), M.acc, new Vector3(0, 0, 20));
                    if (st == 3) Cone(root, new Vector3(0, 0.9f * L, 0), 0.04f * L, 0.22f * L, mBlade);   // varf de lance
                    RB(root, new Vector3(0, 0.82f * L, 0), new Vector3(0.09f, 0.13f, 0.09f) * L, 0.3f, M.acc);
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.02f * L, 0), Vector3.one * 0.08f * L, M.acc);
                    for (int k = 0; k < 2 + st; k++) Part(PrimitiveType.Cylinder, root, new Vector3(0, (0.18f + k * 0.08f) * L, 0), new Vector3(0.065f, 0.012f, 0.065f) * L, M.grip);
                    break;
                case ItemDB.WeaponKind.Hammer:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.42f * L, 0), new Vector3(0.06f * L, 0.42f * L, 0.06f * L), M.wood);
                    if (st == 1)
                    {   // buzdugan cu tepi
                        Vector3 c = new Vector3(0, 0.88f * L, 0);
                        Part(PrimitiveType.Sphere, root, c, Vector3.one * 0.26f * L, mBlade);
                        for (int k = 0; k < 8; k++)
                        {
                            Vector3 d = k < 4 ? Quaternion.Euler(0, k * 90, 0) * Vector3.right : Quaternion.Euler(0, k * 90 + 45, 0) * new Vector3(0.7f, 0.7f, 0);
                            Cone(root, c + d * 0.1f * L, 0.04f * L, 0.12f * L, M.acc, Quaternion.FromToRotation(Vector3.up, d).eulerAngles);
                        }
                        Cone(root, c + Vector3.up * 0.1f * L, 0.04f * L, 0.12f * L, M.acc);
                    }
                    else if (st == 3)
                    {   // ciocan mare rotund
                        Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.86f * L, 0), new Vector3(0.24f * L, 0.22f * L, 0.24f * L), mBlade, new Vector3(0, 0, 90));
                        for (int s = -1; s <= 1; s += 2)
                            Part(PrimitiveType.Cylinder, root, new Vector3(s * 0.22f * L, 0.86f * L, 0), new Vector3(0.27f * L, 0.025f * L, 0.27f * L), M.acc, new Vector3(0, 0, 90));
                    }
                    else
                    {
                        RB(root, new Vector3(0, 0.84f * L, 0), new Vector3(0.38f, 0.19f, 0.19f) * L, 0.18f, mBlade);
                        RB(root, new Vector3(0, 0.84f * L, 0), new Vector3(0.4f, 0.06f, 0.21f) * L, 0.3f, M.acc);
                        if (st == 2) Cone(root, new Vector3(-0.19f * L, 0.84f * L, 0), 0.07f * L, 0.22f * L, mBlade, new Vector3(0, 0, 90));   // tepusa in spate
                        else Part(PrimitiveType.Cylinder, root, new Vector3(-0.2f * L, 0.84f * L, 0), new Vector3(0.14f * L, 0.02f * L, 0.14f * L), M.acc, new Vector3(0, 0, 90));
                        Part(PrimitiveType.Cylinder, root, new Vector3(0.2f * L, 0.84f * L, 0), new Vector3(0.14f * L, 0.02f * L, 0.14f * L), M.acc, new Vector3(0, 0, 90));
                    }
                    Part(PrimitiveType.Sphere, root, new Vector3(0, (st == 1 ? 1.08f : 0.95f) * L, 0), Vector3.one * 0.07f * L, M.gem);
                    break;
                case ItemDB.WeaponKind.Spear:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.55f * L, 0), new Vector3(0.045f * L, 0.55f * L, 0.045f * L), M.wood);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 1.08f * L, 0), new Vector3(0.08f * L, 0.03f * L, 0.08f * L), M.acc);
                    if (st == 1)
                    {   // trident
                        Part(PrimitiveType.Cube, root, new Vector3(0, 1.12f * L, 0), new Vector3(0.3f * L, 0.03f * L, 0.03f * L), mBlade);
                        for (int s = -1; s <= 1; s++)
                            Cone(root, new Vector3(s * 0.14f * L, 1.12f * L, 0), 0.03f * L, (s == 0 ? 0.32f : 0.24f) * L, mBlade);
                    }
                    else if (st == 2)
                    {   // glaive: lama curbata
                        for (int k = 0; k < 4; k++)
                            Part(PrimitiveType.Cube, root, new Vector3(0.01f * L * k * k, (1.14f + k * 0.08f) * L, 0), new Vector3(0.09f * L, 0.1f * L, 0.02f * L), mBlade, new Vector3(0, 0, -8f * k));
                    }
                    else
                    {
                        Part(PrimitiveType.Sphere, root, new Vector3(0, 1.22f * L, 0), new Vector3(0.11f * L, 0.3f * L, 0.035f * L) * (st == 3 ? 0.8f : 1f), mBlade);
                        Part(PrimitiveType.Cube, root, new Vector3(0, 1.05f * L, 0), new Vector3(0.2f * L, 0.025f * L, 0.025f * L), M.acc);
                        if (st == 3)   // pamblica
                            Part(PrimitiveType.Cube, root, new Vector3(0.08f * L, 0.96f * L, 0), new Vector3(0.14f * L, 0.12f * L, 0.01f * L), M.cloth, new Vector3(0, 0, -15));
                    }
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.02f * L, 0), Vector3.one * 0.06f * L, M.acc);
                    break;
                case ItemDB.WeaponKind.Saber:
                    Hilt(root, L, M, st == 1 ? 0.12f : 0.26f, false, st == 1);
                {
                    // lama curbata continua: segmente care se suprapun, orientate dupa tangenta curbei x = c*t^2
                    float curve = st == 1 ? 0.12f : st == 2 ? 0.42f : 0.3f;   // cat de curbata (x la varf, in L)
                    const int segs = 8;
                    float len = 0.66f;
                    Vector3 tipPos = Vector3.zero; float tipAng = 0f;
                    for (int k = 0; k < segs; k++)
                    {
                        float t = (k + 0.5f) / segs;
                        float x = curve * t * t * len, y = 0.28f + t * len;
                        float ang = Mathf.Atan2(2f * curve * t * len, len) * Mathf.Rad2Deg;
                        float wdt = st == 2 ? 0.065f + t * 0.05f : st == 1 ? 0.05f : 0.07f;
                        Part(PrimitiveType.Cube, root, new Vector3(x * L, y * L, 0), new Vector3(wdt * L, (len / segs) * 1.35f * L, 0.02f * L), mBlade, new Vector3(0, 0, -ang));
                        tipPos = new Vector3(x * L, y * L, 0); tipAng = ang;
                    }
                    Vector3 dir = Quaternion.Euler(0, 0, -tipAng) * Vector3.up;
                    Part(PrimitiveType.Cube, root, tipPos + dir * 0.05f * L, new Vector3(0.05f * L, 0.05f * L, 0.02f * L), mBlade, new Vector3(0, 0, 45 - tipAng));
                }
                    if (st == 3) Part(PrimitiveType.Cube, root, new Vector3(0.02f * L, 0.55f * L, 0.011f * L), new Vector3(0.02f * L, 0.4f * L, 0.01f * L), M.acc, new Vector3(0, 0, -8));
                    break;
                case ItemDB.WeaponKind.Greatsword:
                {
                    Hilt(root, L, M, 0.5f, true, false);
                    float gw = st == 1 ? 0.2f : 0.14f;
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.66f * L, 0), new Vector3(gw * L, 0.78f * L, 0.03f * L), mBlade);
                    if (st == 1) Part(PrimitiveType.Cube, root, new Vector3(0.03f * L, 1.06f * L, 0), new Vector3(gw * 0.8f * L, 0.08f * L, 0.03f * L), mBlade, new Vector3(0, 0, 20));
                    else Part(PrimitiveType.Cube, root, new Vector3(0, 1.08f * L, 0), new Vector3(gw * 0.7f * L, gw * 0.7f * L, 0.03f * L), mBlade, new Vector3(0, 0, 45));
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.6f * L, 0.012f * L), new Vector3(0.025f * L, 0.6f * L, 0.012f * L), M.acc);
                    if (st == 2)   // "urechi" laterale (zweihander)
                        for (int s = -1; s <= 1; s += 2) Part(PrimitiveType.Cube, root, new Vector3(s * 0.1f * L, 0.42f * L, 0), new Vector3(0.06f, 0.04f, 0.03f) * L, mBlade, new Vector3(0, 0, s * 30));
                    if (st == 3)   // rune
                        for (int k = 0; k < 3; k++) Gem(root, new Vector3(0, (0.45f + k * 0.18f) * L, 0.02f * L), 0.045f * L, M.gem, true);
                    break;
                }
                default: // sabie
                    Hilt(root, L, M, st == 2 ? 0.22f : 0.34f, false, st == 2);
                    if (st == 1)
                    {   // lama "frunza" lata
                        Part(PrimitiveType.Sphere, root, new Vector3(0, 0.62f * L, 0), new Vector3(0.17f * L, 0.72f * L, 0.025f * L), mBlade);
                    }
                    else if (st == 3)
                    {   // flamberge (lama ondulata)
                        for (int k = 0; k < 7; k++)
                            Part(PrimitiveType.Cube, root, new Vector3((k % 2 == 0 ? 0.012f : -0.012f) * L, (0.32f + k * 0.085f) * L, 0), new Vector3(0.09f * L, 0.1f * L, 0.022f * L), mBlade, new Vector3(0, 0, k % 2 == 0 ? 8 : -8));
                        Part(PrimitiveType.Cube, root, new Vector3(0, 0.93f * L, 0), new Vector3(0.06f * L, 0.06f * L, 0.022f * L), mBlade, new Vector3(0, 0, 45));
                    }
                    else
                    {
                        float bw = st == 2 ? 0.05f : 0.09f;   // stil 2 = rapiera subtire
                        Part(PrimitiveType.Cube, root, new Vector3(0, 0.6f * L, 0), new Vector3(bw * L, 0.66f * L, 0.022f * L), mBlade);
                        Part(PrimitiveType.Cube, root, new Vector3(0, 0.93f * L, 0), new Vector3(bw * 0.7f * L, bw * 0.7f * L, 0.022f * L), mBlade, new Vector3(0, 0, 45));
                        if (st == 0) Part(PrimitiveType.Cube, root, new Vector3(0, 0.55f * L, 0.01f * L), new Vector3(0.018f * L, 0.5f * L, 0.01f * L), M.acc);
                    }
                    break;
            }
            return root;
        }

        static void Hilt(Transform root, float L, Mats M, float guardW, bool spikes, bool roundGuard)
        {
            Part(PrimitiveType.Sphere, root, new Vector3(0, 0.03f * L, 0), Vector3.one * 0.075f * L, M.acc);
            Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.14f * L, 0), new Vector3(0.05f * L, 0.1f * L, 0.05f * L), M.grip);
            if (roundGuard) Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.255f * L, 0), new Vector3(0.16f * L, 0.015f * L, 0.16f * L), M.acc);
            else RB(root, new Vector3(0, 0.255f * L, 0), new Vector3(guardW, 0.05f, 0.07f) * L, 0.35f, M.acc);
            Part(PrimitiveType.Sphere, root, new Vector3(0, 0.255f * L, 0.03f * L), Vector3.one * 0.05f * L, M.gem);
            if (spikes)
                for (int s = -1; s <= 1; s += 2)
                    Part(PrimitiveType.Cube, root, new Vector3(s * guardW * 0.5f * L, 0.29f * L, 0), new Vector3(0.05f * L, 0.08f * L, 0.05f * L), M.acc, new Vector3(0, 0, s * -30));
        }

        // ---------------------------------------------------------------
        //  Coif: root aliniat cu axele lumii in poza de bind, centrat pe cap.
        //  size = dimensiunea capului (world). Fata = +Z.
        // ---------------------------------------------------------------
        public static void BuildHelmet(Item it, Transform root, Vector3 size)
        {
            var M = MatsOf(it);
            float sx = size.x, sy = size.y, sz = size.z;
            // capul Kenney e aproape cub; bounds-ul pe Y include si gatul => il potrivim dupa latime, aliniat la crestet
            float syFit = Mathf.Min(sy, sx * 1.02f);
            if (syFit < sy)
            {
                var fit = new GameObject("Fit").transform;
                fit.SetParent(root, false);
                fit.localPosition = new Vector3(0, (sy - syFit) * 0.5f, 0);
                root = fit;
                sy = syFit;
            }
            int st = Style(it);
            System.Func<float, float, float, Vector3> P = (x, y, z) => new Vector3(x * sx, y * sy, z * sz);

            switch (BaseOf(it))
            {
                case 1: // ---- CAP (palarii / bonete)
                    if (st == 0)
                    {   // sapca cu cozoroc
                        RB(root, P(0, 0.33f, -0.01f), P(1.1f, 0.56f, 1.1f), 0.5f, M.cloth);
                        RB(root, P(0, 0.14f, 0.6f), P(0.92f, 0.06f, 0.42f), 0.5f, M.acc);
                        Part(PrimitiveType.Sphere, root, P(0, 0.57f, 0), Vector3.one * sx * 0.14f, M.acc);
                    }
                    else if (st == 1)
                    {   // bandana cu nod la spate
                        RB(root, P(0, 0.34f, -0.01f), P(1.1f, 0.5f, 1.1f), 0.5f, M.cloth);
                        Part(PrimitiveType.Sphere, root, P(0, 0.26f, -0.56f), Vector3.one * sx * 0.16f, M.cloth);
                        for (int s = -1; s <= 1; s += 2)
                            RB(root, P(s * 0.12f, 0.06f, -0.6f), P(0.12f, 0.36f, 0.05f), 0.5f, M.cloth, new Vector3(0, 0, s * 22));
                        for (int k = 0; k < 4; k++)
                            Part(PrimitiveType.Sphere, root, P(-0.3f + k * 0.2f, 0.38f + (k % 2) * 0.1f, 0.545f), Vector3.one * sx * 0.06f, M.bone);
                    }
                    else if (st == 2)
                    {   // palarie cu bor larg si pana
                        Part(PrimitiveType.Cylinder, root, P(0, 0.47f, 0), new Vector3(sx * 1.62f, sy * 0.025f, sz * 1.62f), M.leather);
                        RB(root, P(0, 0.64f, 0), P(0.86f, 0.38f, 0.86f), 0.38f, M.leather);
                        RB(root, P(0, 0.52f, 0), P(0.9f, 0.09f, 0.9f), 0.45f, M.acc);
                        Part(PrimitiveType.Sphere, root, P(0.36f, 0.86f, -0.15f), new Vector3(sx * 0.12f, sy * 0.55f, sx * 0.05f), M.acc, new Vector3(0, 0, -32));
                    }
                    else
                    {   // caciula cu mot
                        RB(root, P(0, 0.36f, -0.01f), P(1.1f, 0.58f, 1.1f), 0.5f, M.cloth);
                        RB(root, P(0, 0.15f, -0.01f), P(1.14f, 0.11f, 1.14f), 0.5f, M.acc);
                        Part(PrimitiveType.Sphere, root, P(0, 0.64f, 0), Vector3.one * sx * 0.26f, M.bone);
                    }
                    break;

                case 2: // ---- HOOD (glugi / palarii de mag)
                    if (st == 1)
                    {   // palarie de vrajitor
                        Part(PrimitiveType.Cylinder, root, P(0, 0.48f, 0), new Vector3(sx * 1.5f, sy * 0.03f, sz * 1.5f), M.cloth);
                        Cone(root, P(0, 0.46f, -0.02f), sx * 0.5f, sy * 1.1f, M.cloth, new Vector3(-12, 0, 6));
                        Ring(root, P(0, 0.56f, -0.02f), new Vector3(sx * 0.98f, sy * 0.6f, sz * 0.98f), 0.07f, M.acc);
                        Gem(root, P(0, 0.58f, 0.48f), sx * 0.13f, M.gem, true);
                        break;
                    }
                    // gluga (stilurile 0, 2, 3)
                    var hoodMat = st == 2 ? M.dark : M.cloth;
                    RB(root, P(0, 0.3f, -0.05f), P(1.16f, 0.72f, 1.14f), 0.5f, hoodMat);
                    RB(root, P(0, -0.12f, -0.38f), P(1.16f, 0.76f, 0.42f), 0.35f, hoodMat);
                    for (int s = -1; s <= 1; s += 2)
                        RB(root, P(s * 0.555f, -0.12f, -0.06f), P(0.08f, 0.72f, 0.94f), 0.4f, hoodMat);
                    RB(root, P(0, 0f, 0.555f), P(1.18f, 0.08f, 0.07f), 0.45f, M.acc);
                    RB(root, P(0, 0.62f, -0.5f), P(0.3f, 0.2f, 0.3f), 0.5f, hoodMat, new Vector3(-30, 0, 0));   // varful glugii
                    if (st == 2)
                        for (int s = -1; s <= 1; s += 2)   // coarne de demon
                            Cone(root, P(s * 0.3f, 0.56f, 0.05f), sx * 0.1f, sy * 0.34f, M.bone, new Vector3(0, 0, -s * 22));
                    if (st == 3)
                        RB(root, P(0, -0.26f, 0.08f), P(1.12f, 0.3f, 1.0f), 0.4f, hoodMat);   // masca peste gura (ninja)
                    break;

                case 3: // ---- CROWN
                    if (st == 1)
                    {   // diadema
                        Ring(root, P(0, 0.4f, 0), new Vector3(sx * 1.04f, sy * 0.5f, sz * 1.04f), 0.045f, M.gold, new Vector3(-10, 0, 0));
                        Gem(root, P(0, 0.56f, 0.5f), sx * 0.2f, M.gem, true);
                        for (int s = -1; s <= 1; s += 2) Cone(root, P(s * 0.22f, 0.44f, 0.47f), sx * 0.05f, sy * 0.16f, M.gold);
                    }
                    else if (st == 2)
                    {   // cununa de lauri
                        Ring(root, P(0, 0.44f, 0), new Vector3(sx * 1.06f, sy * 0.5f, sz * 1.06f), 0.04f, M.gold, new Vector3(-8, 0, 0));
                        var leaf = Mat(Color.Lerp(new Color(0.35f, 0.75f, 0.3f), AccentOf(it), 0.25f), 0.3f, 0.5f, Glow(it, AccentOf(it)) * 0.3f);
                        for (int k = 0; k < 14; k++)
                        {
                            float a = k / 14f * Mathf.PI * 2f;
                            Vector3 p = new Vector3(Mathf.Sin(a) * sx * 0.53f, sy * 0.46f + Mathf.Cos(a) * sy * 0.04f, Mathf.Cos(a) * sz * 0.53f);
                            Part(PrimitiveType.Sphere, root, p, new Vector3(sx * 0.07f, sy * 0.16f, sx * 0.03f), leaf, new Vector3(0, a * Mathf.Rad2Deg, 35));
                        }
                    }
                    else
                    {   // coroana (0) / coroana regala cu arcuri (3) - asezata PE cap (banda in jurul crestetului)
                        Ring(root, P(0, 0.43f, 0), new Vector3(sx * 1.02f, sy * 0.8f, sz * 1.02f), 0.08f, M.gold);
                        int n = st == 3 ? 4 : 6;
                        for (int k = 0; k < n; k++)
                        {
                            float a = k / (float)n * Mathf.PI * 2f + (st == 3 ? Mathf.PI / 4f : 0f);
                            Cone(root, new Vector3(Mathf.Sin(a) * sx * 0.45f, sy * 0.47f, Mathf.Cos(a) * sz * 0.45f), sx * 0.08f, sy * (st == 3 ? 0.16f : 0.22f), M.gold);
                            Part(PrimitiveType.Sphere, root, new Vector3(Mathf.Sin(a) * sx * 0.5f, sy * 0.43f, Mathf.Cos(a) * sz * 0.5f), Vector3.one * sx * 0.07f, M.gem);
                        }
                        if (st == 3)
                        {
                            // catifea + 2 arcuri joase + globul (fara sa "pluteasca" deasupra capului)
                            var velvet = Mat(Color.Lerp(new Color(0.6f, 0.1f, 0.2f), AccentOf(it), 0.3f), 0f, 0.2f, Color.black);
                            RB(root, P(0, 0.52f, 0), P(0.86f, 0.2f, 0.86f), 0.5f, velvet);
                            Ring(root, P(0, 0.46f, 0), new Vector3(sx * 0.86f, sx * 0.86f, sy * 0.46f), 0.06f, M.gold, new Vector3(90, 0, 0));
                            Ring(root, P(0, 0.46f, 0), new Vector3(sx * 0.86f, sx * 0.86f, sy * 0.46f), 0.06f, M.gold, new Vector3(90, 90, 0));
                            Part(PrimitiveType.Sphere, root, P(0, 0.72f, 0), Vector3.one * sx * 0.12f, M.gold);
                            Gem(root, P(0, 0.8f, 0), sx * 0.07f, M.gem, false);
                        }
                    }
                    break;

                default: // ---- HELM (metal)
                {
                    float top = st == 3 ? 0.64f : 0.6f;
                    RB(root, P(0, (top - 0.06f) * 0.5f, -0.02f), P(1.07f, top + 0.06f, 1.07f), 0.45f, M.metal);   // calota rotunjita
                    RB(root, P(0, 0.01f, -0.02f), P(1.09f, 0.08f, 1.09f), 0.45f, M.acc);
                    if (st != 3) RB(root, P(0, -0.13f, 0.565f), P(0.09f, 0.26f, 0.05f), 0.4f, M.metal);   // aparatoare de nas
                    if (st == 0)
                    {   // cavaler: aparatori de obraz + creasta
                        for (int s = -1; s <= 1; s += 2)
                            RB(root, P(s * 0.565f, -0.15f, 0.1f), P(0.06f, 0.32f, 0.6f), 0.45f, M.metal);
                        RB(root, P(0, 0.6f, -0.03f), P(0.1f, 0.16f, 0.9f), 0.45f, M.acc);
                    }
                    else if (st == 1)
                    {   // viking: coarne
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Cone(root, P(s * 0.5f, 0.3f, 0), sx * 0.11f, sx * 0.34f, M.bone, new Vector3(0, 0, -s * 62), 0.25f);
                            Cone(root, P(s * 0.78f, 0.45f, 0), sx * 0.055f, sx * 0.26f, M.bone, new Vector3(0, 0, -s * 18));
                        }
                    }
                    else if (st == 2)
                    {   // aripi
                        for (int s = -1; s <= 1; s += 2)
                            for (int k = 0; k < 3; k++)
                                RB(root, P(s * 0.6f, 0.36f + k * 0.04f, -0.1f - k * 0.1f), P(0.07f, 0.62f - k * 0.1f, 0.24f), 0.5f, M.gold, new Vector3(-12 - k * 16, 0, -s * (20 + k * 16)));
                    }
                    else
                    {   // coif mare cu viziera si panas
                        RB(root, P(0, 0.06f, 0.56f), P(0.86f, 0.06f, 0.04f), 0.5f, M.dark);
                        for (int k = 0; k < 5; k++)
                        {
                            float t = k / 4f;
                            Part(PrimitiveType.Sphere, root, P(0, 0.66f + Mathf.Sin(t * Mathf.PI) * 0.18f, 0.3f - t * 0.9f), new Vector3(sx * 0.16f, sy * (0.2f + t * 0.06f), sz * 0.26f), M.acc);
                        }
                    }
                    Gem(root, P(0, 0f, 0.585f), sx * 0.1f, M.gem, false);
                    break;
                }
            }
            GearDecor.Helmet(it, root, new Vector3(sx, sy, sz));   // tema hartii + raritatea
        }

        // ---------------------------------------------------------------
        //  Armura: root aliniat cu lumea, centrat pe piept. Fata = +Z.
        //  size = latimea / inaltimea / adancimea trunchiului
        // ---------------------------------------------------------------
        public static void BuildArmor(Item it, Transform root, Vector3 size)
        {
            var M = MatsOf(it);
            float w = size.x, h = size.y, d = size.z;
            int st = Style(it);
            System.Func<float, float, float, Vector3> P = (x, y, z) => new Vector3(x * w, y * h, z * d);
            Vector3 body = new Vector3(w * 1.12f, h * 1.02f, d * 1.2f);

            switch (BaseOf(it))
            {
                case 1: // ---- MAIL
                {
                    var chain = Mat(Color.Lerp(MetalOf(it), new Color(0.55f, 0.57f, 0.6f), 0.5f), 0.7f, 0.35f, Color.black);
                    RB(root, Vector3.zero, body, 0.32f, st == 1 ? M.dark : chain);
                    if (st == 1)
                    {   // solzi
                        for (int r = 0; r < 4; r++)
                            for (int c = -2; c <= 2; c++)
                            {
                                float x = c * 0.2f + (r % 2) * 0.1f;
                                if (Mathf.Abs(x) > 0.45f) continue;
                                Part(PrimitiveType.Sphere, root, P(x, 0.32f - r * 0.21f, 0.6f), new Vector3(w * 0.24f, h * 0.22f, d * 0.1f), M.metal);
                            }
                    }
                    else
                    {   // tabard in fata (si in spate)
                        for (int s = -1; s <= 1; s += 2)
                            RB(root, P(0, -0.06f, s * 0.61f), new Vector3(w * 0.62f, h * 1.02f, d * 0.05f), 0.3f, M.acc);
                        if (st == 2) Gem(root, P(0, 0.12f, 0.65f), w * 0.2f, M.gem, true);
                    }
                    RB(root, P(0, -0.42f, 0), new Vector3(w * 1.15f, h * 0.11f, d * 1.23f), 0.45f, M.leather);
                    for (int s = -1; s <= 1; s += 2)
                        Part(PrimitiveType.Sphere, root, P(s * 0.58f, 0.42f, 0), new Vector3(w * 0.38f, h * 0.24f, d * 0.95f), M.metal);
                    if (st == 3)
                    {   // guler de blana
                        var fur = Mat(new Color(0.55f, 0.42f, 0.3f), 0f, 0.1f, Color.black);
                        for (int k = 0; k < 10; k++)
                        {
                            float a = k / 10f * Mathf.PI * 2f;
                            Part(PrimitiveType.Sphere, root, new Vector3(Mathf.Sin(a) * w * 0.48f, h * 0.5f, Mathf.Cos(a) * d * 0.55f), Vector3.one * w * 0.3f, fur);
                        }
                    }
                    break;
                }
                case 2: // ---- TUNIC (haine / robe)
                    RB(root, Vector3.zero, body, 0.36f, M.cloth);
                    if (st == 1)
                    {   // roba: poale scurte + esarfa pe diagonala
                        RB(root, P(0, -0.62f, 0), new Vector3(w * 1.18f, h * 0.36f, d * 1.24f), 0.35f, M.cloth);
                        RB(root, P(0, 0.02f, 0), new Vector3(w * 0.2f, h * 1.25f, d * 1.28f), 0.4f, M.acc, new Vector3(0, 0, 38));
                    }
                    else if (st == 2)
                    {   // nasturi + guler de gluga la spate
                        for (int k = 0; k < 4; k++) Part(PrimitiveType.Sphere, root, P(0, 0.3f - k * 0.2f, 0.61f), Vector3.one * w * 0.1f, M.gold);
                        RB(root, P(0, 0.48f, -0.52f), new Vector3(w * 0.9f, h * 0.3f, d * 0.36f), 0.5f, M.leather);
                    }
                    else if (st == 3)
                    {   // mantie de mag + rune
                        RB(root, P(0, 0.4f, 0), new Vector3(w * 1.3f, h * 0.3f, d * 1.34f), 0.5f, M.acc);
                        for (int k = 0; k < 3; k++) Gem(root, P(0, 0.08f - k * 0.2f, 0.61f), w * 0.11f, M.gem, true);
                    }
                    else
                    {   // tunica cu guler in V
                        for (int s = -1; s <= 1; s += 2)
                            RB(root, P(s * 0.13f, 0.28f, 0.6f), new Vector3(w * 0.1f, h * 0.45f, d * 0.05f), 0.5f, M.acc, new Vector3(0, 0, s * 22));
                    }
                    if (st != 3) RB(root, P(0, -0.38f, 0), new Vector3(w * 1.15f, h * 0.11f, d * 1.23f), 0.45f, M.leather);
                    break;

                case 3: // ---- VEST (piele)
                    RB(root, Vector3.zero, body, 0.36f, M.leather);
                    if (st == 1)
                    {   // curele in X
                        for (int s = -1; s <= 1; s += 2)
                            RB(root, P(0, 0.02f, 0.61f), new Vector3(w * 0.12f, h * 1.2f, d * 0.05f), 0.5f, M.acc, new Vector3(0, 0, s * 32));
                    }
                    else if (st == 3)
                    {   // tinte
                        for (int r = 0; r < 3; r++)
                            for (int c = -1; c <= 1; c++)
                                Part(PrimitiveType.Sphere, root, P(c * 0.3f, 0.3f - r * 0.25f, 0.61f), Vector3.one * w * 0.09f, M.metal);
                    }
                    else
                    {   // doua curele verticale
                        for (int s = -1; s <= 1; s += 2)
                            RB(root, P(s * 0.28f, 0, 0.61f), new Vector3(w * 0.09f, h * 0.96f, d * 0.05f), 0.5f, M.acc);
                    }
                    RB(root, P(0, -0.38f, 0), new Vector3(w * 1.15f, h * 0.11f, d * 1.23f), 0.45f, M.acc);
                    Gem(root, P(0, -0.38f, 0.64f), w * 0.13f, M.gem, false);
                    if (st == 2)   // buzunare pe curea
                        for (int k = -1; k <= 1; k += 2)
                            RB(root, P(k * 0.36f, -0.46f, 0.62f), new Vector3(w * 0.22f, h * 0.2f, d * 0.16f), 0.4f, M.leather);
                    break;

                default: // ---- PLATE (metal)
                    RB(root, Vector3.zero, body, 0.32f, M.metal);
                    RB(root, P(0, 0.12f, 0.56f), new Vector3(w * 0.9f, h * 0.56f, d * 0.12f), 0.45f, M.metal);   // pieptarul
                    if (st == 2)
                        for (int k = 0; k < 3; k++)
                            RB(root, P(0, 0.22f - k * 0.24f, 0), new Vector3(w * 1.14f, h * 0.06f, d * 1.22f), 0.45f, M.acc);
                    RB(root, P(0, -0.42f, 0), new Vector3(w * 1.15f, h * 0.12f, d * 1.23f), 0.45f, M.acc);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Part(PrimitiveType.Sphere, root, P(s * 0.6f, 0.42f, 0), new Vector3(w * 0.46f, h * 0.34f, d * 1.08f), M.metal);
                        if (st == 1)   // tepi pe umeri
                            for (int k = 0; k < 2; k++)
                                Cone(root, P(s * (0.62f + k * 0.12f), 0.55f, (k == 0 ? 0.18f : -0.18f)), w * 0.08f, w * 0.26f, M.acc, new Vector3(0, 0, -s * 28));
                    }
                    Gem(root, P(0, 0.3f, 0.64f), w * 0.14f, M.gem, st == 2);
                    if (st == 3)
                    {   // mantie la spate
                        RB(root, P(0, -0.42f, -0.66f), new Vector3(w * 1.08f, h * 1.6f, d * 0.08f), 0.3f, M.cloth, new Vector3(6, 0, 0));
                        for (int s = -1; s <= 1; s += 2) Part(PrimitiveType.Sphere, root, P(s * 0.4f, 0.42f, 0.5f), Vector3.one * w * 0.12f, M.gold);
                    }
                    break;
            }
            GearDecor.Armor(it, root, size);   // tema hartii + raritatea
        }

        // ---------------------------------------------------------------
        //  Inel (doar pentru iconita): inelul in planul XY, piatra sus (+Y)
        // ---------------------------------------------------------------
        public static void BuildRing(Item it, Transform root)
        {
            BuildRingBase(it, root);
            GearDecor.Ring(it, root);   // tema hartii + raritatea
        }

        static void BuildRingBase(Item it, Transform root)
        {
            var M = MatsOf(it);
            int st = Style(it), b = BaseOf(it);
            float tube = b == 1 ? 0.16f : b == 2 ? 0.08f : 0.12f;
            var band = b == 2 ? M.metal : M.gold;
            Ring(root, Vector3.zero, Vector3.one, tube, band, new Vector3(90, 0, 0));
            if (b == 3)
            {   // pecete: placa plata cu emblema
                RB(root, new Vector3(0, 0.5f, 0), new Vector3(0.42f, 0.14f, 0.34f), 0.4f, band);
                RB(root, new Vector3(0, 0.58f, 0), new Vector3(0.28f, 0.04f, 0.22f), 0.5f, M.acc);
                return;
            }
            switch (st)
            {
                case 1:
                    Gem(root, new Vector3(0, 0.62f, 0), 0.34f, M.gem, true);
                    for (int s = -1; s <= 1; s += 2) Cone(root, new Vector3(s * 0.12f, 0.46f, 0), 0.03f, 0.2f, band, new Vector3(0, 0, -s * 20));
                    break;
                case 2:
                    for (int k = -1; k <= 1; k++) Gem(root, new Vector3(k * 0.17f, 0.55f - Mathf.Abs(k) * 0.05f, 0.02f), k == 0 ? 0.24f : 0.16f, k == 0 ? M.gem : M.acc, false);
                    break;
                case 3:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.52f, 0), new Vector3(0.3f, 0.05f, 0.3f), band);
                    Gem(root, new Vector3(0, 0.6f, 0), 0.24f, M.gem, false);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k / 6f * Mathf.PI * 2f;
                        Part(PrimitiveType.Sphere, root, new Vector3(Mathf.Cos(a) * 0.16f, 0.56f, Mathf.Sin(a) * 0.16f), Vector3.one * 0.07f, M.acc);
                    }
                    break;
                default:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.5f, 0), new Vector3(0.22f, 0.06f, 0.22f), band);
                    Gem(root, new Vector3(0, 0.6f, 0), 0.28f, M.gem, false);
                    break;
            }
        }
    }
}
