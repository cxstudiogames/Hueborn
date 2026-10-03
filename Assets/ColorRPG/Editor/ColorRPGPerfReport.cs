// =====================================================================
//  ColorRPGPerfReport.cs - raport de performanta (pentru optimizare):
//  ColorRPG > Perf Report (sau Promo/perfreport.flag) -> intra in Play,
//  trece prin fiecare harta + Trade Plaza, masoara 90 de cadre pe fiecare
//  (draw calls, SetPass, batches, triunghiuri, GC, timp CPU) si numara
//  luminile, rendererele, materialele, shaderele, particulele, colliderele.
//  Rezultatul: Promo/Logs/perf_report.txt
// =====================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGPerfReport
    {
        static string Flag { get { return Path.Combine(PromoDirector.Folder, "perfreport.flag"); } }
        static int step = -1, frames;
        static double nextT;
        static StringBuilder sb;
        static ProfilerRecorder rDraw, rSetPass, rBatch, rTris, rVerts, rGc, rGcCount, rMain, rRender;
        static readonly List<long>[] acc = new List<long>[9];

        static ColorRPGPerfReport()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && File.Exists(Flag))
                {
                    step = 0; nextT = EditorApplication.timeSinceStartup + 8.0;
                    EditorApplication.update -= Tick; EditorApplication.update += Tick;
                }
            };
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Flag) && !EditorApplication.isCompiling)
                    EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("ColorRPG/Perf Report (Play mode)", false, 74)]
        static void Run()
        {
            Directory.CreateDirectory(PromoDirector.Folder);
            File.WriteAllText(Flag, "go");
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        static void StartRec()
        {
            rDraw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            rSetPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            rBatch = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            rTris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            rVerts = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            rGc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            rGcCount = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocation In Frame Count");
            rMain = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            rRender = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Thread", 15);
            for (int i = 0; i < acc.Length; i++) acc[i] = new List<long>();
        }

        static void StopRec()
        {
            rDraw.Dispose(); rSetPass.Dispose(); rBatch.Dispose(); rTris.Dispose(); rVerts.Dispose(); rGc.Dispose(); rGcCount.Dispose(); rMain.Dispose(); rRender.Dispose();
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            var g = Game.I;
            if (g == null || g.player == null || EditorApplication.timeSinceStartup < nextT) return;
            int maps = GameData.Maps.Length;
            if (step == 0)
            {
                sb = new StringBuilder("time: " + System.DateTime.Now + "  (Editor - valorile reale pe telefon sunt mai mari la timp, la fel la draw calls)\n\n");
                Settings();
                StartRec();
                step = 1;
            }
            int idx = (step - 1) / 3;   // 0..maps = harti + piata
            int phase = (step - 1) % 3;  // 0 = teleport, 1 = asteapta, 2 = masoara
            if (idx > maps)
            {
                StopRec();
                Directory.CreateDirectory(Path.Combine(PromoDirector.Folder, "Logs"));
                File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "perf_report.txt"), sb.ToString());
                try { File.Delete(Flag); } catch (System.Exception) { }
                EditorApplication.update -= Tick;
                step = -1;
                g.Travel(0, true);
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
                Debug.Log("[ColorRPG] Perf report saved");
                return;
            }
            if (phase == 0)
            {
                if (idx < maps) g.Travel(idx, true); else g.GoPlaza();
                step++; nextT = EditorApplication.timeSinceStartup + 4.0;
                return;
            }
            if (phase == 1) { for (int i = 0; i < acc.Length; i++) acc[i].Clear(); frames = 0; step++; return; }
            // phase 2: colecteaza cate un cadru la fiecare apel (EditorApplication.update ~ un cadru de joc)
            acc[0].Add(rDraw.LastValue); acc[1].Add(rSetPass.LastValue); acc[2].Add(rBatch.LastValue); acc[3].Add(rTris.LastValue); acc[4].Add(rVerts.LastValue);
            acc[5].Add(rGc.LastValue); acc[6].Add(rGcCount.LastValue); acc[7].Add(rMain.LastValue); acc[8].Add(rRender.LastValue);
            if (++frames < 90) return;
            string name = idx < maps ? GameData.Maps[idx].name : "Trade Plaza";
            sb.AppendLine("=== " + name + " ===");
            sb.AppendLine("  draw calls avg " + Avg(acc[0]) + " max " + acc[0].Max() + " | SetPass avg " + Avg(acc[1]) + " | batches avg " + Avg(acc[2]));
            sb.AppendLine("  triangles avg " + Avg(acc[3]) + " max " + acc[3].Max() + " | vertices avg " + Avg(acc[4]));
            sb.AppendLine("  GC alloc/frame avg " + Avg(acc[5]) + " B, max " + acc[5].Max() + " B, allocations/frame avg " + Avg(acc[6]));
            sb.AppendLine("  main thread avg " + (Avg(acc[7]) / 1e6).ToString("0.00") + " ms (max " + (acc[7].Max() / 1e6).ToString("0.00") + ") | render thread avg " + (Avg(acc[8]) / 1e6).ToString("0.00") + " ms");
            Census();
            sb.AppendLine();
            step++;
        }

        static long Avg(List<long> l) { return l.Count == 0 ? 0 : (long)l.Average(); }

        static void Settings()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            sb.AppendLine("--- SETTINGS ---");
            sb.AppendLine("Quality level: " + QualitySettings.names[QualitySettings.GetQualityLevel()] + " (" + string.Join(", ", QualitySettings.names) + ")");
            sb.AppendLine("targetFrameRate " + Application.targetFrameRate + " vSync " + QualitySettings.vSyncCount + " | screen " + Screen.width + "x" + Screen.height);
            if (urp != null)
            {
                sb.AppendLine("URP asset: " + urp.name + " | renderScale " + urp.renderScale + " | HDR " + urp.supportsHDR + " | MSAA " + urp.msaaSampleCount + " | SRP Batcher " + urp.useSRPBatcher);
                sb.AppendLine("shadows: distance " + urp.shadowDistance + " cascades " + urp.shadowCascadeCount + " mainRes " + urp.mainLightShadowmapResolution + " soft " + urp.supportsSoftShadows + " | addLights " + urp.additionalLightsRenderingMode + " perObject " + urp.maxAdditionalLightsCount + " addShadows " + urp.supportsAdditionalLightShadows);
                sb.AppendLine("depthTex " + urp.supportsCameraDepthTexture + " opaqueTex " + urp.supportsCameraOpaqueTexture);
                try
                {
                    var f = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var list = f != null ? f.GetValue(urp) as ScriptableRendererData[] : null;
                    if (list != null) foreach (var d in list) { var ur = d as UniversalRendererData; if (ur != null) sb.AppendLine("renderer " + ur.name + " path " + ur.renderingMode + " features " + ur.rendererFeatures.Count); }
                }
                catch (System.Exception) { }
            }
            var cam = Game.I.cam;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                sb.AppendLine("camera far " + cam.farClipPlane + " | postFX " + (data != null && data.renderPostProcessing) + " AA " + (data != null ? data.antialiasing.ToString() : "?") + " | cameras in scene " + Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length);
            }
            var vols = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var v in vols) if (v.sharedProfile != null) sb.AppendLine("volume " + v.name + ": " + string.Join(", ", v.sharedProfile.components.Where(c => c.active).Select(c => c.GetType().Name)));
            sb.AppendLine("fog " + RenderSettings.fog + " | physics fixedDt " + Time.fixedDeltaTime);
            sb.AppendLine();
        }

        static void Census()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.isActiveAndEnabled).ToArray();
            sb.AppendLine("  lights active " + lights.Length + " (point " + lights.Count(l => l.type == LightType.Point) + ", shadows " + lights.Count(l => l.shadows != LightShadows.None) + ")");
            var rends = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            var mats = new HashSet<Material>(); var shaders = new Dictionary<string, int>();
            int casters = 0, clip = 0, transp = 0, parts = 0;
            foreach (var r in rends)
            {
                if (r is ParticleSystemRenderer) { parts++; continue; }
                if (r.shadowCastingMode != ShadowCastingMode.Off) casters++;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (mats.Add(m))
                    {
                        string sn = m.shader != null ? m.shader.name : "null";
                        shaders[sn] = shaders.TryGetValue(sn, out var c) ? c + 1 : 1;
                        if (m.renderQueue >= 3000) transp++; else if (m.IsKeywordEnabled("_ALPHATEST_ON")) clip++;
                    }
                }
            }
            sb.AppendLine("  renderers " + rends.Length + " (shadow casters " + casters + ", particle systems " + parts + ") | unique materials " + mats.Count + " (alpha clip " + clip + ", transparent " + transp + ")");
            sb.AppendLine("  shaders: " + string.Join(", ", shaders.OrderByDescending(k => k.Value).Select(k => k.Key + " x" + k.Value)));
            // triunghiuri pe model (instante active x triunghiuri) - cine consuma cel mai mult
            var triBy = new Dictionary<string, long>(); var cnt = new Dictionary<string, int>(); long triAll = 0, triCasters = 0;
            foreach (var r in rends)
            {
                var mf = r.GetComponent<MeshFilter>();
                long tr = 0;
                if (mf != null && mf.sharedMesh != null) { var m = mf.sharedMesh; for (int k = 0; k < m.subMeshCount; k++) tr += m.GetIndexCount(k) / 3; }
                else { var sk = r as SkinnedMeshRenderer; if (sk != null && sk.sharedMesh != null) tr = sk.sharedMesh.triangles.Length / 3; }
                if (tr == 0) continue;
                string key = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : r.name;
                triBy[key] = (triBy.TryGetValue(key, out var v0) ? v0 : 0) + tr;
                cnt[key] = (cnt.TryGetValue(key, out var c0) ? c0 : 0) + 1;
                triAll += tr; if (r.shadowCastingMode != ShadowCastingMode.Off) triCasters += tr;
            }
            sb.AppendLine("  scene triangles (active, before culling) " + triAll + " | of which shadow casters " + triCasters);
            sb.AppendLine("  top meshes: " + string.Join(", ", triBy.OrderByDescending(k => k.Value).Take(14).Select(k => k.Key + " " + (k.Value / 1000) + "k (x" + cnt[k.Key] + ")")));
            int mpb = rends.Count(r => r.HasPropertyBlock());
            sb.AppendLine("  renderers with MaterialPropertyBlock " + mpb);
            sb.AppendLine("  colliders " + Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length + " | rigidbodies " + Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length +
                          " | MonoBehaviours " + Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Length + " | canvases " + Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length);
            var top = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).GroupBy(m => m.GetType().Name).OrderByDescending(gp => gp.Count()).Take(12);
            sb.AppendLine("  most scripts: " + string.Join(", ", top.Select(gp => gp.Key + " x" + gp.Count())));
        }
    }
}
