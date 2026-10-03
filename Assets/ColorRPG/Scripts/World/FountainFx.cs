// FountainFx.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
// Apa care curge din fantana: un jet care urca si cade inapoi in bazin + stropi la suprafata apei.
// Sistemele de particule se construiesc la Play (nu se salveaza nimic in scena).
using UnityEngine;

namespace ColorRPG
{
    public class FountainFx : MonoBehaviour
    {
        public Color water = new Color(0.62f, 0.86f, 1f, 0.85f);
        [Tooltip("Inaltimea suprafetei apei fata de obiect (jos = negativ)")]
        public float waterLevel = -1.55f;
        public float basinRadius = 2.6f;

        void Start()
        {
            // fantana a fost scoasa din scena (a ramas doar apa care "curge in sus"): apa dispare si ea
            var par = transform.parent;
            bool hasModel = false;
            if (par != null) foreach (var r in par.GetComponentsInChildren<Renderer>(true)) if (!(r is ParticleSystemRenderer)) { hasModel = true; break; }
            if (!hasModel) { Destroy(gameObject); return; }
            // jetul central: picaturi aruncate in sus care cad cu gravitatie
            var jet = MakeSystem("Jet", Vector3.zero);
            var m = jet.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(6f, 7.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
            m.gravityModifier = 1.25f;
            m.maxParticles = 700;
            var em = jet.emission; em.rateOverTime = 150f;
            var sh = jet.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 11f; sh.radius = 0.12f;
            sh.rotation = new Vector3(-90f, 0, 0);

            // stropi pe suprafata apei (unde cad picaturile) + mici unde
            var splash = MakeSystem("Splash", new Vector3(0, waterLevel, 0));
            var m2 = splash.main;
            m2.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            m2.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            m2.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            m2.gravityModifier = 0.8f;
            m2.maxParticles = 300;
            var em2 = splash.emission; em2.rateOverTime = 70f;
            var sh2 = splash.shape; sh2.shapeType = ParticleSystemShapeType.Circle; sh2.radius = basinRadius * 0.7f; sh2.radiusThickness = 0.5f;
            sh2.rotation = new Vector3(-90f, 0, 0);
        }

        ParticleSystem MakeSystem(string n, Vector3 local)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startColor = new ParticleSystem.MinMaxGradient(water, Color.Lerp(water, Color.white, 0.5f));
            m.playOnAwake = true;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = Art.FxMaterial(false);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }
    }
}
