// DetailGroup.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using UnityEngine;

namespace ColorRPG
{
    /// <summary>Iarba / florile mici: se ascund cand "3D grass" e oprit din Settings (mai multe FPS pe telefon).</summary>
    public class DetailGroup : MonoBehaviour
    {
        bool shown = true;
        float t;
        void Update()
        {
            t -= Time.unscaledDeltaTime;
            if (t > 0f) return;
            t = 0.5f;
            bool want = GameSettings.Grass;
            if (want == shown) return;
            shown = want;
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(want);
        }
    }
}
