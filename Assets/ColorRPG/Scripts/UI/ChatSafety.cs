// =====================================================================
//  ChatSafety.cs - BLOCK si REPORT pentru jucatori (cerinta Google Play
//  pentru jocurile cu chat). Lista de blocati se tine pe dispozitiv;
//  un raport se trimite pe email catre studio, cu ultimele mesaje ale jucatorului.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class ChatSafety
    {
        public const string SupportEmail = "cxstudiodev@gmail.com";
        const string Key = "crpg_blocked";
        static HashSet<string> blocked;

        static HashSet<string> Set
        {
            get
            {
                if (blocked == null)
                {
                    blocked = new HashSet<string>();
                    foreach (var s in PlayerPrefs.GetString(Key, "").Split('\n')) if (s.Length > 0) blocked.Add(s);
                }
                return blocked;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { blocked = null; }

        public static bool IsBlocked(string name) { return !string.IsNullOrEmpty(name) && Set.Contains(name); }
        public static IEnumerable<string> Blocked { get { return Set; } }

        public static void Block(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            Set.Add(name); Save();
        }

        public static void Unblock(string name) { if (Set.Remove(name)) Save(); }

        static void Save() { PlayerPrefs.SetString(Key, string.Join("\n", Set)); PlayerPrefs.Save(); }

        public static readonly string[] Reasons = { "Harassment / bullying", "Scam / cheating in trade", "Spam", "Offensive name or language" };

        /// <summary>Trimite raportul: deschide emailul cu totul completat (jucatorul doar apasa Send). Blocheaza si jucatorul.</summary>
        public static void Report(string name, int reason, IList<string> lastLines)
        {
            Block(name);
            var me = Game.I != null && Game.I.player != null ? Game.I.player.D.playerName : "?";
            var sb = new System.Text.StringBuilder();
            sb.Append("Reported player: ").Append(name).Append('\n');
            sb.Append("Reason: ").Append(Reasons[Mathf.Clamp(reason, 0, Reasons.Length - 1)]).Append('\n');
            sb.Append("Reported by: ").Append(me);
            if (!string.IsNullOrEmpty(Net.Ugs.PlayerId)) sb.Append(" (ID ").Append(Net.Ugs.PlayerId).Append(')');
            sb.Append('\n').Append("Time (UTC): ").Append(System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).Append("\n\nLast messages:\n");
            if (lastLines != null) foreach (var l in lastLines) sb.Append("- ").Append(l).Append('\n');
            sb.Append("\nGame version: ").Append(Application.version);
            string url = "mailto:" + SupportEmail + "?subject=" + System.Uri.EscapeDataString("Hueborn report: " + name) + "&body=" + System.Uri.EscapeDataString(sb.ToString());
            Application.OpenURL(url);
            Debug.Log("[ColorRPG] Report: " + name + " (" + Reasons[Mathf.Clamp(reason, 0, Reasons.Length - 1)] + ")");
        }
    }
}
