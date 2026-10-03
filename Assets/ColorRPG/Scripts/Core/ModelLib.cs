// =====================================================================
//  ModelLib.cs - incarca modelele 3D CC0 (Kenney) din Resources/Models
//  Toate modelele sunt normalizate la o inaltime dorita si "asezate" pe sol,
//  asa ca nu conteaza scara/pivotul din FBX. Daca un model lipseste,
//  Spawn() intoarce null si codul apelant foloseste primitive colorate.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class ModelLib
    {
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        static readonly HashSet<string> missing = new HashSet<string>();

        /// <summary>
        /// Rotatie suplimentara (grade) pe Y aplicata tuturor modelelor Kenney, ca sa priveasca spre +Z.
        /// Daca personajele apar cu spatele, schimba valoarea in 180 (vezi PROGRESS.md).
        /// </summary>
        public static float YawFix = 0f;

        public static GameObject Prefab(string path)
        {
            if (string.IsNullOrEmpty(path) || missing.Contains(path)) return null;
            GameObject p;
            if (cache.TryGetValue(path, out p) && p != null) return p;
            p = Resources.Load<GameObject>(path);
            if (p == null) { missing.Add(path); return null; }
            cache[path] = p;
            return p;
        }

        public static bool Exists(string path) { return Prefab(path) != null; }

        /// <summary>
        /// Instantiaza un model sub "parent". Modelul este scalat astfel incat
        /// inaltimea (sau latimea maxima, daca byWidth=true) sa fie "size",
        /// centrat pe X/Z si cu baza la y=0 fata de parent.
        /// Intoarce transform-ul "holder" (copilul direct al lui parent) sau null.
        /// </summary>
        public static Transform Spawn(string path, Transform parent, float size, float yaw = 0f, bool byWidth = false, bool shadows = true)
        {
            var prefab = Prefab(path);
            if (prefab == null) return null;

            var holder = new GameObject("M_" + prefab.name).transform;
            var inst = Object.Instantiate(prefab);
            inst.name = prefab.name;
            inst.transform.SetParent(holder, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Art.Kill(c);
            Art.FixMaterials(inst);
            Art.Realistic(inst);
            if (!shadows) Art.SetLayerShadows(inst, false);

            // Stop root-motion si orice controller importat
            foreach (var an in inst.GetComponentsInChildren<Animator>(true))
            {
                an.applyRootMotion = false;
                an.runtimeAnimatorController = null;
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            Bounds b;
            if (GetBounds(inst, out b))
            {
                float dim = byWidth ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
                if (dim < 0.0001f) dim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (dim > 0.0001f)
                {
                    float s = size / dim;
                    inst.transform.localScale = inst.transform.localScale * s;
                    GetBounds(inst, out b);
                    inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
                }
            }

            holder.SetParent(parent, false);
            holder.localPosition = Vector3.zero;
            holder.localRotation = Quaternion.Euler(0, yaw + YawFix, 0);
            holder.localScale = Vector3.one;
            return holder;
        }

        /// <summary>Plaseaza un model decorativ in lume (pozitie absoluta).</summary>
        public static Transform Place(string path, Vector3 pos, float size, float yaw, Transform parent, bool byWidth = false)
        {
            var anchor = new GameObject(System.IO.Path.GetFileName(path)).transform;
            anchor.SetParent(parent, false);
            anchor.position = pos;
            var t = Spawn(path, anchor, size, yaw, byWidth);
            if (t == null) { Art.Kill(anchor.gameObject); return null; }
            return anchor;
        }

        public static bool GetBounds(GameObject go, out Bounds b)
        {
            b = new Bounds();
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Bounds rb = r.bounds;
                var smr = r as SkinnedMeshRenderer;
                if (smr != null && smr.sharedMesh != null)
                {
                    // bounds-ul din mesh (bind pose) e mai stabil decat cel skinned la prima aparitie
                    var mb = smr.sharedMesh.bounds;
                    rb = TransformBounds(smr.transform, mb);
                }
                if (!any) { b = rb; any = true; }
                else b.Encapsulate(rb);
            }
            return any;
        }

        static Bounds TransformBounds(Transform t, Bounds lb)
        {
            Vector3 c = lb.center, e = lb.extents;
            var res = new Bounds(t.TransformPoint(c), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                res.Encapsulate(t.TransformPoint(p));
            }
            return res;
        }

        public static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>Toate materialele instantei devin copii proprii (pentru flash/culoare per-inamic).</summary>
        public static List<Material> OwnMaterials(Transform root)
        {
            var list = new List<Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var mats = r.materials; // creeaza copii
                list.AddRange(mats);
            }
            return list;
        }
    }
}
