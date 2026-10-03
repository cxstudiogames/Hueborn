// =====================================================================
//  ColorRPGMapShots.cs - pozele statice ale hartilor pentru fereastra TELEPORT.
//  ColorRPG > Capture Map Pictures (sau fisierul Promo/mapshots.flag):
//  intra in Play, viziteaza fiecare harta + piata + altarul de awakening,
//  face cate o poza (fara HUD) si o salveaza in Resources/UI/Maps/.
// =====================================================================
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGMapShots
    {
        static string Flag { get { return Path.Combine(PromoDirector.Folder, "mapshots.flag"); } }
        static string OutDir { get { return Path.Combine(Application.dataPath, "ColorRPG/Resources/UI/Maps"); } }
        static int step = -1;
        static double nextT;

        static ColorRPGMapShots()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && File.Exists(Flag))
                {
                    step = 0; nextT = EditorApplication.timeSinceStartup + 7.0;
                    EditorApplication.update -= Tick; EditorApplication.update += Tick;
                }
                if (s == PlayModeStateChange.EnteredEditMode && step == 99) { step = -1; AssetDatabase.Refresh(); }
            };
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Flag) && !EditorApplication.isCompiling)
                    EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("ColorRPG/Capture Map Pictures (Teleport)", false, 72)]
        static void Run()
        {
            Directory.CreateDirectory(PromoDirector.Folder);
            File.WriteAllText(Flag, "go");
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
            else { step = 0; nextT = EditorApplication.timeSinceStartup + 1.0; EditorApplication.update -= Tick; EditorApplication.update += Tick; }
        }

        // 0..3 = harti, 4 = Trade Plaza, 5 = altarul de awakening
        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.timeSinceStartup < nextT) return;
            var g = Game.I;
            if (g == null || g.player == null) return;
            int maps = GameData.Maps.Length;
            if (step % 2 == 0)
            {
                // pasul par: mergem acolo
                int k = step / 2;
                if (k < maps) g.Travel(k, true);
                else if (k == maps) g.GoPlaza();
                else if (k == maps + 1) AwakeningUI.GoToShrine();
                step++;
                nextT = EditorApplication.timeSinceStartup + 2.5;
                return;
            }
            int idx = step / 2;
            Vector3 pos, look;
            if (idx < maps)
            {
                Vector3 sp = GameData.Maps[idx].Spawn;
                pos = sp + new Vector3(0, 24f, -12f);
                look = sp + new Vector3(0, 0, 58f);
            }
            else if (idx == maps) { pos = PlazaBuilder.Spawn + new Vector3(0, 15f, -12f); look = PlazaBuilder.Origin; }
            else { pos = AwakeningUI.ShrinePos + new Vector3(11f, 8f, -11f); look = AwakeningUI.ShrinePos + Vector3.up * 2f; }
            Shot(g, pos, look, idx < maps ? "map" + idx : idx == maps ? "plaza" : "shrine");
            // si o poza "ca in joc" (de la inaltimea eroului), pentru verificare: Promo/Renders/view_*.jpg
            if (idx < maps)
            {
                Vector3 sp = GameData.Maps[idx].Spawn;
                Shot(g, sp + new Vector3(3f, 4.2f, 14f), sp + new Vector3(-6f, 1.5f, 50f), "view_map" + idx, PromoDirector.RendersDir);
                Shot(g, sp + new Vector3(-40f, 6f, 30f), sp + new Vector3(-62f, 2f, 70f), "view_border" + idx, PromoDirector.RendersDir);
                Shot(g, sp + new Vector3(7f, 8f, 72f), sp + new Vector3(0f, 1.5f, 94f), "view_arena" + idx, PromoDirector.RendersDir);
                Vector3 egg = sp + (idx == 0 ? new Vector3(28f, 0, 29f) : new Vector3(23f, 0, 24f));
                Shot(g, egg + new Vector3(-9f, 5f, -8f), egg + Vector3.up * 1f, "view_egg" + idx, PromoDirector.RendersDir);
                if (idx == 1) Shot(g, sp + new Vector3(12f, 4.5f, -3f), sp + new Vector3(24f, 3f, 2f), "view_portal", PromoDirector.RendersDir);
                if (idx == 0)
                {
                    Shot(g, sp + new Vector3(30f, 7f, 0f), sp + new Vector3(46f, 3f, 0f), "view_teleport", PromoDirector.RendersDir);
                    Vector3 fg = sp + new Vector3(-28f, 0, 29f);
                    Shot(g, fg + new Vector3(7f, 5f, 8f), fg + Vector3.up * 2f, "view_forge", PromoDirector.RendersDir);
                    Vector3 rb = sp + new Vector3(-30f, 0, 76f);
                    Shot(g, rb + new Vector3(10f, 7f, -10f), rb + Vector3.up * 2f, "view_rebirth", PromoDirector.RendersDir);
                    Shot(g, sp + new Vector3(-3f, 6f, 0f), sp + new Vector3(4f, 0f, -12f), "view_fountain", PromoDirector.RendersDir);
                    // acelasi sol privit in 4 directii (culoarea trebuie sa ramana la fel)
                    for (int d = 0; d < 4; d++)
                    {
                        Vector3 dir = Quaternion.Euler(0, d * 90f, 0) * Vector3.forward;
                        Vector3 c0 = sp + new Vector3(-30f, 0, 40f);
                        Shot(g, c0 - dir * 8f + Vector3.up * 5f, c0 + dir * 6f, "view_dir" + d, PromoDirector.RendersDir);
                    }
                }
            }
            else if (idx == maps)
            {
                Shot(g, PlazaBuilder.Spawn + new Vector3(0, 4f, -2f), PlazaBuilder.Origin + Vector3.up * 2f, "view_plaza", PromoDirector.RendersDir);
                float ba = Mathf.Lerp(-58f, 238f, 2f / (TradeDB.BoothCount - 1)) * Mathf.Deg2Rad;
                Vector3 bp = PlazaBuilder.Origin + new Vector3(Mathf.Cos(ba), 0, Mathf.Sin(ba)) * 24f;
                Vector3 toC = (PlazaBuilder.Origin - bp).normalized;
                Shot(g, bp + toC * 8f + Vector3.Cross(Vector3.up, toC) * 3f + Vector3.up * 3.5f, bp + Vector3.up * 1.8f, "view_booth", PromoDirector.RendersDir);
            }
            step++;
            nextT = EditorApplication.timeSinceStartup + 0.5;
            if (step >= (maps + 2) * 2)
            {
                EditorApplication.update -= Tick;
                try { File.Delete(Flag); } catch (System.Exception) { }
                step = 99;
                g.Travel(0, true);
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
                Debug.Log("[ColorRPG] Map pictures saved in Resources/UI/Maps");
            }
        }

        static void Shot(Game g, Vector3 pos, Vector3 look, string name, string dir = null)
        {
            var go = new GameObject("MapShotCam");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(g.cam);
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(look - pos);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 260f;
            int W = dir == null ? 640 : 1280, H = dir == null ? 360 : 720;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            string od = dir ?? OutDir;
            Directory.CreateDirectory(od);
            File.WriteAllBytes(Path.Combine(od, name + ".jpg"), tex.EncodeToJPG(88));
            Object.DestroyImmediate(tex);
            rt.Release(); Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
