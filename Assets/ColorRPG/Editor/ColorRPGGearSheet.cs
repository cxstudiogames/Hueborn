// =====================================================================
//  ColorRPGGearSheet.cs - plansa cu iconitele echipamentului (verificare):
//  ColorRPG > Gear Sheet (sau Promo/gearsheet.flag) -> intra in Play,
//  randeaza tot catalogul (4 categorii x 4 harti x 6 iteme) si salveaza Promo/Renders/gear_sheet.png
// =====================================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGGearSheet
    {
        static string Flag { get { return Path.Combine(PromoDirector.Folder, "gearsheet.flag"); } }
        static double startT = -1;

        static ColorRPGGearSheet()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && File.Exists(Flag))
                {
                    startT = EditorApplication.timeSinceStartup + 6.0;
                    EditorApplication.update -= Tick; EditorApplication.update += Tick;
                }
            };
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Flag) && !EditorApplication.isCompiling)
                    EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("ColorRPG/Gear Sheet (icons)", false, 73)]
        static void Run()
        {
            Directory.CreateDirectory(PromoDirector.Folder);
            File.WriteAllText(Flag, "go");
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
            else { startT = EditorApplication.timeSinceStartup + 0.5; EditorApplication.update -= Tick; EditorApplication.update += Tick; }
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.timeSinceStartup < startT || Game.I == null || Game.I.player == null) return;
            EditorApplication.update -= Tick;
            try { Make(); }
            catch (System.Exception e) { Debug.LogError("[ColorRPG] Gear sheet: " + e); }
            try { File.Delete(Flag); } catch (System.Exception) { }
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }

        static void Make()
        {
            const int S = 128, cols = 6;
            var rows = new List<Item[]>();
            // catalogul: un rand pe (categorie, harta), 6 iteme pe rand
            for (int sl = 0; sl < 4; sl++)
                for (int m = 0; m < 4; m++)
                {
                    var r = new Item[cols];
                    for (int pos = 0; pos < cols; pos++) { int idx = ItemCatalog.IndexOf(sl, m, pos); if (idx >= 0) r[pos] = ItemDB.FromCatalog(idx); }
                    rows.Add(r);
                }
            var sheet = new Texture2D(cols * S, rows.Count * S, TextureFormat.RGBA32, false);
            var bg = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < bg.Length; i++) bg[i] = new Color32(28, 30, 42, 255);
            sheet.SetPixels32(bg);
            for (int ry = 0; ry < rows.Count; ry++)
                for (int cx = 0; cx < cols; cx++)
                {
                    var it = rows[ry][cx];
                    if (it == null) continue;
                    var sp = GearIcons.Get(it);
                    if (sp == null) continue;
                    var px = Read(sp.texture, S);
                    int y0 = (rows.Count - 1 - ry) * S, x0 = cx * S;
                    // fundal in culoarea raritatii (ca in inventar)
                    Color rc = ItemDB.Col(it.rarity) * 0.35f; rc.a = 1f;
                    var cell = new Color[S * S];
                    for (int i = 0; i < cell.Length; i++) cell[i] = Color.Lerp(rc, px[i], px[i].a);
                    sheet.SetPixels(x0 + 2, y0 + 2, S - 4, S - 4, Crop(cell, S));
                }
            sheet.Apply();
            Directory.CreateDirectory(PromoDirector.RendersDir);
            File.WriteAllBytes(Path.Combine(PromoDirector.RendersDir, "gear_sheet.png"), sheet.EncodeToPNG());
            Debug.Log("[ColorRPG] Gear sheet saved");
        }

        /// <summary>Iconita poate fi necitibila (Apply cu makeNoLongerReadable): o copiem printr-un RenderTexture.</summary>
        static Color[] Read(Texture t, int s)
        {
            var rt = RenderTexture.GetTemporary(s, s, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(t, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tmp = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tmp.ReadPixels(new Rect(0, 0, s, s), 0, 0); tmp.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var px = tmp.GetPixels(); Object.DestroyImmediate(tmp);
            return px;
        }

        static Color[] Crop(Color[] c, int s)
        {
            var o = new Color[(s - 4) * (s - 4)];
            for (int y = 0; y < s - 4; y++)
                for (int x = 0; x < s - 4; x++) o[y * (s - 4) + x] = c[(y + 2) * s + x + 2];
            return o;
        }
    }
}
