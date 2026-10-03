// =====================================================================
//  ColorRPGCleanup.cs (doar in editor) - curatenie in proiect
//
//  Meniu: ColorRPG > Clean Up Unused Assets
//   * sterge modelele / iconitele / sunetele CC0 pe care jocul nu le foloseste
//     (tot ce e in Resources intra in build, deci build-ul devine mai mic)
//   * sterge fisierele ramase de la template-ul Unity (TutorialInfo, Readme)
//   * sterge materialele generate care nu mai sunt folosite de nicio scena
//  Fisierele sunt mutate in Recycle Bin (se pot recupera de acolo).
// =====================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    public static class ColorRPGCleanup
    {
        const string R = "Assets/ColorRPG/Resources/";
        const string GenDir = "Assets/ColorRPG/Generated";

        /// <summary>Asset-uri verificate: nu sunt incarcate nicaieri in cod (septembrie 2026).</summary>
        static readonly string[] Unused =
        {
            // modele nefolosite
            R + "Models/Arena/character-soldier.fbx", R + "Models/Arena/trophy.fbx",
            R + "Models/Arena/weapon-sword.fbx", R + "Models/Arena/weapon-spear.fbx",
            R + "Models/Dungeon/character-human.fbx", R + "Models/Dungeon/banner.fbx", R + "Models/Dungeon/column.fbx",
            R + "Models/Graveyard/altar-stone.fbx", R + "Models/Graveyard/candle-multiple.fbx",
            R + "Models/Graveyard/gravestone-cross.fbx", R + "Models/Graveyard/gravestone-round.fbx",
            R + "Models/Graveyard/lantern-candle.fbx", R + "Models/Graveyard/pine-crooked.fbx",
            R + "Models/Graveyard/pumpkin-carved.fbx", R + "Models/Graveyard/pumpkin.fbx", R + "Models/Graveyard/rocks-tall.fbx",
            R + "Models/Holiday/snowflake-a.fbx",
            R + "Models/Nature/crop_pumpkin.fbx", R + "Models/Nature/log_stack.fbx", R + "Models/Nature/path_stone.fbx",
            R + "Models/Nature/pot_large.fbx", R + "Models/Nature/statue_column.fbx", R + "Models/Nature/statue_head.fbx",
            R + "Models/Nature/statue_obelisk.fbx", R + "Models/Nature/statue_ring.fbx", R + "Models/Nature/stone_tallA.fbx",
            R + "Models/Nature/tree_pineSmallA.fbx",
            R + "Models/Town/fence.fbx", R + "Models/Town/hedge.fbx", R + "Models/Town/pillar-stone.fbx",
            R + "Models/Town/rock-large.fbx", R + "Models/Town/rock-wide.fbx", R + "Models/Town/watermill.fbx", R + "Models/Town/stall.fbx",
            // octombrie 2026 (v1.0.0): modele care nu sunt folosite nicaieri in cod
            R + "Models/Megakit/RockPath_Round_Wide.fbx", R + "Models/Megakit/RockPath_Round_Thin.fbx",
            R + "Models/Megakit/RockPath_Square_Wide.fbx", R + "Models/Megakit/RockPath_Square_Thin.fbx",
            R + "Models/Megakit/Plant_1_Big.fbx", R + "Models/Megakit/Plant_7_Big.fbx",
            R + "Models/Megakit/Pebble_Square_4.fbx", R + "Models/Megakit/Pebble_Square_5.fbx", R + "Models/Megakit/Pebble_Square_6.fbx",
            R + "Models/Nature/campfire_stones.fbx", R + "Models/Nature/tent_detailedOpen.fbx",
            R + "Models/Weapons/KK_axe_C.fbx", R + "Models/Weapons/KK_staff_A.fbx",
            R + "Models/Dungeon/shield-round.fbx", R + "Models/Dungeon/shield-rectangle.fbx",
            R + "Models/Town/banner-green.fbx", R + "Models/Town/banner-red.fbx",
            // UI nefolosit (fontul vechi Kenney Future a fost inlocuit cu Pagella)
            R + "UI/KenneyFuture.ttf", R + "UI/KenneyFutureNarrow.ttf",
            R + "UI/bar_bg.png", R + "UI/bar_green.png",
            R + "UI/button_rectangle_depth_line.png", R + "UI/button_rectangle_line.png",
            R + "UI/button_round_depth_line.png", R + "UI/button_round_line.png",
            R + "UI/button_square_depth_line.png", R + "UI/button_square_line.png",
            R + "UI/divider.png", R + "UI/divider_edges.png",
            R + "UI/icon_arrow_down_dark.png", R + "UI/icon_arrow_down_light.png", R + "UI/icon_arrow_down_outline.png",
            R + "UI/icon_arrow_up_dark.png", R + "UI/icon_arrow_up_outline.png",
            R + "UI/icon_play_dark.png", R + "UI/icon_play_light.png", R + "UI/icon_play_outline.png",
            R + "UI/icon_repeat_dark.png", R + "UI/icon_repeat_light.png", R + "UI/icon_repeat_outline.png",
            R + "UI/input_outline_rectangle.png", R + "UI/input_outline_square.png", R + "UI/input_rectangle.png", R + "UI/input_square.png",
            R + "UI/Icons/audioOff.png", R + "UI/Icons/audioOn.png", R + "UI/Icons/exclamation.png", R + "UI/Icons/home.png",
            R + "UI/Icons/medal1.png", R + "UI/Icons/medal2.png", R + "UI/Icons/minus.png", R + "UI/Icons/musicOff.png",
            R + "UI/Icons/musicOn.png", R + "UI/Icons/plus.png", R + "UI/Icons/return.png", R + "UI/Icons/shoppingBasket.png",
            R + "UI/Icons/trashcan.png", R + "UI/Icons/trophy.png", R + "UI/Icons/unlocked.png", R + "UI/Icons/warning.png",
            R + "UI/Icons/wrench.png",
            // scripturi care nu mai sunt folosite (sesiunea 6: booth-urile NPC au fost scoase)
            "Assets/ColorRPG/Scripts/World/BoothDisplay.cs",
            // sesiunea 18: minimap-ul a fost scos din joc
            "Assets/ColorRPG/Scripts/UI/Minimap.cs",
            // ramasite de la template-ul Unity si fisiere goale vechi
            "Assets/TutorialInfo", "Assets/Readme.asset", "Assets/ColorRPG.cs", "Assets/ColorRPG1.cs",
            // backup-uri stricate (facute inainte de reparatia scripturilor din sesiunea 4)
            "Assets/ColorRPG/Backups/SampleScene_world_v2_20260930_143247.unity",
            "Assets/ColorRPG/Backups/SampleScene_world_v3_20260930_143903.unity",
        };

        [MenuItem("ColorRPG/Clean Up Unused Assets", false, 40)]
        static void CleanUpMenu()
        {
            var list = Unused.Where(p => AssetDatabase.LoadMainAssetAtPath(p) != null || AssetDatabase.IsValidFolder(p)).ToList();
            list.AddRange(OldBackups(2));
            var orphans = FindOrphanGenerated();
            long bytes = 0;
            foreach (var p in list.Concat(orphans)) bytes += SizeOf(p);
            if (list.Count + orphans.Count == 0)
            {
                EditorUtility.DisplayDialog("ColorRPG", "Nothing to clean - the project is already tidy.", "OK");
                return;
            }
            string msg = "Move " + (list.Count + orphans.Count) + " unused assets (" + (bytes / 1048576f).ToString("0.0") + " MB) to the Recycle Bin?\n\n" +
                         "- " + list.Count + " unused models / icons / sounds / template files\n" +
                         "- " + orphans.Count + " unused generated materials / textures\n" +
                         "- old scene backups (the 2 newest are kept)\n\n" +
                         "You can restore them from the Recycle Bin.";
            if (!EditorUtility.DisplayDialog("ColorRPG - Clean Up", msg, "Clean up", "Cancel")) return;
            Trash(list.Concat(orphans).ToList());
            // folderul de test IconDebug (in afara Assets) - nu mai e folosit
            try
            {
                string dbg = Path.GetFullPath(Path.Combine(Application.dataPath, "../IconDebug"));
                if (Directory.Exists(dbg)) Directory.Delete(dbg, true);
            }
            catch (System.Exception) { }
            Debug.Log("[ColorRPG] Clean up: " + (list.Count + orphans.Count) + " assets moved to the Recycle Bin (" + (bytes / 1048576f).ToString("0.0") + " MB).");
        }

        /// <summary>Backup-urile de scena mai vechi (pastreaza ultimele "keep").</summary>
        public static List<string> OldBackups(int keep)
        {
            const string dir = "Assets/ColorRPG/Backups";
            var res = new List<string>();
            if (!AssetDatabase.IsValidFolder(dir)) return res;
            var all = AssetDatabase.FindAssets("t:Scene", new[] { dir }).Select(AssetDatabase.GUIDToAssetPath)
                                   .OrderByDescending(p => File.GetLastWriteTimeUtc(Path.GetFullPath(p))).ToList();
            for (int i = keep; i < all.Count; i++) res.Add(all[i]);
            return res;
        }

        /// <summary>Folosit si dupa fiecare reconstruire a lumii (fara dialog).</summary>
        public static int RemoveOrphanGenerated(bool log)
        {
            var orphans = FindOrphanGenerated();
            if (orphans.Count > 0) Trash(orphans);
            if (log) Debug.Log("[ColorRPG] Removed " + orphans.Count + " unused generated assets.");
            return orphans.Count;
        }

        /// <summary>Materialele / texturile din Generated care nu sunt folosite de nicio scena din proiect.</summary>
        static List<string> FindOrphanGenerated()
        {
            var result = new List<string>();
            if (!AssetDatabase.IsValidFolder(GenDir)) return result;
            var used = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string sp = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var d in AssetDatabase.GetDependencies(sp, true)) used.Add(d);
            }
            foreach (var guid in AssetDatabase.FindAssets("", new[] { GenDir }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(p)) continue;
                if (!used.Contains(p)) result.Add(p);
            }
            return result;
        }

        static void Trash(List<string> paths)
        {
            var failed = new List<string>();
            AssetDatabase.MoveAssetsToTrash(paths.ToArray(), failed);
            foreach (var f in failed) Debug.LogWarning("[ColorRPG] Could not remove: " + f);
            AssetDatabase.Refresh();
        }

        static long SizeOf(string assetPath)
        {
            try
            {
                string full = Path.GetFullPath(assetPath);
                if (File.Exists(full)) return new FileInfo(full).Length;
                if (Directory.Exists(full)) return Directory.GetFiles(full, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
            }
            catch (System.Exception) { }
            return 0;
        }
    }
}
