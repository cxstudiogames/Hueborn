// =====================================================================
//  ColorRPGBuild.cs - meniul ColorRPG > Build PC Version:
//  face jocul ca program de Windows (ColorRPG.exe) pe Desktop, in "Hueborn Builds/PC".
//  Il poti copia oriunde (sau pe alt PC) si te joci fara Unity.
//  Acelasi server online ca pe telefon (clasamente, salvare).
// =====================================================================
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    public static class ColorRPGBuild
    {
        /// <summary>Buildurile merg direct pe Desktop, in folderul "Hueborn Builds" (PC + Android).</summary>
        public static string BuildsRoot { get { return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory), "Hueborn Builds"); } }

        [MenuItem("ColorRPG/Build PC Version", false, 90)]
        public static void BuildPC()
        {
            string oldName = PlayerSettings.productName, oldCompany = PlayerSettings.companyName;   // salvarea din editor e legata de company/product -> le punem inapoi
            PlayerSettings.companyName = ColorRPGAndroid.Studio;
            PlayerSettings.productName = "Hueborn";
            ColorRPGIcon.Apply(false);
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
            string dir = Path.Combine(BuildsRoot, "PC");
            Directory.CreateDirectory(dir);
            var opt = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(dir, "Hueborn.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(opt); }
            finally { PlayerSettings.productName = oldName; PlayerSettings.companyName = oldCompany; }
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[ColorRPG] PC build ready: " + opt.locationPathName + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
                EditorUtility.RevealInFinder(opt.locationPathName);
            }
            else Debug.LogError("[ColorRPG] PC build failed: " + report.summary.result + " - " + report.summary.totalErrors + " errors (see Console)");
        }
    }
}
