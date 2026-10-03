// =====================================================================
//  ColorRPG - mini RPG colorat in stilul Loot Up 24h / Iron Soul / Loot to Forge
//
//  CUM SE FOLOSESTE:
//   1. Deschide ORICE scena (ex. Assets/Scenes/SampleScene) si apasa Play.
//      Jocul se construieste singur din cod (Game.Boot), nu trebuie pus nimic in scena.
//   2. Modelele/sunetele CC0 sunt in Assets/ColorRPG/Resources (vezi CREDITS.md).
//      Daca lipsesc, jocul foloseste primitive colorate.
//
//  CONTROALE:  WASD miscare | SHIFT sprint | SPACE saritura | click / F atac | E interactiune
//              Q potiune | I / Tab inventar | R auto-atac | M sunet | rotita = zoom
// =====================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColorRPG
{
    public class Game : MonoBehaviour
    {
        public static Game I;
        public Player player;
        public HUD hud;
        public Camera cam;
        public int mapIndex;
        public bool InPlaza { get; private set; }
        public TradeSystem trade;
        public Portrait portrait;

        public Interactable CurrentInteractable { get; private set; }

        float saveT, zoom = 1f, zoomTarget = 1f;
        // camera third-person (orbiteaza in jurul jucatorului)
        public float CamYaw { get; private set; }
        float camPitch = 16f, camDistSmoothed = 7f, lastManualLook = -10f;
        Vector3 camPivot;
        bool camSnap = true;
        const float CamBaseDist = 7.5f;
        Light sun;
        Material skyMat;
        Volume volume;
        ColorAdjustments colorAdj;
        Bloom bloomFx;
        Tonemapping tonemap;

        public const float GiftInterval = 300f; // un cadou la fiecare 5 minute de joc

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I == null) new GameObject("=== GAME ===").AddComponent<Game>();
        }

        /// <summary>Siguranta: daca scena reincarcata n-ar avea obiectul jocului, il cream.</summary>
        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
        {
            if (m == UnityEngine.SceneManagement.LoadSceneMode.Single && I == null) new GameObject("=== GAME ===").AddComponent<Game>();
        }

        // ---------------------------------------------------------------
        //  Initializare
        // ---------------------------------------------------------------
        void Awake()
        {
            I = this;
            AdminTools.BlockSave = false;   // dupa o resetare / stergere de date, salvarea porneste din nou
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Art.Reset();
            Enemy.All.Clear();
            Application.targetFrameRate = 60;
            // multiplayer: pe PC (si in Unity) jocul ruleaza si cand fereastra nu e in fata - altfel "ingheta"
            // si nu mai primeste / nu mai afiseaza ceilalti jucatori (ex. cand te joci de pe telefon in paralel)
            if (!Device.Mobile) Application.runInBackground = true;
            Time.timeScale = 1f;
            GameSettings.Load();

            GameData.Init();
            BuildLightAndCamera();
            Sfx.Create();
            Fx.Create();
            portrait = Portrait.Create();
            hud = HUD.Create(this);
            trade = TradeSystem.Create();

            // jucatorul se creeaza inainte de lume (lumea citeste datele lui)
            var pgo = new GameObject("Player");
            pgo.transform.SetParent(SceneGroups.Runtime, false);
            pgo.transform.position = GameData.Maps[0].Spawn;
            player = pgo.AddComponent<Player>();

            // Lumea: daca e deja salvata in scena (Hierarchy -> "=== WORLD ==="), o folosim pe aceea.
            // Altfel o construim din cod (la fel ca meniul ColorRPG > Rebuild World In Scene).
            var world = WorldRoot.Find();
            if (world == null) world = WorldBuilder.BuildAll();
            else if (world.version < GameData.WorldVersion)
                Debug.LogWarning("[ColorRPG] Lumea din scena e mai veche (v" + world.version + " < v" + GameData.WorldVersion + "). Foloseste meniul ColorRPG > Rebuild World In Scene.");
            // monstrii NU mai sunt amestecati: fiecare tip are zona lui pe fiecare harta (vezi GameData.Zone)
            var zl = SceneGroups.Get("Zone Labels");
            for (int i = 0; i < GameData.Maps.Length; i++)
            {
                var m = GameData.Maps[i];
                for (int t = 0; t < m.enemies.Length && t < GameData.ZoneCount.Length; t++)
                {
                    for (int k = 0; k < GameData.ZoneCount[t]; k++) SpawnEnemy(i, false, t, k);
                    Vector3 zc = GameData.ZoneCenter(i, t);
                    WorldLabel.Create(zl, zc + Vector3.up * 3.2f, m.enemies[t].name.ToUpper() + " ZONE", Color.Lerp(m.enemies[t].col, Color.white, 0.35f), 26f, 38f);
                }
                SpawnEnemy(i, true);
            }
            AwakeningShrine.Ensure();   // altarul de awakening din ultima harta
            gameObject.AddComponent<AmbientFx>();
            // (umbrele de nori au fost scoase - nu aveau sens pe harti; fiecare harta are particulele ei in AmbientFx)
            trade.Init();
            GameSettings.Apply();

            mapIndex = Mathf.Clamp(player.D.mapIndex, 0, GameData.Maps.Length - 1);
            if (player.Level < GameData.Maps[mapIndex].req) mapIndex = 0;
            player.Teleport(GameData.Maps[mapIndex].Spawn);
            OptimizeWorldOnce();
            CullMaps();
            ApplyMapAtmosphere(mapIndex, true);
            if (Sfx.I != null) Sfx.I.PlaySong(mapIndex);

            if (player.D.playTime < 1f)
                Say("Welcome! Slay monsters, grab loot, forge your weapon and hatch pets!", 7f);
            hud.Welcome(GameData.Maps[mapIndex].name, mapIndex == 0 ? "MAIN HUB  -  forge, pets, quests, leaderboards" : "Level " + GameData.Maps[mapIndex].req + "+",
                        Color.Lerp(GameData.Maps[mapIndex].ground, Color.white, 0.45f));
            // pe telefon: conectare Google Play (salvare online) inainte de ecranul de incarcare
            gameObject.AddComponent<OnlineAccountPump>();
            NetSync.Create(SceneGroups.Get("Systems"));
            AutoPerf.Create(SceneGroups.Get("Systems"));
            Mp.Create(SceneGroups.Get("Systems"));         // multiplayer real (camere de 4 pe harta, 18 in piata)   // tine FPS-ul ales pe telefon (rezolutie adaptiva)
            // (magazinul Google Play porneste din NetSync, dupa ce serverul e conectat)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;   // ecranul telefonului nu se stinge in timpul jocului   // serverul (Unity Gaming Services): cont, salvare, clasamente
            if (Device.Mobile && !OnlineAccount.Decided) SignInScreen.Show(LoadingScreen.ShowStart);
            else LoadingScreen.ShowStart();
        }

        void BuildLightAndCamera()
        {
            sun = RenderSettings.sun;
            if (sun == null)
            {
#if UNITY_2023_1_OR_NEWER
                var l = FindAnyObjectByType<Light>();
#else
                var l = FindObjectOfType<Light>();
#endif
                if (l != null && l.type == LightType.Directional) sun = l;
            }
            if (sun == null)
            {
                var lg = new GameObject("Sun");
                sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.78f;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.3f;
            sun.transform.rotation = Quaternion.Euler(42, -35, 0);   // soare mai jos = umbre mai lungi, mai realiste

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 380f;   // muntii din jurul hartii se vad ca siluete care se pierd in ceata

            cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera");
                cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
                cg.AddComponent<AudioListener>();
            }
            // cer procedural (shader ColorRPG/Sky); daca lipseste, fundal colorat
            if (RenderSettings.skybox != null && RenderSettings.skybox.shader != null && RenderSettings.skybox.shader.name == "ColorRPG/Sky")
                skyMat = new Material(RenderSettings.skybox);   // copie: nu modificam asset-ul din proiect
            else skyMat = Art.SkyMaterial();
            if (skyMat != null) RenderSettings.skybox = skyMat;
            cam.clearFlags = skyMat != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;   // hartile vecine sunt oricum dezactivate (CullMaps); vedem muntii din jur pana in ceata
            cam.cullingMask &= ~(1 << Portrait.Layer);   // studio-ul portretului nu apare in joc

            SetupPostProcessing();
        }

        /// <summary>Bloom + culori mai vii (doar in URP).</summary>
        void SetupPostProcessing()
        {
            if (!Art.IsURP) return;
            try
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    data.renderPostProcessing = true;
                    data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                }
                var vgo = new GameObject("ColorRPG_Volume");
                vgo.transform.SetParent(SceneGroups.Get("Systems"), false);
                volume = vgo.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 50f;
                var prof = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = prof.Add<Bloom>(true);
                bloomFx = bloom;
                bloom.intensity.Override(0.32f);
                bloom.threshold.Override(1.1f);
                bloom.scatter.Override(0.7f);
                colorAdj = prof.Add<ColorAdjustments>(true);
                // culori mai naturale (inainte: saturatie +10, contrast +12, expunere +0.25 -> prea "aprins" pe telefon)
                colorAdj.saturation.Override(0f);
                colorAdj.contrast.Override(7f);
                colorAdj.postExposure.Override(0.12f);
                var wb = prof.Add<WhiteBalance>(true);
                wb.temperature.Override(6f);
                var smh = prof.Add<ShadowsMidtonesHighlights>(true);
                smh.shadows.Override(new Vector4(0.95f, 0.97f, 1.05f, 0f));   // umbre usor albastrui, ca in natura
                var vig = prof.Add<Vignette>(true);
                vig.intensity.Override(0.22f);
                vig.smoothness.Override(0.5f);
                var tm = prof.Add<Tonemapping>(true);
                tonemap = tm;
                tm.mode.Override(TonemappingMode.ACES);
                volume.sharedProfile = prof;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[ColorRPG] Post-processing indisponibil: " + e.Message);
            }
        }

        /// <summary>Aplica setarile grafice (umbre, post-processing, nori).</summary>
        public void ApplyGraphics()
        {
            if (sun != null) sun.shadows = !GameSettings.Shadows ? LightShadows.None : (Device.Mobile ? LightShadows.Hard : LightShadows.Soft);   // umbrele moi sunt scumpe pe telefon
            if (volume != null) volume.enabled = GameSettings.PostFx;
            if (Art.IsURP && cam != null)
            {
                try
                {
                    var data = cam.GetUniversalAdditionalCameraData();
                    if (data != null)
                    {
                        data.renderPostProcessing = GameSettings.PostFx;
                        data.antialiasing = GameSettings.Quality >= 1 && DeviceTier.Tier >= 1 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
                        data.renderShadows = GameSettings.Shadows;
                    }
                }
                catch (System.Exception) { }
            }
            QualitySettings.shadowDistance = GameSettings.Quality == 0 ? 35f : 55f;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                // nivelul de calitate e cel al fostului MEDIUM (HIGH scos); pe telefon se adapteaza si dupa model
                urp.shadowDistance = GameSettings.Quality == 0 ? 35f : (Device.Mobile ? (DeviceTier.Tier == 0 ? 38f : 48f) : 65f);
                urp.shadowCascadeCount = GameSettings.Quality == 1 && !Device.Mobile ? 2 : 1;   // pe telefon o singura cascada (umbrele = ~60% din triunghiuri)
                urp.msaaSampleCount = 1;
                // optimizare: nimic din joc nu foloseste Opaque/Depth Texture; HDR doar unde e loc
                urp.supportsCameraOpaqueTexture = false;
                urp.supportsCameraDepthTexture = false;
                urp.supportsHDR = !(Device.Mobile && DeviceTier.Tier == 0);
                // fara HDR (telefoane slabe): bloom mai discret si tonemapping Neutral (ACES pe LDR satura si "arde" culorile)
                if (bloomFx != null) { bloomFx.threshold.Override(urp.supportsHDR ? 1.1f : 0.95f); bloomFx.intensity.Override(urp.supportsHDR ? (Device.Mobile ? 0.26f : 0.32f) : 0.2f); }
                if (tonemap != null) tonemap.mode.Override(urp.supportsHDR ? TonemappingMode.ACES : TonemappingMode.Neutral);
                urp.mainLightShadowmapResolution = Device.Mobile ? (DeviceTier.Tier == 0 ? 1024 : 2048) : 2048;
                // rezolutia interna: porneste dupa model + calitate, apoi AutoPerf o ajusteaza din mers
                if (AutoPerf.I == null || !Device.Mobile) AutoPerf.Scale = DeviceTier.BaseScale(GameSettings.Quality);
                urp.renderScale = AutoPerf.Scale;
            }
        }

        /// <summary>Paleta de cer pe zona: zenit, orizont, nori, acoperire nori, culoarea soarelui.</summary>
        public void SetSky(Color top, Color horizon, Color clouds, float cover, Color sunCol)
        {
            RenderSettings.fogColor = horizon;
            cam.backgroundColor = horizon;
            if (skyMat == null) return;
            skyMat.SetColor("_TopColor", top);
            skyMat.SetColor("_HorizonColor", horizon);
            skyMat.SetColor("_BottomColor", Color.Lerp(horizon, Color.black, 0.35f));
            skyMat.SetColor("_CloudColor", clouds);
            skyMat.SetColor("_CloudShade", Color.Lerp(clouds, top, 0.35f) * 0.85f);
            skyMat.SetFloat("_CloudCover", cover);
            skyMat.SetColor("_SunColor", sunCol);
        }

        void ApplyMapAtmosphere(int i, bool instant)
        {
            var m = GameData.Maps[i];
            switch (i)
            {
                case 1: SetSky(new Color(0.28f, 0.52f, 0.92f), new Color(1f, 0.88f, 0.72f), Color.white, 0.22f, new Color(1f, 0.9f, 0.7f)); break;
                case 2: SetSky(new Color(0.42f, 0.58f, 0.88f), new Color(0.84f, 0.9f, 0.98f), new Color(0.95f, 0.97f, 1f), 0.6f, new Color(0.95f, 0.97f, 1f)); break;
                case 3: SetSky(new Color(0.2f, 0.09f, 0.09f), new Color(0.78f, 0.36f, 0.22f), new Color(0.32f, 0.26f, 0.26f), 0.72f, new Color(1f, 0.55f, 0.3f)); break;
                default: SetSky(new Color(0.24f, 0.5f, 0.95f), new Color(0.72f, 0.86f, 1f), Color.white, 0.45f, new Color(1f, 0.95f, 0.8f)); break;
            }
            RenderSettings.ambientSkyColor = Color.Lerp(m.sky, Color.white, 0.25f);
            RenderSettings.ambientEquatorColor = Color.Lerp(m.ground, Color.white, 0.45f);
            RenderSettings.ambientGroundColor = Art.Dk(m.ground, 0.5f);
            if (sun != null) sun.color = i == 1 ? new Color(1f, 0.92f, 0.78f) : i == 2 ? new Color(0.9f, 0.95f, 1f) : i == 3 ? new Color(1f, 0.75f, 0.6f) : new Color(1f, 0.96f, 0.86f);
        }

        // ---------------------------------------------------------------
        //  Inamici
        // ---------------------------------------------------------------
        void SpawnEnemy(int mi, bool boss, int t = 0, int slot = 0)
        {
            var m = GameData.Maps[mi];
            if (!boss) t = Mathf.Clamp(t, 0, m.enemies.Length - 1);
            int id = Enemy.MakeId(mi, boss, t, slot);
            if (Mp.ClientOn(mi) || Enemy.ById(id) != null) return;   // multiplayer: monstrii vin de la gazda
            if (boss)
            {
                Enemy.Spawn(mi, m.boss, true, m.Arena, id);
                return;
            }
            var e = Enemy.Spawn(mi, m.enemies[t], false, GameData.ZonePoint(mi, t), id);
            if (e != null) { e.kind = t; e.slot = slot; }
        }

        public IEnumerator RespawnRoutine(int mi, bool boss, int kind = 0, int slot = 0)
        {
            yield return new WaitForSeconds(boss ? 45f : 7f);
            SpawnEnemy(mi, boss, kind, slot);
        }

        /// <summary>Pune la loc monstrii care lipsesc de pe harta (dupa ce ai iesit dintr-o camera multiplayer).</summary>
        public void RefillMap(int mi)
        {
            if (mi < 0 || mi >= GameData.Maps.Length) return;
            var m = GameData.Maps[mi];
            for (int t = 0; t < m.enemies.Length && t < GameData.ZoneCount.Length; t++)
                for (int k = 0; k < GameData.ZoneCount[t]; k++) SpawnEnemy(mi, false, t, k);
            SpawnEnemy(mi, true);
        }

        /// <summary>Multiplayer (client): gazda are un monstru pe care noi nu-l avem -> il cream.</summary>
        public Enemy SpawnNetEnemy(int id, Vector3 pos)
        {
            int r = id - 1;
            int mi = r / 1000, rr = r % 1000;
            if (mi < 0 || mi >= GameData.Maps.Length) return null;
            var m = GameData.Maps[mi];
            bool boss = rr == 999;
            int t = boss ? 0 : rr / 100, slot = boss ? 0 : rr % 100;
            if (!boss && t >= m.enemies.Length) return null;
            var e = Enemy.Spawn(mi, boss ? m.boss : m.enemies[t], boss, boss ? m.Arena : pos, id);
            if (e != null) { e.kind = t; e.slot = slot; e.transform.position = pos; }
            return e;
        }

        // ---------------------------------------------------------------
        //  Calatorie intre harti
        // ---------------------------------------------------------------
        public void Travel(int t, bool force)
        {
            if (!force && player.Level < GameData.Maps[t].req)
            {
                Say("You need level " + GameData.Maps[t].req + " to enter " + GameData.Maps[t].name + "!", 3f);
                Sfx.Play("Error", 0.6f);
                return;
            }
            LoadingScreen.ShowMap(t);
            if (Sfx.I != null) Sfx.I.FadeIn();   // sunetul revine treptat pe harta noua
            if (InPlaza && TradeSystem.I != null) TradeSystem.I.Unclaim(true);   // pleci din piata = booth unclaimed
            mapIndex = t;
            InPlaza = false;
            CullMaps();
            player.Teleport(GameData.Maps[t].Spawn);
            ApplyMapAtmosphere(t, false);
            if (Sfx.I != null) Sfx.I.PlaySong(t);
            hud.DeathFade(0.6f);
            Sfx.Play("Portal", 0.7f);
            Fx.Ring(GameData.Maps[t].Spawn, GameData.Maps[t].enemies[0].col, 4f);
            hud.Welcome(GameData.Maps[t].name, t == 0 ? "MAIN HUB  -  forge, pets, quests, leaderboards" : "Level " + GameData.Maps[t].req + "+   |   Rec. POWER " + Fmt.Num(GameData.RecPower[Mathf.Clamp(t, 0, GameData.RecPower.Length - 1)]), Color.Lerp(GameData.Maps[t].ground, Color.white, 0.45f));
            player.SaveNow();
        }

        // ---------------------------------------------------------------
        //  Optimizare: doar harta pe care esti ramane pornita (celelalte 3 + piata se opresc)
        // ---------------------------------------------------------------
        Transform worldT;
        void CullMaps()
        {
            if (worldT == null) { var wr = WorldRoot.Find(); if (wr != null) worldT = wr.transform; }
            if (worldT == null) return;
            for (int i = 0; i < worldT.childCount; i++)
            {
                var c = worldT.GetChild(i);
                bool on;
                if (c.name.StartsWith("Map "))
                {
                    int n; string num = c.name.Substring(4).Split(' ')[0];
                    on = !InPlaza && int.TryParse(num, out n) && n - 1 == mapIndex;
                }
                else if (c.name == "Trade Plaza") on = InPlaza;
                else continue;
                if (c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
            }
        }

        public const int DetailLayer = 28;
        static void SetLayerDeep(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerDeep(t.GetChild(i), layer);
        }

        /// <summary>O singura data la pornire: obiectele mici nu mai fac umbra (pietricele, iarba, flori, pavele) si
        /// volumul de post-processing dublat din scena se opreste (avem ColorRPG_Volume).</summary>
        void OptimizeWorldOnce()
        {
            var wr = WorldRoot.Find();
            if (wr != null)
                foreach (var r in wr.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off || r is ParticleSystemRenderer) continue;
                    if (r.bounds.size.magnitude < 1.6f) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            // detaliile mici (iarba, flori, pietricele) pe un layer propriu care nu se mai deseneaza de departe
            if (wr != null)
                foreach (var t in wr.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Details (grass, flowers)") SetLayerDeep(t, DetailLayer);
            if (cam != null)
            {
                var d = new float[32];
                d[DetailLayer] = Device.Mobile ? (DeviceTier.Tier == 0 ? 26f : 34f) : 48f;
                cam.layerCullDistances = d;
                cam.layerCullSpherical = true;
            }
            foreach (var v in Object.FindObjectsByType<Volume>())
                if (v != volume && v.isGlobal && v.gameObject.name != "ColorRPG_Volume") v.enabled = false;
        }

        /// <summary>Intra in Trade Plaza (piata cu booth-uri).</summary>
        public bool TradeLocked { get { return player.Level < GameData.TradeLevel && player.D.rebirth == 0; } }

        public void GoPlaza()
        {
            if (TradeLocked) { Say("The TRADE PLAZA unlocks at level " + GameData.TradeLevel + "!", 3f); Sfx.Play("Error", 0.6f); return; }
            InPlaza = true;
            if (Sfx.I != null) Sfx.I.FadeIn();
            CullMaps();
            player.Teleport(PlazaBuilder.Spawn);
            SetSky(new Color(0.26f, 0.52f, 0.95f), new Color(0.8f, 0.9f, 1f), Color.white, 0.38f, new Color(1f, 0.95f, 0.82f));
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.9f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.95f, 0.9f, 0.8f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.45f, 0.4f);
            if (sun != null) sun.color = new Color(1f, 0.95f, 0.85f);
            if (Sfx.I != null) Sfx.I.PlaySong(GameData.PlazaZone);
            // daca ai booth pe alt server, intri direct pe serverul lui
            var ts = TradeSystem.I;
            if (ts != null && ts.HasBooth && (ts.Server == null || ts.Server.id != player.D.boothServer)) ts.Unclaim(false);
            LoadingScreen.ShowPlaza(ts != null && ts.Server != null ? TradeSystem.ServerName(ts.Server.id) : "");
            hud.DeathFade(0.6f);
            Sfx.Play("Portal", 0.7f);
            Fx.Ring(PlazaBuilder.Spawn, new Color(1f, 0.8f, 0.3f), 4f);
            hud.Welcome("Trade Plaza", ts != null && ts.Server != null ? TradeSystem.ServerName(ts.Server.id) + "  -  " + ts.Server.players + "/" + TradeDB.MaxPlayers + " players" : "", new Color(1f, 0.85f, 0.35f));
            if (ts != null && !ts.HasBooth)
                Say(ts.FreeBooths > 0 ? TradeSystem.ServerName(ts.Server.id) + ": walk to a FREE BOOTH and " + (Device.Mobile ? "tap to claim it!" : "press E to claim it!") : "All booths on this server are taken - use the TRADING button to pick another server.", 6f);
            player.SaveNow();
        }

        // ---------------------------------------------------------------
        //  Cadou (la fiecare 5 minute de joc)
        // ---------------------------------------------------------------
        public string GiftLabel()
        {
            float left = GiftInterval - player.D.giftTimer;
            if (left > 0f) return "Gift ready in " + Mathf.FloorToInt(left / 60f) + ":" + Mathf.FloorToInt(left % 60f).ToString("00");
            return "[E] Open gift #" + (player.D.giftIndex + 1) + "!";
        }

        public void ClaimGift(Vector3 pos)
        {
            if (player.D.giftTimer < GiftInterval) { Sfx.Play("Error", 0.5f); return; }
            player.D.giftTimer = 0f;
            player.D.giftIndex++;
            var m = GameData.Maps[mapIndex];
            long coins = (long)Mathf.Round(m.coins * 25 * (1 + player.D.giftIndex * 0.15f) * player.CoinMul);
            for (int i = 0; i < 8; i++) Loot.SpawnCoin(System.Math.Max(1L, coins / 8), pos);
            for (int i = 0; i < 3; i++) Loot.SpawnOre(2 + m.tier * 2, pos, m.oreCol);
            if (player.D.giftIndex % 3 == 0) Loot.SpawnItem(ItemDB.RandomDrop(m.tier, 1.5f, 1), pos);
            player.AddShards(1, "gift");
            Fx.Fountain(pos + Vector3.up, new Color(1f, 0.85f, 0.3f), 40);
            Sfx.Play("JingleHatch", 0.6f, 1.2f, 0f);
            hud.BigText("GIFT OPENED!", new Color(1f, 0.85f, 0.3f));
        }

        // ---------------------------------------------------------------
        //  Update / camera
        // ---------------------------------------------------------------
        public void Say(string s, float t) { if (hud != null) hud.Say(s, t); }

        void OnApplicationQuit() { if (player != null) player.SaveNow(); }
        void OnApplicationPause(bool p) { if (p && player != null) player.SaveNow(); }

        /// <summary>Cel mai apropiat loot de luat cu [E] (null daca nu e nimic / ai MAGNET PASS).</summary>
        public Loot CurrentLoot { get; private set; }
        public const float PickRadius = 2.6f;

        void Update()
        {
            float dt = Time.deltaTime;
            saveT += dt;
            if (saveT > 15f && player != null) { saveT = 0f; player.SaveNow(); }
            if (player != null) player.D.giftTimer = Mathf.Min(GiftInterval, player.D.giftTimer + dt);

            CurrentInteractable = null;
            if (player != null && !player.dead && !hud.Blocking)
            {
                float best = 1e9f;
                foreach (var it in Interactable.All)
                {
                    Vector3 d = it.transform.position - player.transform.position; d.y = 0;
                    float dist = d.magnitude;
                    if (dist < it.radius && dist < best) { best = dist; CurrentInteractable = it; }
                }
                // loot pe jos (minereu / iteme): se ia cu [E] daca nu ai MAGNET PASS
                CurrentLoot = null;
                if (CurrentInteractable == null && !player.D.passMagnet)
                {
                    float bl = PickRadius;
                    foreach (var l in Loot.Pickable)
                    {
                        if (l == null || !l.Landed) continue;
                        Vector3 d = l.transform.position - player.transform.position; d.y = 0;
                        if (d.magnitude < bl) { bl = d.magnitude; CurrentLoot = l; }
                    }
                }
                if (Inp.InteractDown())
                {
                    if (CurrentInteractable != null) CurrentInteractable.Act();
                    else if (CurrentLoot != null) Loot.PickUpAround(player, PickRadius + 0.6f);
                }
            }
            else CurrentLoot = null;

            float sc = Inp.Scroll();
            if (sc != 0f && !hud.Blocking) zoomTarget = Mathf.Clamp(zoomTarget - sc * 0.12f, 0.5f, 1.6f);
        }

        /// <summary>Camera se aseaza din nou in spatele jucatorului (dupa teleport).</summary>
        public void ResetCamera(float yaw)
        {
            CamYaw = yaw;
            camPitch = 16f;
            camSnap = true;
        }

        void LateUpdate()
        {
            if (player == null || cam == null) return;
            if (PromoDirector.ControlsCamera) return;   // filmarea pentru reclama misca singura camera
            float dt = Time.unscaledDeltaTime;
            bool blocked = hud != null && hud.Blocking;

            // rotire manuala (click dreapta + mouse, stick drept, Z/C)
            Vector2 look = blocked ? Vector2.zero : Inp.Look(!hud.PointerOverUI() || lastManualLook > Time.unscaledTime - 0.1f);
            if (look.sqrMagnitude > 0.0001f)
            {
                float k = GameSettings.CamSensMul;
                CamYaw += look.x * k;
                camPitch = Mathf.Clamp(camPitch - look.y * k, -5f, 65f);
                lastManualLook = Time.unscaledTime;
            }
            // (scos) camera nu se mai intoarce singura dupa erou / dupa inamic cand ataci:
            // o rotesti doar tu (telefon: mana dreapta, PC: mouse)

            zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-8f * dt));
            Vector3 targetPivot = player.transform.position + Vector3.up * 1.7f;
            camPivot = camSnap ? targetPivot : Vector3.Lerp(camPivot, targetPivot, 1f - Mathf.Exp(-18f * dt));

            Quaternion rot = Quaternion.Euler(camPitch, CamYaw, 0f);
            Vector3 back = rot * Vector3.back;
            float want = CamBaseDist * zoom;

            // nu intram in copaci / pereti: tragem camera mai aproape
            float dist = want;
            RaycastHit hit;
            if (Physics.SphereCast(camPivot, 0.35f, back, out hit, want, ~((1 << Portrait.Layer) | (1 << 2)), QueryTriggerInteraction.Ignore))
            {
                bool self = hit.collider != null && (hit.collider.transform == player.transform || hit.collider.GetComponentInParent<Enemy>() != null);
                if (!self) dist = Mathf.Max(1.2f, hit.distance - 0.1f);
            }
            camDistSmoothed = (camSnap || dist < camDistSmoothed) ? dist : Mathf.Lerp(camDistSmoothed, dist, 1f - Mathf.Exp(-4f * dt));
            camSnap = false;

            Vector3 pos = camPivot + back * camDistSmoothed;
            if (pos.y < 0.35f) pos.y = 0.35f;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(camPivot + Vector3.up * 0.25f - pos);
            cam.transform.position += Fx.ShakeOffset();
        }
    }
}
