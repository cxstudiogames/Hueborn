// =====================================================================
//  ColorRPGDeploy.cs - trimite configurarea serverului (clasamentele din
//  Assets/ColorRPG/Services/*.lb) in Unity Cloud, fara sa deschizi
//  fereastra Deployment. Meniu: ColorRPG > Online > Deploy Server Config.
//  Automat: daca exista <proiect>/Promo/deploy.flag (rezultat in deploy_result.txt).
// =====================================================================
#if CRPG_DEPLOY_API
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Services.DeploymentApi.Editor;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGDeploy
    {
        static string Folder { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Promo")); } }
        static string Flag { get { return Path.Combine(Folder, "deploy.flag"); } }
        static double t0;

        static ColorRPGDeploy()
        {
            if (!File.Exists(Flag)) return;
            t0 = EditorApplication.timeSinceStartup;
            EditorApplication.update += WaitThenDeploy;
        }

        static void WaitThenDeploy()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.timeSinceStartup - t0 < 5.0) return;
            EditorApplication.update -= WaitThenDeploy;
            try { File.Delete(Flag); } catch (System.Exception) { }
            Deploy();
        }

        [MenuItem("ColorRPG/Online/Deploy Server Config", false, 80)]
        public static async void Deploy()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("time: " + System.DateTime.Now);
            try
            {
                var w = Deployments.Instance.DeploymentWindow;
                if (w == null)
                {
                    // fereastra Deployment inca neinitializata - o deschidem si asteptam putin
                    EditorApplication.ExecuteMenuItem("Services/Deployment");
                    await System.Threading.Tasks.Task.Delay(4000);
                    w = Deployments.Instance.DeploymentWindow;
                }
                if (w == null) { sb.AppendLine("ERROR: Deployment window API not available"); Write(sb); return; }
                // fereastra Deployment trebuie sa fie deschisa pentru deploy
                w.OpenWindow();
                await System.Threading.Tasks.Task.Delay(3000);
                w = Deployments.Instance.DeploymentWindow;   // dupa deschidere, fereastra reala inlocuieste implementarea "goala"
                var files = Directory.GetFiles(Path.Combine(Application.dataPath, "ColorRPG", "Services"))
                                     .Where(f => !f.EndsWith(".meta"))
                                     .Select(f => "Assets/ColorRPG/Services/" + Path.GetFileName(f)).ToList();
                sb.AppendLine("files: " + string.Join(", ", files));
                var items = w.GetFromFiles(files);
                sb.AppendLine("items: " + items.Count);
                var res = await w.Deploy(items);
                foreach (var it in items) sb.AppendLine(" - " + it.Name + "  " + (it.Status.Message ?? "") + "  " + it.Status.MessageDetail + "  " + it.Status.MessageSeverity);
                sb.AppendLine("deployed: " + (res != null && res.Deployed != null ? res.Deployed.Count : 0));
            }
            catch (System.Exception e) { sb.AppendLine("EXCEPTION: " + e); }
            Write(sb);
        }

        static void Write(System.Text.StringBuilder sb)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "deploy_result.txt"), sb.ToString());
            Debug.Log("[ColorRPG] Deploy:\n" + sb);
        }
    }
}
#endif
