// =====================================================================
//  GlobalChat.cs - chat GLOBAL real (Vivox, gratuit pana la 5000 jucatori
//  online deodata): toti jucatorii din joc sunt in canalul "hueborn-global".
//  Daca Vivox nu e pornit in Unity Cloud, chatul GLOBAL ramane pe camera ta.
//  Fara dependinte de restul jocului (la fel ca Ugs.cs / MpLink.cs).
// =====================================================================
using System;
using UnityEngine;
#if CRPG_VIVOX
using Unity.Services.Vivox;
#endif

namespace ColorRPG.Net
{
    public static class GlobalChat
    {
        public const string Channel = "hueborn-global";
        public static bool Ready { get; private set; }
        public static bool Busy { get; private set; }
        public static string LastError { get; private set; }
        /// <summary>(playerId, nume, text, metadata) - mesajele altor jucatori.</summary>
        public static event Action<string, string, string, string> OnMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Ready = false; Busy = false; LastError = null; OnMessage = null; }

        public static bool Installed
        {
            get
            {
#if CRPG_VIVOX
                return true;
#else
                return false;
#endif
            }
        }

        static string Clean(string n)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in n ?? "") if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
            string s = sb.ToString();
            if (s.Length == 0) s = "Hero";
            return s.Length > 30 ? s.Substring(0, 30) : s;
        }

        /// <summary>Porneste chatul global (dupa ce contul Unity e conectat).</summary>
        public static async void Init(string displayName)
        {
#if CRPG_VIVOX
            if (Ready || Busy) return;
            Busy = true;
            try
            {
                var v = VivoxService.Instance;
                if (v == null) { LastError = "Vivox not available"; return; }
                await v.InitializeAsync();
                if (!v.IsLoggedIn) await v.LoginAsync(new LoginOptions { DisplayName = Clean(displayName) });
                v.ChannelMessageReceived -= Received;
                v.ChannelMessageReceived += Received;
                await v.JoinGroupChannelAsync(Channel, ChatCapability.TextOnly);
                Ready = true;
                Debug.Log("[ColorRPG] Global chat ready");
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogWarning("[ColorRPG] Global chat (Vivox) unavailable: " + e.Message);
            }
            finally { Busy = false; }
#else
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

#if CRPG_VIVOX
        static void Received(VivoxMessage m)
        {
            if (m == null || m.FromSelf || m.ChannelName != Channel || OnMessage == null) return;
            try { OnMessage(m.SenderPlayerId, m.SenderDisplayName, m.MessageText, m.Metadata); }
            catch (Exception e) { Debug.LogWarning("[ColorRPG] chat message error: " + e.Message); }
        }
#endif

        /// <summary>Cati jucatori sunt online acum (toti intra in canalul global). 0 = necunoscut.</summary>
        public static int OnlineCount
        {
            get
            {
#if CRPG_VIVOX
                if (!Ready) return 0;
                try
                {
                    var ch = VivoxService.Instance.ActiveChannels;
                    if (ch != null && ch.ContainsKey(Channel)) return Math.Max(1, ch[Channel].Count);
                }
                catch (Exception) { }
                return 1;
#else
                return 0;
#endif
            }
        }

        /// <summary>Trimite in chatul global. metadata = puterea (pentru culoarea numelui).</summary>
        public static async void Send(string text, string metadata)
        {
#if CRPG_VIVOX
            if (!Ready) return;
            try { await VivoxService.Instance.SendChannelTextMessageAsync(Channel, text, new MessageOptions { Metadata = metadata ?? "" }); }
            catch (Exception e) { LastError = e.Message; }
#else
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }
    }
}
