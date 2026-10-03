// =====================================================================
//  Obstacle.cs - obstacol rotund (copac, stanca, cactus, piramida...) pentru
//  INAMICI: ei se misca fara fizica, asa ca ii "impingem" afara din obstacole.
//  (Eroul e oprit de colliderul normal.) Se inregistreaza doar cat timp
//  harta lui e activa (CullMaps dezactiveaza celelalte harti).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class Obstacle : MonoBehaviour
    {
        public float r = 1f;
        static readonly List<Obstacle> active = new List<Obstacle>();
        Vector3 pos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { active.Clear(); }

        void OnEnable() { pos = transform.position; active.Add(this); }
        void OnDisable() { active.Remove(this); }

        /// <summary>Impinge pozitia p (raza rad) in afara obstacolelor active. true = a fost impinsa.</summary>
        public static bool PushOut(ref Vector3 p, float rad)
        {
            bool hit = false;
            for (int i = 0; i < active.Count; i++)
            {
                var o = active[i];
                float dx = p.x - o.pos.x, dz = p.z - o.pos.z;
                float min = o.r + rad;
                float d2 = dx * dx + dz * dz;
                if (d2 >= min * min) continue;
                float d = Mathf.Sqrt(d2);
                if (d < 0.001f) { dx = 1f; dz = 0f; d = 1f; }
                p.x = o.pos.x + dx / d * min;
                p.z = o.pos.z + dz / d * min;
                hit = true;
            }
            return hit;
        }

        /// <summary>Doar obstacol pentru inamici (obiectul are deja coliziunea lui, ex. piramide din cuburi).</summary>
        public static void Marker(Transform parent, Vector3 p, float r)
        {
            var go = new GameObject("Obstacle");
            go.transform.SetParent(parent, false);
            go.transform.position = p;
            go.AddComponent<Obstacle>().r = r;
        }

        /// <summary>Stalp invizibil (coliziune + obstacol) intr-un punct - pentru obiecte construite din mai multe piese (cactusi).</summary>
        public static void Pillar(Transform parent, Vector3 p, float r, float h = 6f)
        {
            var go = new GameObject("Pillar");
            go.transform.SetParent(parent, false);
            go.transform.position = p;
            Add(go, r, h);
        }

        /// <summary>Adauga coliziune (erou) + obstacol (inamici) pe un obiect din lume. r / h in metri (lumea).</summary>
        public static void Add(GameObject go, float r, float h)
        {
            // pe un copil separat, pe layer-ul "Ignore Raycast" (2): eroul si inamicii se lovesc de el,
            // dar camera nu (altfel stalpul invizibil ar trage camera in fata cand esti in spatele unei pietre)
            var old = go.GetComponent<CapsuleCollider>();
            if (old != null) Object.DestroyImmediate(old);
            var cgo = new GameObject("Collider");
            cgo.layer = 2;
            cgo.transform.SetParent(go.transform, false);
            go = cgo;
            var c = go.AddComponent<CapsuleCollider>();
            Vector3 s = go.transform.lossyScale;
            float sx = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z))), sy = Mathf.Max(0.0001f, Mathf.Abs(s.y));
            // "stalp" vertical inalt (min. 6 m), cu capatul rotunjit de jos ingropat in pamant:
            // la nivelul solului peretele e drept, deci eroul nu mai poate "urca" pe pietre mici
            float H = Mathf.Max(h, 6f) + 2f * r;
            c.direction = 1;
            c.radius = r / sx;
            c.height = H / sy;
            c.center = new Vector3(0, (H * 0.5f - r) / sy, 0);
            var o = go.GetComponent<Obstacle>();
            if (o == null) o = go.AddComponent<Obstacle>();
            o.r = r;
        }
    }
}
