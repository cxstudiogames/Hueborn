// =====================================================================
//  MobileControls.cs - controalele de pe TELEFON (apar doar in modul mobil):
//   * stanga-jos: joystick de miscare (impins pana la margine = alergi)
//   * dreapta-jos: FIGHT (tii apasat = ataci continuu) si JUMP
//   * tragi cu degetul pe ecran (nu pe butoane) = rotesti camera,
//     doua degete (pinch) = zoom
//  Discrete (semi-transparente) ca sa nu incurce. Restul se apasa direct.
// =====================================================================
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ColorRPG
{
    public class MobileControls : MonoBehaviour
    {
        public static MobileControls I;

        // citite de Inp
        public static Vector2 MoveVec;
        public static bool Running;
        static bool jumpFlag, attackFlag, interactFlag, potionFlag;
        public static Vector2 LookDelta;
        public static float Pinch;

        RectTransform root, stickBase, stickKnob, fightRt, jumpRt, healRt, stickZone;
        static readonly Vector2 StickHome = new Vector2(270, 250);
        Image stickBaseImg, stickKnobImg;
        int stickFinger = -1;
        bool fightHeld;
        float fightRepeat;
        const float StickR = 110f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; MoveVec = Vector2.zero; LookDelta = Vector2.zero; Pinch = 0f; jumpFlag = attackFlag = interactFlag = false; Running = false; }

        public static bool ConsumeJump() { bool b = jumpFlag; jumpFlag = false; return b; }
        public static bool ConsumePotion() { bool b = potionFlag; potionFlag = false; return b; }
        public static bool ConsumeAttack() { bool b = attackFlag; attackFlag = false; return b; }
        public static bool ConsumeInteract() { bool b = interactFlag; interactFlag = false; return b; }
        public static void TapInteract() { interactFlag = true; }
        public static void PressAttack() { attackFlag = true; }

        public static MobileControls Create(RectTransform hudRoot)
        {
            var go = new GameObject("MobileControls", typeof(RectTransform));
            go.transform.SetParent(hudRoot, false);
            var m = go.AddComponent<MobileControls>();
            I = m;
            m.Build();
            return m;
        }

        void Build()
        {
            root = (RectTransform)transform;
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            Vector2 BL = Vector2.zero, BR = new Vector2(1, 0), C = new Vector2(0.5f, 0.5f);

            // ---- joystick
            // zona mare (stanga-jos): unde pui degetul, acolo apare joystick-ul (sub celelalte butoane, ca sa nu le blocheze)
            stickZone = UIKit.Rect((RectTransform)root.parent, "StickZone", BL, BL, Vector2.zero, Vector2.zero);
            stickZone.anchorMin = Vector2.zero; stickZone.anchorMax = new Vector2(0.42f, 0.62f); stickZone.offsetMin = new Vector2(150, 0); stickZone.offsetMax = Vector2.zero;
            var zi = stickZone.gameObject.AddComponent<Image>(); zi.color = new Color(0, 0, 0, 0.001f); zi.raycastTarget = true;
            stickZone.SetAsFirstSibling();
            var za = stickZone.gameObject.AddComponent<TouchArea>();
            za.down = (id, pos) => { if (stickFinger >= 0) return; stickFinger = id; PlaceStick(pos); StickTo(pos); };
            za.drag = (id, pos) => { if (id == stickFinger) StickTo(pos); };
            za.up = id => { if (id == stickFinger) ReleaseStick(); };

            stickBase = UIKit.Rect(root, "Stick", BL, C, StickHome, new Vector2(StickR * 2f, StickR * 2f));
            var bi = UIKit.Img(stickBase, "round_Grey", new Color(1f, 1f, 1f, 0.16f), 30f, 1f);
            stickBaseImg = bi;
            bi.raycastTarget = true;
            var ring = UIKit.Rect(stickBase, "Ring", C, C, Vector2.zero, new Vector2(StickR * 2f + 8f, StickR * 2f + 8f));
            UIKit.Img(ring, "round_Grey", new Color(0f, 0f, 0f, 0.12f), 30f, 1f).raycastTarget = false;
            ring.SetAsFirstSibling();
            stickKnob = UIKit.Rect(stickBase, "Knob", C, C, Vector2.zero, new Vector2(96, 96));
            stickKnobImg = UIKit.Img(stickKnob, "round_Grey", new Color(1f, 1f, 1f, 0.45f), 30f, 1f);
            stickKnobImg.raycastTarget = false;
            var st = stickBase.gameObject.AddComponent<TouchArea>();
            st.down = (id, pos) => { stickFinger = id; StickTo(pos); };
            st.drag = (id, pos) => { if (id == stickFinger) StickTo(pos); };
            st.up = id => { if (id == stickFinger) ReleaseStick(); };

            // ---- FIGHT (mare) + JUMP
            fightRt = RoundButton("Fight", BR, new Vector2(-215, 165), 190f, new Color(1f, 0.45f, 0.4f), ItemIcons.Get(0), "FIGHT");
            var ft = fightRt.gameObject.AddComponent<TouchArea>();
            ft.down = (id, p) => { fightHeld = true; attackFlag = true; fightRepeat = 0.28f; Pulse(fightRt); };
            ft.up = id => { fightHeld = false; };
            jumpRt = RoundButton("Jump", BR, new Vector2(-420, 96), 128f, new Color(0.5f, 0.8f, 1f), UIKit.Sprite("icon_arrow_up_light", 0f), "JUMP");
            var jt = jumpRt.gameObject.AddComponent<TouchArea>();
            jt.down = (id, p) => { jumpFlag = true; Pulse(jumpRt); };
            SetStickAlpha(0.55f);
            // ---- HEAL (potiunea de viata - pe PC e tasta Q)
            healRt = RoundButton("Heal", BR, new Vector2(-420, 245), 104f, new Color(0.4f, 1f, 0.5f), ItemIcons.Get(Player.PotionIcons[0]), "HEAL");
            var ht = healRt.gameObject.AddComponent<TouchArea>();
            ht.down = (id, p) => { potionFlag = true; Pulse(healRt); };
            healRt.gameObject.SetActive(false);   // pe telefon nu exista heal (doar fugi)
        }

        RectTransform RoundButton(string n, Vector2 anchor, Vector2 pos, float size, Color c, Sprite icon, string label)
        {
            var rt = UIKit.Rect(root, n, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var img = UIKit.Img(rt, "round_Grey", new Color(c.r, c.g, c.b, 0.32f), 30f, 1f);
            img.raycastTarget = true;
            var ic = UIKit.Rect(rt, "I", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(size * 0.48f, size * 0.48f)).gameObject.AddComponent<Image>();
            ic.sprite = icon; ItemIcons.Art(ic); ic.preserveAspect = true; ic.color = new Color(1f, 1f, 1f, 0.85f); ic.raycastTarget = false;
            UIKit.Text(rt, "L", label, Mathf.RoundToInt(size * 0.12f), new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, size * 0.1f), new Vector2(size, 30));
            return rt;
        }

        void Pulse(RectTransform r) { r.localScale = Vector3.one * 0.9f; }

        /// <summary>Joystick-ul sare sub deget (stanga-jos), oriunde atingi in zona.</summary>
        void PlaceStick(Vector2 screen)
        {
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out local))
                stickBase.anchoredPosition = local - root.rect.min;
            SetStickAlpha(1f);
        }

        void ReleaseStick()
        {
            stickFinger = -1; MoveVec = Vector2.zero; Running = false;
            stickKnob.anchoredPosition = Vector2.zero;
            stickBase.anchoredPosition = StickHome;
            SetStickAlpha(0.55f);
        }

        void SetStickAlpha(float a)
        {
            if (stickBaseImg != null) stickBaseImg.color = new Color(1f, 1f, 1f, 0.08f + 0.1f * a);
            if (stickKnobImg != null) stickKnobImg.color = new Color(1f, 1f, 1f, 0.45f * a);
        }

        void StickTo(Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(stickBase, screen, null, out local);
            Vector2 v = Vector2.ClampMagnitude(local, StickR);
            stickKnob.anchoredPosition = v;
            MoveVec = v / StickR;
            if (MoveVec.magnitude < 0.12f) MoveVec = Vector2.zero;
            Running = MoveVec.magnitude > 0.92f;
        }

        // ---------------------------------------------------------------
        //  camera: degetele care NU sunt pe UI
        // ---------------------------------------------------------------
        readonly Dictionary<int, Vector2> camTouches = new Dictionary<int, Vector2>();
        float lastPinchDist;

        static bool OverUI(Vector2 pos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var ped = new PointerEventData(es) { position = pos };
            var hits = new List<RaycastResult>();
            es.RaycastAll(ped, hits);
            return hits.Count > 0;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            // FIGHT tinut apasat = loveste continuu
            if (fightHeld) { fightRepeat -= dt; if (fightRepeat <= 0f) { fightRepeat = 0.28f; attackFlag = true; } }
            if (fightRt != null) fightRt.localScale = Vector3.Lerp(fightRt.localScale, Vector3.one, 1f - Mathf.Exp(-14f * dt));
            if (jumpRt != null) jumpRt.localScale = Vector3.Lerp(jumpRt.localScale, Vector3.one, 1f - Mathf.Exp(-14f * dt));
            if (healRt != null) healRt.localScale = Vector3.Lerp(healRt.localScale, Vector3.one, 1f - Mathf.Exp(-14f * dt));

            // ascunse cand e deschisa o fereastra
            var g = Game.I;
            bool show = g == null || g.hud == null || !g.hud.Blocking;
            if (stickBase.gameObject.activeSelf != show)
            {
                stickBase.gameObject.SetActive(show); stickZone.gameObject.SetActive(show); fightRt.gameObject.SetActive(show); jumpRt.gameObject.SetActive(show); healRt.gameObject.SetActive(false);
                if (!show) { fightHeld = false; ReleaseStick(); }
            }

            LookDelta = Vector2.zero; Pinch = 0f;
            var now = new Dictionary<int, Vector2>();
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts != null)
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    now[t.touchId.ReadValue()] = t.position.ReadValue();
                }
