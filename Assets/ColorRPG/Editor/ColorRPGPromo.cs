// =====================================================================
//  ColorRPGPromo.cs - meniul ColorRPG > Record Promo Video (in Play mode):
//  porneste PromoDirector si encodeaza cadrele direct in MP4 (H.264,
//  1920x1080, 30 fps) cu MediaEncoder-ul din Unity. Rezultatul: <proiect>/Promo
// =====================================================================
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGPromo
    {
        static MediaEncoder enc;

        // inregistrare automata: daca exista fisierul Promo/record.flag, Unity intra singur in Play,
        // asteapta sa se incarce jocul, filmeaza reclama si iese din Play (fisierul se sterge)
        static string Flag { get { return System.IO.Path.Combine(PromoDirector.Folder, "record.flag"); } }
        static string IconFlag { get { return System.IO.Path.Combine(PromoDirector.Folder, "icon.flag"); } }
        static double iconT; static bool iconStarted;
        static double enterT;
        static bool auto;

        static ColorRPGPromo()
        {
            Hook();
            EditorApplication.playModeStateChanged += s =>
            {
                Hook();
                if (s == PlayModeStateChange.EnteredPlayMode && System.IO.File.Exists(Flag))
                {
                    auto = true; enterT = EditorApplication.timeSinceStartup;
                    EditorApplication.update -= WaitAndRecord; EditorApplication.update += WaitAndRecord;
                }
                else if (s == PlayModeStateChange.EnteredPlayMode && System.IO.File.Exists(IconFlag))
                {
                    iconT = EditorApplication.timeSinceStartup; iconStarted = false;
                    EditorApplication.update -= WaitAndIcon; EditorApplication.update += WaitAndIcon;
                }
            };
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && (System.IO.File.Exists(Flag) || System.IO.File.Exists(IconFlag)) && !EditorApplication.isCompiling)
                    EditorApplication.isPlaying = true;
            };
        }

        static bool started;
        static void WaitAndRecord()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= WaitAndRecord; started = false; return; }
            if (!started)
            {
                if (EditorApplication.timeSinceStartup - enterT < 12.0 || Game.I == null || Game.I.player == null || LoadingScreen.Active) return;
                started = true;
                try { System.IO.File.Delete(Flag); } catch (System.Exception) { }
                PromoDirector.Begin();
                return;
            }
            if (!PromoDirector.Recording)
            {
                EditorApplication.update -= WaitAndRecord;
                started = false;
                if (auto) { auto = false; EditorApplication.delayCall += () => EditorApplication.isPlaying = false; }
            }
        }

        // poze pentru iconita: asteapta incarcarea jocului, randeaza, iese din Play
        static void WaitAndIcon()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= WaitAndIcon; return; }
            if (!iconStarted)
            {
                if (EditorApplication.timeSinceStartup - iconT < 12.0 || Game.I == null || Game.I.player == null || LoadingScreen.Active) return;
                iconStarted = true;
                try { System.IO.File.Delete(IconFlag); } catch (System.Exception) { }
                IconShot.Begin();
                return;
            }
            if (!IconShot.Busy && EditorApplication.timeSinceStartup - iconT > 14.0)
            {
                EditorApplication.update -= WaitAndIcon;
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
            }
        }

        [MenuItem("ColorRPG/Capture Icon Renders (Play mode)", false, 62)]
        static void IconMenu()
        {
            if (!EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hueborn", "Press Play first, wait for the game to load, then use this menu again.", "OK"); return; }
            IconShot.Begin();
        }

        static void Hook()
        {
            PromoDirector.OnBegin = path =>
            {
                Close();
                var a = new VideoTrackAttributes
                {
                    frameRate = new MediaRational(PromoDirector.Fps),
                    width = PromoDirector.W,
                    height = PromoDirector.H,
                    includeAlpha = false,
                    bitRateMode = VideoBitrateMode.High
                };
                try { enc = new MediaEncoder(path, a); }
                catch (System.Exception e) { Debug.LogError("[ColorRPG] MediaEncoder: " + e.Message); enc = null; }
            };
            PromoDirector.OnFrame = tex => { if (enc != null) enc.AddFrame(tex); };
            PromoDirector.OnEnd = Close;
        }

        static void Close()
        {
            if (enc == null) return;
            enc.Dispose();
            enc = null;
        }

        [MenuItem("ColorRPG/Record Promo Video (Play mode)", false, 60)]
        static void Record()
        {
            if (!EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hueborn", "Press Play first, wait for the game to load, then use this menu again.", "OK"); return; }
            Hook();
            PromoDirector.Begin();
        }

        [MenuItem("ColorRPG/Open Promo Folder", false, 61)]
        static void OpenFolder()
        {
            System.IO.Directory.CreateDirectory(PromoDirector.Folder);
            EditorUtility.RevealInFinder(PromoDirector.Folder);
        }
    }
}
