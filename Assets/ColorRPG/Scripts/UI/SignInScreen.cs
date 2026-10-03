// =====================================================================
//  SignInScreen.cs - pe TELEFON, inainte de ecranul de incarcare:
//  "SIGN IN WITH GOOGLE PLAY" (progresul se salveaza online) sau
//  "PLAY OFFLINE" (doar pe telefonul asta). Pe PC nu apare.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class SignInScreen : MonoBehaviour
    {
        System.Action onDone;
        Text status;
        Button signBtn;
        RectTransform logo;
        float t;
        bool busy;

        /// <summary>true cat timp ecranul de conectare e deschis (sunetul asteapta la volum mic).</summary>
        public static bool Open { get; private set; }
        void OnDestroy() { Open = false; }

        public static void Show(System.Action done)
        {
            // fara Google Play Games (pluginul nu e instalat inca) ecranul n-are rost:
            // contul online (Unity) se face automat, progresul se salveaza oricum pe server
            if (!OnlineAccount.Available) { if (done != null) done(); return; }
            var go = new GameObject("SignInScreen");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            var s = go.AddComponent<SignInScreen>();
            s.onDone = done;
            Open = true;
            s.Build();
        }

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)transform;
            Vector2 C = new Vector2(0.5f, 0.5f);
            Time.timeScale = 0f;   // jocul asteapta

            var bg = UIKit.Stretch(root, "BG").gameObject.AddComponent<RawImage>();
            bg.texture = LoadingScreen.Gradient(new Color(0.42f, 0.36f, 0.85f), new Color(0.08f, 0.07f, 0.2f));
            bg.raycastTarget = true;
            var vig = UIKit.Stretch(root, "Vig").gameObject.AddComponent<RawImage>();
            vig.texture = LoadingScreen.Vignette(); vig.raycastTarget = false;

            logo = UIKit.Rect(root, "Logo", C, C, new Vector2(0, 250), new Vector2(170, 170));
            var li = logo.gameObject.AddComponent<Image>(); li.sprite = ItemIcons.Get(26); ItemIcons.Art(li); li.raycastTarget = false;
            var title = UIKit.Text(root, "Title", "HUEBORN", 110, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 110), new Vector2(1600, 130));
            UIKit.Text(root, "Tag", "LOOT LEGENDS", 40, new Color(1f, 0.84f, 0.35f), TextAnchor.MiddleCenter, C, C, new Vector2(0, 38), new Vector2(1600, 54));
            var sh = title.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(4, -6);
            UIKit.Text(root, "Sub", "Sign in to keep your progress safe online", 32, new Color(0.85f, 0.85f, 1f), TextAnchor.MiddleCenter, C, C, new Vector2(0, -16), new Vector2(1600, 44));

            signBtn = UIKit.Button(root, "Sign", "SIGN IN WITH GOOGLE PLAY", "Green", C, C, new Vector2(0, -110), new Vector2(760, 120), () => Begin(false), 36);
            UIKit.Button(root, "Offline", "PLAY OFFLINE", "Grey", C, C, new Vector2(0, -250), new Vector2(460, 90), Offline, 28);
            UIKit.Text(root, "OffNote", "offline = progress saved only on this phone", 22, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter, C, C, new Vector2(0, -320), new Vector2(1200, 34));
            status = UIKit.Text(root, "Status", "", 26, new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1600, 40));

            // incercare automata, fara fereastra (daca te-ai mai conectat inainte)
            if (OnlineAccount.Available && !Application.isEditor) Begin(true);
        }

        void Begin(bool silent)
        {
            if (busy) return;
            busy = true;
            status.text = silent ? "Connecting..." : "Opening Google Play...";
            signBtn.interactable = false;
            OnlineAccount.SignIn(silent, ok =>
            {
                busy = false;
                signBtn.interactable = true;
                if (!ok)
                {
                    status.text = silent ? "" : (OnlineAccount.Available ? "Could not sign in. Try again or play offline." : "Google Play is not available on this device - play offline.");
                    return;
                }
                status.text = "Signed in as " + OnlineAccount.PlayerName + " - loading your save...";
                OnlineAccount.LoadCloud(json =>
                {
                    if (OnlineAccount.CloudIsBetter(json, SaveSystem.RawJson) && SaveSystem.ReplaceWith(json))
                    {
                        // salvarea din cloud e mai noua -> repornim scena cu ea
                        Time.timeScale = 1f;
                        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                        return;
                    }
                    if (Game.I != null && Game.I.player != null) Game.I.player.SaveNow();   // trimitem si in cloud salvarea locala
                    OnlineAccount.Flush();
                    Finish();
                });
            });
        }

        void Offline()
        {
            OnlineAccount.PlayOffline();
            Finish();
        }

        void Finish()
        {
            Time.timeScale = 1f;
            if (onDone != null) onDone();
            Destroy(gameObject);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (logo != null)
            {
                logo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.6f) * 8f);
                logo.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 3f));
            }
        }
    }
}