#else
            for (int i = 0; i < Input.touchCount; i++) { var t = Input.GetTouch(i); if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled) now[t.fingerId] = t.position; }
#endif
            // degetele noi: le folosim pentru camera doar daca nu au inceput pe UI
            foreach (var kv in now)
                if (!camTouches.ContainsKey(kv.Key) && kv.Key != stickFinger && !OverUI(kv.Value)) camTouches[kv.Key] = kv.Value;
            var gone = new List<int>();
            foreach (var k in camTouches.Keys) if (!now.ContainsKey(k)) gone.Add(k);
            foreach (var k in gone) camTouches.Remove(k);

            if (camTouches.Count == 1)
            {
                foreach (var k in new List<int>(camTouches.Keys))
                {
                    Vector2 d = now[k] - camTouches[k];
                    LookDelta += d * (1920f / Mathf.Max(1, Screen.width)) * 0.18f;
                    camTouches[k] = now[k];
                }
                lastPinchDist = 0f;
            }
            else if (camTouches.Count >= 2)
            {
                var keys = new List<int>(camTouches.Keys);
                float dist = Vector2.Distance(now[keys[0]], now[keys[1]]);
                if (lastPinchDist > 0f) Pinch = (dist - lastPinchDist) / Mathf.Max(1, Screen.height) * 6f;
                lastPinchDist = dist;
                foreach (var k in keys) camTouches[k] = now[k];
            }
            else lastPinchDist = 0f;
        }
    }

    /// <summary>Zona apasabila cu degetul (stie care deget a apasat).</summary>
    public class TouchArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public System.Action<int, Vector2> down, drag;
        public System.Action<int> up;
        public void OnPointerDown(PointerEventData e) { if (down != null) down(e.pointerId, e.position); }
        public void OnDrag(PointerEventData e) { if (drag != null) drag(e.pointerId, e.position); }
        public void OnPointerUp(PointerEventData e) { if (up != null) up(e.pointerId); }
    }
}
