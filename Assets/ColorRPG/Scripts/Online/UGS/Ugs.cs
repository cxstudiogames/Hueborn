// =====================================================================
//  Ugs.cs - legatura cu Unity Gaming Services (serverul jocului):
//   * Authentication - cont pentru fiecare jucator (anonim acum; se poate
//     lega de Google Play mai tarziu, ca sa-l pastrezi pe orice telefon)
//   * Cloud Save     - salvarea jocului pe server
//   * Leaderboards   - clasamente REALE (POWER, TIME PLAYED)
//  Daca pachetele UGS nu sunt instalate (sau nu e internet), totul ramane
//  offline si jocul merge ca inainte. Fara dependinte de restul jocului.
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
#if CRPG_UGS_AUTH
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif
#if CRPG_UGS_SAVE
using Unity.Services.CloudSave;
#endif
#if CRPG_UGS_LB
using Unity.Services.Leaderboards;
#endif

namespace ColorRPG.Net
{
    public static class Ugs
    {
        public struct Entry { public string playerId, name; public double score; public int rank; public DateTime updated; }

        public static bool Ready { get; private set; }
        public static bool Busy { get; private set; }
        public static string PlayerId { get; private set; }
        public static string PlayerName { get; private set; }
        public static string LastError { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Ready = false; Busy = false; PlayerId = null; PlayerName = null; LastError = null; }

        /// <summary>Pachetele UGS sunt instalate in proiect.</summary>
        public static bool Installed
        {
            get
            {
#if CRPG_UGS_AUTH && CRPG_UGS_SAVE && CRPG_UGS_LB
                return true;
#else
                return false;
#endif
            }
        }

        static string CleanName(string n)
        {
            // numele UGS: fara spatii, max 50 caractere
            var sb = new System.Text.StringBuilder();
            foreach (char c in n ?? "") if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
            string s = sb.ToString();
            if (s.Length == 0) s = "Hero";
            return s.Length > 30 ? s.Substring(0, 30) : s;
        }

        /// <summary>Porneste serviciile si intra in cont (anonim; contul ramane pe acest dispozitiv).</summary>
        public static async void Init(string displayName, Action<bool> done)
        {
#if CRPG_UGS_AUTH
            if (Ready) { if (done != null) done(true); return; }
            if (Busy) return;
            Busy = true;
            try
            {
                // un singur InitializeAsync pentru tot jocul (IAP si UGS pornesc aproape deodata)
                while (UnityServices.State == ServicesInitializationState.Initializing) await System.Threading.Tasks.Task.Delay(100);
                if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                PlayerId = AuthenticationService.Instance.PlayerId;
                try { PlayerName = await AuthenticationService.Instance.UpdatePlayerNameAsync(CleanName(displayName)); }
                catch (Exception e) { PlayerName = displayName; Debug.LogWarning("[ColorRPG] UGS name: " + e.Message); }
                Ready = true;
                LastError = null;
                Debug.Log("[ColorRPG] Online: signed in as " + PlayerName + " (" + PlayerId + ")");
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogWarning("[ColorRPG] Online unavailable: " + e.Message);
            }
            Busy = false;
            if (done != null) done(Ready);
#else
            LastError = "UGS packages not installed";
            if (done != null) done(false);
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        public static async void Rename(string displayName)
        {
#if CRPG_UGS_AUTH
            if (!Ready) return;
            try { PlayerName = await AuthenticationService.Instance.UpdatePlayerNameAsync(CleanName(displayName)); }
            catch (Exception e) { Debug.LogWarning("[ColorRPG] UGS rename: " + e.Message); }
#else
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        /// <summary>Sterge contul online si toate datele lui de pe server (Cloud Save + contul UGS).
        /// Cerinta Google Play: jucatorul trebuie sa-si poata sterge contul din joc.</summary>
        public static async void DeleteAccount(Action<bool> done)
        {
#if CRPG_UGS_AUTH
            bool ok = false;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
#if CRPG_UGS_SAVE
                try { await CloudSaveService.Instance.Data.Player.DeleteAllAsync(); }
                catch (Exception e) { Debug.LogWarning("[ColorRPG] delete cloud save: " + e.Message); }
#endif
                await AuthenticationService.Instance.DeleteAccountAsync();
                ok = true;
                Debug.Log("[ColorRPG] Online account deleted.");
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("[ColorRPG] delete account: " + e.Message); }
            if (ok) { Ready = false; PlayerId = null; PlayerName = null; }
            if (done != null) done(ok);
#else
            if (done != null) done(true);
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        // ---------------------------------------------------------------
        //  Cloud Save
        // ---------------------------------------------------------------
        public static async void SaveJson(string key, string json, Action<bool> done = null)
        {
#if CRPG_UGS_SAVE
            if (!Ready) { if (done != null) done(false); return; }
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, json } });
                if (done != null) done(true);
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("[ColorRPG] Cloud save: " + e.Message); if (done != null) done(false); }
#else
            if (done != null) done(false);
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        /// <summary>Citeste o valoare text (null = nu exista / eroare).</summary>
        public static async void LoadJson(string key, Action<string> done)
        {
#if CRPG_UGS_SAVE
            if (!Ready) { done(null); return; }
            try
            {
                var res = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
                done(res != null && res.TryGetValue(key, out var item) ? item.Value.GetAs<string>() : null);
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("[ColorRPG] Cloud load: " + e.Message); done(null); }
#else
            done(null);
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        /// <summary>Valoare PUBLICA (o pot citi si ceilalti jucatori) - ex. ora ultimului reset al clasamentelor.</summary>
        public static async void SavePublic(string key, string value, Action<bool> done = null)
        {
#if CRPG_UGS_SAVE
            if (!Ready) { if (done != null) done(false); return; }
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, value } },
                    new Unity.Services.CloudSave.Models.Data.Player.SaveOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions()));
                if (done != null) done(true);
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("[ColorRPG] Cloud save (public): " + e.Message); if (done != null) done(false); }
#else
            if (done != null) done(false);
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        /// <summary>Citeste o valoare publica a altui jucator (null = nu exista / eroare).</summary>
        public static async void LoadPublic(string playerId, string key, Action<string> done)
        {
#if CRPG_UGS_SAVE
            if (!Ready || string.IsNullOrEmpty(playerId)) { done(null); return; }
            try
            {
                var res = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key },
                    new Unity.Services.CloudSave.Models.Data.Player.LoadOptions(new Unity.Services.CloudSave.Models.Data.Player.PublicReadAccessClassOptions(playerId)));
                done(res != null && res.TryGetValue(key, out var item) ? item.Value.GetAs<string>() : null);
            }
            catch (Exception e) { LastError = e.Message; done(null); }
