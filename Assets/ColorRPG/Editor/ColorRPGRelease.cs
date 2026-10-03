// =====================================================================
//  ColorRPGRelease.cs - pasii pentru versiunea finala (v1) a jocului:
//   1) Final Scene Patch  (Promo/release1.flag)
//        * ITEM INDEX: cartea cu fata spre locul de spawn
//        * coliziunile aliniate pe modele (Fix Colliders To Visuals)
//        * versiunea aplicatiei 1.0.0, nume / studio
//        * salveaza scena
//   2) Save Final Backup  (Promo/release2.flag)
//        * copie a scenei: _Backups/Scenes/SampleScene_FINAL_v1_<data>.unity
//        * celelalte backup-uri de scena merg in Cosul de reciclare (Recycle Bin),
//          deci se pot recupera daca e nevoie
//  Raport: Promo/Logs/release.txt
// =====================================================================
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGRelease
    {
        public const string Version = "1.0.0";
        static string Flag1 { get { return Path.Combine(PromoDirector.Folder, "release1.flag"); } }
        static string Flag2 { get { return Path.Combine(PromoDirector.Folder, "release2.flag"); } }
        static string LogFile { get { return Path.Combine(PromoDirector.Folder, "Logs", "release.txt"); } }
        static string BackupDir { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "_Backups", "Scenes")); } }

        static ColorRPGRelease()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (File.Exists(Flag1)) { try { File.Delete(Flag1); } catch (System.Exception) { } FinalPatch(); }
                if (File.Exists(Flag2)) { try { File.Delete(Flag2); } catch (System.Exception) { } FinalBackup(); }
            };
        }

        static void Log(string s)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
            File.AppendAllText(LogFile, "[" + System.DateTime.Now.ToString("HH:mm:ss") + "] " + s + "\n");
            Debug.Log("[ColorRPG] Release: " + s);
        }

        // ---------------------------------------------------------------
        [MenuItem("ColorRPG/Release/1. Final Scene Patch", false, 200)]
        public static void FinalPatch()
        {
            var scene = EditorSceneManager.GetActiveScene();
            // ITEM INDEX: fata cartii (partea de jos a pupitrului, -Z local) spre spawn
            Transform idx = null;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t != null && t.name == "ItemIndex" && !EditorUtility.IsPersistent(t) && t.gameObject.scene == scene) { idx = t; break; }
            if (idx != null)
            {
                if (GameData.Maps == null) GameData.Init();
                Vector3 spawn = GameData.Maps[0].Spawn;
                Vector3 away = idx.position - spawn; away.y = 0f;
                if (away.sqrMagnitude > 0.01f)
                {
                    Undo.RecordObject(idx, "Index facing");
                    idx.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
                    Log("ITEM INDEX turned to face the spawn (yaw " + idx.eulerAngles.y.ToString("0") + ")");
                }
            }
            else Log("ITEM INDEX not found in the scene!");

            ColorRPGFixColliders.Run();

            PlayerSettings.bundleVersion = Version;
            Log("app version " + PlayerSettings.bundleVersion + " (versionCode " + PlayerSettings.Android.bundleVersionCode + ", the .aab build adds +1)");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Log("scene saved: " + scene.path);
        }

        // ---------------------------------------------------------------
        [MenuItem("ColorRPG/Release/2. Save Final Backup (old backups to Recycle Bin)", false, 201)]
        public static void FinalBackup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory(BackupDir);
            string name = "SampleScene_FINAL_v" + Version + "_" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".unity";
            string dst = Path.Combine(BackupDir, name);
            File.Copy(Path.GetFullPath(scene.path), dst, true);
            Log("final backup: _Backups/Scenes/" + name);

            int n = 0;
            foreach (var f in Directory.GetFiles(BackupDir))
            {
                if (Path.GetFileName(f) == name) continue;
                if (ToRecycleBin(f)) n++; else Log("could not move to Recycle Bin: " + Path.GetFileName(f));
            }
            // backup-urile vechi din Assets (daca mai exista)
            const string oldDir = "Assets/ColorRPG/Backups";
            if (AssetDatabase.IsValidFolder(oldDir))
            {
                var failed = new System.Collections.Generic.List<string>();
                AssetDatabase.MoveAssetsToTrash(new[] { oldDir }, failed);
                n++;
            }
            Log(n + " old scene backups moved to the Recycle Bin");
        }

        // ---- Cosul de reciclare (Windows): se poate recupera oricand
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEOPSTRUCT
        {
            public System.IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public System.IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT op);

        public static bool ToRecycleBin(string path)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor) return false;
            const uint FO_DELETE = 3;
            const ushort FOF_SILENT = 0x4, FOF_NOCONFIRMATION = 0x10, FOF_ALLOWUNDO = 0x40, FOF_NOERRORUI = 0x400;
            var op = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = Path.GetFullPath(path) + "\0\0",
                fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI)
            };
            return SHFileOperation(ref op) == 0 && !File.Exists(path) && !Directory.Exists(path);
        }
    }
}
