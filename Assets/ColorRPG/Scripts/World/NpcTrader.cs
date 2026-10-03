// NpcTrader.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    // ------------------------------------------------------------------
    //  Negustor NPC in spatele tarabei
    // ------------------------------------------------------------------
    public class NpcTrader : MonoBehaviour
    {
        [Tooltip("Modelul negustorului (Resources/...)")]
        public string modelPath;
        SimpleAnimator anim;
        float t;

        /// <summary>Pune modelul (merge si in editor, cand construim scena).</summary>
        public void Init(string path)
        {
            modelPath = path;
            var m = ModelLib.Spawn(path, transform, 2f);
            if (m == null) Art.Prim(PrimitiveType.Capsule, new Vector3(0, 1, 0), Vector3.one, new Color(0.9f, 0.6f, 0.3f), transform, false);
        }

        void Start()
        {
            anim = SceneAnim.Attach(transform, modelPath);
            if (anim != null) anim.Play("idle");
            t = Random.Range(3f, 8f);
        }

        public void Greet()
        {
            if (anim != null && anim.Play("emote-yes", 0.1f, false, true)) t = 2f;
            Sfx.Play("Click", 0.5f, 1.4f);
        }

        void Update()
        {
            if (anim == null) return;
            t -= Time.deltaTime;
            if (t <= 0f)
            {
                t = Random.Range(4f, 9f);
                string[] e = { "interact-right", "interact-left", "emote-yes", "idle" };
                if (!anim.Play(e[Random.Range(0, e.Length)], 0.15f, false, true)) anim.Play("idle");
            }
            if (anim.Current != "idle" && anim.Done) anim.Play("idle", 0.2f);
        }
    }
}
