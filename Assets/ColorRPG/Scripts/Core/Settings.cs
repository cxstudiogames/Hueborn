// =====================================================================
//  Settings.cs - setari de performanta, grafica si sunet (salvate in PlayerPrefs)
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class GameSettings
    {
        public static float MusicVolume = 0.8f;   // sunet ambiental (muzica)
        public static float SfxVolume = 0.8f;     // efecte sonore
        public static int Quality = 1;            // 0 = BETTER PERFORMANCE, 1 = BETTER QUALITY (fostul HIGH a fost scos - bloca telefoanele)
        public static int FpsIndex = 1;           // 30 / 60 / 120 / Unlimited
        public static bool Shadows = true, PostFx = true;
        public static float Particles = 1f;       // densitatea particulelor ambientale
        public static bool ShowDamageNumbers = true;
        public static bool Grass = true;          // iarba 3D
        public static bool OnlineMaps = true;     // joci cu altii pe harti (oprit = farmezi singur; Trade Plaza ramane online)
        public static float CamSens = 0.5f;       // sensibilitatea camerei (0..1)
        public static float CamSensMul { get { return Mathf.Lerp(0.3f, 2.3f, CamSens); } }

        public static readonly string[] QualityNames = { "BETTER PERFORMANCE", "BETTER QUALITY" };
        public static readonly int[] FpsValues = { 30, 60, 120 };
        public static readonly string[] FpsNames = { "30", "60", "120" };

        /// <summary>Cel mai mare refresh rate al ecranului (Hz) - 120 FPS se vad doar pe ecrane de 90/120 Hz.</summary>
        public static int MaxRefresh
        {
            get
            {
                double best = Screen.currentResolution.refreshRateRatio.value;
                foreach (var r in Screen.resolutions) if (r.refreshRateRatio.value > best) best = r.refreshRateRatio.value;
                return Mathf.Max(30, Mathf.RoundToInt((float)best));
            }
        }

        public static void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat("set_music", 0.8f);
            SfxVolume = PlayerPrefs.GetFloat("set_sfx", 0.8f);
            Quality = Mathf.Clamp(PlayerPrefs.GetInt("set_quality", 1), 0, 1);   // vechiul HIGH devine BETTER QUALITY
            FpsIndex = Mathf.Clamp(PlayerPrefs.GetInt("set_fps", 1), 0, FpsValues.Length - 1);   // vechiul "MAX" devine 120
            Shadows = PlayerPrefs.GetInt("set_shadows", 1) == 1;
            PostFx = PlayerPrefs.GetInt("set_post", 1) == 1;
            Particles = PlayerPrefs.GetFloat("set_particles", 1f);
            ShowDamageNumbers = PlayerPrefs.GetInt("set_dmgnum", 1) == 1;
            Grass = PlayerPrefs.GetInt("set_grass", 1) == 1;
            CamSens = PlayerPrefs.GetFloat("set_camsens", 0.5f);
            OnlineMaps = PlayerPrefs.GetInt("set_mp_maps", 1) == 1;
            // prima pornire: setarile se aleg dupa cat de puternic e telefonul / PC-ul
            if (!PlayerPrefs.HasKey("set_quality_v2"))
            {
                int t = DeviceTier.Tier;
                if (!Device.Mobile) { Quality = 1; Shadows = PostFx = Grass = true; Particles = 0.8f; FpsIndex = 1; }
                else if (t == 0) { Quality = 0; Shadows = false; PostFx = false; Grass = false; Particles = 0.25f; FpsIndex = 0; }
                else if (t == 1) { Quality = 0; Shadows = true; PostFx = false; Grass = true; Particles = 0.45f; FpsIndex = 1; }
                else { Quality = 1; Shadows = PostFx = Grass = true; Particles = 0.6f; FpsIndex = 1; }
                PlayerPrefs.SetInt("set_quality_v2", 1);
            }
            // FPS-ul nu se mai alege din Settings: 30 pe telefoanele slabe, 60 in rest
            FpsIndex = Device.Mobile && DeviceTier.Tier == 0 ? 0 : 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("set_music", MusicVolume);
            PlayerPrefs.SetFloat("set_sfx", SfxVolume);
            PlayerPrefs.SetInt("set_quality", Quality);
            PlayerPrefs.SetInt("set_fps", FpsIndex);
            PlayerPrefs.SetInt("set_shadows", Shadows ? 1 : 0);
            PlayerPrefs.SetInt("set_post", PostFx ? 1 : 0);
            PlayerPrefs.SetFloat("set_particles", Particles);
            PlayerPrefs.SetInt("set_dmgnum", ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt("set_grass", Grass ? 1 : 0);
            PlayerPrefs.SetFloat("set_camsens", CamSens);
            PlayerPrefs.SetInt("set_mp_maps", OnlineMaps ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Preset-urile de calitate seteaza mai multe optiuni deodata.</summary>
        public static void ApplyPreset(int q)
        {
            // BETTER PERFORMANCE: fara umbre / post FX / iarba 3D; BETTER QUALITY: toate pornite (nivelul fostului MEDIUM)
            Quality = Mathf.Clamp(q, 0, 1);
            Shadows = PostFx = Grass = Quality == 1;
            Particles = Quality == 0 ? 0.3f : 0.6f;
            Apply();
            AutoPerf.ResetScale();
        }

        public static void Apply()
        {
            FpsIndex = Mathf.Clamp(FpsIndex, 0, FpsValues.Length - 1);
            int fps = FpsValues[FpsIndex];
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
#if !UNITY_ANDROID && !UNITY_IOS
            // PC: fara limita - urmeaza monitorul (VSync)
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
#endif
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            // pe telefoanele cu ecran de 90/120 Hz cerem si refresh-ul mare (altfel Android tine ecranul la 60 Hz)
            try
            {
                Resolution best = Screen.currentResolution;
                foreach (var r in Screen.resolutions)
                    if (r.width == best.width && r.height == best.height && r.refreshRateRatio.value > best.refreshRateRatio.value) best = r;
                var want = fps > 60 ? best.refreshRateRatio : Screen.currentResolution.refreshRateRatio;
                if (fps > 60 && best.refreshRateRatio.value > Screen.currentResolution.refreshRateRatio.value + 1)
                    Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreenMode, want);
            }
            catch (System.Exception) { }
#endif
            if (Sfx.I != null)
            {
                Sfx.I.SfxVol = SfxVolume;
                Sfx.I.SetMusicVolume(MusicVolume);
            }
            AmbientFx.Density = Particles;
            var g = Game.I;
            if (g != null) g.ApplyGraphics();
            Save();
        }
    }
}
