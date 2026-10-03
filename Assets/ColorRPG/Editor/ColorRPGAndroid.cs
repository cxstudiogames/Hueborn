// =====================================================================
//  ColorRPGAndroid.cs - meniul ColorRPG > Prepare Android (Google Play):
//  seteaza o data tot ce trebuie pentru build-ul de telefon (fara sa
//  schimbe nimic la versiunea de PC):
//   * ecran landscape, nume, id pachet, IL2CPP + ARM64 (cerut de Google Play)
//   * API minim 24 (Android 7), target = cel mai nou instalat
//   * App Bundle (.aab) pentru Play Store
//  Pentru salvarea online: vezi Scripts/Online/OnlineAccount.cs (COLORRPG_GPGS).
// =====================================================================
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGAndroid
    {
        // la fiecare pornire / recompilare: URP pe toate platformele (altfel pe Android totul e roz)
        static ColorRPGAndroid() { EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) FixRenderPipeline(); }; }

        // cheia de semnare (upload key) - in <proiect>/Keystore, NU in Assets (nu intra in joc)
        public const string Studio = "CX Studio Dev";                  // numele studioului (peste tot)
        public const string AppLabel = "Hueborn";                      // sub iconita (scurt; numele complet e in Play Store)
        public const string PackageId = "com.cxstudio.hueborn";       // NU se mai schimba dupa primul upload pe Google Play
        static string KeystoreDir { get { return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Keystore")); } }

        /// <summary>Seteaza semnarea aplicatiei din Keystore/keystore_info.txt (parola nu se salveaza in proiect, Unity o cere la fiecare pornire).</summary>
        public static bool ApplyKeystore()
        {
            string info = System.IO.Path.Combine(KeystoreDir, "keystore_info.txt");
            if (!System.IO.File.Exists(info)) { Debug.LogWarning("[ColorRPG] No Keystore/keystore_info.txt - the .aab will not be signed for Google Play."); return false; }
            string ks = null, alias = null, pass = null;
            foreach (var line in System.IO.File.ReadAllLines(info))
            {
                int i = line.IndexOf('=');
                if (i <= 0) continue;
                string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
                if (k == "keystore") ks = v; else if (k == "alias") alias = v; else if (k == "password") pass = v;
            }
            if (ks == null || alias == null || pass == null) return false;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", ks));
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = pass;
            return true;
        }

        /// <summary>ColorRPG > Build Android (.aab): fisierul pentru Google Play in Builds/Android. Creste singur versionCode.</summary>
        [MenuItem("ColorRPG/Build Android (.aab for Google Play)")]
        public static void BuildAab()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Hueborn - Android", "Android Build Support is not installed.\n\nUnity Hub > Installs > 6000.6.3f1 > Add modules > Android Build Support (with OpenJDK and Android SDK & NDK Tools).", "OK");
                return;
            }
            Prepare();
            PlayerSettings.Android.bundleVersionCode++;
            // numele de sub iconita de pe telefon; in editor il punem inapoi dupa build
            // (salvarea din editor e legata de productName - nu vrem sa o pierdem)
            string oldName = PlayerSettings.productName, oldCompany = PlayerSettings.companyName;
            PlayerSettings.productName = AppLabel;
            PlayerSettings.companyName = Studio;
            EditorUserBuildSettings.buildAppBundle = true;
            var scenes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(EditorBuildSettings.scenes, s => s.enabled), s => s.path));
            if (scenes.Length == 0) scenes = new[] { UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
            string dir = System.IO.Path.Combine(ColorRPGBuild.BuildsRoot, "Android");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, "Hueborn_" + PlayerSettings.bundleVersion + "_" + PlayerSettings.Android.bundleVersionCode + ".aab");
            UnityEditor.Build.Reporting.BuildReport report;
            try { report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android }); }
            finally { PlayerSettings.productName = oldName; PlayerSettings.companyName = oldCompany; }
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) { Debug.Log("[ColorRPG] AAB ready: " + path); EditorUtility.RevealInFinder(path); }
            else Debug.LogError("[ColorRPG] Android build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors) - see Console");
        }

        /// <summary>ColorRPG > Build Android TEST (.apk): fisier pe care il copiezi prin cablu pe telefon si il instalezi direct.</summary>
        [MenuItem("ColorRPG/Build Android TEST (.apk for your phone)")]
        public static void BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Hueborn - Android", "Android Build Support is not installed.\n\nUnity Hub > Installs > Add modules > Android Build Support (with OpenJDK and Android SDK & NDK Tools).", "OK");
                return;
            }
            Prepare();
            bool oldAab = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = false;
            string oldName = PlayerSettings.productName, oldCompany = PlayerSettings.companyName;
            PlayerSettings.productName = AppLabel;
            PlayerSettings.companyName = Studio;
            var scenes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(EditorBuildSettings.scenes, s => s.enabled), s => s.path));
            if (scenes.Length == 0) scenes = new[] { UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
            string dir = System.IO.Path.Combine(ColorRPGBuild.BuildsRoot, "Android");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, "Hueborn_TEST.apk");
            UnityEditor.Build.Reporting.BuildReport report;
            try { report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android }); }
            finally { PlayerSettings.productName = oldName; PlayerSettings.companyName = oldCompany; EditorUserBuildSettings.buildAppBundle = oldAab; }
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) { Debug.Log("[ColorRPG] APK ready: " + path); EditorUtility.RevealInFinder(path); }
            else Debug.LogError("[ColorRPG] Android APK build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors) - see Console");
        }

        /// <summary>Android foloseste acelasi URP ca PC: nivelul de calitate nu mai e exclus pe Android,
        /// iar URP devine pipeline-ul implicit (altfel build-ul da "assets that belong to different render pipelines").</summary>
        public static void FixRenderPipeline()
        {
            var urp = QualitySettings.GetRenderPipelineAssetAt(0);
            if (urp != null && UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline == null)
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = urp;
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qs == null || qs.Length == 0) return;
            var so = new SerializedObject(qs[0]);
            var levels = so.FindProperty("m_QualitySettings");
            bool changed = false;
            for (int i = 0; levels != null && i < levels.arraySize; i++)
            {
                var ex = levels.GetArrayElementAtIndex(i).FindPropertyRelative("excludedTargetPlatforms");
                if (ex != null && ex.arraySize > 0) { ex.ClearArray(); changed = true; }
            }
            if (changed) { so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); Debug.Log("[ColorRPG] Quality level now also used on Android / iOS."); }
        }

        [MenuItem("ColorRPG/Prepare Android (Google Play)")]
        static void PrepareMenu() { Prepare(true); }

        public static void Prepare(bool dialog = false)
        {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1") PlayerSettings.bundleVersion = "1.0.0";
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
            ApplyKeystore();
            ColorRPGIcon.Apply(false);
            FixRenderPipeline();
            Debug.Log("[ColorRPG] Android settings ready: landscape, IL2CPP ARM64, API 26+, App Bundle. " +
                      "Switch platform with File > Build Profiles > Android. For online saves import Google Play Games plugin and add COLORRPG_GPGS.");
            if (dialog) EditorUtility.DisplayDialog("Hueborn - Android", "Android settings are ready (landscape, IL2CPP ARM64, API 26+, .aab).\n\n" +
                "Next: File > Build Profiles > Android > Switch Platform.\nFor online saves: import the Google Play Games plugin and add the define COLORRPG_GPGS.", "OK");
        }
    }
}
