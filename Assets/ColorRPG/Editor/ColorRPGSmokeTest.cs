// =====================================================================
//  ColorRPGSmokeTest.cs - test automat: daca exista Promo/test.flag, Unity
//  intra singur in Play, asteapta ~30s, scrie Promo/test_result.txt
//  (erori din consola + starea serverului online) si iese din Play.
//  Meniu: ColorRPG > Run Smoke Test.
// =====================================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGSmokeTest
    {
        static string Flag { get { return Path.Combine(PromoDirector.Folder, "test.flag"); } }
        static string Result { get { return Path.Combine(PromoDirector.Folder, "test_result.txt"); } }
        static double t0;
        static readonly List<string> log = new List<string>();

        static ColorRPGSmokeTest()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && File.Exists(Flag))
                {
                    log.Clear(); t0 = EditorApplication.timeSinceStartup;
                    Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
                    EditorApplication.update -= Tick; EditorApplication.update += Tick;
                }
            };
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Flag) && !EditorApplication.isCompiling)
                    EditorApplication.isPlaying = true;
            };
        }

        [MenuItem("ColorRPG/Run Smoke Test", false, 70)]
        static void Run()
        {
            Directory.CreateDirectory(PromoDirector.Folder);
            File.WriteAllText(Flag, "go");
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || msg.Contains("[ColorRPG]"))
                log.Add("[" + type + "] " + msg + (type == LogType.Exception ? "\n" + stack : ""));
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.timeSinceStartup - t0 < 35.0) return;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("time: " + System.DateTime.Now);
            sb.AppendLine("UGS installed: " + ColorRPG.Net.Ugs.Installed + "  ready: " + ColorRPG.Net.Ugs.Ready + "  name: " + ColorRPG.Net.Ugs.PlayerName + "  id: " + ColorRPG.Net.Ugs.PlayerId);
            sb.AppendLine("UGS last error: " + ColorRPG.Net.Ugs.LastError);
            for (int k = 0; k < 2; k++)
                sb.AppendLine("top[" + k + "]: " + (NetSync.TopLists[k] == null ? "null" : NetSync.TopLists[k].Count + " entries") + "  myRank: " + NetSync.MyRank[k]);
            if (Game.I != null && Game.I.player != null) sb.AppendLine("player: Lv " + Game.I.player.D.level + "  power " + Game.I.player.Power + "  map " + Game.I.mapIndex);
            sb.AppendLine("MP installed: " + ColorRPG.Net.MpLink.Installed + "  online: " + Mp.Online + "  active: " + Mp.Active + "  loc: " + ColorRPG.Net.MpLink.Loc + "  host: " + ColorRPG.Net.MpLink.IsHost + "  players: " + ColorRPG.Net.MpLink.PlayerCount + "  err: " + ColorRPG.Net.MpLink.LastError);
            sb.AppendLine("Global chat ready: " + ColorRPG.Net.GlobalChat.Ready + "  err: " + ColorRPG.Net.GlobalChat.LastError);
            sb.AppendLine("---- log (" + log.Count + ") ----");
            foreach (var l in log) sb.AppendLine(l);
            File.WriteAllText(Result, sb.ToString());
            try { File.Delete(Flag); } catch (System.Exception) { }
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}
