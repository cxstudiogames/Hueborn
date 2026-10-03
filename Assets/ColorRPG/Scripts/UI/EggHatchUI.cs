// =====================================================================
//  EggHatchUI.cs - animatia de deschidere a oului, pe tot ecranul:
//  oul 3D se clatina tot mai tare, apar crapaturi, BLITZ alb, coaja zboara
//  in bucati si apare animalutul castigat (sare, se roteste) cu raritatea lui.
//  Oul si pet-ul sunt randate intr-un "studio" ascuns (layer 31) intr-o RenderTexture.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class EggHatchUI : MonoBehaviour
    {
        const int Layer = Portrait.Layer;
        static readonly Vector3 Stage = new Vector3(0f, -620f, 0f);
        const float CrackTime = 2.0f;   // cat se clatina oul pana se sparge

        Game game;
        RectTransform dim;
        RawImage view;
        Image flash, rays;
        Text title, sub, hint;
        Camera cam;
        RenderTexture rt;
        Transform studio, egg, pet;
        readonly List<GameObject> cracks = new List<GameObject>();
        readonly List<Transform> shells = new List<Transform>();
        readonly List<Vector3> shellVel = new List<Vector3>();
        PetData shown;
        float t;
        bool broke;
        SimpleAnimator petAnim;

        public bool IsOpen { get { return dim != null && dim.gameObject.activeSelf; } }

        public static EggHatchUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("EggHatchUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<EggHatchUI>();
            ui.game = g;
            ui.Build(parent);
            return ui;
        }

        void Build(RectTransform parent)
        {
            Vector2 C = new Vector2(0.5f, 0.5f);
            dim = UIKit.Stretch(parent, "HatchDim");
            var di = dim.gameObject.AddComponent<Image>();
            di.color = new Color(0.02f, 0.02f, 0.05f, 0.82f);
            di.raycastTarget = true;
            var btn = dim.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Skip);

            // raze care se rotesc in spatele pet-ului
            var rr = UIKit.Rect(dim, "Rays", C, C, new Vector2(0, 40), new Vector2(1100, 1100));
            rays = rr.gameObject.AddComponent<Image>();
            rays.sprite = UIKit.FromTexture(RaysTexture());
            rays.raycastTarget = false;

            rt = new RenderTexture(640, 640, 16, RenderTextureFormat.ARGB32) { name = "HatchRT" };
            var vr = UIKit.Rect(dim, "View", C, C, new Vector2(0, 40), new Vector2(640, 640));
            view = vr.gameObject.AddComponent<RawImage>();
            view.texture = rt;
            view.raycastTarget = false;

            title = UIKit.Text(dim, "Title", "", 72, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, -330), new Vector2(1600, 100));
            sub = UIKit.Text(dim, "Sub", "", 32, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, -400), new Vector2(1600, 50));
            sub.supportRichText = true;
            hint = UIKit.Text(dim, "Hint", "", 22, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, C, C, new Vector2(0, -460), new Vector2(1200, 34));

            var fr = UIKit.Stretch(dim, "Flash");
            flash = fr.gameObject.AddComponent<Image>();
            flash.color = new Color(1, 1, 1, 0);
            flash.raycastTarget = false;

            // studio 3D
            studio = new GameObject("HatchStudio").transform;
            studio.SetParent(SceneGroups.Get("Systems"), false);
            studio.position = Stage;
            var cgo = new GameObject("HatchCam");
            cgo.transform.SetParent(studio, false);
            cam = cgo.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
            cam.targetTexture = rt;
            cam.depth = -21;
            cam.transform.position = Stage + new Vector3(0, 1.4f, 6.2f);
            cam.transform.LookAt(Stage + new Vector3(0, 1.1f, 0));
            try
            {
                var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
                if (data != null) { data.renderPostProcessing = false; data.renderShadows = false; }
            }
            catch (System.Exception) { }
            cam.enabled = false;
            var lg = new GameObject("HatchLight");
            lg.transform.SetParent(studio, false);
            lg.transform.position = Stage + new Vector3(2f, 4f, 4f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = 14f; l.intensity = 3f; l.cullingMask = 1 << Layer;
            var lg2 = new GameObject("HatchFill");
            lg2.transform.SetParent(studio, false);
            lg2.transform.position = Stage + new Vector3(-3f, 2f, 3f);
            var l2 = lg2.AddComponent<Light>();
            l2.type = LightType.Point; l2.range = 12f; l2.intensity = 1.2f; l2.cullingMask = 1 << Layer; l2.color = new Color(0.7f, 0.8f, 1f);

            dim.gameObject.SetActive(false);
        }

        static Texture2D RaysTexture()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "HatchRays", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                    float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 6f)), 6f);
                    float fall = Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r);
                    float glow = Mathf.Exp(-r * r * 9f);
                    float al = Mathf.Clamp01(ray * fall * 0.7f + glow * 0.8f);
                    px[y * N + x] = new Color(1f, 1f, 1f, al);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static void SetLayer(Transform tr, int layer)
        {
            tr.gameObject.layer = layer;
            for (int i = 0; i < tr.childCount; i++) SetLayer(tr.GetChild(i), layer);
        }

        // ---------------------------------------------------------------
        public void Play(PetData pd)
        {
            shown = pd;
            Clear();
            Color rc = ItemDB.Col(pd.rarity);
            // oul (sfera alungita + pete colorate)
            egg = new GameObject("Egg").transform;
            egg.SetParent(studio, false);
            egg.position = Stage + new Vector3(0, 1.15f, 0);
            var shell = Art.Prim(PrimitiveType.Sphere, Vector3.zero, new Vector3(1.5f, 1.95f, 1.5f), Color.white, egg, false, false);
            Art.SetEmission(shell.GetComponent<Renderer>().sharedMaterial, new Color(0.12f, 0.12f, 0.16f));
            for (int k = 0; k < 9; k++)
            {
                float dy = new[] { -0.4f, 0.1f, 0.5f }[k % 3];
                float hr = 0.75f * Mathf.Sqrt(1f - dy * dy) - 0.04f;
                float ang = k * 0.7f;
                Art.Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(ang) * hr, dy * 0.97f, Mathf.Sin(ang) * hr), Vector3.one * 0.3f, Color.HSVToRGB(k / 9f, 0.65f, 1f), egg, false, false);
            }
            // crapaturile (apar treptat)
            for (int k = 0; k < 7; k++)
            {
                float ang = (k * 51f + 20f) * Mathf.Deg2Rad;
                float y = Mathf.Lerp(-0.2f, 0.45f, (k % 4) / 3f);
                var c = Art.Prim(PrimitiveType.Cube, new Vector3(Mathf.Cos(ang) * 0.74f, y, Mathf.Sin(ang) * 0.74f), new Vector3(0.05f, 0.42f, 0.05f), new Color(0.08f, 0.06f, 0.05f), egg, false, false);
                c.transform.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang))) * Quaternion.Euler(0, 0, (k % 2 == 0 ? 25f : -30f));
                c.SetActive(false);
                cracks.Add(c);
            }
            SetLayer(egg, Layer);

            // pet-ul (ascuns pana se sparge oul)
            pet = new GameObject("Pet").transform;
            pet.SetParent(studio, false);
            pet.position = Stage;
            string path = GameData.PETS + pd.model;
            var m = ModelLib.Spawn(path, pet, 1.7f, 0f, false, false);
            if (m == null) Art.Prim(PrimitiveType.Sphere, new Vector3(0, 0.8f, 0), Vector3.one * 1.4f, rc, pet, false, false);
            else petAnim = SimpleAnimator.Setup(m, path);
            SetLayer(pet, Layer);
            pet.gameObject.SetActive(false);

            t = 0f; broke = false;
            title.text = ""; sub.text = ""; hint.text = "click to skip";
            rays.color = new Color(1, 1, 1, 0);
            flash.color = new Color(1, 1, 1, 0);
            dim.gameObject.SetActive(true);
            dim.SetAsLastSibling();
            cam.enabled = true;
            Sfx.Play("Open", 0.5f, 0.8f);
        }

        void Clear()
        {
            if (egg != null) Destroy(egg.gameObject);
            if (pet != null) Destroy(pet.gameObject);
            foreach (var s in shells) if (s != null) Destroy(s.gameObject);
            cracks.Clear(); shells.Clear(); shellVel.Clear();
            petAnim = null;
        }

        void Break()
        {
            broke = true;
            Color rc = ItemDB.Col(shown.rarity);
            flash.color = new Color(1, 1, 1, 1);
            // coaja in bucati
            for (int k = 0; k < 10; k++)
            {
                var s = Art.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(0.35f, 0.08f, 0.3f), k % 3 == 0 ? Color.HSVToRGB(k / 10f, 0.6f, 1f) : Color.white, studio, false, false).transform;
                s.position = egg.position + Random.insideUnitSphere * 0.5f;
                s.rotation = Random.rotation;
                s.gameObject.layer = Layer;
                shells.Add(s);
                Vector3 dir = (s.position - egg.position).normalized;
                shellVel.Add(dir * Random.Range(4f, 7f) + Vector3.up * Random.Range(2f, 5f));
            }
            egg.gameObject.SetActive(false);
            pet.gameObject.SetActive(true);
            pet.localScale = Vector3.zero;
            if (petAnim != null) petAnim.Play(petAnim.Has("jump") ? "jump" : "idle", 0.05f, false, true);
            title.text = ItemDB.RarityNames[Mathf.Clamp(shown.rarity, 0, 5)].ToUpper() + "!";
            title.color = rc;
            sub.text = GameData.PetName(shown.model).ToUpper() + "   <color=#ffd84d>+" + Mathf.RoundToInt(Player.PetPower(shown) * 100) + "% damage</color>" + (shown.equipped ? "   <color=#7dff8a>EQUIPPED</color>" : "");
            hint.text = "click to continue";
            Sfx.Play("JingleHatch", 0.7f, 1f, 0f);
            if (shown.rarity >= 3) Sfx.Play("ItemRare", 0.7f);
            if (shown.rarity >= 4) Fx.Shake(0.25f);
        }

        void Skip()
        {
            if (!broke) { t = CrackTime; return; }
            if (t < CrackTime + 0.6f) return;   // nu inchidem din greseala imediat dupa blitz
            Close();
        }

        public void Close()
        {
            dim.gameObject.SetActive(false);
            cam.enabled = false;
            Clear();
            Sfx.Play("Close", 0.5f);
        }

        void Update()
        {
            if (!IsOpen) return;
            float dt = Time.unscaledDeltaTime;
            t += dt;
            if (!broke)
            {
                // se clatina din ce in ce mai tare, crapaturile apar pe rand
                float k = Mathf.Clamp01(t / CrackTime);
                float wob = Mathf.Sin(t * (10f + k * 18f)) * (4f + k * k * 22f);
                egg.localRotation = Quaternion.Euler(0, t * 40f, wob);
                egg.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(t * 9f)) * 0.04f * k);
                int show = Mathf.FloorToInt(k * (cracks.Count + 1));
                for (int i = 0; i < cracks.Count; i++)
                    if (i < show && !cracks[i].activeSelf) { cracks[i].SetActive(true); Sfx.Play("ForgeHit", 0.25f, 1.6f, 0.2f); }
                if (t >= CrackTime) Break();
                return;
            }
            float a = t - CrackTime;
            flash.color = new Color(1, 1, 1, Mathf.Clamp01(1f - a * 2.2f));
            Color rc = ItemDB.Col(shown.rarity);
            if (shown.rarity >= 5) rc = ItemDB.AnimCol(5);
            rays.color = new Color(rc.r, rc.g, rc.b, Mathf.Clamp01(a * 2f) * 0.85f);
            rays.rectTransform.localRotation = Quaternion.Euler(0, 0, -t * 25f);
            rays.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(t * 3f));
            if (shown.rarity >= 5) title.color = rc;
            // pet-ul "sare" afara: scara cu revenire elastica + rotatie lenta
            float s = a < 0.45f ? Mathf.Sin(a / 0.45f * Mathf.PI * 0.5f) * 1.18f : 1f + 0.18f * Mathf.Exp(-(a - 0.45f) * 6f) * Mathf.Cos((a - 0.45f) * 18f);
            pet.localScale = Vector3.one * s;
            pet.localRotation = Quaternion.Euler(0, Mathf.Sin(a * 1.2f) * 35f, 0);
            pet.position = Stage + Vector3.up * Mathf.Max(0f, Mathf.Sin(Mathf.Min(a, 0.6f) / 0.6f * Mathf.PI)) * 0.6f;
            if (petAnim != null && petAnim.Current != "idle" && petAnim.Done) petAnim.Play("idle", 0.2f);
            title.rectTransform.localScale = Vector3.one * (a < 0.25f ? Mathf.Lerp(2f, 1f, a / 0.25f) : 1f + 0.03f * Mathf.Sin(a * 4f));
            for (int i = 0; i < shells.Count; i++)
            {
                if (shells[i] == null) continue;
                shellVel[i] += Vector3.down * 14f * dt;
                shells[i].position += shellVel[i] * dt;
                shells[i].Rotate(400f * dt, 300f * dt, 0);
            }
            if (a > 6f) Close();
            if (Inp.EscapeDown()) Close();
        }
    }
}
