// WorldLabel.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    /// <summary>Text care pluteste in lume. Se poate edita din Inspector (text, culoare, marime).</summary>
    public class WorldLabel : MonoBehaviour
    {
        public string text = "LABEL";
        public Color color = Color.white;
        public float size = 24f;
        public float maxDistance = 45f;
        bool registered;

        void Update()
        {
            if (registered) { enabled = false; return; }
            var g = Game.I;
            if (g == null || g.hud == null) return;
            g.hud.Label(transform.position, text, color, size, maxDistance);
            registered = true;
        }

        /// <summary>Creeaza o eticheta ca obiect copil (vizibila si in Hierarchy).</summary>
        public static WorldLabel Create(Transform parent, Vector3 worldPos, string text, Color c, float size = 24f, float maxDist = 45f)
        {
            var go = new GameObject("Label - " + text);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var l = go.AddComponent<WorldLabel>();
            l.text = text; l.color = c; l.size = size; l.maxDistance = maxDist;
            return l;
        }
    }
}
