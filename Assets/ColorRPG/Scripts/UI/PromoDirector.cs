// =====================================================================
//  PromoDirector.cs - filmeaza singur reclama pentru Google Play:
//  ~33 secunde 1920x1080 (16:9) la 30 fps, cu titluri animate, plus
//  3 capturi (PNG 1920x1080). Pornire: meniul ColorRPG > Record Promo Video
//  (in Play mode) sau ADMIN > PLAYER > TOOLS > RECORD PROMO.
//  Rezultatul: folderul "Promo" din proiect (langa Assets):
//    colorrpg_promo.mp4 (encodat de Editor/ColorRPGPromo.cs) + shot_1..3.png
//  Daca encoderul video nu e disponibil, cadrele se salveaza ca JPG in Promo/frames.
// =====================================================================
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class PromoDirector : MonoBehaviour
    {
        public const int Fps = 30, W = 1920, H = 1080;
        public static PromoDirector I;
        public static bool ControlsCamera { get { return I != null && I.camFn != null; } }
        public static bool Recording { get { return I != null; } }

        // legatura cu encoderul video din Editor (ColorRPGPromo.cs)
        public static System.Action<string> OnBegin;
        public static System.Action<Texture2D> OnFrame;
        public static System.Action OnEnd;

        public static string Folder { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Promo")); } }
        public static string VideoDir { get { return Path.Combine(Folder, "Video"); } }
        public static string ShotsDir { get { return Path.Combine(Folder, "Screenshots"); } }
        public static string RendersDir { get { return Path.Combine(Folder, "Renders"); } }

        Game game;
        Player P { get { return game.player; } }
        System.Func<int, Vector3[]> camFn;   // frame -> {pozitie, tinta}
        int shotFrame, totalFrames;
        RectTransform capRoot;
        Text capTitle, capSub;
        Image capBand, dim;
        CanvasGroup capCg;
        RenderTexture rt;
        Texture2D frameTex, grab;
        int saved;
        bool useJpg;

        public static void Begin()
        {
            if (I != null || Game.I == null || Game.I.player == null) return;
            var go = new GameObject("PromoDirector");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            I = go.AddComponent<PromoDirector>();
            I.game = Game.I;
            I.StartCoroutine(I.Run());
        }

        // ---------------------------------------------------------------
        //  Titluri
        // ---------------------------------------------------------------
        void BuildCaptions()
        {
            var c = gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 800;
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(W, H);
            sc.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)transform;
            dim = UIKit.Stretch(root, "Dim").gameObject.AddComponent<Image>();
            dim.color = new Color(0.05f, 0.03f, 0.15f, 0f); dim.raycastTarget = false;
            capRoot = UIKit.Rect(root, "Caption", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 70), new Vector2(1100, 190));
            capCg = capRoot.gameObject.AddComponent<CanvasGroup>();
            capBand = UIKit.Rect(capRoot, "Band", new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(1100, 190)).gameObject.AddComponent<Image>();
            capBand.sprite = UIKit.Sprite("flat_Grey", 16f); capBand.type = Image.Type.Sliced; capBand.raycastTarget = false;
            capTitle = UIKit.Text(capRoot, "T", "", 74, Color.white, TextAnchor.LowerLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(60, 20), new Vector2(1500, 100));
            capSub = UIKit.Text(capRoot, "S", "", 34, new Color(1f, 0.92f, 0.6f), TextAnchor.UpperLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(64, -32), new Vector2(1500, 50));
            capSub.supportRichText = true;
        }

        string cTitle = "", cSub = "";
        Color cCol = Color.white;
        bool cCenter;

        void Caption(string t, string s, Color col, bool center = false)
        {
            cTitle = t; cSub = s; cCol = col; cCenter = center;
        }

        void AnimateCaption(int f, int len)
        {
            bool has = cTitle.Length > 0;
            capRoot.gameObject.SetActive(has);
            if (!has) return;
            capTitle.text = cTitle; capSub.text = cSub;
            capTitle.color = Color.white;
            capBand.color = new Color(cCol.r * 0.55f, cCol.g * 0.55f, cCol.b * 0.55f, 0.82f);
            float inK = Mathf.Clamp01(f / 9f), outK = Mathf.Clamp01((len - f) / 7f);
            float e = 1f - Mathf.Pow(1f - inK, 3f);
            capCg.alpha = Mathf.Min(inK * 1.5f, outK);
            if (cCenter)
            {
                capRoot.anchorMin = capRoot.anchorMax = new Vector2(0.5f, 0.5f); capRoot.pivot = new Vector2(0.5f, 0.5f);
                capRoot.anchoredPosition = new Vector2(0, -40);
                capRoot.sizeDelta = new Vector2(1300, 260);
                capBand.rectTransform.sizeDelta = new Vector2(1300, 260);
                capBand.color = new Color(0, 0, 0, 0);
                capTitle.alignment = TextAnchor.LowerCenter; capSub.alignment = TextAnchor.UpperCenter;
                capTitle.fontSize = Mathf.RoundToInt(150 * UIKit.FontScale);
                capTitle.rectTransform.anchorMin = capTitle.rectTransform.anchorMax = capTitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                capSub.rectTransform.anchorMin = capSub.rectTransform.anchorMax = capSub.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                capTitle.rectTransform.anchoredPosition = new Vector2(0, 40); capTitle.rectTransform.sizeDelta = new Vector2(1800, 190);
                capSub.rectTransform.anchoredPosition = new Vector2(0, -90); capSub.rectTransform.sizeDelta = new Vector2(1800, 60);
                capSub.fontSize = Mathf.RoundToInt(44 * UIKit.FontScale);
                capRoot.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, e);
            }
            else
            {
                capRoot.anchorMin = capRoot.anchorMax = Vector2.zero; capRoot.pivot = Vector2.zero;
                capRoot.anchoredPosition = new Vector2(Mathf.Lerp(-1150f, 0f, e), 70);
                capRoot.sizeDelta = new Vector2(1100, 190);
                capBand.rectTransform.sizeDelta = new Vector2(1100, 190);
                capTitle.alignment = TextAnchor.LowerLeft; capSub.alignment = TextAnchor.UpperLeft;
                capTitle.fontSize = Mathf.RoundToInt(70 * UIKit.FontScale);
                capSub.fontSize = Mathf.RoundToInt(34 * UIKit.FontScale);
                capTitle.rectTransform.anchorMin = capTitle.rectTransform.anchorMax = capTitle.rectTransform.pivot = new Vector2(0, 0.5f);
                capSub.rectTransform.anchorMin = capSub.rectTransform.anchorMax = capSub.rectTransform.pivot = new Vector2(0, 0.5f);
                capTitle.rectTransform.anchoredPosition = new Vector2(60, 26); capTitle.rectTransform.sizeDelta = new Vector2(1500, 100);
                capSub.rectTransform.anchoredPosition = new Vector2(64, -42); capSub.rectTransform.sizeDelta = new Vector2(1500, 50);
                capRoot.localScale = Vector3.one;
            }
        }

        // ---------------------------------------------------------------
        //  Camera
        // ---------------------------------------------------------------
        static System.Func<int, Vector3[]> Orbit(System.Func<Vector3> center, float radius, float height, float startDeg, float degPerFrame, float lookUp = 1.2f)
        {
            return f =>
            {
                float a = (startDeg + degPerFrame * f) * Mathf.Deg2Rad;
                Vector3 c = center();
                return new[] { c + new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius), c + Vector3.up * lookUp };
            };
        }

        void LateUpdate()
        {
            if (camFn == null || game.cam == null) return;
            var v = camFn(shotFrame);
            var t = game.cam.transform;
            t.position = v[0];
            t.rotation = Quaternion.LookRotation(v[1] - v[0]);
        }

        // ---------------------------------------------------------------
        //  Inregistrare
        // ---------------------------------------------------------------
        IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();
            // citim ecranul (joc + UI) la sfarsitul frame-ului
            int sw = Screen.width, sh = Screen.height;
            if (frameTex == null) frameTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            RenderTexture.active = null;
            if (sw == W && sh == H)
            {
                frameTex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                frameTex.Apply(false);
            }
            else
            {
                // alta rezolutie (Game view) -> scalam la 1920x1080
                if (grab == null || grab.width != sw || grab.height != sh) { if (grab != null) Destroy(grab); grab = new Texture2D(sw, sh, TextureFormat.RGBA32, false); }
                grab.ReadPixels(new Rect(0, 0, sw, sh), 0, 0);
                grab.Apply(false);
                if (rt == null) rt = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(grab, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                frameTex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                frameTex.Apply(false);
                RenderTexture.active = prev;
            }
            if (!useJpg && OnFrame != null) OnFrame(frameTex);
            else File.WriteAllBytes(Path.Combine(Folder, "frames", "f_" + totalFrames.ToString("0000") + ".jpg"), frameTex.EncodeToJPG(88));
            totalFrames++;
        }

        void Still(string name)
        {
            if (frameTex == null) return;
            Directory.CreateDirectory(ShotsDir);
            File.WriteAllBytes(Path.Combine(ShotsDir, name), frameTex.EncodeToPNG());
            saved++;
        }

        /// <summary>Un cadru: logica shot-ului, apoi captura la sfarsitul frame-ului.</summary>
        IEnumerator Shot(int frames, System.Action<int> step, string still = null, int stillAt = -1)
        {
            for (int f = 0; f < frames; f++)
            {
                shotFrame = f;
                if (step != null) step(f);
                AnimateCaption(f, frames);
                yield return Capture();
                if (still != null && f == stillAt) Still(still);
                yield return null;
            }
        }

        void SpawnAround(int map, int kind, int n, float r)
        {
            var m = GameData.Maps[map];
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f + 0.4f;
                var e = Enemy.Spawn(map, m.enemies[Mathf.Clamp(kind, 0, m.enemies.Length - 1)], false, P.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r);
                if (e != null) e.kind = kind;
            }
        }

        void Go(int map, Vector3 pos)
        {
            if (game.InPlaza || game.mapIndex != map) game.Travel(map, true);
            P.Teleport(pos);
        }

        void CloseWindows()
        {
            var h = game.hud;
            if (h.inventory.IsOpen) h.inventory.Close();
            if (h.pets.IsOpen) h.pets.Close();
            if (h.titles.IsOpen) h.titles.Close();
        }

        IEnumerator Run()
        {
            Directory.CreateDirectory(Folder);
            useJpg = OnFrame == null;
            if (useJpg) Directory.CreateDirectory(Path.Combine(Folder, "frames"));
            BuildCaptions();
            bool godWas = AdminTools.God;
            AdminTools.God = true;
            LoadingScreen.Suppress = true;
            Application.runInBackground = true;
            Time.captureFramerate = Fps;
            Directory.CreateDirectory(VideoDir);
            if (!useJpg && OnBegin != null) OnBegin(Path.Combine(VideoDir, "hueborn_promo_raw.mp4"));
            var h = game.hud;
            CloseWindows();
            Color gold = new Color(1f, 0.8f, 0.25f), red = new Color(1f, 0.35f, 0.3f), blue = new Color(0.35f, 0.6f, 1f),
                  pink = new Color(1f, 0.45f, 0.85f), green = new Color(0.35f, 0.9f, 0.5f), purple = new Color(0.65f, 0.4f, 1f);

            // 1. INTRO - hub-ul, camera se roteste
            Go(0, GameData.Maps[0].Spawn + new Vector3(0, 0, 8));
            h.SetCanvasVisible(false);
            yield return null; yield return null;
            Caption("HUEBORN", "LOOT LEGENDS", gold, true);
            camFn = Orbit(() => P.transform.position, 9f, 3.6f, 200f, 0.55f, 1.6f);
            yield return Shot(85, f => dim.color = new Color(0.05f, 0.03f, 0.15f, Mathf.Lerp(0.55f, 0.25f, f / 85f)));
            dim.color = new Color(0, 0, 0, 0);

            // 2. LUPTA - Meadow, zona de slime
            Go(0, GameData.Maps[0].Spawn + new Vector3(3f, 0, 20f));   // pe drumul principal (fara copaci in fata camerei)
            h.SetCanvasVisible(true);
            yield return null;
            SpawnAround(0, 0, 5, 4.5f);
            Caption("SLAY MONSTERS", "4 colorful worlds full of monsters & loot", green);
            camFn = Orbit(() => P.transform.position, 6.5f, 3.8f, 165f, 0.3f, 1.0f);
            yield return Shot(130, f => { if (f % 6 == 0) MobileControls.PressAttack(); }, "shot_1_combat.png", 95);

            // 3. SKILL-URI - Desert
            Go(1, GameData.ZoneCenter(1, 1));
            yield return null;
            SpawnAround(1, 1, 6, 4.5f);
            Caption("12 HEROES, 3 SKILLS EACH", "Fireballs, meteors, blizzards, chain lightning...", purple);
            camFn = Orbit(() => P.transform.position, 9f, 4.2f, 30f, -0.4f);
            yield return Shot(125, f =>
            {
                if (f == 12 || f == 50 || f == 88) { P.ResetSkillCooldowns(); P.UseSkill(f == 12 ? 0 : f == 50 ? 1 : 2); }
                if (f % 7 == 0) MobileControls.PressAttack();
            });

            // 4. BOSS - Frozen Peaks, in arena
            var arena = GameData.Maps[2].Arena;
            Go(2, arena + new Vector3(0, 0.1f, -5f));
            yield return null;
            Caption("DEFEAT EPIC BOSSES", "Fight inside the arena - win shards & rare loot", blue);
            camFn = Orbit(() => P.transform.position, 8.5f, 4.8f, 200f, 0.32f, 1.2f);
            yield return Shot(140, f =>
            {
                if (f % 6 == 0) MobileControls.PressAttack();
                if (f == 40 || f == 95) { P.ResetSkillCooldowns(); P.UseSkill(f == 40 ? 1 : 2); }
            }, "shot_2_boss.png", 105);

            // 5. LOOT - inventarul
            camFn = Orbit(() => P.transform.position, 7f, 3f, 0f, 0.2f);
            h.ToggleInventory(false);
            yield return null;
            Caption("LOOT & FORGE TO +15", "96 unique items, up to MYTHIC", gold);
            yield return Shot(80, null);
            CloseWindows();

            // 6. PETS
            h.pets.Open();
            h.pets.SelectBest();
            yield return null;
            Caption("HATCH & COLLECT PETS", "They fight by your side", pink);
            yield return Shot(80, null);
            CloseWindows();

            // 7. TITLURI
            h.titles.Open();
            yield return null;
            Caption("60+ LEGENDARY TITLES", "The harder to get, the more epic they look", purple);
            yield return Shot(75, null);
            CloseWindows();

            // 8. TRADE PLAZA
            if (!game.TradeLocked) game.GoPlaza();
            yield return null;
            Caption("TRADE WITH PLAYERS", "Open your booth, set your price, earn TOKENS", new Color(0.3f, 0.85f, 0.65f));
            camFn = Orbit(() => P.transform.position, 15f, 6.5f, 160f, 0.35f, 1.5f);
            yield return Shot(115, null, "shot_3_trade.png", 80);

            // 9. LAVA - urca in TOP
            Go(3, GameData.ZoneCenter(3, 0));
            yield return null;
            SpawnAround(3, 0, 5, 4.5f);
            Caption("CLIMB TO THE TOP", "Ranks, rebirths, leaderboards & friends", red);
            camFn = Orbit(() => P.transform.position, 6f, 2.2f, 100f, 0.6f, 1.4f);
            yield return Shot(95, f => { if (f % 6 == 0) MobileControls.PressAttack(); if (f == 30) { P.ResetSkillCooldowns(); P.UseSkill(0); } });

            // 10. OUTRO
            h.SetCanvasVisible(false);
            Caption("HUEBORN: LOOT LEGENDS", "PLAY FREE NOW", gold, true);
            camFn = Orbit(() => P.transform.position, 8f, 3.5f, 160f, 0.5f, 1.6f);
            yield return Shot(85, f => dim.color = new Color(0.05f, 0.03f, 0.15f, Mathf.Lerp(0.15f, 0.6f, f / 85f)));

            // gata
            if (!useJpg && OnEnd != null) OnEnd();
            Time.captureFramerate = 0;
            LoadingScreen.Suppress = false;
            AdminTools.God = godWas;
            camFn = null;
            h.SetCanvasVisible(true);
            game.Travel(0, true);
            Debug.Log("[ColorRPG] Promo done: " + totalFrames + " frames, " + saved + " screenshots -> " + Folder + (useJpg ? " (JPG frames)" : " (colorrpg_promo.mp4)"));
            game.Say("PROMO RECORDED! See the Promo folder next to Assets.", 5f);
            if (rt != null) rt.Release();
            I = null;
            Destroy(gameObject);
        }
    }
}
