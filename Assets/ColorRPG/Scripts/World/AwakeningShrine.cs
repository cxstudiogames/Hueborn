// =====================================================================
//  AwakeningShrine.cs - ALTARUL DE AWAKENING din ultima harta (langa spawn,
//  in stanga cercului): platforma de pavele, 4 coloane cu cristale, altar cu
//  steaua plutitoare si aleea lui de pavele. [E] = fereastra AWAKENING.
//  Se construieste la Play (lumea din scena ramane neschimbata).
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class AwakeningShrine
    {
        public static void Ensure()
        {
            if (GameObject.Find("Awakening Shrine") != null) return;
            var m = GameData.Maps[AwakeningUI.ShrineMap];
            Vector3 c = AwakeningUI.ShrinePos;
            var root = new GameObject("Awakening Shrine").transform;
            root.SetParent(SceneGroups.Runtime, false);
            root.position = c;

            Color stone = new Color(0.42f, 0.36f, 0.34f), stoneLt = new Color(0.62f, 0.55f, 0.5f), gold = new Color(1f, 0.72f, 0.25f), glow = new Color(1f, 0.55f, 0.15f);
            var th = NatureKit.For(AwakeningUI.ShrineMap);
            if (NatureKit.Available)
            {
                // platforma pavata cu bordura, cerc de rune aurii, 4 menhire cu cristale de foc
                ShrineArt.Platform(root, Vector3.zero, 6.2f, new Color(0.72f, 0.62f, 0.56f), th);
                ShrineArt.RuneCircle(root, Vector3.up * 0.24f, 4.5f, gold);
                ShrineArt.Menhirs(root, Vector3.zero, 4, 5.9f, 4.4f, th, glow, 45f);
                ShrineArt.Glow(root, Vector3.up * 3f, glow, 12f, 1.8f);
            }
            else
            {
            var b0 = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.06f, 0), new Vector3(14f, 0.06f, 14f), stone, root, false);
            b0.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int k = 0; k < 4; k++)
            {
                float a = (k * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 cp = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5.6f;
                Art.Prim(PrimitiveType.Cylinder, cp + Vector3.up * 2f, new Vector3(0.75f, 2f, 0.75f), stone, root);
            }
            }
            // altarul + steaua de awakening
            var alt = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0), new Vector3(2.8f, 0.4f, 2.8f), stoneLt, root, true, false);
            var cobA = Art.PaintedTex("Cobble");
            if (cobA != null) Art.ApplyTexture(alt.GetComponent<Renderer>().sharedMaterial, cobA, 1.1f);
            var bowl = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 1.1f, 0), new Vector3(2f, 0.14f, 2f), gold, root, false, false);
            Art.SetEmission(bowl.GetComponent<Renderer>().sharedMaterial, gold * 0.35f);
            var magma = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 1.25f, 0), new Vector3(1.6f, 0.02f, 1.6f), glow, root, false, false);
            Art.SetEmission(magma.GetComponent<Renderer>().sharedMaterial, glow * 2f);
            var star = new GameObject("Star").transform;
            star.SetParent(root, false);
            star.localPosition = new Vector3(0, 2.6f, 0);
            for (int k = 0; k < 3; k++)
            {
                var s = Art.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f), gold, star, false, false);
                s.transform.localRotation = Quaternion.Euler(k * 30f, k * 30f + 45f, 45f);
                Art.SetEmission(s.GetComponent<Renderer>().sharedMaterial, gold * 0.9f);
            }
            var ss = star.gameObject.AddComponent<Spinner>(); ss.rot = new Vector3(0, 50, 0); ss.bobAmp = 0.25f; ss.bobSpeed = 1.6f;
            ShrineArt.Orbit(root, new Vector3(0, 2.7f, 0), 1.5f, 8, Color.Lerp(gold, Color.white, 0.3f), -40f);
            var fire = new GameObject("FireFx");
            fire.transform.SetParent(root, false);
            fire.transform.localPosition = new Vector3(0, 1.2f, 0);
            var f = fire.AddComponent<FireFx>(); f.col = glow; f.rate = 6f;

            // aleea de pavele spre cercul de spawn
            Vector3 from = new Vector3(7.2f, 0, 0), to = new Vector3(12.2f, 0, -2f);
            Color pave = Color.Lerp(m.ground, new Color(0.75f, 0.72f, 0.68f), 0.6f);
            for (int k = 0; k <= 3; k++)
            {
                Vector3 lp = Vector3.Lerp(from, to, k / 3f);
                Transform ps = NatureKit.Available ? NatureKit.Place(NatureKit.PathStones[k % NatureKit.PathStones.Length], root.TransformPoint(lp) + Vector3.up * 0.01f, 2f, k * 40f, root, th, true) : null;
                if (ps != null) { ps.localScale = new Vector3(1f, 0.6f, 1f); continue; }
                var pv = Art.Prim(PrimitiveType.Cylinder, lp + Vector3.up * 0.03f, new Vector3(1.9f, 0.02f, 1.9f), pave, root, false);
                pv.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            WorldLabel.Create(root, c + Vector3.up * 6.2f, "AWAKENING SHRINE", gold, 30f, 45f);
            var it = root.gameObject.AddComponent<Interactable>();
            it.kind = InteractKind.Custom;
            it.radius = 4.2f;
            it.color = gold;
            it.label = () => "[E] AWAKENING  " + Game.I.player.D.awakenTier + " / " + GameData.MaxAwaken + "  -  choose / upgrade your class";
            it.action = () => { var h = Game.I.hud; if (h != null && h.awakening != null) h.awakening.Open(); };
            Art.MatteGround(root);
        }
    }
}
