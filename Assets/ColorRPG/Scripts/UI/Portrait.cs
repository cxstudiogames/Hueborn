// =====================================================================
//  Portrait.cs - portretul eroului (randat live intr-o RenderTexture)
//  O copie a modelului sta intr-un "studio" ascuns sub lume, pe layer-ul 31.
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public class Portrait : MonoBehaviour
    {
        public const int Layer = 31;
        static readonly Vector3 Stage = new Vector3(0f, -500f, 0f);
        /// <summary>Portretul animat (fereastra CHARACTER).</summary>
        public RenderTexture Texture { get; private set; }
        /// <summary>Poza statica (coltul din stanga-sus). Se reface doar cand schimbi eroul.</summary>
        public Texture2D Snapshot { get; private set; }
        int snapFrames = -1;
        Camera cam;
        Transform holder, model;
        SimpleAnimator anim;
        int shownHero = -1;
        // avatarul din INVENTORY: o copie a eroului din joc, cu armele / coiful / armura echipate
        Transform gearClone;
        string gearKey = "";
        SimpleAnimator gearAnim;
        float emoteT = 4f;

        public static Portrait Create()
        {
            var go = new GameObject("PortraitStudio");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            var p = go.AddComponent<Portrait>();
            p.Build();
            return p;
        }

        void Build()
        {
            Texture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            Texture.name = "PortraitRT";
            Snapshot = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Snapshot.name = "PortraitSnapshot";
            Snapshot.SetPixels32(new Color32[256 * 256]);
            Snapshot.Apply();
            var cgo = new GameObject("PortraitCam");
            cgo.transform.SetParent(transform, false);
            cam = cgo.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.targetTexture = Texture;
            cam.depth = -20;
            cam.transform.position = Stage + new Vector3(0, 1.55f, 4.6f);
            cam.transform.LookAt(Stage + new Vector3(0, 1.25f, 0));
            try
            {
                var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
                if (data != null) { data.renderPostProcessing = false; data.renderShadows = false; }
            }
            catch (System.Exception) { }

            // lumina proprie (doar pentru layer-ul portretului)
            var lg = new GameObject("PortraitLight");
            lg.transform.SetParent(transform, false);
            lg.transform.position = Stage + new Vector3(1.5f, 3f, 3f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 10f;
            l.intensity = 2.2f;
            l.cullingMask = 1 << Layer;
            l.color = new Color(1f, 0.95f, 0.9f);

            holder = new GameObject("Holder").transform;
            holder.SetParent(transform, false);
            holder.position = Stage;
            holder.rotation = Quaternion.identity;   // modelul priveste spre +Z = spre camera
        }

        void AddLight(Vector3 local, float intensity, Color c)
        {
            var lg = new GameObject("PortraitFill");
            lg.transform.SetParent(transform, false);
            lg.transform.position = Stage + local;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 9f;
            l.intensity = intensity;
            l.cullingMask = 1 << Layer;
            l.color = c;
            l.shadows = LightShadows.None;
        }

        static Material uiMat;
        /// <summary>Materialul UI pentru portret: contrast + saturatie ca in joc (portretul nu are post-processing).</summary>
        public static Material UIMaterial
        {
            get
            {
                if (uiMat == null)
                {
                    var sh = Resources.Load<Shader>("Shaders/UIPortrait");
                    if (sh != null) uiMat = new Material(sh) { name = "UIPortrait" };
                }
                return uiMat;
            }
        }

        public void Refresh(int hero)
        {
            if (hero == shownHero && model != null) return;
            shownHero = hero;
            if (model != null) Destroy(model.gameObject);
            string path = GameData.CHAR + GameData.Heroes[Mathf.Clamp(hero, 0, GameData.Heroes.Length - 1)];
            model = ModelLib.Spawn(path, holder, 2.1f, 0f, false, false);
            if (model == null)
            {
                model = new GameObject("Fallback").transform;
                model.SetParent(holder, false);
                Art.Prim(PrimitiveType.Capsule, new Vector3(0, 1, 0), Vector3.one, new Color(0.2f, 0.55f, 1f), model, false);
            }
            SetLayer(model, Layer);
            anim = SimpleAnimator.Setup(model, path);
            if (anim != null) anim.Play("idle");
            snapFrames = 4;   // asteptam cateva frame-uri ca poza "idle" sa se aplice
        }

        /// <summary>Randeaza o singura data si copiaza imaginea in Snapshot.</summary>
        void TakeSnapshot()
        {
            holder.rotation = Quaternion.identity;
            cam.transform.position = Stage + new Vector3(0, 1.55f, 4.6f);
            cam.transform.LookAt(Stage + new Vector3(0, 1.25f, 0));
            bool gearOn = gearClone != null && gearClone.gameObject.activeSelf;
            if (gearOn) gearClone.gameObject.SetActive(false);
            if (model != null) model.gameObject.SetActive(true);
            cam.Render();
            if (gearOn) { gearClone.gameObject.SetActive(true); if (model != null) model.gameObject.SetActive(false); }
            var prev = RenderTexture.active;
            RenderTexture.active = Texture;
            Snapshot.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            Snapshot.Apply();
            RenderTexture.active = prev;
        }

        /// <summary>In INVENTORY aratam eroul cu tot echipamentul (copie a modelului din joc).</summary>
        void UpdateGearClone(Player pl, bool on)
        {
            // in INVENTORY se vede tot corpul (cu armele), in CHARACTER doar bustul
            cam.transform.position = Stage + (on ? new Vector3(0, 1.15f, 5.9f) : new Vector3(0, 1.55f, 4.6f));
            cam.transform.LookAt(Stage + new Vector3(0, on ? 1.08f : 1.25f, 0));
            if (model != null) model.gameObject.SetActive(!on || gearClone == null);
            if (!on)
            {
                if (gearClone != null) gearClone.gameObject.SetActive(false);
                return;
            }
            string key = pl.GearKey;
            if (gearClone == null || key != gearKey)
            {
                gearKey = key;
                if (gearClone != null) Destroy(gearClone.gameObject);
                gearClone = null; gearAnim = null;
                var src = pl.ModelRoot;
                if (src == null) return;
                gearClone = Instantiate(src.gameObject, holder).transform;
                gearClone.name = "GearPreview";
                gearClone.localPosition = Vector3.zero;
                gearClone.localRotation = Quaternion.identity;
                gearClone.localScale = Vector3.one;
                // fara efecte (trail, particule) si fara coliziuni in studio
                foreach (var tr in gearClone.GetComponentsInChildren<TrailRenderer>(true)) Destroy(tr);
                foreach (var ps in gearClone.GetComponentsInChildren<ParticleSystem>(true)) Destroy(ps.gameObject);
                foreach (var c in gearClone.GetComponentsInChildren<Collider>(true)) Destroy(c);
                SetLayer(gearClone, Layer);
                // animatorul copiat nu are graful de animatie -> il refacem pe copie
                foreach (var sa in gearClone.GetComponentsInChildren<SimpleAnimator>(true)) Destroy(sa);
                string path = GameData.CHAR + GameData.Heroes[Mathf.Clamp(pl.D.hero, 0, GameData.Heroes.Length - 1)];
                gearAnim = SimpleAnimator.Setup(gearClone, path);
                if (gearAnim != null) { gearAnim.Speed = 1f; gearAnim.Play("idle", 0f); }
                if (model != null) model.gameObject.SetActive(false);
            }
            gearClone.gameObject.SetActive(true);
            if (gearAnim != null)
            {
                gearAnim.Speed = 1f;
                if (gearAnim.Current != "idle" && gearAnim.Done) gearAnim.Play("idle", 0.25f);
            }
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }

        void Update()
        {
            var g = Game.I;
            if (g == null || g.player == null) return;
            Refresh(g.player.D.hero);
            if (snapFrames > 0 && --snapFrames == 0) TakeSnapshot();

            // animat doar cat timp e deschisa fereastra CHARACTER sau INVENTORY
            bool inv = g.hud != null && g.hud.inventory != null && g.hud.inventory.IsOpen;
            bool live = inv || (g.hud != null && g.hud.character != null && g.hud.character.IsOpen);
            cam.enabled = live && (Time.frameCount % 2) == 0;
            UpdateGearClone(g.player, inv);
            if (!live)
            {
                if (anim != null && anim.Current != "idle") anim.Play("idle", 0.1f);
                return;
            }
            // mica rotatie + emote din cand in cand
            holder.rotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 0.6f) * 18f, 0);
            emoteT -= Time.deltaTime;
            if (anim != null)
            {
                if (emoteT <= 0f) { emoteT = Random.Range(6f, 11f); anim.Play("emote-yes", 0.2f, false, true); }
                if (anim.Current != "idle" && anim.Done) anim.Play("idle", 0.25f);
            }
        }
    }
}
