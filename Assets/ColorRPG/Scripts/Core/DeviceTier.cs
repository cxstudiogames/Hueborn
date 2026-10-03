// =====================================================================
//  DeviceTier.cs - cat de puternic e dispozitivul (0 = slab, 1 = mediu, 2 = puternic),
//  dupa memorie, nucleele procesorului si placa video. Pe baza lui:
//   * prima pornire alege setarile potrivite telefonului
//   * rezolutia interna (render scale) porneste de la o valoare potrivita
//   * AutoPerf o ajusteaza din mers ca jocul sa tina FPS-ul ales
// =====================================================================
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColorRPG
{
    public static class DeviceTier
    {
        static int tier = -1;

        /// <summary>0 = slab, 1 = mediu, 2 = puternic.</summary>
        public static int Tier
        {
            get
            {
                if (tier >= 0) return tier;
                int mem = SystemInfo.systemMemorySize;          // MB
                int cores = SystemInfo.processorCount;
                int vram = SystemInfo.graphicsMemorySize;       // MB (pe telefon e memoria partajata)
                if (!Device.Mobile) tier = mem < 6000 || vram < 1500 ? 1 : 2;
                else if (mem < 3500 || cores <= 4) tier = 0;
                else if (mem >= 7000 && cores >= 8) tier = 2;
                else tier = 1;
                Debug.Log("[ColorRPG] Device tier " + tier + " (RAM " + mem + " MB, " + cores + " cores, GPU " + SystemInfo.graphicsDeviceName + " " + vram + " MB)");
                return tier;
            }
        }

        public static string TierName { get { return Tier == 0 ? "basic" : Tier == 1 ? "mid-range" : "high-end"; } }

        /// <summary>Rezolutia interna de pornire (pe PC mereu 100%).</summary>
        public static float BaseScale(int quality)
        {
            if (!Device.Mobile) return 1f;
            float[] perf = { 0.62f, 0.72f, 0.82f }, qual = { 0.72f, 0.82f, 0.92f };
            return quality == 0 ? perf[Tier] : qual[Tier];
        }

        public static float MinScale { get { return Device.Mobile ? 0.5f : 0.75f; } }
    }

    /// <summary>Tine FPS-ul ales: daca jocul nu-l prinde, scade putin rezolutia interna; cand e loc, o creste la loc.</summary>
    public class AutoPerf : MonoBehaviour
    {
        public static AutoPerf I;
        public static float Scale = 1f;
        float acc; int frames; float goodT;

        public static void Create(Transform parent)
        {
            if (I != null) return;
            var go = new GameObject("AutoPerf");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<AutoPerf>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; Scale = 1f; }

        /// <summary>Repornire de la valoarea potrivita calitatii (cand schimbi setarile).</summary>
        public static void ResetScale()
        {
            Scale = DeviceTier.BaseScale(GameSettings.Quality);
            Push();
        }

        static void Push()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) urp.renderScale = Scale;
        }

        void Update()
        {
            if (!Device.Mobile || Time.timeScale < 0.01f) return;
            acc += Time.unscaledDeltaTime; frames++;
            if (acc < 2f) return;
            float fps = frames / acc; acc = 0f; frames = 0;
            int target = GameSettings.FpsValues[Mathf.Clamp(GameSettings.FpsIndex, 0, GameSettings.FpsValues.Length - 1)];
            target = Mathf.Min(target, GameSettings.MaxRefresh);
            float max = DeviceTier.BaseScale(GameSettings.Quality);
            if (fps < target * 0.85f && Scale > DeviceTier.MinScale)
            {
                Scale = Mathf.Max(DeviceTier.MinScale, Scale - 0.06f); goodT = 0f; Push();
            }
            else if (fps > target * 0.97f)
            {
                goodT += 2f;
                if (goodT >= 8f && Scale < max) { Scale = Mathf.Min(max, Scale + 0.04f); goodT = 0f; Push(); }
            }
            else goodT = 0f;
        }
    }
}
