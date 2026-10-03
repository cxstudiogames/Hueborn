// GrassField.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
// Iarba 3D: mii de smocuri de fire desenate cu GPU instancing, doar in jurul camerei.
// Setarile (densitate, zona, zone fara iarba) se pot schimba din Inspector.
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class GrassField : MonoBehaviour
    {
        [Tooltip("Materialul cu shaderul ColorRPG/Grass (culorile ierbii).")]
        public Material material;
        [Tooltip("Smocuri pe metru patrat.")]
        public float density = 1.4f;
        [Tooltip("Inaltimea medie a firelor (m).")]
        public float height = 0.55f;
        [Tooltip("Pana la ce distanta de camera se deseneaza iarba.")]
        public float drawDistance = 48f;
        [Tooltip("Zona cu iarba (colturile, in coordonate lume XZ).")]
        public Vector2 areaMin = new Vector2(-68, -68), areaMax = new Vector2(68, 68);
        [Tooltip("Cercuri fara iarba: x, z = centru (lume), w = raza.")]
        public List<Vector4> holes = new List<Vector4>();
        [Tooltip("Iarba doar in afara acestui cerc (x, z, raza) - pentru piata. Raza 0 = oprit.")]
        public Vector3 keepOutside;
        public int seed = 1;

        const float Cell = 8f;
        class Chunk { public Vector3 center; public Matrix4x4[] m; }
        readonly List<Chunk> chunks = new List<Chunk>();
        static Mesh blade;
        readonly Matrix4x4[] batch = new Matrix4x4[1023];
        RenderParams rp;
        bool ready;

        void Start()
        {
            if (material == null) { enabled = false; return; }
            material.enableInstancing = true;
            if (blade == null) blade = BuildClump();
            Generate();
            rp = new RenderParams(material)
            {
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows = true,
                layer = gameObject.layer,
            };
            ready = true;
        }

        bool InHole(float x, float z)
        {
            foreach (var h in holes)
            {
                float dx = x - h.x, dz = z - h.z;
                if (dx * dx + dz * dz < h.w * h.w) return true;
            }
            if (keepOutside.z > 0f)
            {
                float dx = x - keepOutside.x, dz = z - keepOutside.y;
                if (dx * dx + dz * dz < keepOutside.z * keepOutside.z) return true;
            }
            return false;
        }

        void Generate()
        {
            chunks.Clear();
            var rng = new System.Random(seed);
            var list = new List<Matrix4x4>();
            for (float cx = areaMin.x; cx < areaMax.x; cx += Cell)
                for (float cz = areaMin.y; cz < areaMax.y; cz += Cell)
                {
                    list.Clear();
                    int n = Mathf.RoundToInt(Cell * Cell * density);
                    for (int k = 0; k < n; k++)
                    {
                        float x = cx + (float)rng.NextDouble() * Cell, z = cz + (float)rng.NextDouble() * Cell;
                        if (x > areaMax.x || z > areaMax.y || InHole(x, z)) continue;
                        // pete mai rare / mai dese (arata natural)
                        float patch = Mathf.PerlinNoise(x * 0.08f + seed, z * 0.08f);
                        if (rng.NextDouble() > 0.35 + patch) continue;
                        float s = height * (0.6f + (float)rng.NextDouble() * 0.8f) * (0.7f + patch * 0.6f);
                        var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                        list.Add(Matrix4x4.TRS(new Vector3(x, 0f, z), rot, new Vector3(s * 1.1f, s, s * 1.1f)));
                    }
                    if (list.Count > 0) chunks.Add(new Chunk { center = new Vector3(cx + Cell * 0.5f, 0, cz + Cell * 0.5f), m = list.ToArray() });
                }
        }

        void Update()
        {
            if (!ready || !GameSettings.Grass) return;
            var cam = Game.I != null ? Game.I.cam : Camera.main;
            if (cam == null) return;
            Vector3 cp = cam.transform.position;
            // harta nu e cea curenta -> nu desenam nimic
            if (cp.x < areaMin.x - drawDistance || cp.x > areaMax.x + drawDistance || cp.z < areaMin.y - drawDistance || cp.z > areaMax.y + drawDistance) return;
            float dd = drawDistance * (GameSettings.Quality == 0 ? 0.55f : 0.8f) * (DeviceTier.Tier == 0 ? 0.7f : 1f);
            float dd2 = (dd + Cell) * (dd + Cell);
            Vector3 fwd = cam.transform.forward; fwd.y = 0; fwd.Normalize();
            int count = 0;
            foreach (var c in chunks)
            {
                Vector3 d = c.center - cp; d.y = 0;
                float m2 = d.sqrMagnitude;
                if (m2 > dd2) continue;
                if (m2 > 400f && Vector3.Dot(d, fwd) < -Cell) continue;   // in spatele camerei
                for (int i = 0; i < c.m.Length; i++)
                {
                    batch[count++] = c.m[i];
                    if (count == batch.Length) { Graphics.RenderMeshInstanced(rp, blade, 0, batch, count); count = 0; }
                }
            }
            if (count > 0) Graphics.RenderMeshInstanced(rp, blade, 0, batch, count);
        }

        /// <summary>Un smoc: 7 fire subtiri, curbate, cu culoare in vertex (r = inaltime, g = variatie).</summary>
        static Mesh BuildClump()
        {
            var verts = new List<Vector3>(); var cols = new List<Color>(); var norms = new List<Vector3>(); var tris = new List<int>();
            var rng = new System.Random(7);
            for (int b = 0; b < 7; b++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                Vector3 side = new Vector3(-dir.z, 0, dir.x);
                Vector3 root = dir * (float)rng.NextDouble() * 0.18f;
                float h = 0.7f + (float)rng.NextDouble() * 0.5f;
                float lean = 0.15f + (float)rng.NextDouble() * 0.35f;
                float w = 0.035f + (float)rng.NextDouble() * 0.025f;
                float var = (float)rng.NextDouble();
                int baseI = verts.Count;
                const int seg = 3;
                for (int s = 0; s <= seg; s++)
                {
                    float t = s / (float)seg;
                    Vector3 c = root + dir * (lean * t * t) + Vector3.up * (h * t);
                    float ww = w * (1f - t * 0.85f);
                    verts.Add(c - side * ww); verts.Add(c + side * ww);
                    Vector3 n = (dir * -0.3f + Vector3.up * 0.2f + Vector3.Cross(side, Vector3.up) * 0.5f).normalized;
                    norms.Add(n); norms.Add(n);
                    cols.Add(new Color(t, var, 0, 1)); cols.Add(new Color(t, var, 0, 1));
                }
                for (int s = 0; s < seg; s++)
                {
                    int i0 = baseI + s * 2;
                    tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 1);
                    tris.Add(i0 + 1); tris.Add(i0 + 2); tris.Add(i0 + 3);
                }
            }
            var m = new Mesh { name = "GrassClump" };
            m.SetVertices(verts); m.SetNormals(norms); m.SetColors(cols); m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            m.bounds = new Bounds(new Vector3(0, 0.6f, 0), new Vector3(1.5f, 1.5f, 1.5f));
            return m;
        }
    }
}
