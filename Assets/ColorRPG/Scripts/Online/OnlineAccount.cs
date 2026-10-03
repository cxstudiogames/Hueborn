// =====================================================================
//  OnlineAccount.cs - contul online pe TELEFON (Google Play Games):
//   * te conectezi inainte de ecranul de incarcare (SignInScreen)
//   * progresul se salveaza si in cloud (Saved Games), ca sa nu-l pierzi
//     cand schimbi / resetezi telefonul
//  Pe PC nu se foloseste (salvarea ramane locala).
//
//  ACTIVARE (o singura data, cand pregatim build-ul de Google Play):
//   1. importa pluginul "Google Play Games plugin for Unity" (v11+)
//   2. Window > Google Play Games > Setup > Android setup (resursele din Play Console)
//   3. Player Settings > Scripting Define Symbols: adauga  COLORRPG_GPGS
//  Fara pluginul instalat, codul de mai jos merge in "mod test"
//  (in editor conectarea e simulata, pe telefon joci offline).
// =====================================================================
using System;
using UnityEngine;
#if COLORRPG_GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

namespace ColorRPG
{
    public static class OnlineAccount
    {
        public enum State { None, Connecting, SignedIn, Offline }
        public static State Status { get; private set; }
        public static string PlayerName { get; private set; }
        /// <summary>Ai ales deja (conectat sau offline) in sesiunea asta - nu mai apare ecranul.</summary>
        public static bool Decided { get; private set; }
        public const string CloudFile = "colorrpg_save";

        static string pendingJson;
        static float lastCloudSave = -999f;
        const float CloudInterval = 60f;   // cel mult o salvare in cloud pe minut (+ la iesire)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        /// <summary>DELETE MY DATA: uita salvarea in asteptare / simulata (la Google Play Saved Games se sterge cand pluginul e instalat).</summary>
        public static void ForgetCloud() { pendingJson = null; PlayerPrefs.DeleteKey("crpg_cloud_sim"); }

        static void ResetStatics() { pendingJson = null; lastCloudSave = -999f; }   // Decided ramane pe durata sesiunii (si la reload de scena)

        public static bool Available
        {
            get
            {
#if COLORRPG_GPGS && UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return Application.isEditor;   // in editor: simulare
#endif
            }
        }

        public static void PlayOffline()
        {
            Status = State.Offline; Decided = true;
        }

        /// <summary>Conectare Google Play. silent = incearca fara fereastra (la pornire).</summary>
        public static void SignIn(bool silent, Action<bool> done)
        {
            Status = State.Connecting;
#if COLORRPG_GPGS && UNITY_ANDROID && !UNITY_EDITOR
            PlayGamesPlatform.Activate();
            Action<SignInStatus> cb = st =>
            {
                bool ok = st == SignInStatus.Success;
                Status = ok ? State.SignedIn : State.None;
                if (ok) { PlayerName = PlayGamesPlatform.Instance.GetUserDisplayName(); Decided = true; }
                if (done != null) done(ok);
            };
            if (silent) PlayGamesPlatform.Instance.Authenticate(cb);
            else PlayGamesPlatform.Instance.ManuallyAuthenticate(cb);
#else
            // mod test (editor): conectarea reuseste dupa o clipa
            bool ok = Application.isEditor && !silent;
            Status = ok ? State.SignedIn : State.None;
            if (ok) { PlayerName = "Editor Player"; Decided = true; }
            if (done != null) done(ok);
#endif
        }

        // ---------------------------------------------------------------
        //  Cloud save
        // ---------------------------------------------------------------
        /// <summary>Citeste salvarea din cloud (null = nu exista / eroare).</summary>
        public static void LoadCloud(Action<string> done)
        {
#if COLORRPG_GPGS && UNITY_ANDROID && !UNITY_EDITOR
            if (Status != State.SignedIn) { done(null); return; }
            var sg = PlayGamesPlatform.Instance.SavedGame;
            sg.OpenWithAutomaticConflictResolution(CloudFile, DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime,
                (st, meta) =>
                {
                    if (st != SavedGameRequestStatus.Success) { done(null); return; }
                    sg.ReadBinaryData(meta, (rs, data) =>
                    {
                        if (rs != SavedGameRequestStatus.Success || data == null || data.Length == 0) { done(null); return; }
                        done(System.Text.Encoding.UTF8.GetString(data));
                    });
                });
#else
            done(Application.isEditor ? PlayerPrefs.GetString("crpg_cloud_sim", null) : null);
#endif
        }

        public static void QueueCloudSave(string json)
        {
            if (Status != State.SignedIn) return;
            pendingJson = json;
            if (Time.realtimeSinceStartup - lastCloudSave >= CloudInterval) Flush();
        }

        /// <summary>Trimite acum ultima salvare in cloud (la iesire / cand aplicatia trece in fundal).</summary>
        public static void Flush()
        {
            if (Status != State.SignedIn || pendingJson == null) return;
            string json = pendingJson; pendingJson = null;
            lastCloudSave = Time.realtimeSinceStartup;
#if COLORRPG_GPGS && UNITY_ANDROID && !UNITY_EDITOR
            var sg = PlayGamesPlatform.Instance.SavedGame;
            sg.OpenWithAutomaticConflictResolution(CloudFile, DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime,
                (st, meta) =>
                {
                    if (st != SavedGameRequestStatus.Success) return;
                    var upd = new SavedGameMetadataUpdate.Builder()
                        .WithUpdatedDescription("Hueborn - " + DateTime.Now.ToString("g"))
                        .WithUpdatedPlayedTime(meta.TotalTimePlayed + TimeSpan.FromSeconds(CloudInterval))
                        .Build();
                    sg.CommitUpdate(meta, upd, System.Text.Encoding.UTF8.GetBytes(json), (cs, m2) => { });
                });
#else
            if (Application.isEditor) PlayerPrefs.SetString("crpg_cloud_sim", json);
#endif
        }

        /// <summary>Alege salvarea cu progresul cel mai mare (timp de joc) intre cea locala si cea din cloud.</summary>
        public static bool CloudIsBetter(string cloudJson, string localJson)
        {
            if (string.IsNullOrEmpty(cloudJson)) return false;
            if (string.IsNullOrEmpty(localJson)) return true;
            try
            {
                var c = JsonUtility.FromJson<SaveData>(cloudJson);
                var l = JsonUtility.FromJson<SaveData>(localJson);
                if (c == null) return false;
                if (l == null) return true;
                return c.playTime > l.playTime + 30f;
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>Salveaza in cloud cand aplicatia trece in fundal / se inchide.</summary>
    public class OnlineAccountPump : MonoBehaviour
    {
        void OnApplicationPause(bool paused) { if (paused) { if (Game.I != null && Game.I.player != null) Game.I.player.SaveNow(); OnlineAccount.Flush(); } }
        void OnApplicationQuit() { if (Game.I != null && Game.I.player != null) Game.I.player.SaveNow(); OnlineAccount.Flush(); }
    }
}
