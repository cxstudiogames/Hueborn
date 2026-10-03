// =====================================================================
//  ShrineArt.cs - piese comune pentru locurile speciale (Forja, altarul de
//  REBIRTH, altarul de AWAKENING): platforma de piatra pavata cu bordura,
//  cerc de rune care se roteste pe jos, menhire cu cristale plutitoare,
//  inel de cristale care orbiteaza, lumina colorata.
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class ShrineArt
    {
        static void NoShadow(GameObject g) { g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }

        /// <summary>Platforma rotunda: bordura inchisa + piatra pavata + bordura din pietre mici (MegaKit).</summary>
        public static void Platform(Transform root, Vector3 lp, float r, Color tint, NatureKit.Theme th, float h = 0.22f)
        {
            var cob = Art.PaintedTex("Cobble");
            var rim = Art.Prim(PrimitiveType.Cylinder, lp + Vector3.up * (h * 0.5f), new Vector3(r * 2f + 1.2f, h * 0.5f, r * 2f + 1.2f), Art.Dk(tint, 0.62f), root, false, false);
            var top = Art.Prim(PrimitiveType.Cylinder, lp + Vector3.up * (h + 0.01f), new Vector3(r * 2f, 0.02f, r * 2f), tint, root, false, false);
            if (cob != null)
            {
                Art.ApplyTexture(rim.GetComponent<Renderer>().sharedMaterial, cob, r / 1.6f);
                Art.ApplyTexture(top.GetComponent<Renderer>().sharedMaterial, cob, r / 2.2f);
            }
            NoShadow(top);
            if (!NatureKit.Available) return;
            int n = Mathf.Max(12, Mathf.RoundToInt((r + 0.6f) * 2f * Mathf.PI / 1.2f));
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n * Mathf.PI * 2f;
                Vector3 sp = lp + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r + 0.6f);
                var st = NatureKit.Place(NatureKit.PathStones[k % NatureKit.PathStones.Length], root.TransformPoint(sp) + Vector3.up * (h * 0.5f), 1.2f, -a * Mathf.Rad2Deg, root, th, true);
                if (st == null) continue;
                st.localScale = new Vector3(1f, 1.6f, 1f);
                foreach (var rr in st.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>Cerc de rune pe jos: spirala luminoasa care se roteste incet + puncte de lumina pe margine.</summary>
        public static void RuneCircle(Transform root, Vector3 lp, float r, Color c)
        {
            var tex = Art.PaintedTex("Swirl");
            var d = Art.Prim(PrimitiveType.Cylinder, lp + Vector3.up * 0.02f, new Vector3(r * 2f, 0.005f, r * 2f), Color.white, root, false, false);
            d.GetComponent<Renderer>().sharedMaterial = Art.MeshFx(Art.A(c, 0.55f), true, tex);
            NoShadow(d);
            d.AddComponent<Spinner>().rot = new Vector3(0, 18f, 0);
            var ring = new GameObject("RuneRing").transform;
            ring.SetParent(root, false);
            ring.localPosition = lp + Vector3.up * 0.06f;
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                var g = Art.Prim(PrimitiveType.Cube, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r * 1.02f), new Vector3(0.28f, 0.06f, 0.5f), c, ring, false, false);
                g.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                Art.SetEmission(g.GetComponent<Renderer>().sharedMaterial, c * 0.6f);
                NoShadow(g);
            }
            ring.gameObject.AddComponent<Spinner>().rot = new Vector3(0, -10f, 0);
        }

        /// <summary>Menhire (stanci inalte) in cerc, fiecare cu un cristal care pluteste deasupra.</summary>
        public static void Menhirs(Transform root, Vector3 lp, int count, float radius, float height, NatureKit.Theme th, Color crystal, float startDeg = 30f)
        {
            for (int k = 0; k < count; k++)
            {
                float a = (startDeg + k * 360f / count) * Mathf.Deg2Rad;
                Vector3 p = lp + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                float hh = height * (k % 2 == 0 ? 1f : 0.82f);
                Transform t = NatureKit.Available ? NatureKit.Place(NatureKit.Rocks[k % NatureKit.Rocks.Length], root.TransformPoint(p) - Vector3.up * 0.2f, hh, k * 47f, root, th) : null;
                if (t != null)
                {
                    t.localScale = new Vector3(0.42f, 1f, 0.42f);
                    var cc = t.gameObject.AddComponent<CapsuleCollider>();
                    cc.radius = 0.7f; cc.height = hh; cc.center = new Vector3(0, hh * 0.5f, 0);
                }
                else Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * hh * 0.5f, new Vector3(0.8f, hh * 0.5f, 0.8f), new Color(0.5f, 0.5f, 0.55f), root);
                Crystal(root, p + Vector3.up * (hh + 0.9f), 0.55f, crystal, 60f + k * 7f);
            }
        }

        /// <summary>Cristal (doua piramide) care pluteste si se roteste.</summary>
        public static Transform Crystal(Transform root, Vector3 lp, float s, Color c, float spin = 60f)
        {
            var g = new GameObject("Crystal").transform;
            g.SetParent(root, false);
            g.localPosition = lp;
            var a = Art.Prim(PrimitiveType.Cube, Vector3.zero, Vector3.one * s, c, g, false, false);
            a.transform.localRotation = Quaternion.Euler(45f, 0, 45f);
            a.transform.localScale = new Vector3(s * 0.75f, s * 1.5f, s * 0.75f);
            Art.SetEmission(a.GetComponent<Renderer>().sharedMaterial, c * 0.45f);
            NoShadow(a);
            var sp = g.gameObject.AddComponent<Spinner>();
            sp.rot = new Vector3(0, spin, 0); sp.bobAmp = 0.18f; sp.bobSpeed = 1.5f + s;
            return g;
        }

        /// <summary>Inel de cristale mici care orbiteaza in jurul unui punct (deasupra altarului).</summary>
        public static void Orbit(Transform root, Vector3 lp, float r, int n, Color c, float speed)
        {
            var o = new GameObject("Orbit").transform;
            o.SetParent(root, false);
            o.localPosition = lp;
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n * Mathf.PI * 2f;
                var g = Art.Prim(PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a * 2f) * 0.25f, Mathf.Sin(a) * r), Vector3.one * 0.26f, c, o, false, false);
                g.transform.localRotation = Quaternion.Euler(45, 45, 0);
                Art.SetEmission(g.GetComponent<Renderer>().sharedMaterial, c * 0.5f);
                NoShadow(g);
            }
            o.gameObject.AddComponent<Spinner>().rot = new Vector3(0, speed, 0);
        }

        public static void Glow(Transform root, Vector3 lp, Color c, float range, float intensity)
        {
            return;   // optimizare: fara lumini punctiforme (cristalele / runele sunt emisive)
#pragma warning disable CS0162
            var lg = new GameObject("ShrineLight");
            lg.transform.SetParent(root, false);
            lg.transform.localPosition = lp;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = c; l.shadows = LightShadows.None;
#pragma warning restore CS0162
        }
    }
}
