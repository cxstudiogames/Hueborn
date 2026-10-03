// =====================================================================
//  Ambient.cs - atmosfera: particule ambientale per zona (polen, licurici,
//  praf, fulgi de zapada, confetti), umbre de nori care trec peste harta,
//  copaci care se leagana in vant.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class AmbientFx : MonoBehaviour
    {
        float acc;
        public static float Density = 1f;   // din Settings (0 = oprit)

        void Update()
        {
            if (Density <= 0.01f) return;
            var g = Game.I;
            if (g == null || g.player == null || g.cam == null) return;
            Vector3 c = g.player.transform.position;
            float dt = Time.deltaTime;
            int zone = g.InPlaza ? GameData.PlazaZone : g.mapIndex;
            float rate = zone == 2 ? 70f : zone == 3 ? 30f : zone == GameData.PlazaZone ? 16f : zone == 1 ? 14f : 18f;
            acc += dt * rate * Density;
            while (acc >= 1f)
            {
                acc -= 1f;
                Vector3 p = c + new Vector3(Random.Range(-26f, 26f), 0, Random.Range(-16f, 28f));
                switch (zone)
                {
                    case 0: // pajiste: polen auriu + licurici + frunze care cad din copaci
                        if (Random.value < 0.25f)
                            Fx.Ambient(p + Vector3.up * Random.Range(5f, 9f), new Vector3(Random.Range(0.4f, 1.2f), Random.Range(-1.1f, -0.6f), Random.Range(-0.4f, 0.4f)),
                                       Random.value < 0.5f ? new Color(0.95f, 0.6f, 0.18f, 0.95f) : new Color(0.45f, 0.7f, 0.2f, 0.95f), Random.Range(0.14f, 0.22f), Random.Range(6f, 9f), false);
                        else if (Random.value < 0.7f)
                            Fx.Ambient(p + Vector3.up * Random.Range(0.5f, 4f), new Vector3(Random.Range(0.3f, 1f), Random.Range(-0.1f, 0.3f), Random.Range(-0.3f, 0.3f)),
                                       new Color(1f, 0.95f, 0.6f, 0.7f), Random.Range(0.08f, 0.16f), Random.Range(3f, 6f), true);
                        else
                            Fx.Ambient(p + Vector3.up * Random.Range(0.3f, 2f), new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.1f, 0.4f), Random.Range(-0.4f, 0.4f)),
                                       new Color(0.7f, 1f, 0.4f, 0.9f), Random.Range(0.18f, 0.3f), Random.Range(2f, 4f), true);
                        break;
                    case 1: // desert: praf purtat de vant
                        Fx.Ambient(p + Vector3.up * Random.Range(0.2f, 1.5f), new Vector3(Random.Range(3f, 6f), Random.Range(0f, 0.4f), Random.Range(-0.5f, 0.5f)),
                                   new Color(1f, 0.85f, 0.6f, 0.35f), Random.Range(0.5f, 1.4f), Random.Range(2f, 4f), false);
                        break;
                    case 2: // munte: ninsoare
                        Fx.Ambient(p + Vector3.up * Random.Range(8f, 14f), new Vector3(Random.Range(0.3f, 1.2f), Random.Range(-2.6f, -1.6f), Random.Range(-0.3f, 0.3f)),
                                   new Color(1f, 1f, 1f, 0.9f), Random.Range(0.12f, 0.26f), Random.Range(4.5f, 7f), false);
                        break;
                    case 3: // vulcan: scantei/cenusa care urca
                        Fx.Ambient(p + Vector3.up * Random.Range(0.2f, 2f), new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.8f, 2f), Random.Range(-0.4f, 0.4f)),
                                   Random.value < 0.7f ? new Color(1f, 0.55f, 0.15f, 0.9f) : new Color(0.35f, 0.3f, 0.3f, 0.6f), Random.Range(0.08f, 0.2f), Random.Range(2f, 4f), true);
                        break;
                    default: // piata: confetti colorat
                        Fx.Ambient(p + Vector3.up * Random.Range(6f, 10f), new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-1.6f, -0.8f), Random.Range(-0.6f, 0.6f)),
                                   Color.HSVToRGB(Random.value, 0.65f, 1f), Random.Range(0.14f, 0.24f), Random.Range(5f, 8f), true);
                        break;
                }
            }
        }
    }


}
