// =====================================================================
//  OuterLands.cs - peisajul din jurul fiecarei harti (dupa marginea ei),
//  ca sa nu se mai vada "golul" de la capat:
//   * un sol larg sub harta (aceeasi culoare / textura), pana departe
//   * un inel de dealuri joase + munti low-poly (fete plate), in tema hartii:
//       Meadow  - munti verzi-cenusii, cei inalti cu varf de zapada
//       Desert  - mese de gresie cu varf plat + dune
//       Frozen  - munti albastrui, aproape toti inzapeziti
//       Lava    - munti de bazalt, cativa cu crater de lava care straluceste
//   * ceata (setata in Game) ii estompeaza spre orizont
//  Fara coliziuni si fara umbre (sunt doar decor, foarte ieftini).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class OuterLands
    {
        struct Look { public Color low, high, cap, hill; public float capLine, flat, hMin, hMax; public bool craters; }

        static Look For(int map)
        {
            switch (map)
            {
                case 1: return new Look { low = new Color(0.82f, 0.56f, 0.34f), high = new Color(0.93f, 0.72f, 0.48f), cap = new Color(0.97f, 0.82f, 0.6f), hill = new Color(0.93f, 0.8f, 0.56f), capLine = 0.78f, flat = 0.62f, hMin = 26f, hMax = 44f };
                case 2: return new Look { low = new Color(0.5f, 0.56f, 0.68f), high = new Color(0.66f, 0.72f, 0.84f), cap = new Color(0.95f, 0.97f, 1f), hill = new Color(0.88f, 0.92f, 0.98f), capLine = 0.38f, flat = 1f, hMin = 28f, hMax = 58f };
                case 3: return new Look { low = new Color(0.3f, 0.23f, 0.24f), high = new Color(0.36f, 0.25f, 0.23f), cap = new Color(0.55f, 0.3f, 0.2f), hill = new Color(0.32f, 0.23f, 0.21f), capLine = 0.78f, flat = 0.86f, hMin = 30f, hMax = 60f, craters = true };
                // Meadow (si Trade Plaza): munti verzi, cu varful intr-un verde mai deschis (fara zapada)
                default: return new Look { low = new Color(0.34f, 0.56f, 0.28f), high = new Color(0.28f, 0.48f, 0.26f), cap = new Color(0.5f, 0.72f, 0.36f), hill = new Color(0.42f, 0.66f, 0.3f), capLine = 0.68f, flat = 1f, hMin = 22f, hMax = 46f };
            }
        }

        static int meshNo;

        static float half = 71f;

        /// <summary>map = tema (0..3; Trade Plaza foloseste 0), halfSize = jumatatea laturii zonei (harta 71, piata 56), meshBase = prefix unic pentru plase.</summary>
        public static void Build(int map, Vector3 o, Transform parent, System.Random rng, float halfSize = 71f, int meshBase = -1)
        {
            meshNo = meshBase >= 0 ? meshBase : map * 1000;
            half = halfSize;
            var root = new GameObject("Outer Lands").transform;
            root.SetParent(parent, false);
            var L = For(map);
            System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);

            // 1) solul larg de sub harta (iese ~75 m in fiecare parte)
            var ground = Art.Prim(PrimitiveType.Cube, o + new Vector3(0, -0.56f, 0), new Vector3(420, 1f, 420), Color.white, root, false, false);
            ground.name = "Apron";
            var gm = ground.GetComponent<Renderer>();
            var painted = Art.PaintedTex("Ground" + Mathf.Clamp(map, 0, 3));
            if (painted != null) { Art.SetColor(gm.sharedMaterial, new Color(0.92f, 0.92f, 0.92f)); Art.ApplyTexture(gm.sharedMaterial, painted, 23f); }
            else Art.SetColor(gm.sharedMaterial, GameData.Maps[map].ground * 0.9f);
            NoShadow(gm);

            var mLow = GearVisuals.Mat(L.low, 0f, 0.05f, Color.black);
            var mHigh = GearVisuals.Mat(L.high, 0f, 0.05f, Color.black);
            var mCap = GearVisuals.Mat(L.cap, 0f, 0.15f, Color.black);
            var mHill = GearVisuals.Mat(L.hill, 0f, 0.05f, Color.black);

            // 2) dealuri joase, chiar dupa marginea hartii (fac trecerea spre munti)
            for (float a = 0f; a < 360f; a += R(9f, 14f))
            {
                float h = R(4f, 9f), br = h * R(2f, 2.6f);
                Vector3 p = o + Dir(a) * (Edge(a) + br * 1.15f + 4f);   // piciorul dealului incepe dupa marginea hartii
                Place(MountainMesh(rng, br, h, 1f, 2f), p, root, new[] { mHill, mHill }, R(0, 360));
            }
            // 3) muntii (doua randuri, cei din spate mai inalti)
            int k = 0;
            for (float a = 0f; a < 360f; a += R(10f, 15f), k++)
            {
                for (int row = 0; row < 2; row++)
                {
                    float h = R(L.hMin, L.hMax) * (row == 0 ? 0.55f : 1.15f);
                    float baseR = h * R(0.9f, 1.2f) * (map == 1 ? 1.2f : 1f);
                    float aa = a + R(-4f, 4f) + row * 6f;
                    float rad = Edge(aa) + (row == 0 ? 38f : 70f) + baseR * 1.1f;   // departe de marginea hartii (nu "intra" peste camera)
                    float flat = map == 1 ? (rng.NextDouble() < 0.7 ? L.flat : 1f) : L.flat;   // desert: mese cu varf plat
                    float cap = L.capLine;
                    var mesh = MountainMesh(rng, baseR, h, flat, cap);
                    Vector3 p = o + Dir(aa) * rad;
                    Place(mesh, p, root, new[] { row == 0 ? mLow : mHigh, mCap }, R(0, 360));
                }
            }
        }

        static Vector3 Dir(float deg) { float a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); }

        /// <summary>Distanta de la centru pana la marginea hartii (patrat de 140 m) pe directia data.</summary>
        static float Edge(float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return half / Mathf.Max(Mathf.Abs(Mathf.Cos(a)), Mathf.Abs(Mathf.Sin(a)));
        }

        static void NoShadow(Renderer r)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void Place(Mesh mesh, Vector3 p, Transform root, Material[] mats, float yaw)
        {
            var go = new GameObject("Mountain");
            go.transform.SetParent(root, false);
            go.transform.position = p + Vector3.down * 0.4f;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;   // nu se poate intra in munti / dealuri
            NoShadow(r);
        }

        /// <summary>Munte low-poly cu fete plate: baza neregulata, un inel la mijloc, varful (sau platou daca flat < 1).
        /// Submesh 0 = roca, submesh 1 = varful (zapada / strat deschis) deasupra lui capLine (0..1 din inaltime).</summary>
        static Mesh MountainMesh(System.Random rng, float baseR, float h, float flat, float capLine)
        {
            System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);
            int n = rng.Next(7, 10);
            var ring0 = new Vector3[n]; var ring1 = new Vector3[n]; var ring2 = new Vector3[n];
            float off = R(0f, 6.28f);
            Vector3 peak = new Vector3(R(-0.12f, 0.12f) * baseR, h, R(-0.12f, 0.12f) * baseR);
            float topH = h * flat;
            for (int i = 0; i < n; i++)
            {
                float a = off + i / (float)n * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                ring0[i] = d * baseR * R(0.8f, 1.1f);
                float mh = topH * R(0.42f, 0.58f);
                ring1[i] = d * baseR * R(0.45f, 0.62f) + Vector3.up * mh + new Vector3(peak.x, 0, peak.z) * 0.4f;
                // platou (mese de desert) sau un inel aproape de varf
                ring2[i] = flat < 1f ? d * baseR * R(0.28f, 0.36f) + new Vector3(peak.x, topH, peak.z)
                                     : Vector3.Lerp(ring1[i], peak, R(0.45f, 0.6f));
            }
            var v = new List<Vector3>(); var nn = new List<Vector3>(); var t0 = new List<int>(); var t1 = new List<int>();
            System.Action<Vector3, Vector3, Vector3> Tri = (a, b, c) =>
            {
                Vector3 nrm = Vector3.Cross(b - a, c - a).normalized;
                if (nrm.y < 0f) { var tmp = b; b = c; c = tmp; nrm = -nrm; }
                int k = v.Count;
                v.Add(a); v.Add(b); v.Add(c); nn.Add(nrm); nn.Add(nrm); nn.Add(nrm);
                float cy = (a.y + b.y + c.y) / 3f;
                var list = cy > h * capLine ? t1 : t0;
                list.Add(k); list.Add(k + 1); list.Add(k + 2);
            };
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Tri(ring0[i], ring1[i], ring0[j]); Tri(ring0[j], ring1[i], ring1[j]);
                Tri(ring1[i], ring2[i], ring1[j]); Tri(ring1[j], ring2[i], ring2[j]);
                if (flat < 1f) Tri(ring2[i], new Vector3(peak.x, topH, peak.z), ring2[j]);
                else Tri(ring2[i], peak, ring2[j]);
            }
            var mesh = new Mesh { name = "Mountain_" + (meshNo++) };   // nume unic: editorul salveaza plasele ca asset dupa nume
            mesh.SetVertices(v); mesh.SetNormals(nn);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(t0, 0); mesh.SetTriangles(t1, 1);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
