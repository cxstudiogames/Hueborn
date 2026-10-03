// Diagnostic: Promo/matdump.flag -> Promo/Logs/mat_dump.txt (materialele modelelor MegaKit)
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGMatDump
    {
        static ColorRPGMatDump()
        {
            EditorApplication.delayCall += () =>
            {
                string flag = Path.Combine(PromoDirector.Folder, "matdump.flag");
                if (!File.Exists(flag) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                try { File.Delete(flag); } catch (System.Exception) { }
                var sb = new System.Text.StringBuilder();
                foreach (var n in new[] { "CommonTree_1", "CommonTree_3", "TwistedTree_1", "Pine_1", "Bush_Common" })
                {
                    var p = Resources.Load<GameObject>("Models/Megakit/" + n);
                    sb.AppendLine("== prefab " + n + (p == null ? " MISSING" : ""));
                    if (p == null) continue;
                    foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        sb.AppendLine("  " + r.name + " mesh=" + (mf && mf.sharedMesh ? mf.sharedMesh.name + " uv0=" + mf.sharedMesh.uv.Length + " uv1=" + mf.sharedMesh.uv2.Length + " col=" + mf.sharedMesh.colors.Length : "-"));
                        foreach (var m in r.sharedMaterials) Dump(sb, m);
                    }
                }
                // peisajul din jurul hartilor (OuterLands)
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                {
                    if (t.name != "Outer Lands") continue;
                    sb.AppendLine("== " + t.parent.parent.name + "/" + t.name + " children=" + t.childCount + " pos=" + t.position + " scale=" + t.lossyScale);
                    // centrul zonei = Apron; cat de mult intra fiecare munte in zona (|x|,|z| < half)
                    Vector3 ctr = t.childCount > 0 ? t.GetChild(0).position : Vector3.zero;
                    for (int i = 1; i < t.childCount; i++)
                    {
                        var c = t.GetChild(i);
                        var r = c.GetComponent<Renderer>();
                        if (r == null) continue;
                        var b = r.bounds;
                        float dx = Mathf.Max(0f, Mathf.Max(b.min.x - ctr.x, ctr.x - b.max.x));
                        float dz = Mathf.Max(0f, Mathf.Max(b.min.z - ctr.z, ctr.z - b.max.z));
                        float inX = Mathf.Min(Mathf.Abs(b.min.x - ctr.x), Mathf.Abs(b.max.x - ctr.x));
                        bool overlapX = b.min.x - ctr.x < 71f && b.max.x - ctr.x > -71f, overlapZ = b.min.z - ctr.z < 71f && b.max.z - ctr.z > -71f;
                        if (overlapX && overlapZ || i < 4)
                            sb.AppendLine("   " + c.name + " mesh=" + (c.GetComponent<MeshFilter>().sharedMesh ? c.GetComponent<MeshFilter>().sharedMesh.name : "-") + " pos=" + c.position + " min=" + b.min + " max=" + b.max + (overlapX && overlapZ ? "  <-- INSIDE" : ""));
                    }
                }
                var go = GameObject.Find("CommonTree_1");
                sb.AppendLine("== scene " + (go ? go.name : "none"));
                if (go) foreach (var r in go.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) Dump(sb, m);
                Directory.CreateDirectory(Path.Combine(PromoDirector.Folder, "Logs"));
                File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "mat_dump.txt"), sb.ToString());
            };
        }

        static void Dump(System.Text.StringBuilder sb, Material m)
        {
            if (m == null) { sb.AppendLine("    mat NULL"); return; }
            var t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            sb.AppendLine("    mat " + m.name + " shader=" + m.shader.name + " tex=" + (t ? t.name : "none") + " col=" + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")
                + " clip=" + (m.HasProperty("_AlphaClip") ? m.GetFloat("_AlphaClip").ToString() : "-") + " kw=" + string.Join(",", m.shaderKeywords) + " path=" + AssetDatabase.GetAssetPath(m));
        }
    }
}
// reload 1790972254
