// FireFx.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    // ------------------------------------------------------------------
    //  Efecte ambientale mici
    // ------------------------------------------------------------------
    public class FireFx : MonoBehaviour
    {
        public Color col = new Color(1f, 0.55f, 0.1f);
        public float rate = 8f;
        float acc;
        void Update()
        {
            if (Game.I == null || Game.I.player == null) return;
            if ((transform.position - Game.I.player.transform.position).sqrMagnitude > 70f * 70f) return;
            acc += Time.deltaTime * rate;
            while (acc >= 1f)
            {
                acc -= 1f;
                Fx.Sparkle(transform.position + Vector3.up * 0.8f, Color.Lerp(col, new Color(1f, 0.9f, 0.3f), Random.value), 1, 0.3f);
            }
        }
    }
}
