// Shopper.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    // ------------------------------------------------------------------
    //  Cumparator care se plimba prin piata
    // ------------------------------------------------------------------
    public class Shopper : MonoBehaviour
    {
        public string modelPath;
        public int idx;
        SimpleAnimator anim;
        Vector3 goal;
        float wait;
        bool excited;
        bool shown = true;

        /// <summary>Pe server sunt doar "players - 1" jucatori in afara de tine: restul se ascund.</summary>
        void UpdateVisible()
        {
            var ts = TradeSystem.I;
            bool want = !Mp.Online && (ts == null || ts.Server == null || idx < ts.Server.players - 1);
            if (want == shown) return;
            shown = want;
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(want);
        }

        /// <summary>Pune modelul (merge si in editor, cand construim scena).</summary>
        public void Init(string hero, int i)
        {
            idx = i;
            modelPath = GameData.CHAR + hero;
            var m = ModelLib.Spawn(modelPath, transform, 1.8f + (i % 4) * 0.1f);
            if (m == null) Art.Prim(PrimitiveType.Capsule, new Vector3(0, 1, 0), Vector3.one, Color.HSVToRGB((i * 0.13f) % 1f, 0.5f, 1f), transform, false);
        }

        void Start()
        {
            anim = SceneAnim.Attach(transform, modelPath);
            if (anim != null) anim.Play("idle");
            PickGoal();
            if (TradeSystem.I != null) TradeSystem.I.OnSold += OnSold;
        }

        void OnDestroy() { if (TradeSystem.I != null) TradeSystem.I.OnSold -= OnSold; }

        void OnSold(BoothListing l)
        {
            // un singur cumparator "vine" la booth-ul tau
            if (!shown || idx != (Time.frameCount % 12)) return;
            goal = TradeSystem.I.playerBoothPos + (Game.I.player.transform.position - TradeSystem.I.playerBoothPos).normalized * 0f + new Vector3(Random.Range(-1f, 1f), 0, 2.6f);
            wait = 0f;
            excited = true;
        }

        /// <summary>Raza fantanii din centrul pietei (jucatorii simulati nu trec prin ea).</summary>
        public const float FountainR = 4.4f;

        void PickGoal()
        {
            var spots = PlazaBuilder.BoothSpots;
            if (spots.Count == 0)
                foreach (var it in Interactable.All)
                    if (it != null && it.kind == InteractKind.PlazaBooth) spots.Add(it.transform);
            spots.RemoveAll(x => x == null);
            if (spots.Count == 0) { goal = PlazaBuilder.Origin; return; }
            var b = spots[Random.Range(0, spots.Count)];
            goal = b.position + b.forward * Random.Range(2.4f, 3.6f) + b.right * Random.Range(-1.5f, 1.5f);
            if (Random.value < 0.25f) goal = PlazaBuilder.Origin + new Vector3(Random.Range(-18f, 18f), 0, Random.Range(-18f, 18f));
            // nu prin fantana
            Vector3 gr = goal - PlazaBuilder.Origin; gr.y = 0;
            if (gr.magnitude < FountainR + 0.8f) goal = PlazaBuilder.Origin + (gr.sqrMagnitude > 0.01f ? gr.normalized : Vector3.forward) * (FountainR + 1.5f);
        }

        void Update()
        {
            var g = Game.I;
            if (g == null || !g.InPlaza) return;  // doarme cand nu esti in piata
            UpdateVisible();
            if (!shown) return;
            float dt = Time.deltaTime;
            Vector3 to = goal - transform.position; to.y = 0;
            if (to.magnitude > 0.4f)
            {
                // ocoleste fantana din centru (mergi pe langa ea, nu prin ea)
                Vector3 dir = to.normalized;
                Vector3 rel = transform.position - PlazaBuilder.Origin; rel.y = 0;
                float rd = rel.magnitude;
                if (rd < FountainR + 3f && rd > 0.01f && Vector3.Dot(dir, -rel / rd) > 0f)
                {
                    Vector3 tan = new Vector3(-rel.z, 0, rel.x) / rd;
                    if (Vector3.Dot(tan, dir) < 0f) tan = -tan;
                    dir = Vector3.Lerp(dir, tan, Mathf.Clamp01((FountainR + 3f - rd) / 3f)).normalized;
                }
                Vector3 np = transform.position + dir * (excited ? 5.5f : 3f) * dt;
                Vector3 nr = np - PlazaBuilder.Origin; nr.y = 0;
                if (nr.magnitude < FountainR) np = new Vector3(PlazaBuilder.Origin.x, np.y, PlazaBuilder.Origin.z) + nr.normalized * FountainR;
                transform.position = np;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-8f * dt));
                if (anim != null) { anim.Play(excited && anim.Has("sprint") ? "sprint" : "walk", 0.15f); anim.Speed = 1.2f; }
            }
            else
            {
                if (wait <= 0f)
                {
                    wait = Random.Range(3f, 8f);
                    if (excited)
                    {
                        excited = false;
                        if (anim != null) anim.Play("emote-yes", 0.1f, false, true);
                        g.hud.Pop(transform.position + Vector3.up * 2.6f, "!", new Color(1f, 0.9f, 0.3f), 1.4f);
                    }
                    else if (anim != null) anim.Play(Random.value < 0.4f ? "interact-right" : "idle", 0.2f, false, true);
                }
                wait -= dt;
                if (anim != null && anim.Done && anim.Current != "idle") anim.Play("idle", 0.2f);
                if (wait <= 0f) PickGoal();
            }
        }
    }
}
