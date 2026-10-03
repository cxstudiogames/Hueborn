// =====================================================================
//  ColorRPGSceneBuilder.cs (doar in editor)
//
//  Pune TOATA lumea jocului in scena, ca sa o vezi si sa o modifici in Hierarchy:
//
//    === GAME ===                 -> componenta Game (porneste jocul)
//    === ENVIRONMENT ===          -> camera, soarele, volumul de post-processing
//    === WORLD ===                -> hartile + Trade Plaza, fiecare cu foldere:
//        Map 1 - Meadow Fields / Terrain, Start Area, Teleport Corner, Boss Arena, Decor
//        Trade Plaza / Terrain, Booths, Decor, Shoppers (NPC), ReturnPortal
//    === MY OBJECTS (kept on rebuild) ===  -> pune aici obiectele tale; nu sunt sterse niciodata
//
//  Meniu:  ColorRPG > Rebuild World In Scene
//  Automat: cand jocul primeste un update (GameData.WorldVersion creste), lumea din
//           scena se reconstruieste singura. Inainte, scena veche e salvata in
//           Assets/ColorRPG/Backups (ca sa nu pierzi modificarile facute de mana).
//  Materialele si texturile generate sunt salvate in Assets/ColorRPG/Generated.
// =====================================================================
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGSceneBuilder
    {
        const string GenDir = "Assets/ColorRPG/Generated";
        const string BackupDir = "Assets/ColorRPG/Backups";   // (vechi) - backup-urile noi merg in <proiect>/_Backups/Scenes
        /// <summary>LUMEA E BLOCATA: scena contine modificarile tale facute de mana (obiecte sterse / mutate).
        /// Update-urile jocului NU o mai reconstruiesc automat; "Rebuild World" cere confirmare dubla.</summary>
        public const bool WorldLocked = true;
        /// <summary>BLOCARE TOTALA: cat timp e true, lumea din scena NU se mai reconstruieste deloc
        /// (nici din meniu, nici din rebuildworld.flag, nici la update-uri). Modificarile tale manuale
        /// (obiecte mutate / rotite / sterse) raman. Schimbarile viitoare la lume se fac direct pe scena.</summary>
        public const bool WorldFinal = true;
        static string BackupDiskDir { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "_Backups", "Scenes")); } }
        const string AutoKey = "ColorRPG_AutoRebuildWorld";
        const string AutoMenu = "ColorRPG/Auto-update World On Game Updates";

        public const string GameRootName = "=== GAME ===";
        public const string EnvRootName = "=== ENVIRONMENT ===";
        public const string MyRootName = "=== MY OBJECTS (kept on rebuild) ===";

        static ColorRPGSceneBuilder()
        {
            EditorApplication.delayCall += AutoCheck;
            EditorSceneManager.sceneOpened += (s, m) => { EditorApplication.delayCall += AutoCheck; };
        }

        static bool AutoEnabled
        {
            get { return !WorldLocked && EditorPrefs.GetBool(AutoKey, true); }
            set { EditorPrefs.SetBool(AutoKey, value); }
        }

        // ---------------------------------------------------------------
        //  Meniu
        // ---------------------------------------------------------------
        [MenuItem("ColorRPG/Rebuild World In Scene", false, 1)]
        static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog("ColorRPG",
                "Rebuild '=== WORLD ===' in this scene?\n\nA backup of the scene is saved first in Assets/ColorRPG/Backups.\nObjects under '" + MyRootName + "' are kept.",
                "Rebuild", "Cancel")) return;
            if (WorldLocked && !EditorUtility.DisplayDialog("ColorRPG - WORLD IS LOCKED",
                "The world contains YOUR manual edits (removed / moved objects).\nA rebuild generates the world again from code and your edits will be LOST\n(a backup is saved in _Backups/Scenes).\n\nAre you really sure?",
                "Yes, rebuild anyway", "Cancel")) return;
            Rebuild();
        }

        [MenuItem("ColorRPG/Select World", false, 2)]
        static void SelectWorld()
        {
            var w = FindWorld();
            if (w != null) { Selection.activeGameObject = w.gameObject; EditorGUIUtility.PingObject(w.gameObject); }
        }

        [MenuItem(AutoMenu, false, 20)]
        static void ToggleAuto() { AutoEnabled = !AutoEnabled; }

        [MenuItem(AutoMenu, true)]
        static bool ToggleAutoValidate() { Menu.SetChecked(AutoMenu, AutoEnabled); return true; }

        // ---------------------------------------------------------------
        //  Verificare automata (la fiecare recompilare / deschidere de scena)
        // ---------------------------------------------------------------
        static void AutoCheck()
        {
            if (!AutoEnabled) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoCheck;   // incercam mai tarziu
                return;
            }
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path)) return;
            var w = FindWorld();
            if (w != null)
            {
                if (w.version < GameData.WorldVersion)
                {
                    Debug.Log("[ColorRPG] Game update: world v" + w.version + " -> v" + GameData.WorldVersion + ". Rebuilding the scene...");
                    Rebuild();
                }
            }
            else if (scene.name == "SampleScene" || RootByName(scene, "=== WORLD ===") != null)
            {
                Debug.Log("[ColorRPG] The scene has no world yet. Building it in the Hierarchy...");
                Rebuild();
            }
        }

        static WorldRoot FindWorld()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<WorldRoot>())
                if (w != null && !EditorUtility.IsPersistent(w) && w.gameObject.scene.IsValid()) return w;
            return null;
        }

        // ---------------------------------------------------------------
        //  Reconstruire
        // ---------------------------------------------------------------
        public static void Rebuild()
        {
            var scene = SceneManager.GetActiveScene();
            var old = FindWorld();
            if (WorldFinal && old != null)
            {
                Debug.LogWarning("[ColorRPG] World is FINAL (WorldFinal = true): rebuild skipped, your manual edits are kept.");
                return;
            }

            // 1) backup la scena veche (daca avea deja o lume)
            if (old != null && !string.IsNullOrEmpty(scene.path))
            {
                if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                System.IO.Directory.CreateDirectory(BackupDiskDir);
                string bp = System.IO.Path.Combine(BackupDiskDir, scene.name + "_world_v" + old.version + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
                System.IO.File.Copy(System.IO.Path.GetFullPath(scene.path), bp, true);
            }

            // 2) stergem lumea veche (doar "=== WORLD ===")
            var toKill = new List<GameObject>();
            foreach (var w in Resources.FindObjectsOfTypeAll<WorldRoot>())
                if (w != null && !EditorUtility.IsPersistent(w) && w.gameObject.scene == scene) toKill.Add(w.gameObject);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "=== WORLD ===" && !toKill.Contains(go)) toKill.Add(go);   // si lumi vechi fara componenta
            foreach (var go in toKill) Object.DestroyImmediate(go);

            // 3) folderele de baza
            EnsureRoots(scene);

            // 4) construim lumea cu acelasi cod ca jocul
            Art.Reset();
            var world = WorldBuilder.BuildAll();
            world.transform.SetSiblingIndex(2);

            // 5) salvam materialele / texturile generate ca asset-uri (editabile)
            SaveGeneratedAssets(world.gameObject);

            // 5b) cerul procedural (ColorRPG/Sky) salvat ca asset si pus in scena
            var sky = Art.SkyMaterial();
            if (sky != null)
            {
                string sp = GenDir + "/Materials/Sky.mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(sp);
                if (existing != null) { EditorUtility.CopySerialized(sky, existing); existing.name = "Sky"; sky = existing; }
                else AssetDatabase.CreateAsset(sky, sp);
                RenderSettings.skybox = sky;
            }

            // 6) optimizare: obiectele care nu se misca devin "Static" (static batching = mai putine draw calls)
            int st = MarkStatic(world.transform, false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);

            // 7) stergem materialele generate care nu mai sunt folosite de nicio scena
            int removed = ColorRPGCleanup.RemoveOrphanGenerated(false);
            // pastram doar ultimele 3 backup-uri de scena (fiecare are ~8 MB)
            var oldB = ColorRPGCleanup.OldBackups(3);
            if (oldB.Count > 0) { AssetDatabase.MoveAssetsToTrash(oldB.ToArray(), new List<string>()); removed += oldB.Count; }
            Debug.Log("[ColorRPG] Optimization: " + st + " static objects, " + removed + " unused generated assets removed.");
            Selection.activeGameObject = world.gameObject;
            Debug.Log("[ColorRPG] World v" + GameData.WorldVersion + " built in the scene: " + CountChildren(world.transform) + " objects under '=== WORLD ==='.");
        }

        /// <summary>Marcheaza static tot ce nu se misca (fara copaci care se leagana, NPC, obiecte care se rotesc).</summary>
        static int MarkStatic(Transform t, bool moving)
        {
            moving = moving || t.GetComponent<Sway>() != null || t.GetComponent<Spinner>() != null ||
                     t.GetComponent<NpcTrader>() != null || t.GetComponent<Shopper>() != null ||
                     t.GetComponent<NpcTrader>() != null || t.GetComponent<Animator>() != null ||
                     t.GetComponent<SkinnedMeshRenderer>() != null;
            int n = 0;
            if (!moving)
            {
                // plasele cu Mesh LOD nu suporta static batching (ar ramane mereu pe LOD0) -> doar OccludeeStatic
                var mf = t.GetComponent<MeshFilter>();
                bool lod = mf != null && mf.sharedMesh != null && mf.sharedMesh.lodCount > 1;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, lod ? StaticEditorFlags.OccludeeStatic : StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                n++;
            }
            for (int i = 0; i < t.childCount; i++) n += MarkStatic(t.GetChild(i), moving);
            return n;
        }

        static int CountChildren(Transform t)
        {
            int n = 1;
            for (int i = 0; i < t.childCount; i++) n += CountChildren(t.GetChild(i));
            return n;
        }

        static GameObject RootByName(Scene scene, string n)
        {
            foreach (var go in scene.GetRootGameObjects()) if (go.name == n) return go;
            return null;
        }

        static void EnsureRoots(Scene scene)
        {
            // === GAME ===
            var game = Object.FindAnyObjectByType<Game>();
            if (game == null)
            {
                var g = RootByName(scene, GameRootName) ?? new GameObject(GameRootName);
                g.AddComponent<Game>();
            }
            else
            {
                game.gameObject.name = GameRootName;
                game.transform.SetParent(null, true);
            }
            var gr = RootByName(scene, GameRootName);
            if (gr != null) gr.transform.SetSiblingIndex(0);

            // === ENVIRONMENT === (camera, lumini, volume)
            var env = RootByName(scene, EnvRootName) ?? new GameObject(EnvRootName);
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go == env) continue;
                bool isEnv = go.GetComponent<Camera>() != null || go.GetComponent<Light>() != null ||
                             go.GetComponent<UnityEngine.Rendering.Volume>() != null;
                if (isEnv) go.transform.SetParent(env.transform, true);
            }
            env.transform.SetSiblingIndex(1);

            // === MY OBJECTS === (niciodata sters)
            var mine = RootByName(scene, MyRootName) ?? new GameObject(MyRootName);
            mine.transform.SetAsLastSibling();
        }

        // ---------------------------------------------------------------
        //  Materiale + texturi -> Assets/ColorRPG/Generated
        // ---------------------------------------------------------------
        static void SaveGeneratedAssets(GameObject root)
        {
            EnsureFolder(GenDir);
            EnsureFolder(GenDir + "/Materials");
            EnsureFolder(GenDir + "/Textures");
            var texMap = new Dictionary<Texture, Texture>();
            var matMap = new Dictionary<Material, Material>();
            var byKey = new Dictionary<string, Material>();

            usedTexPaths.Clear();
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m == null || EditorUtility.IsPersistent(m)) continue;
                        Material saved;
                        if (!matMap.TryGetValue(m, out saved))
                        {
                            SaveTextures(m, texMap);
                            string key = MatKey(m);
                            if (!byKey.TryGetValue(key, out saved))
                            {
                                saved = SaveMaterial(m, key);
                                byKey[key] = saved;
                            }
                            matMap[m] = saved;
                        }
                        if (saved != m) { mats[i] = saved; changed = true; }
                    }
                    if (changed) r.sharedMaterials = mats;
                }
            }
            // plasele generate din cod (copaci / tufe / stanci procedurale)
            EnsureFolder(GenDir + "/Meshes");
            var meshMap = new Dictionary<Mesh, Mesh>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh == null || EditorUtility.IsPersistent(mesh)) continue;
                Mesh saved;
                if (!meshMap.TryGetValue(mesh, out saved))
                {
                    string mn = Safe(string.IsNullOrEmpty(mesh.name) ? "Mesh" : mesh.name);
                    string mp = GenDir + "/Meshes/" + mn + ".asset";
                    var ex = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
                    if (ex != null) { EditorUtility.CopySerialized(mesh, ex); ex.name = mn; saved = ex; }
                    else { mesh.name = mn; AssetDatabase.CreateAsset(mesh, mp); saved = mesh; }
                    meshMap[mesh] = saved;
                }
                if (saved != mesh) mf.sharedMesh = saved;
            }

            // materialele ierbii 3D (GrassField nu e Renderer, le salvam separat)
            int gi = 0;
            foreach (var gf in root.GetComponentsInChildren<GrassField>(true))
            {
                if (gf.material == null || EditorUtility.IsPersistent(gf.material)) continue;
                string gp = GenDir + "/Materials/Grass_" + (gi++) + ".mat";
                var ex = AssetDatabase.LoadAssetAtPath<Material>(gp);
                if (ex != null) { EditorUtility.CopySerialized(gf.material, ex); ex.name = "Grass_" + (gi - 1); gf.material = ex; }
                else { gf.material.name = "Grass_" + (gi - 1); AssetDatabase.CreateAsset(gf.material, gp); }
                EditorUtility.SetDirty(gf);
            }
            AssetDatabase.SaveAssets();
        }

        static readonly HashSet<string> usedTexPaths = new HashSet<string>();

        static void SaveTextures(Material m, Dictionary<Texture, Texture> texMap)
        {
            var sh = m.shader;
            for (int p = 0; p < sh.GetPropertyCount(); p++)
            {
                if (sh.GetPropertyType(p) != UnityEngine.Rendering.ShaderPropertyType.Texture) continue;
                string pn = sh.GetPropertyName(p);
                var tex = m.GetTexture(pn);
                if (tex == null || EditorUtility.IsPersistent(tex)) continue;
                Texture saved;
                if (!texMap.TryGetValue(tex, out saved))
                {
                    string name = Safe(string.IsNullOrEmpty(tex.name) ? "Tex" : tex.name) + "_" + tex.width + "x" + tex.height;
                    string path = GenDir + "/Textures/" + name + ".asset";
                    for (int k = 2; usedTexPaths.Contains(path); k++) path = GenDir + "/Textures/" + name + "_" + k + ".asset";
                    usedTexPaths.Add(path);
                    var existing = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    if (existing != null && existing.GetType() == tex.GetType())
                    {
                        EditorUtility.CopySerialized(tex, existing);
                        existing.name = fileName;
                        saved = existing;
                    }
                    else
                    {
                        if (existing != null) AssetDatabase.DeleteAsset(path);
                        tex.name = fileName;
                        AssetDatabase.CreateAsset(tex, path);
                        saved = tex;
                    }
                    texMap[tex] = saved;
                }
                if (saved != tex) m.SetTexture(pn, saved);
            }
        }

        static Material SaveMaterial(Material m, string key)
        {
            string baseName = Safe(string.IsNullOrEmpty(m.name) ? "Mat" : m.name);
            if (baseName.Length > 40) baseName = baseName.Substring(0, 40);
            string fileName = baseName + "_" + Hash(key);
            string path = GenDir + "/Materials/" + fileName + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(m, existing);
                existing.name = fileName;   // numele din fisier = numele obiectului (fara warning)
                existing.enableInstancing = true;
                return existing;
            }
            m.name = fileName;
            m.enableInstancing = true;   // GPU instancing: acelasi material desenat de multe ori mai ieftin
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Cheie stabila din continutul materialului (ca sa nu salvam acelasi material de 100 de ori).</summary>
        static string MatKey(Material m)
        {
            var sb = new StringBuilder();
            var sh = m.shader;
            sb.Append(sh != null ? sh.name : "null").Append('|').Append(m.renderQueue).Append('|');
            foreach (var k in m.shaderKeywords) sb.Append(k).Append(',');
            if (sh != null)
            {
                for (int p = 0; p < sh.GetPropertyCount(); p++)
                {
                    string pn = sh.GetPropertyName(p);
                    sb.Append('|').Append(pn).Append('=');
                    switch (sh.GetPropertyType(p))
                    {
                        case UnityEngine.Rendering.ShaderPropertyType.Color: sb.Append(m.GetColor(pn).ToString("F3")); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Vector: sb.Append(m.GetVector(pn).ToString("F3")); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Float:
                        case UnityEngine.Rendering.ShaderPropertyType.Range: sb.Append(m.GetFloat(pn).ToString("F3")); break;
                        case UnityEngine.Rendering.ShaderPropertyType.Texture:
                            var t = m.GetTexture(pn);
                            sb.Append(t == null ? "none" : AssetDatabase.GetAssetPath(t) + ":" + t.name);
                            sb.Append(m.GetTextureScale(pn).ToString("F3")).Append(m.GetTextureOffset(pn).ToString("F3"));
                            break;
                    }
                }
            }
            return sb.ToString();
        }

        static string Hash(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s) { h ^= c; h *= 16777619; }
                return h.ToString("x8");
            }
        }

        static string Safe(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            return sb.ToString();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
