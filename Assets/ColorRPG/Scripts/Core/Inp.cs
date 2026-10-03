// =====================================================================
//  Inp.cs - input unificat (merge cu Input System nou SI cu Input Manager vechi)
// =====================================================================
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ColorRPG
{
    public static class Inp
    {
        /// <summary>true cat timp scrii intr-un camp de text (ex. numele) - tastele de joc sunt ignorate.</summary>
        public static bool Typing
        {
            get
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es == null || es.currentSelectedGameObject == null) return false;
                var f = es.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>();
                return f != null && f.isFocused;
            }
        }

        public static Vector2 Move()
        {
            if (Typing) return Vector2.zero;
            if (MobileControls.MoveVec.sqrMagnitude > 0.0001f) return MobileControls.MoveVec;   // joystick (telefon)
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            Vector2 v = Vector2.zero;
            if (k != null)
            {
                v.x = ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1 : 0) - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1 : 0);
                v.y = ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1 : 0) - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1 : 0);
            }
            var g = Gamepad.current;
            if (g != null && v == Vector2.zero)
            {
                Vector2 s = g.leftStick.ReadValue();
                if (s.sqrMagnitude > 0.04f) v = s;
            }
            return v;
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        public static bool Sprint()
        {
            if (Typing) return false;
            if (MobileControls.Running) return true;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)) || (g != null && g.leftTrigger.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        /// <summary>Atac tinut apasat (click stanga, F sau butonul X de pe gamepad). SPACE = saritura.</summary>
        public static bool Attack(bool allowMouse)
        {
            if (Typing) return false;
            if (MobileControls.ConsumeAttack()) return true;    // butonul FIGHT (telefon)
            if (Device.Mobile) allowMouse = false;              // pe telefon nu ataci apasand pe ecran
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;
            // un click = o lovitura (cu cat dai click mai repede, cu atat ataci mai repede)
            return (k != null && k.fKey.wasPressedThisFrame) ||
                   (allowMouse && m != null && m.leftButton.wasPressedThisFrame) ||
                   (g != null && g.buttonWest.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.F) || (allowMouse && Input.GetMouseButtonDown(0));
#endif
        }

        /// <summary>Saritura (SPACE sau butonul A de pe gamepad).</summary>
        public static bool JumpDown()
        {
            if (Typing) return false;
            if (MobileControls.ConsumeJump()) return true;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.spaceKey.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool InteractDown()
        {
            if (Typing) return false;
            if (MobileControls.ConsumeInteract()) return true;   // apasare pe mesajul de interactiune (telefon)
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.eKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool PotionDown()
        {
            if (Typing) return false;
            if (MobileControls.ConsumePotion()) return true;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.qKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }

        public static bool InventoryDown()
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.iKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)) || (g != null && g.selectButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        public static bool AutoDown()
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.rightShoulder.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        public static bool MuteDown()
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.mKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.M);
#endif
        }

        /// <summary>Tastele 1 / 2 / 3 = potiunile de boost (XP / DMG / COINS).</summary>
        public static bool BoostDown(int i)
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            return i == 0 ? k.digit1Key.wasPressedThisFrame : i == 1 ? k.digit2Key.wasPressedThisFrame : k.digit3Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(i == 0 ? KeyCode.Alpha1 : i == 1 ? KeyCode.Alpha2 : KeyCode.Alpha3);
#endif
        }

        /// <summary>Tastele 1 / 2 / 3 = cele 3 skill-uri ale eroului (doar PC; pe telefon se apasa pe ele).</summary>
        public static bool SkillDown(int i)
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            return i == 0 ? (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) : i == 1 ? (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) : (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(i == 0 ? KeyCode.Alpha1 : i == 1 ? KeyCode.Alpha2 : KeyCode.Alpha3);
#endif
        }

        /// <summary>F9 = poza pentru iconita / reclame (doar admin).</summary>
        public static bool PhotoDown()
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.f9Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F9);
#endif
        }

        public static bool AdminDown()
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.f1Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F1);
#endif
        }

        /// <summary>ENTER sau "/" = scrii in chat.</summary>
        public static bool ChatDown()
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.slashKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Slash);
#endif
        }

        public static bool EscapeDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>
        /// Rotirea camerei third-person: miscarea mouse-ului cat tii apasat click dreapta
        /// (in "pixeli"), plus stick-ul drept de pe gamepad (in grade/secunda, deja inmultit cu dt).
        /// </summary>
        public static Vector2 Look(bool allowMouse)
        {
            Vector2 v = MobileControls.LookDelta;   // tragi cu degetul pe ecran (telefon)
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (allowMouse && m != null && m.rightButton.isPressed) v += m.delta.ReadValue() * 0.15f;
            var g = Gamepad.current;
            if (g != null)
            {
                Vector2 s = g.rightStick.ReadValue();
                if (s.sqrMagnitude > 0.03f) v += s * 160f * Time.unscaledDeltaTime;
            }
            // (Z / X / C sunt acum skill-urile - camera se roteste doar cu click dreapta / stick-ul drept)
#else
            if (allowMouse && Input.GetMouseButton(1)) v += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 3f;
#endif
            return v;
        }

        /// <summary>Pozitia mouse-ului pe ecran (pixeli).</summary>
        public static Vector2 MousePos()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null && Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return m != null ? m.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        /// <summary>Rotita mouse-ului (pozitiv = zoom in).</summary>
        public static float Scroll()
        {
            if (Mathf.Abs(MobileControls.Pinch) > 0.001f) return MobileControls.Pinch;   // pinch zoom (telefon)
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null) return 0f;
            float s = m.scroll.ReadValue().y;
            return Mathf.Abs(s) > 0.01f ? Mathf.Sign(s) : 0f;
#else
            float s = Input.mouseScrollDelta.y;
            return Mathf.Abs(s) > 0.01f ? Mathf.Sign(s) : 0f;
#endif
        }

        /// <summary>Tasta 1..9 apasata in acest frame (sau -1).</summary>
        public static int NumberDown()
        {
            if (Typing) return -1;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return -1;
            if (k.digit1Key.wasPressedThisFrame) return 1;
            if (k.digit2Key.wasPressedThisFrame) return 2;
            if (k.digit3Key.wasPressedThisFrame) return 3;
            return -1;
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 3;
            return -1;
#endif
        }
    }
}
