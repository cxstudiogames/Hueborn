// =====================================================================
//  PetStudio.cs - "studioul" pentru pets (ascuns sub lume, layer 31):
//   * Preview(pet)  -> RenderTexture cu pet-ul ANIMAT (fereastra PETS, dreapta),
//                      la fel ca avatarul din INVENTORY
//   * Icon(pet)     -> poza statica a pet-ului (pentru grila din PETS), in cache
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class PetStudio : MonoBehaviour
    {
        const int Layer = 31;
        static readonly Vector3 Stage = new Vector3(0f, -1100f, 0f);
        static PetStudio I;
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; icons.Clear(); }

        Camera cam;
        RenderTexture liveRT, iconRT;
        Transform liveHolder;
        Transform liveModel;
        SimpleAnimator liveAnim;
        string liveKey = "";
        List<string> extraClips = new List<string>();
        float emoteT, liveTime;
        bool liveWanted;

        static PetStudio Get()
        {
            if (I != null) return I;
            var go = new GameObject("PetStudio");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            go.transform.position = Stage;
            I = go.AddComponent<PetStudio>();
            I.Build();
            return I;
        }

        void Build()
        {
            liveRT = new RenderTexture(320, 320, 16, RenderTextureFormat.ARGB32) { name = "PetPreviewRT", antiAliasing = 4 };
            iconRT = new RenderTexture(128, 128, 16, RenderTextureFormat.ARGB32) { name = "PetIconRT", antiAliasing = 4 };
            var cgo = new GameObject("PetCam");
            cgo.transform.SetParent(transform, false);
            cam = cgo.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            try
            {
                var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
                if (data != null) { data.renderPostProcessing = false; data.renderShadows = false; }
            }
            catch (System.Exception) { }
            AddLight(new Vector3(2f, 4f, 4f), 3f, new Color(1f, 0.96f, 0.9f));
            AddLight(new Vector3(-3.5f, 1.5f, 2.5f), 1.4f, new Color(0.75f, 0.85f, 1f));
            AddLight(new Vector3(0f, 3f, -4f), 1.2f, Color.white);
            liveHolder = new GameObject("LiveHolder").transform;
            liveHolder.SetParent(transform, false);
        }

        void AddLight(Vector3 local, float intensity, Color c)
        {
            var lg = new GameObject("Light");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = local;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = 14f; l.intensity = intensity; l.color = c;
            l.cullingMask = 1 << Layer; l.shadows = LightShadows.None;
        }

        static Transform SpawnPet(PetData pd, Transform parent, out string path)
        {
            path = GameData.PETS + pd.model;
            var m = ModelLib.Spawn(path, parent, 1f, 0f, false, false);
            if (m == null)
            {
                m = new GameObject("Fallback").transform;
                m.SetParent(parent, false);
                Art.Prim(PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), new Vector3(0.9f, 0.8f, 0.9f), ItemDB.Col(pd.rarity), m, false);
            }
            foreach (var c in m.GetComponentsInChildren<Collider>(true)) Destroy(c);
            SetLayer(m, Layer);
            return m;
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        /// <summary>Pune camera sa incadreze modelul (din fata, putin de sus, ca in inventar).</summary>
        void Frame(Transform model, float pad)
        {
            Bounds b = new Bounds(model.position, Vector3.one * 0.5f);
            bool any = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            float size = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
            float dist = size * pad / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 dir = new Vector3(0.35f, 0.32f, 1f).normalized;
            cam.transform.position = b.center + dir * dist;
            cam.transform.LookAt(b.center);
        }

        // ---------------------------------------------------------------
        //  Preview animat
        // ---------------------------------------------------------------
        /// <summary>Textura cu pet-ul animat (null = ascunde preview-ul).</summary>
        public static RenderTexture Preview(PetData pd)
        {
            var s = Get();
            s.liveWanted = pd != null;
            if (pd == null) { s.SetLive(null); return null; }
            s.SetLive(pd);
            return s.liveRT;
        }

        public static void StopPreview() { if (I != null) { I.liveWanted = false; I.SetLive(null); } }

        void SetLive(PetData pd)
        {
            string key = pd == null ? "" : pd.model + "_" + pd.rarity;
            if (key == liveKey) return;
            liveKey = key;
            if (liveModel != null) Destroy(liveModel.gameObject);
            liveModel = null; liveAnim = null; extraClips.Clear();
            if (pd == null) return;
            string path;
            liveModel = SpawnPet(pd, liveHolder, out path);
            liveAnim = SimpleAnimator.Setup(liveModel, path);
            if (liveAnim != null)
            {
                liveAnim.Play("idle", 0f);
                var clips = SimpleAnimator.Clips(path);
                if (clips != null)
                    foreach (var k in clips.Keys)
                        if (k != "idle" && k != "static" && !k.Contains("death") && !k.Contains("die")) extraClips.Add(k);
            }
            emoteT = 1.2f; liveTime = 0f;
        }

        void LateUpdate()
        {
            if (!liveWanted || liveModel == null) return;
            liveTime += Time.unscaledDeltaTime;
            // se roteste incet + din cand in cand face o animatie (mers, saritura...)
            liveHolder.localRotation = Quaternion.Euler(0, Mathf.Sin(liveTime * 0.7f) * 35f, 0);
            if (liveAnim != null)
            {
                emoteT -= Time.unscaledDeltaTime;
                if (emoteT <= 0f && extraClips.Count > 0)
                {
                    emoteT = Random.Range(2.5f, 4.5f);
                    liveAnim.Play(extraClips[Random.Range(0, extraClips.Count)], 0.2f, false, true);
                }
                if (liveAnim.Current != "idle" && liveAnim.Done) liveAnim.Play("idle", 0.25f);
            }
            liveModel.gameObject.SetActive(true);
            cam.targetTexture = liveRT;
            Frame(liveModel, 0.95f);
            cam.Render();
        }

        // ---------------------------------------------------------------
        //  Iconite statice (grila)
        // ---------------------------------------------------------------
        public static Sprite Icon(PetData pd)
        {
            if (pd == null) return null;
            string key = pd.model;
            Sprite sp;
            if (icons.TryGetValue(key, out sp) && sp != null) return sp;
            try { sp = Get().RenderIcon(pd); }
            catch (System.Exception e) { Debug.LogWarning("PetStudio: " + e.Message); sp = null; }
            if (sp != null) { sp.name = "art_pet_" + key; icons[key] = sp; }
            return sp;
        }

        Sprite RenderIcon(PetData pd)
        {
            bool liveOn = liveModel != null && liveModel.gameObject.activeSelf;
            if (liveModel != null) liveModel.gameObject.SetActive(false);
            var holder = new GameObject("IconHolder").transform;
            holder.SetParent(transform, false);
            holder.localPosition = new Vector3(0, 0, 0);
            string path;
            var m = SpawnPet(pd, holder, out path);
            Frame(m, 0.9f);
            cam.targetTexture = iconRT;
            cam.Render();
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            GearIcons.ReadBack(iconRT, tex);
            tex.wrapMode = TextureWrapMode.Clamp;
            DestroyImmediate(holder.gameObject);
            if (liveModel != null) liveModel.gameObject.SetActive(liveOn);
            return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
