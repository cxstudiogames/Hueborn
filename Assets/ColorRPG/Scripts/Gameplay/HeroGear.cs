// =====================================================================
//  HeroGear.cs - pune coiful / armura pe un erou Kenney (oasele "head" si
//  "torso" + dimensiunile lor din poza de bind). Folosit de jucatorii din
//  multiplayer (RemotePlayer), ca sa arate exact ca la ei pe ecran.
//  (Player.cs are aceeasi logica pentru eroul tau.)
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class HeroGear
    {
        public struct Anchors
        {
            public Transform headBone, torsoBone;
            public Vector3 headLocal, headSize, torsoLocal, torsoSize;
            public Quaternion headRot, torsoRot;
        }

        /// <summary>Apelat cu modelul in rotatie neutra (poza de bind).</summary>
        public static Anchors Find(Transform model)
        {
            var a = new Anchors();
            if (model == null) return a;
            var headBone = ModelLib.FindDeep(model, "head");
            var torsoBone = ModelLib.FindDeep(model, "torso");
            Transform headMesh = ModelLib.FindDeep(model, "head-mesh");
            Bounds hb = new Bounds(), bb = new Bounds();
            bool hasHead = headMesh != null && ModelLib.GetBounds(headMesh.gameObject, out hb);
            bool hasBody = ModelLib.GetBounds(model.gameObject, out bb);
            if (headBone != null && hasHead)
            {
                a.headBone = headBone;
                a.headLocal = headBone.InverseTransformPoint(hb.center);
                a.headRot = Quaternion.Inverse(headBone.rotation);
                a.headSize = hb.size;
            }
            if (torsoBone != null && hasHead && hasBody)
            {
                float top = hb.min.y, bottom = Mathf.Min(torsoBone.position.y, top - Player.HeroHeight * 0.12f);
                float h = Mathf.Max(0.1f, top - bottom);
                Vector3 c = new Vector3(torsoBone.position.x, bottom + h * 0.5f, torsoBone.position.z);
                a.torsoBone = torsoBone;
                a.torsoLocal = torsoBone.InverseTransformPoint(c);
                a.torsoRot = Quaternion.Inverse(torsoBone.rotation);
                a.torsoSize = new Vector3(hb.size.x * 0.72f, h, Mathf.Min(bb.size.z, hb.size.z) * 0.7f);
            }
            return a;
        }

        public static Transform Root(string n, Transform bone, Vector3 local, Quaternion rot)
        {
            var t = new GameObject(n).transform;
            t.SetParent(bone, false);
            t.localPosition = local;
            t.localRotation = rot;
            float s = bone.lossyScale.x;
            t.localScale = Vector3.one / Mathf.Max(0.0001f, s);
            return t;
        }

        public static Transform Helmet(Anchors a, Item it)
        {
            if (it == null || a.headBone == null) return null;
            var r = Root("Helmet", a.headBone, a.headLocal, a.headRot);
            GearVisuals.BuildHelmet(it, r, a.headSize);
            return r;
        }

        public static Transform Armor(Anchors a, Item it)
        {
            if (it == null || a.torsoBone == null) return null;
            var r = Root("Armor", a.torsoBone, a.torsoLocal, a.torsoRot);
            GearVisuals.BuildArmor(it, r, a.torsoSize);
            return r;
        }

        /// <summary>Marimea armei in mana (ca la Player: creste putin cu forja si raritatea).</summary>
        public static float WeaponScale(Item w)
        {
            int r = w == null ? 0 : w.rarity;
            return 1f + Mathf.Min(0.2f, (w == null ? 0 : w.plus) * 0.008f) + r * 0.025f;
        }

        /// <summary>Item din JSON (null daca e gol / invalid).</summary>
        public static Item FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { var it = JsonUtility.FromJson<Item>(json); if (it != null) ItemDB.ToCatalog(it); return it; }
            catch (System.Exception) { return null; }
        }
    }
}
