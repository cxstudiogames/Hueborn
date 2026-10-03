// =====================================================================
//  LevelUpFx.cs - efectul de LEVEL UP pe erou (1.6 s, il urmareste):
//   * doua unde de lumina aurii care se intind pe jos
//   * o coloana de lumina care urca si se stinge
//   * doua spirale de scantei care se rotesc in sus in jurul eroului
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public class LevelUpFx : MonoBehaviour
    {
        const float Dur = 1.6f;
        static readonly Color Gold = new Color(1f, 0.82f, 0.32f), Cyan = new Color(0.45f, 0.92f, 1f);
        Transform target;
        float t, emit;
        Transform ring1, ring2, column;
        Material ringMat1, ringMat2, colMat;

        public static void Play(Transform who)
        {
            if (who == null) return;
            var go = new GameObject("LevelUpFx");
            go.transform.SetParent(SceneGroups.Get("Fx"), false);
            go.transform.position = who.position;
            var fx = go.AddComponent<LevelUpFx>();
            fx.target = who;
            fx.Build();
        }

        Transform Disc(Color c, out Material m)
        {
            var q = Art.Prim(PrimitiveType.Quad, new Vector3(0, 0.06f, 0), Vector3.one, Color.white, transform, false, true);
            q.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            m = Art.MeshFx(Art.A(c, 0.8f), true, Art.SoftDot);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q.transform;
        }

        void Build()
        {
            ring1 = Disc(Gold, out ringMat1);
            ring2 = Disc(Cyan, out ringMat2);
            var c = Art.Prim(PrimitiveType.Cylinder, Vector3.zero, Vector3.one, Color.white, transform, false, true);
            colMat = Art.GlowMaterial(Art.A(Gold, 0.35f));
            var cr = c.GetComponent<Renderer>();
            cr.sharedMaterial = colMat;
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            column = c.transform;
            Fx.Sparkle(transform.position + Vector3.up * 1.2f, Gold, 18, 1.1f);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (target != null) transform.position = target.position;
            float k = t / Dur;
            if (k >= 1f) { Destroy(gameObject); return; }

            // undele de pe jos
            float a1 = Mathf.Clamp01(k / 0.6f), a2 = Mathf.Clamp01((k - 0.18f) / 0.6f);
            ring1.localScale = Vector3.one * Mathf.Lerp(0.5f, 9f, Ease(a1));
            ring2.localScale = Vector3.one * Mathf.Lerp(0.5f, 7f, Ease(a2));
            Art.SetColor(ringMat1, Art.A(Gold, 0.8f * (1f - a1)));
            Art.SetColor(ringMat2, Art.A(Cyan, a2 > 0f ? 0.7f * (1f - a2) : 0f));

            // coloana de lumina
            float h = Mathf.Lerp(0.5f, 7f, Ease(Mathf.Clamp01(k / 0.35f)));
            float w = Mathf.Lerp(1.6f, 0.3f, k);
            column.localScale = new Vector3(w, h * 0.5f, w);
            column.localPosition = new Vector3(0, h * 0.5f, 0);
            Art.SetColor(colMat, Art.A(Gold, 0.4f * (1f - k)));

            // spirale de scantei
            if (k < 0.8f)
            {
                emit += Time.deltaTime * 60f;
                while (emit >= 1f)
                {
                    emit -= 1f;
                    float ang = t * 9f;
                    for (int s = 0; s < 2; s++)
                    {
                        float a = ang + s * Mathf.PI;
                        Vector3 p = transform.position + new Vector3(Mathf.Cos(a) * 0.95f, 0.2f + k * 3.2f, Mathf.Sin(a) * 0.95f);
                        Fx.Sparkle(p, s == 0 ? Gold : Cyan, 1, 0.05f);
                    }
                }
            }
        }

        static float Ease(float x) { return 1f - (1f - x) * (1f - x); }

        void OnDestroy()
        {
            if (ringMat1 != null) Destroy(ringMat1);
            if (ringMat2 != null) Destroy(ringMat2);
            if (colMat != null) Destroy(colMat);
        }
    }
}
