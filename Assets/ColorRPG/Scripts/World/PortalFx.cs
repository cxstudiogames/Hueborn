// PortalFx.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class PortalFx : MonoBehaviour
    {
        public Color col = Color.white;
        float acc;
        void Update()
        {
            if (Game.I == null || Game.I.player == null) return;
            if ((transform.position - Game.I.player.transform.position).sqrMagnitude > 60f * 60f) return;
            acc += Time.deltaTime * 14f;
            while (acc >= 1f)
            {
                acc -= 1f;
                float a = Random.value * Mathf.PI * 2f;
                Vector3 off = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * Random.Range(0.6f, 2.1f);
                Fx.Sparkle(transform.position + transform.rotation * off, Color.Lerp(col, Color.white, Random.value * 0.5f), 1, 0.1f);
            }
        }
    }
}
