// =====================================================================
//  ColorRPGFixColliders.cs - dupa modificarile manuale din scena:
//  cand muti doar modelul (ex. "Rock_Medium_3" din interiorul obiectului),
//  coliziunea (copilul "Collider" al obiectului) ramane in locul vechi.
//  Acest tool muta fiecare "Collider" exact pe centrul modelului vizibil
//  (asa cum il punea si generatorul lumii), fara sa schimbe nimic altceva.
//   * ColorRPG > Fix Colliders To Visuals   (sau Promo/fixcolliders.flag)
//   * salveaza scena + raport in Promo/Logs/fix_colliders.txt
// =====================================================================
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGFixColliders
    {
        static string Flag { get { return Path.Combine(PromoDirector.Folder, "fixcolliders.flag"); } }

        static ColorRPGFixColliders()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!File.Exists(Flag)) return;
                try { File.Delete(Flag); } catch (System.Exception) { }
                Run();
            };
        }

        [MenuItem("ColorRPG/Fix Colliders To Visuals", false, 3)]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var log = new StringBuilder("time: " + System.DateTime.Now + "\n");
            int moved = 0, total = 0, noVisual = 0;
            foreach (var ob in Resources.FindObjectsOfTypeAll<Obstacle>())
            {
                if (ob == null || EditorUtility.IsPersistent(ob) || ob.gameObject.scene != scene) continue;
                var col = ob.transform;
                if (col.name != "Collider" || col.parent == null) continue;   // doar coliziunile puse pe obiecte (nu stalpii cactusilor / piramidelor)
                total++;
                var anchor = col.parent;
                Bounds b = new Bounds(); bool any = false;
                foreach (var r in anchor.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.transform == col || r.transform.IsChildOf(col)) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (!any)
                {
                    noVisual++;
                    // modelul a fost sters de mana -> coliziunea ramasa e un "perete invizibil": o dezactivam (nu o stergem)
                    if (anchor.name != "Pillar" && anchor.gameObject.activeSelf)
                    {
                        Undo.RecordObject(anchor.gameObject, "Fix Collider");
                        anchor.gameObject.SetActive(false);
                        moved++;
                        log.Append(FullPath(anchor)).Append("  no model left -> disabled\n");
                    }
                    continue;
                }
                Vector3 p = col.position;
                float d = new Vector2(b.center.x - p.x, b.center.z - p.z).magnitude;
                if (d < 0.05f) continue;
                Undo.RecordObject(col, "Fix Collider");
                col.position = new Vector3(b.center.x, p.y, b.center.z);
                moved++;
                log.Append(FullPath(anchor)).Append("  moved ").Append(d.ToString("0.0")).Append(" m\n");
            }
            log.Insert(0, "colliders checked " + total + ", moved " + moved + ", without visual " + noVisual + "\n");
            if (moved > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            }
            Directory.CreateDirectory(System.IO.Path.Combine(PromoDirector.Folder, "Logs"));
            File.WriteAllText(System.IO.Path.Combine(PromoDirector.Folder, "Logs", "fix_colliders.txt"), log.ToString());
            Debug.Log("[ColorRPG] Fix Colliders: " + moved + " of " + total + " colliders moved onto their models (see Promo/Logs/fix_colliders.txt).");
        }

        static string FullPath(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }
    }
}
