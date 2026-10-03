// =====================================================================
//  IconShot.cs - poze cu eroul (cu tot echipamentul tau) si cu animale,
//  pe fundal transparent, 1024x1024 -> <proiect>/Promo/icon/*.png.
//  Folosite pentru iconita din Google Play si grafica de prezentare.
//  Pornit din editor: ColorRPG > Capture Icon Renders (Play) sau Promo/icon.flag.
// =====================================================================
using System.Collections;
using System.IO;
using UnityEngine;

namespace ColorRPG
{
    public class IconShot : MonoBehaviour
    {
        public static bool Busy { get; private set; }
        const int Size = 1024, Layer = 30;
        static readonly Vector3 Stage = new Vector3(0f, -900f, 0f);

        public static void Begin()
        {
            if (Busy || Game.I == null || Game.I.player == null) return;
            var go = new GameObject("IconShot");
            go.AddComponent<IconShot>().StartCoroutine(go.GetComponent<IconShot>().Scene());
        }

        /// <summary>F9 (admin): poze patrate 1024x1024 din unghiul camerei tale, fara HUD si fara texte -> Promo/icon/snap_*.png</summary>
        public static void Snap()
        {
            if (Busy || Game.I == null || Game.I.player == null) return;
            var go = new GameObject("IconSnap");
            go.AddComponent<IconShot>().StartCoroutine(go.GetComponent<IconShot>().SnapRun());
        }

