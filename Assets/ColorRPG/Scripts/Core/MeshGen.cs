// =====================================================================
//  MeshGen.cs - mesh-uri simple generate din cod (pentru echipament):
//   * RoundedBox - cutie cu colturi rotunjite (stilul capetelor Kenney)
//   * Cone       - con / trunchi de con (coarne, tepi, varfuri de coroana)
//   * Torus      - inel (inele, coroane, borduri)
//  Mesh-urile sunt de marime 1 (se scaleaza din transform) si se tin in cache.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class MeshGen
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        /// <summary>Sfera "low poly" (diametru 1, ca sfera Unity) - ~260 triunghiuri in loc de 768 (optimizare).</summary>
        public static Mesh LowSphere() { return UvCapsule("LowSphere", 0f, 12, 6); }

        /// <summary>Capsula "low poly" (inaltime 2, diametru 1, ca la Unity) - ~150 triunghiuri in loc de 832.</summary>
        public static Mesh LowCapsule() { return UvCapsule("LowCapsule", 0.5f, 12, 3); }

        /// <summary>Capsula / sfera cu UV: half = jumatatea cilindrului din mijloc (0 = sfera), sides = segmente in jur,
        /// rings = inele pe fiecare emisfera.</summary>
        static Mesh UvCapsule(string key, float half, int sides, int rings)
        {
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            const float r = 0.5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            int rows = rings * 2 + (half > 0f ? 1 : 0);   // inelele de jos -> sus (fara poli)
            // randuri de latitudine: de la -90 la +90, cu o "intindere" pe Y pentru capsula
            var lat = new List<float>(); var yOff = new List<float>();
            for (int i = 0; i <= rings; i++) { lat.Add(-90f + 90f * i / rings); yOff.Add(-half); }
            if (half > 0f) { lat.Add(0f); yOff.Add(half); }
            for (int i = 1; i <= rings; i++) { lat.Add(90f * i / rings); yOff.Add(half); }
            int cols = sides + 1;
            for (int i = 0; i < lat.Count; i++)
            {
                float a = lat[i] * Mathf.Deg2Rad;
                float cy = Mathf.Sin(a), cr = Mathf.Cos(a);
                for (int j = 0; j < cols; j++)
                {
                    float b = j / (float)sides * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(b) * cr, cy, Mathf.Sin(b) * cr);
                    v.Add(dir * r + Vector3.up * yOff[i]);
                    n.Add(dir);
                    uv.Add(new Vector2(j / (float)sides, i / (float)(lat.Count - 1)));
                }
            }
            for (int i = 0; i < lat.Count - 1; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a0 = i * cols + j, a1 = a0 + 1, b0 = a0 + cols, b1 = b0 + 1;
                    if (i > 0) { tri.Add(a0); tri.Add(b0); tri.Add(a1); }               // la poli un singur triunghi
                    if (i < lat.Count - 2) { tri.Add(a1); tri.Add(b0); tri.Add(b1); }
                }
            m = new Mesh { name = key };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateBounds(); m.RecalculateTangents();
            cache[key] = m;
            return m;
        }

        /// <summary>Cutie rotunjita in [-0.5, 0.5]^3. round = raza colturilor raportata la latura (0..0.5).
        /// aspect = proportiile reale (ca rotunjirea sa ramana rotunda dupa scalare).</summary>
        public static Mesh RoundedBox(float round, Vector3 aspect, int seg = 6)
        {
            // proportiile (relative, max = 1) se cuantizeaza ca sa nu umplem cache-ul
            float mx = Mathf.Max(aspect.x, Mathf.Max(aspect.y, aspect.z));
            if (mx > 0f) aspect /= mx;
            aspect = new Vector3(Q(aspect.x), Q(aspect.y), Q(aspect.z));
            float m = Mathf.Max(0.001f, Mathf.Min(aspect.x, Mathf.Min(aspect.y, aspect.z)));
            string key = "rb" + round.ToString("0.00") + "_" + aspect.x + "_" + aspect.y + "_" + aspect.z + "_" + seg;
            Mesh mesh;
            if (cache.TryGetValue(key, out mesh) && mesh != null) return mesh;

            Vector3 half = aspect * 0.5f;
            float r = Mathf.Clamp(round, 0.01f, 0.5f) * m;
            Vector3 inner = new Vector3(Mathf.Max(0, half.x - r), Mathf.Max(0, half.y - r), Mathf.Max(0, half.z - r));
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var n in dirs)
            {
                Vector3 a = new Vector3(n.y, n.z, n.x), b = Vector3.Cross(n, a);
                int start = verts.Count;
                for (int i = 0; i <= seg; i++)
                    for (int j = 0; j <= seg; j++)
                    {
                        Vector3 p = n + a * (i / (float)seg * 2f - 1f) + b * (j / (float)seg * 2f - 1f);   // pe cubul [-1,1]
                        p = Vector3.Scale(p, half);
                        Vector3 c = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 d = p - c;
                        Vector3 nn = d.sqrMagnitude > 1e-8f ? d.normalized : n;
                        Vector3 v = c + nn * r;
                        verts.Add(new Vector3(v.x / aspect.x, v.y / aspect.y, v.z / aspect.z));
                        norms.Add(Vector3.Scale(nn, aspect).normalized);   // normala in spatiul mesh-ului (inainte de scalare)
                    }
                for (int i = 0; i < seg; i++)
                    for (int j = 0; j < seg; j++)
                    {
                        int v0 = start + i * (seg + 1) + j, v1 = v0 + 1, v2 = v0 + seg + 1, v3 = v2 + 1;
                        tris.Add(v0); tris.Add(v1); tris.Add(v2);
                        tris.Add(v2); tris.Add(v1); tris.Add(v3);
                    }
            }
            mesh = Finish(key, verts, norms, tris);
            FixWinding(mesh);
            return mesh;
        }

        static float Q(float v) { return Mathf.Max(0.05f, Mathf.Round(v * 20f) / 20f); }

        /// <summary>Con de inaltime 1 (baza la y=0, varful la y=1). top = raza de sus (0 = varf ascutit), raza de jos 0.5.</summary>
        public static Mesh Cone(float top = 0f, int sides = 14)
        {
            string key = "cone" + top.ToString("0.00") + "_" + sides;
            Mesh mesh;
            if (cache.TryGetValue(key, out mesh) && mesh != null) return mesh;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            float rb = 0.5f, rt = Mathf.Clamp(top, 0f, 0.5f);
            float slope = (rb - rt);
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 n = (dir + Vector3.up * slope).normalized;
                verts.Add(dir * rb); norms.Add(n);
                verts.Add(dir * rt + Vector3.up); norms.Add(n);
            }
            for (int i = 0; i < sides; i++)
            {
                int v = i * 2;
                tris.Add(v); tris.Add(v + 1); tris.Add(v + 2);
                tris.Add(v + 2); tris.Add(v + 1); tris.Add(v + 3);
            }
            // capacul de jos
            int c0 = verts.Count; verts.Add(Vector3.zero); norms.Add(Vector3.down);
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rb); norms.Add(Vector3.down);
            }
            for (int i = 0; i < sides; i++) { tris.Add(c0); tris.Add(c0 + 1 + i); tris.Add(c0 + 2 + i); }
            if (rt > 0.001f)
            {
                int c1 = verts.Count; verts.Add(Vector3.up); norms.Add(Vector3.up);
                for (int i = 0; i <= sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rt + Vector3.up); norms.Add(Vector3.up);
                }
                for (int i = 0; i < sides; i++) { tris.Add(c1); tris.Add(c1 + 2 + i); tris.Add(c1 + 1 + i); }
            }
            mesh = Finish(key, verts, norms, tris);
            FixWinding(mesh);
            return mesh;
        }

        /// <summary>Inel in planul XZ (axa pe Y). R = raza mare (0.5 = diametru 1), tube = raza tubului.</summary>
        public static Mesh Torus(float tube = 0.1f, int seg = 24, int sides = 10)
        {
            string key = "torus" + tube.ToString("0.000") + "_" + seg + "_" + sides;
            Mesh mesh;
            if (cache.TryGetValue(key, out mesh) && mesh != null) return mesh;
            float R = 0.5f - tube;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int j = 0; j <= sides; j++)
                {
                    float b = j / (float)sides * Mathf.PI * 2f;
                    Vector3 n = c * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    verts.Add(c * R + n * tube); norms.Add(n);
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < sides; j++)
                {
                    int v0 = i * (sides + 1) + j, v1 = v0 + 1, v2 = v0 + sides + 1, v3 = v2 + 1;
                    tris.Add(v0); tris.Add(v1); tris.Add(v2);
                    tris.Add(v2); tris.Add(v1); tris.Add(v3);
                }
            mesh = Finish(key, verts, norms, tris);
            FixWinding(mesh);
            return mesh;
        }

        static Mesh Finish(string key, List<Vector3> v, List<Vector3> n, List<int> t)
        {
            var mesh = new Mesh { name = key };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Intoarce triunghiurile care "privesc" spre interior (normala geometrica opusa normalei vertexului),
        /// ca mesh-urile sa fie corecte indiferent de ordinea in care le-am generat.</summary>
        static void FixWinding(Mesh m)
        {
            var v = m.vertices; var n = m.normals; var t = m.triangles;
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                Vector3 gn = Vector3.Cross(b - a, c - a);
                Vector3 avg = n[t[i]] + n[t[i + 1]] + n[t[i + 2]];
                if (Vector3.Dot(gn, avg) < 0f) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
            }
            m.triangles = t;
            m.RecalculateBounds();
        }
    }
}
