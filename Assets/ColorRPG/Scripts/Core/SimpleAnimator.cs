// =====================================================================
//  SimpleAnimator.cs - reda animatiile incluse in FBX-urile Kenney
//  fara Animator Controller (foloseste Playables API + crossfade).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ColorRPG
{
    public class SimpleAnimator : MonoBehaviour
    {
        static readonly Dictionary<string, Dictionary<string, AnimationClip>> clipCache =
            new Dictionary<string, Dictionary<string, AnimationClip>>();

        Dictionary<string, AnimationClip> clips;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable cur, prev;
        bool hasCur, hasPrev, curLoop;
        float fadeT, fadeDur;
        string curName = "";
        public float Speed = 1f;

        public string Current { get { return curName; } }

        /// <summary>Ataseaza animatorul pe un model instantiat. resPath = calea Resources a FBX-ului.</summary>
        public static SimpleAnimator Setup(Transform model, string resPath)
        {
            if (model == null) return null;
            var map = Clips(resPath);
            if (map.Count == 0) return null;

            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                // Animatorul trebuie pus pe radacina ierarhiei importate (parintele oaselor)
                Transform root = model.childCount > 0 ? model.GetChild(0) : model;
                animator = root.gameObject.AddComponent<Animator>();
            }
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = null;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var sa = animator.gameObject.AddComponent<SimpleAnimator>();
            sa.Init(animator, map);
            return sa;
        }

        public static Dictionary<string, AnimationClip> Clips(string resPath)
        {
            Dictionary<string, AnimationClip> map;
            if (clipCache.TryGetValue(resPath, out map)) return map;
            map = new Dictionary<string, AnimationClip>();
            var all = Resources.LoadAll<AnimationClip>(resPath);
            foreach (var c in all)
            {
                if (c == null || c.name.StartsWith("__preview__")) continue;
                string n = c.name;
                int bar = n.LastIndexOf('|');
                if (bar >= 0) n = n.Substring(bar + 1);
                n = n.ToLowerInvariant();
                if (!map.ContainsKey(n)) map[n] = c;
            }
            clipCache[resPath] = map;
            return map;
        }

        void Init(Animator animator, Dictionary<string, AnimationClip> map)
        {
            clips = map;
            graph = PlayableGraph.Create("ColorRPG_" + name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            var output = AnimationPlayableOutput.Create(graph, "anim", animator);
            output.SetSourcePlayable(mixer);
            graph.Play();
        }

        public bool Has(string n) { return clips != null && clips.ContainsKey(n); }

        public float Length(string n)
        {
            AnimationClip c;
            return clips != null && clips.TryGetValue(n, out c) ? c.length : 0f;
        }

        /// <summary>Porneste o animatie. Intoarce false daca nu exista.</summary>
        public bool Play(string n, float fade = 0.15f, bool loop = true, bool restart = false)
        {
            if (clips == null || !graph.IsValid()) return false;
            AnimationClip clip;
            if (!clips.TryGetValue(n, out clip)) return false;
            if (!restart && n == curName && hasCur) { curLoop = loop; return true; }

            // arunca vechiul "prev"
            if (hasPrev)
            {
                mixer.DisconnectInput(1);
                if (prev.IsValid()) prev.Destroy();
                hasPrev = false;
            }
            // cur -> prev
            if (hasCur)
            {
                mixer.DisconnectInput(0);
                prev = cur;
                hasPrev = true;
                graph.Connect(prev, 0, mixer, 1);
            }

            cur = AnimationClipPlayable.Create(graph, clip);
            cur.SetApplyFootIK(false);
            cur.SetTime(0);
            graph.Connect(cur, 0, mixer, 0);
            hasCur = true;
            curLoop = loop;
            curName = n;
            fadeDur = Mathf.Max(0.0001f, fade);
            fadeT = hasPrev ? 0f : fadeDur;
            ApplyWeights();
            return true;
        }

        /// <summary>Animatia curenta (non-loop) s-a terminat?</summary>
        public bool Done
        {
            get
            {
                if (!hasCur || !cur.IsValid()) return true;
                var c = cur.GetAnimationClip();
                return c == null || cur.GetTime() >= c.length - 0.02f;
            }
        }

        void ApplyWeights()
        {
            float k = Mathf.Clamp01(fadeT / fadeDur);
            mixer.SetInputWeight(0, hasCur ? k : 0f);
            mixer.SetInputWeight(1, hasPrev ? 1f - k : 0f);
        }

        void Update()
        {
            if (!graph.IsValid()) return;
            float dt = Time.deltaTime;
            if (hasCur && cur.IsValid())
            {
                cur.SetSpeed(Speed);
                var c = cur.GetAnimationClip();
                if (c != null && c.length > 0f)
                {
                    double t = cur.GetTime();
                    if (curLoop && t >= c.length) cur.SetTime(t % c.length);
                    else if (!curLoop && t > c.length) cur.SetTime(c.length);
                }
            }
            if (hasPrev && prev.IsValid())
            {
                var pc = prev.GetAnimationClip();
                if (pc != null && pc.length > 0f && prev.GetTime() >= pc.length) prev.SetTime(prev.GetTime() % pc.length);
            }
            if (fadeT < fadeDur)
            {
                fadeT += dt;
                ApplyWeights();
                if (fadeT >= fadeDur && hasPrev)
                {
                    mixer.DisconnectInput(1);
                    if (prev.IsValid()) prev.Destroy();
                    hasPrev = false;
                    ApplyWeights();
                }
            }
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