        IEnumerator SnapRun()
        {
            Busy = true;
            yield return new WaitForEndOfFrame();
            dir = PromoDirector.RendersDir;
            Directory.CreateDirectory(dir);
            var g = Game.I; var pl = g.player;
            var main = g.cam != null ? g.cam : Camera.main;
            rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var cgo = new GameObject("IconSnapCam");
            cam = cgo.AddComponent<Camera>();
            cam.CopyFrom(main);
            cam.targetTexture = rt; cam.enabled = false;
            cam.cullingMask = main.cullingMask & ~(1 << 5);
            try
            {
                var src = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(main);
                var dst = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
                if (src != null && dst != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; dst.volumeLayerMask = src.volumeLayerMask; dst.volumeTrigger = src.volumeTrigger; }
            }
            catch (System.Exception) { }
            var hidden = new System.Collections.Generic.List<Behaviour>();
            foreach (var cv in Object.FindObjectsByType<Canvas>())
                if (cv.enabled && cv.renderMode == RenderMode.WorldSpace) { cv.enabled = false; hidden.Add(cv); }
            string stamp = System.DateTime.Now.ToString("HHmmss");
            Vector3 target = pl.transform.position + Vector3.up * 1.0f;
            float[] fovs = { 18f, 24f, 32f, 42f };
            for (int i = 0; i < fovs.Length; i++)
            {
                cam.transform.position = main.transform.position;
                cam.transform.LookAt(target);
                cam.fieldOfView = fovs[i];
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, "snap_" + stamp + "_" + i + ".png"), tex.EncodeToPNG());
            }
            foreach (var b in hidden) if (b != null) b.enabled = true;
            Destroy(cgo); Destroy(rt); Destroy(tex);
            Busy = false;
            if (g.hud != null) g.hud.Toast("PHOTO SAVED\nPromo/Renders/snap_" + stamp + "_0..3.png", new Color(0.5f, 0.9f, 1f));
            Destroy(gameObject);
        }

        /// <summary>Poze din joc (eroul in lume, cu animalele langa el), fara HUD -> Promo/icon/scene_*.png</summary>
        IEnumerator Scene()
        {
            Busy = true;
            dir = PromoDirector.RendersDir;
            Directory.CreateDirectory(dir);
            if (Time.timeScale < 0.5f) Time.timeScale = 1f;
            var g = Game.I; var pl = g.player;
            var main = g.cam != null ? g.cam : Camera.main;
            rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var cgo = new GameObject("IconSceneCam");
            cam = cgo.AddComponent<Camera>();
            cam.CopyFrom(main);
            cam.targetTexture = rt; cam.enabled = false;
            cam.cullingMask = main.cullingMask & ~(1 << 5);   // fara layer-ul UI
            try
            {
                var src = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(main);
                var dst = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
                if (src != null && dst != null) { dst.renderPostProcessing = src.renderPostProcessing; dst.antialiasing = src.antialiasing; }
            }
            catch (System.Exception) { }

            // fara texte in lume (nume, panouri) - le ascundem doar pe durata pozelor
            var hidden = new System.Collections.Generic.List<Behaviour>();
            foreach (var cv in Object.FindObjectsByType<Canvas>())
                if (cv.enabled && cv.renderMode == RenderMode.WorldSpace) { cv.enabled = false; hidden.Add(cv); }

            yield return new WaitForSecondsRealtime(1.5f);   // animalele se aseaza langa erou
            var pets = Object.FindObjectsByType<Pet>();
            System.Array.Sort(pets, (a, b) => (a.transform.position - pl.transform.position).sqrMagnitude.CompareTo((b.transform.position - pl.transform.position).sqrMagnitude));
            Vector3 pos = pl.transform.position, fwd = pl.transform.forward; fwd.y = 0f; fwd.Normalize();
            float[] dists = { 4.0f, 5.2f, 6.6f };
            float[] yaws = { 0f, 28f, -28f };
            for (int mode = 0; mode < 3; mode++)   // 0 = toate animalele, 1 = doua, 2 = fara
            {
                for (int i = 0; i < pets.Length; i++) if (pets[i] != null) pets[i].gameObject.SetActive(mode == 0 || (mode == 1 && i < 2));
                yield return null;
                foreach (float d in dists)
                    foreach (float y in yaws)
                    {
                        Vector3 dirv = Quaternion.Euler(0f, y, 0f) * fwd;
                        cam.fieldOfView = 34f;
                        cam.transform.position = pos + dirv * d + Vector3.up * (1.2f + d * 0.16f);
                        cam.transform.LookAt(pos + Vector3.up * 1.05f);
                        cam.Render();
                        var prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                        tex.Apply();
                        RenderTexture.active = prev;
                        File.WriteAllBytes(Path.Combine(dir, "scene_" + (mode == 0 ? "allpets" : mode == 1 ? "2pets" : "nopets") + "_d" + d.ToString("0.0") + "_y" + y.ToString("0") + ".png"), tex.EncodeToPNG());
                    }
            }
            foreach (var p in pets) if (p != null) p.gameObject.SetActive(true);
            foreach (var b in hidden) if (b != null) b.enabled = true;
            Destroy(cgo); Destroy(rt); Destroy(tex);
            Debug.Log("[ColorRPG] Icon scene shots saved to " + dir);
            Busy = false;
            Destroy(gameObject);
        }

        Camera cam;
        RenderTexture rt;
        Texture2D tex;
        string dir;

        IEnumerator Run()
        {
            Busy = true;
            dir = PromoDirector.RendersDir;
            Directory.CreateDirectory(dir);
            if (Time.timeScale < 0.5f) Time.timeScale = 1f;

            rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var cgo = new GameObject("IconCam");
            cgo.transform.SetParent(transform, false);
            cam = cgo.AddComponent<Camera>();
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
            cam.targetTexture = rt;
            cam.enabled = false;
            try
            {
                var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
                if (data != null) { data.renderPostProcessing = false; data.renderShadows = false; }
            }
            catch (System.Exception) { }

            AddLight(new Vector3(35f, 205f, 0f), new Color(1f, 0.96f, 0.9f), 1.35f);     // lumina principala (fata-stanga)
            AddLight(new Vector3(20f, -35f, 0f), new Color(0.55f, 0.8f, 1f), 1.6f);     // contur albastru din spate-dreapta
            AddLight(new Vector3(15f, 35f, 0f), new Color(1f, 0.5f, 0.85f), 0.9f);    // contur roz din spate-stanga

            // ---- eroul tau, cu echipamentul din joc
            var pl = Game.I.player;
            Transform hero = null; SimpleAnimator ha = null;
            if (pl.ModelRoot != null)
            {
                hero = Instantiate(pl.ModelRoot.gameObject, transform).transform;
                hero.name = "IconHero";
                hero.position = Stage; hero.localScale = Vector3.one;
                foreach (var tr in hero.GetComponentsInChildren<TrailRenderer>(true)) Destroy(tr);
                foreach (var ps in hero.GetComponentsInChildren<ParticleSystem>(true)) Destroy(ps.gameObject);
                foreach (var c in hero.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var sa in hero.GetComponentsInChildren<SimpleAnimator>(true)) Destroy(sa);
                foreach (var r in hero.GetComponentsInChildren<Renderer>(true)) if (r.name.ToLower().Contains("cylinder")) r.enabled = false;   // umbra "blob"
                SetLayer(hero, Layer);
                yield return null;
                string path = GameData.CHAR + GameData.Heroes[Mathf.Clamp(pl.D.hero, 0, GameData.Heroes.Length - 1)];
                ha = SimpleAnimator.Setup(hero, path);
            }
            if (hero != null)
            {
                string[] poses = { "idle", "attack-melee-right", "attack-melee-right", "attack-melee-right", "emote-yes", "jump", "interact-right" };
                float[] at = { 0.3f, 0.22f, 0.4f, 0.55f, 0.45f, 0.35f, 0.5f };
                for (int i = 0; i < poses.Length; i++)
                {
                    if (ha != null && ha.Has(poses[i]))
                    {
                        ha.Speed = 1f;
                        ha.Play(poses[i], 0f, poses[i] == "idle", true);
                        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, ha.Length(poses[i]) * at[i]));
                        ha.Speed = 0f;
                        yield return null;
                    }
                    foreach (float yaw in new[] { 28f, -28f })
                    {
                        hero.rotation = Quaternion.Euler(0f, yaw, 0f);
                        // bust (pentru iconita) si corp intreg (pentru grafica mare)
                        Shot("hero_" + i + "_" + poses[i] + (yaw > 0 ? "_R" : "_L") + "_bust.png", new Vector3(0f, 1.5f, 4.3f), new Vector3(0f, 1.15f, 0f), 30f);
                        Shot("hero_" + i + "_" + poses[i] + (yaw > 0 ? "_R" : "_L") + "_full.png", new Vector3(0f, 1.2f, 6.4f), new Vector3(0f, 1.0f, 0f), 30f);
                    }
                }
                hero.gameObject.SetActive(false);
            }

            // ---- cateva animale (pets)
            string[] pets = { "animal-tiger", "animal-fox", "animal-panda", "animal-lion", "animal-bunny", "animal-penguin" };
            foreach (var p in pets)
            {
                var holder = new GameObject("PetHolder").transform;
                holder.SetParent(transform, false);
                holder.position = Stage;
                var m = ModelLib.Spawn(GameData.PETS + p, holder, 1.3f, 0f, false, false);
                if (m == null) { Destroy(holder.gameObject); continue; }
                SetLayer(holder, Layer);
                var pa = SimpleAnimator.Setup(m, GameData.PETS + p);
                if (pa != null) pa.Play("idle", 0f);
                yield return new WaitForSecondsRealtime(0.3f);
                holder.rotation = Quaternion.Euler(0f, -30f, 0f);
                Shot("pet_" + p + ".png", new Vector3(0f, 1.0f, 4.2f), new Vector3(0f, 0.62f, 0f), 30f);
                Destroy(holder.gameObject);
                yield return null;
            }

            Debug.Log("[ColorRPG] Icon renders saved to " + dir);
            Destroy(rt); Destroy(tex);
            Busy = false;
            Destroy(gameObject);
        }

        void Shot(string name, Vector3 camOff, Vector3 lookOff, float fov)
        {
            cam.fieldOfView = fov;
            cam.transform.position = Stage + camOff;
            cam.transform.LookAt(Stage + lookOff);
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
        }

        void AddLight(Vector3 euler, Color c, float intensity)
        {
            var go = new GameObject("IconLight");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = c; l.intensity = intensity;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << Layer;
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i), layer);
        }
    }
}
