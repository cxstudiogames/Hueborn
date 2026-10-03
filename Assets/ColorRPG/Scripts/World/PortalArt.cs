// =====================================================================
//  PortalArt.cs - aspectul comun al tuturor portalurilor (teleport, inapoi,
//  Trade Plaza): soclu rotund de piatra cu trepte, inel din blocuri de piatra
//  cu rune care lumineaza in culoarea destinatiei, cristal in varf care
//  pluteste si un vartej care se roteste in mijloc.
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class PortalArt
    {
        public const float Center = 3.0f;   // inaltimea centrului inelului
        public const float Ring = 2.35f;    // raza inelului
        public const float LabelY = 7.4f;

        public static void Build(Transform root, Color c, Color stone)
        {
            Color stoneDk = Art.Dk(stone, 0.82f);
            Color glow = Color.Lerp(c, Color.white, 0.25f);
            var cob = Art.PaintedTex("Cobble");

            // soclu cu doua trepte (pavaj de piatra)
            var b0 = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(5.8f, 0.15f, 5.8f), Art.Dk(stone, 0.9f), root, false, false);
            var b1 = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.38f, 0), new Vector3(4.6f, 0.08f, 4.6f), Color.Lerp(stone, Color.white, 0.2f), root, false, false);
            if (cob != null)
            {
                Art.ApplyTexture(b0.GetComponent<Renderer>().sharedMaterial, cob, 1.6f);
                Art.ApplyTexture(b1.GetComponent<Renderer>().sharedMaterial, cob, 1.3f);
            }
            // un inel de lumina pe soclu
            var halo = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.47f, 0), new Vector3(4.9f, 0.01f, 4.9f), Color.white, root, false, false);
            halo.GetComponent<Renderer>().sharedMaterial = Art.GlowMaterial(Art.A(glow, 0.35f));
            halo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // inelul: 16 blocuri de piatra, la fiecare 4 o runa care lumineaza
            int n = 16;
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n * Mathf.PI * 2f;
                Vector3 lp = new Vector3(Mathf.Cos(a) * Ring, Center + Mathf.Sin(a) * Ring, 0);
                bool rune = k % 4 == 2;
                var blk = Art.Prim(PrimitiveType.Cube, lp, new Vector3(0.82f, 0.96f, 0.85f), k % 2 == 0 ? stone : stoneDk, root);
                blk.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                if (rune)
                {
                    var rn = Art.Prim(PrimitiveType.Cube, lp, new Vector3(0.36f, 0.5f, 0.98f), glow, root, false, false);
                    rn.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                    Art.SetEmission(rn.GetComponent<Renderer>().sharedMaterial, glow * 0.6f);
                }
            }
            // picioarele inelului (il tin pe soclu)
            for (int s = -1; s <= 1; s += 2)
            {
                var foot = Art.Prim(PrimitiveType.Cube, new Vector3(s * 1.55f, 0.85f, 0), new Vector3(1.1f, 0.9f, 1.1f), stoneDk, root);
                foot.transform.localRotation = Quaternion.Euler(0, 0, s * 12f);
            }

            // cristalul din varf (pluteste si se roteste)
            var cr = new GameObject("Crystal").transform;
            cr.SetParent(root, false);
            cr.localPosition = new Vector3(0, Center + Ring + 0.95f, 0);
            var gem = Art.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(0.55f, 0.55f, 0.55f), glow, cr, false, false);
            gem.transform.localRotation = Quaternion.Euler(45f, 0, 45f);
            gem.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
            Art.SetEmission(gem.GetComponent<Renderer>().sharedMaterial, glow * 0.5f);
            var sp = cr.gameObject.AddComponent<Spinner>();
            sp.rot = new Vector3(0, 70f, 0); sp.bobAmp = 0.15f; sp.bobSpeed = 2f;

            // vartejul: fundal moale + doua spirale care se rotesc in sens opus
            var tex = Art.PaintedTex("Swirl");
            Disc(root, 4.2f, Art.A(c, 0.4f), null, 0f, -0.02f);
            Disc(root, 4.3f, Art.A(glow, 0.9f), tex, -55f, 0f);
            Disc(root, 3.0f, Art.A(Color.Lerp(glow, Color.white, 0.4f), 0.9f), tex, 110f, 0.02f);

            var pf = new GameObject("PortalFx").transform;
            pf.SetParent(root, false);
            pf.localPosition = Vector3.up * Center;
            pf.gameObject.AddComponent<PortalFx>().col = c;
        }

        static void Disc(Transform root, float d, Color c, Texture tex, float spin, float z)
        {
            var g = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, Center, z), new Vector3(d, 0.01f, d), Color.white, root, false, false);
            g.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = Art.MeshFx(c, true, tex);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (spin != 0f) g.AddComponent<Spinner>().rot = new Vector3(0, spin, 0);
        }
    }
}
