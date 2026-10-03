// =====================================================================
//  ColorRPGGcReport.cs - cine aloca memorie (GC) in fiecare cadru:
//  ColorRPG > GC Report (sau Promo/gcreport.flag) -> intra in Play, porneste
//  Profiler-ul ~3 secunde si aduna alocarile pe fiecare marker (ex.
//  "HUD.Update() [Invoke]"). Rezultat: Promo/Logs/gc_report.txt
// =====================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGGcReport
    {
        static string Flag { get { return Path.Combine(PromoDirector.Folder, "gcreport.flag"); } }
        static int phase = -1;
        static double nextT;
        static int firstFrame;

        static ColorRPGGcReport()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && File.Exists(Flag))
                {
                    phase = 0; nextT = EditorApplication.timeSinceStartup + 10.0;
                    EditorApplication.update -= Tick; EditorApplication.update += Tick;
                }
            };
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Flag) && !EditorApplication.isCompiling)
                    EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("ColorRPG/GC Report (Play mode)", false, 75)]
        static void Run()
        {
            Directory.CreateDirectory(PromoDirector.Folder);
            File.WriteAllText(Flag, "go");
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.timeSinceStartup < nextT) return;
            if (phase == 0 && (Game.I == null || Game.I.player == null || LoadingScreen.Active)) return;   // asteptam sa se termine incarcarea
            if (phase == 0)
            {
                ProfilerDriver.ClearAllFrames();
                ProfilerDriver.profileEditor = false;
                ProfilerDriver.enabled = true;
                UnityEngine.Profiling.Profiler.enabled = true;
                phase = 1; nextT = EditorApplication.timeSinceStartup + 4.0;
                return;
            }
            if (phase == 1)
            {
                ProfilerDriver.enabled = false;
                try { Write(); } catch (System.Exception e) { Debug.LogError("[ColorRPG] GC report: " + e); }
                try { File.Delete(Flag); } catch (System.Exception) { }
                EditorApplication.update -= Tick;
                phase = -1;
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
            }
        }

        static void Write()
        {
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            var selfGc = new Dictionary<string, double>();
            var calls = new Dictionary<string, int>();
            int frames = 0; double total = 0;
            var kids = new List<int>();
            for (int f = Mathf.Max(first, last - 150); f <= last; f++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnGcMemory, false))
                {
                    if (view == null || !view.valid) continue;
                    frames++;
                    var stack = new Stack<KeyValuePair<int, string>>();
                    stack.Push(new KeyValuePair<int, string>(view.GetRootItemID(), ""));
                    while (stack.Count > 0)
                    {
                        var cur = stack.Pop();
                        int id = cur.Key;
                        float gc = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory);
                        if (gc <= 0f && id != view.GetRootItemID()) continue;
                        kids.Clear();
                        view.GetItemChildren(id, kids);
                        float childGc = 0f;
                        string name = id == view.GetRootItemID() ? "" : view.GetItemName(id);
                        string path = cur.Value.Length > 0 ? cur.Value + " > " + name : name;
                        foreach (var k in kids)
                        {
                            childGc += view.GetItemColumnDataAsFloat(k, HierarchyFrameDataView.columnGcMemory);
                            stack.Push(new KeyValuePair<int, string>(k, path));
                        }
                        float self = gc - childGc;
                        if (id == view.GetRootItemID()) { total += gc; continue; }
                        if (self > 0.5f)
                        {
                            // calea scurtata: ultimele 3 niveluri
                            var parts = path.Split(new[] { " > " }, System.StringSplitOptions.None);
                            string key = string.Join(" > ", parts.Skip(Mathf.Max(0, parts.Length - 3)));
                            selfGc[key] = (selfGc.TryGetValue(key, out var v) ? v : 0) + self;
                            calls[key] = (calls.TryGetValue(key, out var c) ? c : 0) + 1;
                        }
                    }
                }
            }
            var sb = new StringBuilder("time: " + System.DateTime.Now + "  frames " + frames + "  avg GC/frame " + (frames > 0 ? (total / frames).ToString("0") : "?") + " B\n\nTop allocators (avg bytes per frame, self):\n");
            foreach (var kv in selfGc.OrderByDescending(k => k.Value).Take(40))
                sb.AppendLine("  " + (kv.Value / Mathf.Max(1, frames)).ToString("0").PadLeft(7) + " B   " + kv.Key);
            Directory.CreateDirectory(Path.Combine(PromoDirector.Folder, "Logs"));
            File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "gc_report.txt"), sb.ToString());
            Debug.Log("[ColorRPG] GC report saved");
        }
    }
}
