// =====================================================================
//  NetSync.cs - leaga jocul de server (Unity Gaming Services, vezi UGS/Ugs.cs):
//   * la pornire: intra in cont si incarca salvarea de pe server
//     (daca aceea are mai mult timp de joc decat cea locala, o foloseste)
//   * salveaza pe server la fiecare minut + cand iesi / pui jocul in fundal
//   * trimite scorurile in clasamentele reale (POWER si TIME PLAYED)
//  Fara internet / fara pachete: jocul merge offline ca pana acum.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using ColorRPG.Net;

namespace ColorRPG
{
    public class NetSync : MonoBehaviour
    {
        public static NetSync I;
        public const string SaveKey = "save";
        public const string BoardPower = "power", BoardTime = "playtime";

        /// <summary>Esti conectat la server (clasamente si salvare reale).</summary>
        public static bool Online { get { return Ugs.Ready; } }
        /// <summary>Clasamentele de pe server (null = inca nu au venit / offline).</summary>
        public static List<Ugs.Entry>[] TopLists = new List<Ugs.Entry>[2];
        public static int[] MyRank = new int[2];

        /// <summary>RESET LEADERBOARDS (din ADMIN): scorurile mai vechi de aceasta ora (UTC, ticks) nu se mai afiseaza.</summary>
        public static long ResetTicks;
        public const string ResetKey = "lbReset";
        public const string ResetMeta = "lbreset:";
        static bool cloudChecked;   // o singura data pe sesiune (si dupa reincarcarea scenei)
        float saveT = 60f, scoreT = 8f, topT = 2f, retryT;
        string lastSaved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; TopLists = new List<Ugs.Entry>[2]; MyRank = new int[2]; cloudChecked = false; ResetTicks = 0; }

        public static void Create(Transform parent)
        {
            if (I != null) return;
            var go = new GameObject("NetSync");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<NetSync>();
        }

        void Start() { Connect(); }

        void Connect()
        {
            var g = Game.I;
            if (g == null || g.player == null || !Ugs.Installed) { Shop.Init(); return; }
            Ugs.Init(g.player.D.playerName, ok =>
            {
                Shop.Init();   // Google Play Billing - dupa serverul UGS (aceeasi initializare Unity Services)
                if (!ok) { retryT = 60f; return; }
                GlobalChat.Init(g.player.D.playerName);   // chat GLOBAL real (Vivox)
                LoadResetTime();
                if (!cloudChecked)
                {
                    cloudChecked = true;
                    Ugs.LoadJson(SaveKey, json =>
                    {
                        if (OnlineAccount.CloudIsBetter(json, SaveSystem.RawJson) && SaveSystem.ReplaceWith(json))
                        {
                            Debug.Log("[ColorRPG] Loaded a newer save from the server - restarting the scene.");
                            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                            return;
                        }
                        PushSave(true);
                        PushScores();
                        if (Game.I != null && Game.I.hud != null) Game.I.hud.Toast("ONLINE - your progress is saved on the server", new Color(0.5f, 1f, 0.6f));
                    });
                }
                else { PushSave(true); PushScores(); }
            });
        }

        /// <summary>Trimite salvarea pe server (doar daca s-a schimbat).</summary>
        public void PushSave(bool force = false)
        {
            if (!Online) return;
            var g = Game.I;
            if (g == null || g.player == null) return;
            g.player.SaveNow();
            string json = SaveSystem.RawJson;
            if (string.IsNullOrEmpty(json) || (!force && json == lastSaved)) return;
            lastSaved = json;
            Ugs.SaveJson(SaveKey, json);
        }

        void PushScores()
        {
            var g = Game.I;
            if (!Online || g == null || g.player == null || AdminTools.God) return;   // fara scoruri din modul de test (GOD)
            Ugs.SubmitScore(BoardPower, g.player.Power);
            Ugs.SubmitScore(BoardTime, System.Math.Round(g.player.D.playTime / 3600.0, 2));
        }

        void PullTops()
        {
            if (!Online) return;
            string[] boards = { BoardPower, BoardTime };
            for (int k = 0; k < 2; k++)
            {
                int kk = k;
                Ugs.Top(boards[k], ResetTicks > 0 ? 60 : 10, l =>
                {
                    if (l == null) return;
                    // dupa un RESET: doar scorurile trimise de atunci (jucatorii activi), renumerotate 1..10
                    if (ResetTicks > 0)
                    {
                        var f = new List<Ugs.Entry>();
                        foreach (var e in l)
                            if (e.updated.ToUniversalTime().Ticks >= ResetTicks || e.playerId == Ugs.PlayerId) { var x = e; x.rank = f.Count + 1; f.Add(x); if (f.Count >= 10) break; }
                        l = f;
                        int mine = l.FindIndex(e => e.playerId == Ugs.PlayerId);
                        if (mine >= 0) MyRank[kk] = mine + 1;
                    }
                    TopLists[kk] = l;
                });
                Ugs.Mine(boards[k], e => { if (ResetTicks <= 0 || TopLists[kk] == null || TopLists[kk].FindIndex(x => x.playerId == Ugs.PlayerId) < 0) MyRank[kk] = e.rank; });
            }
        }

        /// <summary>Ora ultimului reset (salvata public de contul de admin).</summary>
        void LoadResetTime()
        {
            Ugs.LoadPublic(AdminAccess.MainId, ResetKey, v =>
            {
                long t;
                if (!string.IsNullOrEmpty(v) && long.TryParse(v, out t) && t > ResetTicks) ResetTicks = t;
                PullTops();
            });
        }

        /// <summary>ADMIN: reseteaza clasamentele pentru toti, in timp real (anunt prin chatul global + ora salvata pe server).</summary>
        public static void AdminResetLeaderboards()
        {
            long t = System.DateTime.UtcNow.Ticks;
            ResetTicks = t;
            TopLists = new List<Ugs.Entry>[2];
            MyRank = new int[2];
            if (!Online) return;
            Ugs.SavePublic(ResetKey, t.ToString());
            if (GlobalChat.Ready) GlobalChat.Send("Leaderboards have been reset!", ResetMeta + t);
            if (I != null) { I.PushScores(); I.topT = 3f; }
        }

        /// <summary>Am primit anuntul de reset (doar de la un cont de admin).</summary>
        public static void ApplyReset(long t)
        {
            if (t <= ResetTicks) return;
            ResetTicks = t;
            TopLists = new List<Ugs.Entry>[2];
            MyRank = new int[2];
            if (I != null) { I.PushScores(); I.topT = 3f; }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!Online)
            {
                if (retryT > 0f && (retryT -= dt) <= 0f) Connect();
                return;
            }
            if ((saveT -= dt) <= 0f) { saveT = 60f; PushSave(); }
            if ((scoreT -= dt) <= 0f) { scoreT = 120f; PushScores(); }
            if ((topT -= dt) <= 0f) { topT = 60f; PullTops(); }
        }

        void OnApplicationPause(bool paused) { if (paused) PushSave(true); }
        void OnApplicationQuit() { PushSave(true); }
    }
}
