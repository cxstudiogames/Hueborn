// =====================================================================
//  QuestGuide.cs - obiectul de tracking pentru questuri:
//   * o sageata 3D aurie (cu contur inchis) langa jucator, care arata spre
//     cel mai apropiat inamic cerut de questurile urmarite; se micsoreaza
//     cand esti aproape
//   * deasupra tintei: un "pin" auriu care sare + un cerc luminos pe jos
//  Se opreste din TRACK (pe quest) sau HIDE (tot tracker-ul).
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public class QuestGuide : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.8f, 0.22f), Dark = new Color(0.16f, 0.1f, 0.05f);
        Transform arrow, marker, ring;
        Material ringMat;
        float scanT;
        Enemy target;
        static Mesh arrowMesh;

        public static QuestGuide Create()
        {
            var go = new GameObject("QuestGuide");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            return go.AddComponent<QuestGuide>();
        }

        /// <summary>Sageata plata extrudata (coada + varf), in planul XZ, varful spre +Z.</summary>
        static Mesh ArrowMesh()
        {
            if (arrowMesh != null) return arrowMesh;
            // conturul (in sensul acelor de ceasornic, privit de sus)
            Vector2[] p =
            {
                new Vector2(-0.13f, -0.5f), new Vector2(-0.13f, 0.02f), new Vector2(-0.34f, 0.02f), new Vector2(0f, 0.52f),
                new Vector2(0.34f, 0.02f), new Vector2(0.13f, 0.02f), new Vector2(0.13f, -0.5f),
            };
            // triunghiuri pentru fata de sus (coada = 2, varful = 1)
            int[] top = { 0, 1, 5, 0, 5, 6, 2, 3, 4 };
            const float h = 0.09f;
            var v = new System.Collections.Generic.List<Vector3>();
            var n = new System.Collections.Generic.List<Vector3>();
            var t = new System.Collections.Generic.List<int>();
            // sus
            for (int i = 0; i < p.Length; i++) { v.Add(new Vector3(p[i].x, h * 0.5f, p[i].y)); n.Add(Vector3.up); }
            for (int i = 0; i < top.Length; i += 3) { t.Add(top[i]); t.Add(top[i + 1]); t.Add(top[i + 2]); }
            // jos
            int b0 = v.Count;
            for (int i = 0; i < p.Length; i++) { v.Add(new Vector3(p[i].x, -h * 0.5f, p[i].y)); n.Add(Vector3.down); }
            for (int i = 0; i < top.Length; i += 3) { t.Add(b0 + top[i]); t.Add(b0 + top[i + 2]); t.Add(b0 + top[i + 1]); }
            // laterale
            for (int i = 0; i < p.Length; i++)
            {
                Vector2 a = p[i], c = p[(i + 1) % p.Length];
                Vector2 e = (c - a).normalized;
                Vector3 nn = new Vector3(-e.y, 0, e.x);   // spre exterior (contur in sensul acelor)
                int k = v.Count;
                v.Add(new Vector3(a.x, h * 0.5f, a.y)); v.Add(new Vector3(c.x, h * 0.5f, c.y));
                v.Add(new Vector3(c.x, -h * 0.5f, c.y)); v.Add(new Vector3(a.x, -h * 0.5f, a.y));
                for (int q = 0; q < 4; q++) n.Add(nn);
                t.Add(k); t.Add(k + 2); t.Add(k + 1); t.Add(k); t.Add(k + 3); t.Add(k + 2);
            }
            arrowMesh = new Mesh { name = "QuestArrow" };
            arrowMesh.SetVertices(v); arrowMesh.SetNormals(n); arrowMesh.SetTriangles(t, 0);
            arrowMesh.RecalculateBounds();
            return arrowMesh;
        }

        static Transform MeshObj(string name, Mesh mesh, Transform parent, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        void Start()
        {
            var gold = GearVisuals.Mat(Gold, 0.3f, 0.7f, Gold * 0.9f);
            var dark = GearVisuals.Mat(Dark, 0f, 0.2f, Color.black);

            // sageata: miezul auriu + conturul inchis (aceeasi forma, putin mai mare, dedesubt)
            arrow = new GameObject("QuestArrow").transform;
            arrow.SetParent(transform, false);
            var outline = MeshObj("Outline", ArrowMesh(), arrow, dark);
            outline.localScale = new Vector3(1.22f, 0.8f, 1.16f);
            outline.localPosition = new Vector3(0, -0.02f, -0.01f);
            MeshObj("Core", ArrowMesh(), arrow, gold);

            // pin-ul de deasupra tintei: con cu varful in jos + bila
            marker = new GameObject("QuestMarker").transform;
            marker.SetParent(transform, false);
            var cone = MeshObj("Cone", MeshGen.Cone(0f), marker, gold);
            cone.localRotation = Quaternion.Euler(180f, 0, 0);
            cone.localScale = new Vector3(0.42f, 0.55f, 0.42f);
            cone.localPosition = new Vector3(0, 0.55f, 0);
            var ball = Art.Prim(PrimitiveType.Sphere, new Vector3(0, 0.78f, 0), Vector3.one * 0.5f, Gold, marker, false, true);
            var br = ball.GetComponent<Renderer>(); br.sharedMaterial = gold; br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var dot = Art.Prim(PrimitiveType.Sphere, new Vector3(0, 0.78f, 0.2f), Vector3.one * 0.2f, Dark, marker, false, true);
            dot.GetComponent<Renderer>().sharedMaterial = dark;

            // cercul de pe jos, sub tinta
            var q = Art.Prim(PrimitiveType.Quad, Vector3.zero, Vector3.one, Color.white, transform, false, true);
            q.name = "QuestRing";
            q.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            ringMat = Art.MeshFx(Art.A(Gold, 0.45f), true, Art.SoftDot);
            var qr = q.GetComponent<Renderer>();
            qr.sharedMaterial = ringMat;
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring = q.transform;

            arrow.gameObject.SetActive(false);
            marker.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
        }

        void Update()
        {
            var g = Game.I;
            if (g == null || g.player == null || arrow == null) return;
            var p = g.player;
            scanT -= Time.deltaTime;
            if (scanT <= 0f || (target != null && target.Dying))
            {
                scanT = 0.4f;
                target = p.dead ? null : p.QuestTarget();
            }
            bool on = target != null && !target.Dying;
            if (arrow.gameObject.activeSelf != on) arrow.gameObject.SetActive(on);
            if (marker.gameObject.activeSelf != on) marker.gameObject.SetActive(on);
            if (ring.gameObject.activeSelf != on) ring.gameObject.SetActive(on);
            if (!on) return;

            Vector3 pp = p.transform.position;
            Vector3 d = target.transform.position - pp; d.y = 0;
            float dist = d.magnitude;
            if (dist < 0.01f) d = Vector3.forward; else d /= dist;
            float near = dist < 6f ? Mathf.Lerp(0.35f, 1f, dist / 6f) : 1f;
            // sageata: "inoata" spre tinta (mica miscare inainte-inapoi) si se inclina usor
            float swim = Mathf.Sin(Time.time * 5f);
            arrow.position = pp + d * (2.2f + 0.15f * swim) + Vector3.up * 1.0f;
            arrow.rotation = Quaternion.LookRotation(d) * Quaternion.Euler(-8f, 0, 0);
            arrow.localScale = Vector3.one * 1.5f * near;

            Vector3 tp = target.transform.position;
            float bounce = Mathf.Abs(Mathf.Sin(Time.time * 3.2f));
            marker.position = tp + Vector3.up * (target.height + 0.9f + 0.35f * bounce);
            marker.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            float rs = Mathf.Max(1.6f, target.height * 1.3f) * (1f + 0.08f * Mathf.Sin(Time.time * 4f));
            ring.position = new Vector3(tp.x, tp.y + 0.06f, tp.z);
            ring.localScale = new Vector3(rs, rs, 1f);
        }

        void OnDestroy() { if (ringMat != null) Destroy(ringMat); }
    }
}
