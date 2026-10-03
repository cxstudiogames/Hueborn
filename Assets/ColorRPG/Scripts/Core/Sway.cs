// Sway.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    /// <summary>Leganare usoara in vant (copaci, palmieri, brazi).</summary>
    public class Sway : MonoBehaviour
    {
        float seed, amp;
        Quaternion baseRot;
        Transform cam;

        void Start()
        {
            seed = Random.value * 20f;
            amp = Random.Range(1.2f, 2.6f);
            baseRot = transform.localRotation;
        }

        void Update()
        {
            if (cam == null) { var c = Camera.main; if (c == null) return; cam = c.transform; }
            // doar copacii din apropierea camerei (economie)
            if ((transform.position - cam.position).sqrMagnitude > 85f * 85f) return;
            float t = Time.time * 1.3f + seed;
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t) * amp, 0, Mathf.Sin(t * 0.7f + 1.3f) * amp * 0.7f);
        }
    }
}
