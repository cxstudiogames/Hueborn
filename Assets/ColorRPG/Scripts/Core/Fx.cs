// =====================================================================
//  Fx.cs - efecte vizuale: particule (scantei, fum, sclipiri),
//  tremurat de camera, hit-stop, inel de level-up.
// =====================================================================
using System.Collections;
using UnityEngine;

namespace ColorRPG
{
    public class PillarFade : MonoBehaviour
    {
        public Color col; public float dur = 1f, height = 10f;
        float t; Material m;
        void Start() { m = GetComponent<Renderer>().sharedMaterial; }
        void Update()
        {
            t += Time.deltaTime;
            float k = t / dur;
            float w = k < 0.15f ? Mathf.Lerp(0.2f, 2.2f, k / 0.15f) : Mathf.Lerp(2.2f, 0f, (k - 0.15f) / 0.85f);
            transform.localScale = new Vector3(w, height * 0.5f, w);
            if (m != null) Art.SetColor(m, Art.A(col, 0.55f * (1f - k)));
            if (t >= dur) { if (m != null) Destroy(m); Destroy(gameObject); }
        }
    }

    public class Fx : MonoBehaviour
    {
        public static Fx I;

        ParticleSystem sparks, puffs, stars, rings, ambient, ambientGlow;
        float shake;
        bool stopping;

        public static Fx Create()
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            I = go.AddComponent<Fx>();
            return I;
        }

        void Awake()
        {
            I = this;
            sparks = MakeSystem("Sparks", true, Art.SoftDot, 25f, 0.5f);
            puffs = MakeSystem("Puffs", false, Art.SoftDot, 0f, 0.8f);
            stars = MakeSystem("Stars", true, Art.Spark, -1.5f, 0.3f);
            rings = MakeSystem("Rings", true, Art.SoftDot, 0f, 0f);
            ambient = MakeSystem("Ambient", false, Art.SoftDot, 0f, 0f);
            ambientGlow = MakeSystem("AmbientGlow", true, Art.SoftDot, 0f, 0f);
        }

        ParticleSystem MakeSystem(string n, bool additive, Texture tex, float gravity, float drag)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;          // ramane "playing" pentru Emit()
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            main.gravityModifier = gravity / 9.81f;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = 0.3f;

            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;

            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, additive ? 1f : 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            if (drag > 0f)
            {
                var lim = ps.limitVelocityOverLifetime;
                lim.enabled = true;
                lim.drag = drag;
                lim.dampen = 0f;
                lim.limit = 100f;
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Art.FxMaterial(additive, tex);
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = additive ? -10 : 0;
            ps.Play();
            return ps;
        }