#else
            done(null);
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        // ---------------------------------------------------------------
        //  Leaderboards (create in Unity Cloud: id-urile "power" si "playtime")
        // ---------------------------------------------------------------
        public static async void SubmitScore(string board, double score)
        {
#if CRPG_UGS_LB
            if (!Ready) return;
            try { await LeaderboardsService.Instance.AddPlayerScoreAsync(board, score); }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("[ColorRPG] Leaderboard " + board + ": " + e.Message); }
#else
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        public static async void Top(string board, int count, Action<List<Entry>> done)
        {
#if CRPG_UGS_LB
            if (!Ready) { done(null); return; }
            try
            {
                var page = await LeaderboardsService.Instance.GetScoresAsync(board, new GetScoresOptions { Limit = count });
                var l = new List<Entry>();
                foreach (var r in page.Results) l.Add(new Entry { playerId = r.PlayerId, name = StripTag(r.PlayerName), score = r.Score, rank = r.Rank + 1, updated = r.UpdatedTime });
                done(l);
            }
            catch (Exception e) { LastError = e.Message; done(null); }
#else
            done(null);
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        /// <summary>Locul tau in clasament (rank 0 = nu ai scor inca).</summary>
        public static async void Mine(string board, Action<Entry> done)
        {
#if CRPG_UGS_LB
            if (!Ready) { done(new Entry()); return; }
            try
            {
                var r = await LeaderboardsService.Instance.GetPlayerScoreAsync(board);
                done(new Entry { playerId = r.PlayerId, name = StripTag(r.PlayerName), score = r.Score, rank = r.Rank + 1, updated = r.UpdatedTime });
            }
            catch (Exception) { done(new Entry()); }
#else
            done(new Entry());
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        /// <summary>"Nume#1234" -> "Nume".</summary>
        public static string StripTag(string n)
        {
            if (string.IsNullOrEmpty(n)) return "?";
            int i = n.LastIndexOf('#');
            return i > 0 ? n.Substring(0, i) : n;
        }
    }
}
