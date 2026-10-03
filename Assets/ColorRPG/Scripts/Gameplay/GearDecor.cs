// =====================================================================
//  GearDecor.cs - piesele care fac coifurile, armurile si inelele unice:
//   * tema hartii: Meadow = frunze, Desert = aur + scarabeu, Frozen =
//     cristale de gheata, Lava = coarne / tepi de obsidian cu jar
//   * raritatea: Rare = piatra pretioasa, Epic = stralucire + pietre in plus,
//     Legendary = cristal care pluteste deasupra, Mythic = culori care curg
//  Se adauga peste modelul facut de GearVisuals (acelasi pe erou si in iconita).
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class GearDecor
    {
        static GameObject Prim(PrimitiveType t, Transform p, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default(Vector3))
        {
            var go = Art.Prim(t, pos, scale, Color.white, p, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        static GameObject Mesh(Mesh mesh, Transform p, Vector3 pos, Vector3 scale, Material m, Vector3 euler)
        {
            var go = new GameObject("Decor");
            go.transform.SetParent(p, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>Con cu baza in pos, orientat dupa directia dir.</summary>
        static GameObject Spike(Transform p, Vector3 pos, Vector3 dir, float r, float h, Material m)
        {
            return Mesh(MeshGen.Cone(0f), p, pos, new Vector3(r * 2f, h, r * 2f), m, Quaternion.FromToRotation(Vector3.up, dir.normalized).eulerAngles);
        }

        struct Th { public Material main, second, glow; }

        static Th Theme(Item it)
        {
            var t = new Th();
            switch (Mathf.Clamp(it.tier, 0, 3))
            {
                case 1:   // Desert: aur + turcoaz
                    t.main = GearVisuals.Mat(new Color(1f, 0.78f, 0.3f), 0.9f, 0.75f, Color.black);
                    t.second = GearVisuals.Mat(new Color(0.2f, 0.75f, 0.7f), 0.3f, 0.8f, new Color(0.02f, 0.1f, 0.09f));
                    t.glow = t.second; break;
                case 2:   // Frozen: gheata
                    t.main = GearVisuals.Mat(new Color(0.65f, 0.88f, 1f), 0.2f, 0.95f, new Color(0.08f, 0.16f, 0.22f));
                    t.second = GearVisuals.Mat(new Color(0.9f, 0.97f, 1f), 0.1f, 0.9f, new Color(0.1f, 0.14f, 0.18f));
                    t.glow = t.main; break;
                case 3:   // Lava: obsidian + jar
                    t.main = GearVisuals.Mat(new Color(0.16f, 0.12f, 0.14f), 0.5f, 0.7f, Color.black);
                    t.second = GearVisuals.Mat(new Color(1f, 0.45f, 0.1f), 0f, 0.5f, new Color(1f, 0.38f, 0.06f) * 0.9f);
                    t.glow = t.second; break;
                default:  // Meadow: frunze + floare
                    t.main = GearVisuals.Mat(new Color(0.36f, 0.75f, 0.3f), 0f, 0.35f, Color.black);
                    t.second = GearVisuals.Mat(new Color(1f, 0.6f, 0.8f), 0f, 0.4f, Color.black);
                    t.glow = t.main; break;
            }
            return t;
        }

        static Material GemMat(Item it)
        {
            Color rc = ItemDB.Col(it.rarity);
            return GearVisuals.Mat(rc, 0.2f, 0.9f, rc * (0.25f + 0.12f * it.rarity));
        }

        // ---------------------------------------------------------------
        //  Coif: coordonate relative la cap (cap ~ -0.5..0.5 pe fiecare axa, fata = +Z)
        // ---------------------------------------------------------------
        public static void Helmet(Item it, Transform root, Vector3 size)
        {
            if (it.cat > 0) return;   // itemele din catalog au forma lor curata (fara cristale / podoabe in plus)
            var T = Theme(it);
            System.Func<float, float, float, Vector3> P = (x, y, z) => new Vector3(x * size.x, y * size.y, z * size.z);
            float u = size.x;
            switch (Mathf.Clamp(it.tier, 0, 3))
            {
                case 1:   // scarabeu de aur in frunte + aripioare laterale
                    Prim(PrimitiveType.Sphere, root, P(0, 0.34f, 0.56f), new Vector3(0.22f, 0.16f, 0.08f) * u, T.main);
                    Prim(PrimitiveType.Sphere, root, P(0, 0.34f, 0.6f), new Vector3(0.1f, 0.08f, 0.05f) * u, T.second);
                    for (int s = -1; s <= 1; s += 2)
                        Prim(PrimitiveType.Cube, root, P(s * 0.6f, 0.3f, 0f), new Vector3(0.06f, 0.26f, 0.34f) * u, T.main, new Vector3(0, 0, s * -18f));
                    break;
                case 2:   // coroana de cristale de gheata
                    for (int k = -2; k <= 2; k++)
                        Spike(root, P(k * 0.16f, 0.5f, 0.12f - Mathf.Abs(k) * 0.05f), new Vector3(k * 0.25f, 1f, 0.1f), 0.06f * u, (0.34f - Mathf.Abs(k) * 0.07f) * u, k % 2 == 0 ? T.main : T.second);
                    break;
                case 3:   // doua coarne de obsidian cu varf de jar
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Spike(root, P(s * 0.42f, 0.34f, 0.02f), new Vector3(s * 0.8f, 0.7f, 0.15f), 0.09f * u, 0.36f * u, T.main);
                        Spike(root, P(s * 0.62f, 0.56f, 0.06f), new Vector3(s * 0.35f, 1f, 0.1f), 0.05f * u, 0.18f * u, T.second);
                    }
                    break;
                default:  // doua frunze in lateral + o floare
                    for (int s = -1; s <= 1; s += 2)
                        Prim(PrimitiveType.Sphere, root, P(s * 0.5f, 0.42f, -0.05f), new Vector3(0.08f, 0.34f, 0.18f) * u, T.main, new Vector3(0, 0, s * -35f));
                    Prim(PrimitiveType.Sphere, root, P(0.3f, 0.5f, 0.32f), Vector3.one * 0.12f * u, T.second);
                    break;
            }
            Rarity(it, root, P(0, 0.3f, 0.6f), P(0, 1.05f, 0), u);
        }

        // ---------------------------------------------------------------
        //  Armura: coordonate relative la trunchi (umerii la x = +-0.5, y = 0.42)
        // ---------------------------------------------------------------
        public static void Armor(Item it, Transform root, Vector3 size)
        {
            if (it.cat > 0) return;   // itemele din catalog au forma lor curata (fara cristale / podoabe in plus)
            var T = Theme(it);
            System.Func<float, float, float, Vector3> P = (x, y, z) => new Vector3(x * size.x, y * size.y, z * size.z);
            float u = size.x;
            switch (Mathf.Clamp(it.tier, 0, 3))
            {
                case 1:   // centura de aur cu scarabeu
                    Prim(PrimitiveType.Cylinder, root, P(0, -0.32f, 0), new Vector3(1.18f, 0.035f, 1.3f) * u, T.main);
                    Prim(PrimitiveType.Sphere, root, P(0, -0.32f, 0.66f), new Vector3(0.2f, 0.16f, 0.07f) * u, T.second);
                    break;
                case 2:   // cristale de gheata pe umeri
                    for (int s = -1; s <= 1; s += 2)
                        for (int k = 0; k < 3; k++)
                            Spike(root, P(s * (0.48f + k * 0.04f), 0.48f, -0.15f + k * 0.15f), new Vector3(s * 0.4f, 1f, 0f), 0.05f * u, (0.22f + (k == 1 ? 0.08f : 0f)) * u, k == 1 ? T.second : T.main);
                    break;
                case 3:   // tepi de obsidian pe umeri + inima de jar pe piept
                    for (int s = -1; s <= 1; s += 2)
                        for (int k = 0; k < 2; k++)
                            Spike(root, P(s * 0.52f, 0.46f, -0.08f + k * 0.16f), new Vector3(s * 0.6f, 1f, 0f), 0.06f * u, 0.22f * u, T.main);
                    Prim(PrimitiveType.Sphere, root, P(0, 0.12f, 0.62f), Vector3.one * 0.16f * u, T.second);
                    break;
                default:  // umerari din frunze
                    for (int s = -1; s <= 1; s += 2)
                        for (int k = 0; k < 2; k++)
                            Prim(PrimitiveType.Sphere, root, P(s * 0.56f, 0.44f - k * 0.08f, -0.05f + k * 0.12f), new Vector3(0.3f, 0.07f, 0.18f) * u, T.main, new Vector3(0, k * 30f, s * -22f));
                    break;
            }
            Rarity(it, root, P(0, 0.22f, 0.64f), P(0.7f, 0.8f, 0), u);
        }

        // ---------------------------------------------------------------
        //  Inel (iconita): inelul in planul XY, piatra sus
        // ---------------------------------------------------------------
        public static void Ring(Item it, Transform root)
        {
            if (it.cat > 0) return;   // itemele din catalog au forma lor curata (fara cristale / podoabe in plus)
            var T = Theme(it);
            switch (Mathf.Clamp(it.tier, 0, 3))
            {
                case 1: for (int s = -1; s <= 1; s += 2) Prim(PrimitiveType.Cube, root, new Vector3(s * 0.3f, 0.42f, 0), new Vector3(0.1f, 0.26f, 0.08f), T.main, new Vector3(0, 0, s * -40f)); break;
                case 2: for (int s = -1; s <= 1; s++) Spike(root, new Vector3(s * 0.14f, 0.62f, 0), new Vector3(s * 0.4f, 1f, 0), 0.05f, 0.24f - Mathf.Abs(s) * 0.06f, T.main); break;
                case 3: for (int s = -1; s <= 1; s += 2) Spike(root, new Vector3(s * 0.22f, 0.5f, 0), new Vector3(s * 0.5f, 1f, 0), 0.06f, 0.22f, T.second); break;
                default: for (int s = -1; s <= 1; s += 2) Prim(PrimitiveType.Sphere, root, new Vector3(s * 0.26f, 0.5f, 0), new Vector3(0.18f, 0.07f, 0.1f), T.main, new Vector3(0, 0, s * -30f)); break;
            }
            if (it.rarity >= 4) Crystal(it, root, new Vector3(0, 1.05f, 0), 0.18f);
        }

        static void Rarity(Item it, Transform root, Vector3 front, Vector3 top, float u)
        {
            if (it.rarity >= 2)
            {
                var g = GemMat(it);
                Prim(PrimitiveType.Cube, root, front, new Vector3(0.11f, 0.11f, 0.06f) * u * (1f + 0.12f * (it.rarity - 2)), g, new Vector3(0, 0, 45f));
                if (it.rarity >= 3)
                    for (int s = -1; s <= 1; s += 2)
                        Prim(PrimitiveType.Sphere, root, front + new Vector3(s * 0.16f * u, -0.02f * u, -0.01f * u), Vector3.one * 0.06f * u, g);
            }
            if (it.rarity >= 4) Crystal(it, root, top, 0.14f * u);
        }

        static void Crystal(Item it, Transform root, Vector3 pos, float s)
        {
            var c = new GameObject("RarityCrystal").transform;
            c.SetParent(root, false);
            c.localPosition = pos;
            var g = Prim(PrimitiveType.Cube, c, Vector3.zero, new Vector3(s * 0.7f, s * 1.3f, s * 0.7f), GemMat(it), new Vector3(0, 45f, 0));
            g.transform.localRotation = Quaternion.Euler(45f, 0, 45f);
            var sp = c.gameObject.AddComponent<Spinner>();
            sp.rot = new Vector3(0, 80f, 0); sp.bobAmp = s * 0.3f; sp.bobSpeed = 2f;
            if (it.rarity >= 5) c.gameObject.AddComponent<MythicHue>();
        }
    }

    /// <summary>Mythic: culoarea pietrei curge prin curcubeu (material propriu, nu cel partajat).</summary>
    public class MythicHue : MonoBehaviour
    {
        Material m;
        void Start()
        {
            var r = GetComponentInChildren<Renderer>();
            if (r == null) { enabled = false; return; }
            m = new Material(r.sharedMaterial);
            r.sharedMaterial = m;
        }
        void Update()
        {
            if (m == null) return;
            Color c = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.2f, 1f), 0.75f, 1f);
            Art.SetColor(m, c);
            Art.SetEmission(m, c * 0.6f);
        }
        void OnDestroy() { if (m != null) Destroy(m); }
    }
}