        // ---------------------------------------------------------------
        //  API public
        // ---------------------------------------------------------------
        static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, Color c, float size, float life)
        {
            var ep = new ParticleSystem.EmitParams();
            ep.position = pos;
            ep.velocity = vel;
            ep.startColor = c;
            ep.startSize = size;
            ep.startLifetime = life;
            ep.rotation = Random.Range(0f, 360f);
            ps.Emit(ep, 1);
        }

        /// <summary>Scantei la lovitura.</summary>
        public static void Hit(Vector3 pos, Color c, bool crit)
        {
            if (I == null) return;
            int n = crit ? 22 : 10;
            for (int i = 0; i < n; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(4f, crit ? 12f : 8f);
                v.y = Mathf.Abs(v.y) * 0.8f + 2f;
                Emit(I.sparks, pos, v, Color.Lerp(c, Color.white, Random.value * 0.6f), Random.Range(0.15f, 0.35f), Random.Range(0.25f, 0.5f));
            }
            Emit(I.stars, pos, Vector3.zero, crit ? new Color(1f, 0.8f, 0.3f) : Color.white, crit ? 2.6f : 1.5f, 0.18f);
        }

        /// <summary>"Poof" colorat cand moare un inamic.</summary>
        public static void Poof(Vector3 pos, Color c, float scale)
        {
            if (I == null) return;
            int n = Mathf.RoundToInt(14 * Mathf.Clamp(scale, 1f, 3f));
            for (int i = 0; i < n; i++)
            {
                Vector3 v = Random.insideUnitSphere * 3.5f * scale;
                v.y = Mathf.Abs(v.y) + 1f;
                Emit(I.puffs, pos + Random.insideUnitSphere * 0.5f * scale, v,
                     Color.Lerp(c, Color.white, Random.Range(0.1f, 0.5f)), Random.Range(0.6f, 1.3f) * scale, Random.Range(0.5f, 0.9f));
            }
            for (int i = 0; i < n; i++)
            {
                Vector3 v = Random.onUnitSphere * Random.Range(3f, 9f) * Mathf.Sqrt(scale);
                v.y = Mathf.Abs(v.y) + 3f;
                Emit(I.sparks, pos, v, Color.Lerp(c, Color.white, 0.4f), Random.Range(0.12f, 0.28f), Random.Range(0.4f, 0.8f));
            }
        }

        /// <summary>Sclipiri (loot, monede, forja reusita).</summary>
        public static void Sparkle(Vector3 pos, Color c, int n = 8, float radius = 0.6f)
        {
            if (I == null) return;
            for (int i = 0; i < n; i++)
            {
                Vector3 v = Random.insideUnitSphere * 1.5f; v.y = Mathf.Abs(v.y) * 2f + 1f;
                Emit(I.stars, pos + Random.insideUnitSphere * radius, v, Color.Lerp(c, Color.white, 0.3f), Random.Range(0.35f, 0.7f), Random.Range(0.4f, 0.8f));
            }
        }

        /// <summary>Inel care se extinde pe sol (level up, rebirth, portal).</summary>
        public static void Ring(Vector3 pos, Color c, float radius, int n = 40)
        {
            if (I == null) return;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Emit(I.rings, pos + dir * 0.5f + Vector3.up * 0.3f, dir * radius * 1.6f + Vector3.up * 0.8f, c, 0.55f, 0.65f);
                if (i % 2 == 0)
                    Emit(I.stars, pos + dir * 0.8f + Vector3.up * 0.5f, dir * 1.2f + Vector3.up * Random.Range(3f, 7f), Color.Lerp(c, Color.white, 0.5f), 0.5f, 0.9f);
            }
        }

        /// <summary>Fantana de scantei in sus (forja, hatch).</summary>
        public static void Fountain(Vector3 pos, Color c, int n = 30)
        {
            if (I == null) return;
            for (int i = 0; i < n; i++)
            {
                Vector3 v = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(7f, 13f), Random.Range(-2.5f, 2.5f));
                Emit(I.sparks, pos, v, Color.Lerp(c, Color.white, Random.value * 0.5f), Random.Range(0.18f, 0.4f), Random.Range(0.7f, 1.2f));
            }
        }

        /// <summary>Stalp de lumina care apare si se stinge (level up, boss, drop legendar).</summary>
        public static void Pillar(Vector3 pos, Color c, float height, float dur)
        {
            if (I == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Pillar";
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Art.GlowMaterial(Art.A(c, 0.55f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.position = pos + Vector3.up * height * 0.5f;
            var f = go.AddComponent<PillarFade>();
            f.col = c; f.dur = dur; f.height = height;
        }

        /// <summary>Particula ambientala (polen, praf, fulgi de zapada, confetti).</summary>
        public static void Ambient(Vector3 pos, Vector3 vel, Color c, float size, float life, bool glow)
        {
            if (I == null) return;
            Emit(glow ? I.ambientGlow : I.ambient, pos, vel, c, size, life);
        }

        /// <summary>Tremurat de camera (se aduna, scade rapid).</summary>
        public static void Shake(float amount)
        {
            if (I == null) return;
            I.shake = Mathf.Min(1.2f, I.shake + amount);
        }

        public static Vector3 ShakeOffset()
        {
            if (I == null || I.shake <= 0.001f) return Vector3.zero;
            float s = I.shake * I.shake;
            float t = Time.unscaledTime * 38f;
            return new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f) * s * 1.6f;
        }

        /// <summary>Pauza scurta (impact greu) - da "greutate" loviturilor.</summary>
        public static void HitStop(float seconds)
        {
            if (I == null || I.stopping || Time.timeScale < 0.99f) return;
            I.StartCoroutine(I.HitStopRoutine(seconds));
        }

        IEnumerator HitStopRoutine(float s)
        {
            stopping = true;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(s);
            Time.timeScale = 1f;
            stopping = false;
        }

        void Update()
        {
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 2.2f);
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
        }
    }
}
