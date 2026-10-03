// =====================================================================
//  ColorRPGOrganize.cs - ordine in proiect:
//   * ColorRPG > Organize Hierarchy: in fiecare folder din "=== WORLD ===" cu multe
//     obiecte, obiectele de acelasi fel intra in sub-foldere ("Oak (64)", "Rock (40)"...).
//     Pozitiile NU se schimba; se poate da Undo (Ctrl+Z).
//   * ColorRPG > Organize Project Folders: Promo/ pe sub-foldere (Video, Screenshots,
//     Renders, Store, Brand, Logs), backup-urile vechi de scena mutate in _Backups/Scenes
//     (in afara Assets -> Unity nu le mai importa, proiectul se incarca mai repede).
//   Automat: fisierul Promo/organize.flag -> le face pe amandoua si salveaza scena.
// =====================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGOrganize
    {
        static string Root { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
        static string Flag { get { return Path.Combine(PromoDirector.Folder, "organize.flag"); } }

        static ColorRPGOrganize()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Flag) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                try { File.Delete(Flag); } catch (System.Exception) { }
                var log = new System.Text.StringBuilder();
                log.AppendLine("time: " + System.DateTime.Now);
                try
                {
                    log.AppendLine(OrganizeHierarchy(false));
                    log.AppendLine(OrganizeFolders());
                    var sc = EditorSceneManager.GetActiveScene();
                    if (sc.IsValid() && !string.IsNullOrEmpty(sc.path)) { EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc); log.AppendLine("scene saved: " + sc.path); }
                }
                catch (System.Exception e) { log.AppendLine("EXCEPTION: " + e); }
                Directory.CreateDirectory(Path.Combine(PromoDirector.Folder, "Logs"));
                File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "organize_result.txt"), log.ToString());
                Debug.Log("[ColorRPG] Organize: " + log);
            };
        }

        // ------------------------------------------------------------------ HIERARCHY
        [MenuItem("ColorRPG/Organize Hierarchy", false, 3)]
        static void HierarchyMenu() { EditorUtility.DisplayDialog("ColorRPG", OrganizeHierarchy(true), "OK"); }

        static readonly Regex Suffix = new Regex(@"\s*\(\d+\)$");

        static string Key(string n)
        {
            n = Suffix.Replace(n, "");
            if (n.StartsWith("M_")) n = n.Substring(2);
            return n.Trim();
        }

        /// <summary>Obiect "simplu" (decor): fara scripturi de joc pe el, in afara de cele de animatie.</summary>
        static bool Plain(Transform t)
        {
            foreach (var mb in t.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                var n = mb.GetType().Name;
                if (n == "Sway" || n == "Spinner") continue;
                return false;
            }
            return true;
        }

        public static string OrganizeHierarchy(bool undo)
        {
            var world = Object.FindObjectsByType<WorldRoot>(FindObjectsInactive.Include).FirstOrDefault();
            if (world == null) return "No '=== WORLD ===' in this scene.";
            int folders = 0, moved = 0;
            var stack = new Stack<Transform>(); stack.Push(world.transform);
            var parents = new List<Transform>();
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                if (t.childCount >= 12 && !t.name.StartsWith("[")) parents.Add(t);   // folderele facute deja nu se mai impart
                for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).childCount > 0 && t.GetChild(i).GetComponent<Renderer>() == null && Plain(t.GetChild(i))) stack.Push(t.GetChild(i));
            }
            foreach (var p in parents)
            {
                var groups = new Dictionary<string, List<Transform>>();
                for (int i = 0; i < p.childCount; i++)
                {
                    var c = p.GetChild(i);
                    if (c.name.StartsWith("[") || !Plain(c)) continue;   // deja folder / obiect cu logica
                    string k = Key(c.name);
                    List<Transform> l;
                    if (!groups.TryGetValue(k, out l)) groups[k] = l = new List<Transform>();
                    l.Add(c);
                }
                foreach (var kv in groups.OrderBy(g => g.Key))
                {
                    if (kv.Value.Count < 3) continue;
                    var f = new GameObject("[" + kv.Key + "] x" + kv.Value.Count);
                    if (undo) Undo.RegisterCreatedObjectUndo(f, "Organize Hierarchy");
                    f.transform.SetParent(p, false);
                    f.isStatic = kv.Value[0].gameObject.isStatic;
                    foreach (var c in kv.Value)
                    {
                        if (undo) Undo.SetTransformParent(c, f.transform, "Organize Hierarchy");
                        else c.SetParent(f.transform, true);
                        moved++;
                    }
                    folders++;
                }
            }
            if (folders > 0) EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            return "Hierarchy: " + moved + " objects grouped into " + folders + " folders (positions unchanged). Save the scene (Ctrl+S) to keep it.";
        }

        // ------------------------------------------------------------------ FOLDERE PE DISC
        [MenuItem("ColorRPG/Organize Project Folders", false, 4)]
        static void FoldersMenu() { EditorUtility.DisplayDialog("ColorRPG", OrganizeFolders(), "OK"); }

        static int MoveFile(string from, string toDir)
        {
            if (!File.Exists(from)) return 0;
            Directory.CreateDirectory(toDir);
            string to = Path.Combine(toDir, Path.GetFileName(from));
            if (File.Exists(to)) to = Path.Combine(toDir, Path.GetFileNameWithoutExtension(from) + "_" + System.DateTime.Now.ToString("HHmmss") + Path.GetExtension(from));
            File.Move(from, to);
            return 1;
        }

        static int MoveDirContents(string from, string toDir)
        {
            if (!Directory.Exists(from)) return 0;
            if (string.Equals(Path.GetFullPath(from).TrimEnd('\\', '/'), Path.GetFullPath(toDir).TrimEnd('\\', '/'), System.StringComparison.OrdinalIgnoreCase)) return 0;   // Windows: "brand" == "Brand"
            int n = 0;
            foreach (var f in Directory.GetFiles(from)) n += MoveFile(f, toDir);
            try { if (Directory.GetFileSystemEntries(from).Length == 0) Directory.Delete(from); } catch (System.Exception) { }
            return n;
        }

        public static string OrganizeFolders()
        {
            string promo = PromoDirector.Folder;
            int n = 0;
            if (Directory.Exists(promo))
            {
                foreach (var f in Directory.GetFiles(promo))
                {
                    string name = Path.GetFileName(f).ToLowerInvariant();
                    if (name.EndsWith(".flag")) continue;
                    if (name.EndsWith(".mp4")) n += MoveFile(f, PromoDirector.VideoDir);
                    else if (name.StartsWith("shot_")) n += MoveFile(f, PromoDirector.ShotsDir);
                    else if (name.EndsWith(".txt")) n += MoveFile(f, Path.Combine(promo, "Logs"));
                    else if (name.Contains("playstore") || name.Contains("feature")) n += MoveFile(f, Path.Combine(promo, "Store"));
                }
                n += MoveDirContents(Path.Combine(promo, "icon"), PromoDirector.RendersDir);
                n += MoveDirContents(Path.Combine(promo, "icon_options"), Path.Combine(promo, "Store", "IconOptions"));
                n += MoveDirContents(Path.Combine(promo, "brand"), Path.Combine(promo, "Brand"));
                n += MoveDirContents(Path.Combine(promo, "frames"), Path.Combine(promo, "Video", "frames"));
            }
            // reparatie: fisiere redenumite cu "_HHmmss" cand sursa si destinatia erau acelasi folder
            foreach (var dir in new[] { Path.Combine(promo, "Brand"), Path.Combine(promo, "Store"), PromoDirector.RendersDir })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var f in Directory.GetFiles(dir))
                {
                    var m = Regex.Match(Path.GetFileNameWithoutExtension(f), @"^(.*)_\d{6}$");
                    if (!m.Success) continue;
                    string orig = Path.Combine(dir, m.Groups[1].Value + Path.GetExtension(f));
                    if (!File.Exists(orig)) { File.Move(f, orig); n++; }
                }
            }
            // backup-urile de scena din Assets -> _Backups/Scenes (in afara Assets)
            string assetBackups = "Assets/ColorRPG/Backups";
            if (AssetDatabase.IsValidFolder(assetBackups))
            {
                string dst = Path.Combine(Root, "_Backups", "Scenes");
                Directory.CreateDirectory(dst);
                foreach (var guid in AssetDatabase.FindAssets("", new[] { assetBackups }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(p)) continue;
                    string full = Path.GetFullPath(p);
                    string to = Path.Combine(dst, Path.GetFileName(p));
                    if (!File.Exists(to)) File.Copy(full, to);
                    AssetDatabase.DeleteAsset(p);
                    n++;
                }
                AssetDatabase.DeleteAsset(assetBackups);
            }
            // fisiere ramase prin radacina proiectului
            n += MoveFile(Path.Combine(Root, "uidebug.txt"), Path.Combine(Root, "_Backups", "Misc"));
            AssetDatabase.Refresh();
            return "Folders: " + n + " files moved. Promo -> Video / Screenshots / Renders / Store / Brand / Logs; old scene backups -> _Backups/Scenes.";
        }
    }
}
