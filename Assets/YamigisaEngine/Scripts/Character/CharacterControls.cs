using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace Yamigisa
{
    public class CharacterControls : MonoBehaviour
    {
        [Header("MOVEMENT")]
        public InputBinding moveUp;
        public InputBinding moveDown;
        public InputBinding moveLeft;
        public InputBinding moveRight;
        public InputBinding sprint;
        public InputBinding jump;
        public InputBinding crouch;

        [Header("INTERACTION INPUTS")]
        public InputBinding interaction1;
        public InputBinding interaction2;

        [Header("INVENTORY / USE ITEM")]
        public InputBinding inventory;
        public InputBinding useItem;

        [Header("CRAFTING")]
        public InputBinding crafting;

        [Header("CANCEL ACTIONS")]
        public InputBinding cancel;

        [Header("PAUSE")]
        public InputBinding pause;

#if ENABLE_INPUT_SYSTEM
        public Gamepad gamepad { get; private set; }
#endif

        private void Awake()
        {
            EnsureBindingsExist();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            gamepad = Gamepad.current;
#endif
        }

        public bool IsPressed(InputBinding binding)
        {
            if (binding == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            if (IsPressedNewInputSystem(binding))
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (IsPressedLegacyInput(binding))
                return true;
#endif

            return false;
        }

        public bool IsPressedDown(InputBinding binding)
        {
            if (binding == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            if (IsPressedDownNewInputSystem(binding))
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (IsPressedDownLegacyInput(binding))
                return true;
#endif

            return false;
        }

        public bool IsPressedUp(InputBinding binding)
        {
            if (binding == null)
                return false;

#if ENABLE_INPUT_SYSTEM
            if (IsPressedUpNewInputSystem(binding))
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (IsPressedUpLegacyInput(binding))
                return true;
#endif

            return false;
        }

        public int GetPressedInteractionActionIndexDown()
        {
            if (IsPressedDown(interaction1))
                return 0;

            if (IsPressedDown(interaction2))
                return 1;

            return -1;
        }

        public bool IsInteractionIndexPressedDown(int index)
        {
            switch (index)
            {
                case 0:
                    return IsPressedDown(interaction1);

                case 1:
                    return IsPressedDown(interaction2);

                default:
                    return false;
            }
        }

        public Ray GetMouseCameraRay()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return new Ray(Vector3.zero, Vector3.forward);

            return mainCamera.ScreenPointToRay(GetPointerScreenPosition());
        }

        private Vector3 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector3.zero;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private bool IsPressedNewInputSystem(InputBinding binding)
        {
            foreach (KeyCode key in binding.keyboardKeys)
            {
                if (IsKeyPressedNewInputSystem(key))
                    return true;
            }

            if (gamepad != null)
            {
                foreach (GamepadButton button in binding.gamepadButtons)
                {
                    if (gamepad[button].isPressed)
                        return true;
                }
            }

            return false;
        }

        private bool IsPressedDownNewInputSystem(InputBinding binding)
        {
            foreach (KeyCode key in binding.keyboardKeys)
            {
                if (IsKeyPressedDownNewInputSystem(key))
                    return true;
            }

            if (gamepad != null)
            {
                foreach (GamepadButton button in binding.gamepadButtons)
                {
                    if (gamepad[button].wasPressedThisFrame)
                        return true;
                }
            }

            return false;
        }

        private bool IsPressedUpNewInputSystem(InputBinding binding)
        {
            foreach (KeyCode key in binding.keyboardKeys)
            {
                if (IsKeyPressedUpNewInputSystem(key))
                    return true;
            }

            if (gamepad != null)
            {
                foreach (GamepadButton button in binding.gamepadButtons)
                {
                    if (gamepad[button].wasReleasedThisFrame)
                        return true;
                }
            }

            return false;
        }

        private bool IsKeyPressedNewInputSystem(KeyCode key)
        {
            ButtonControl button = GetButtonControlNewInputSystem(key);
            return button != null && button.isPressed;
        }

        private bool IsKeyPressedDownNewInputSystem(KeyCode key)
        {
            ButtonControl button = GetButtonControlNewInputSystem(key);
            return button != null && button.wasPressedThisFrame;
        }

        private bool IsKeyPressedUpNewInputSystem(KeyCode key)
        {
            ButtonControl button = GetButtonControlNewInputSystem(key);
            return button != null && button.wasReleasedThisFrame;
        }

        private ButtonControl GetButtonControlNewInputSystem(KeyCode key)
        {
            ButtonControl mouseButton = GetMouseButtonControl(key);

            if (mouseButton != null)
                return mouseButton;

            return GetKeyboardButtonControl(key);
        }

        private ButtonControl GetMouseButtonControl(KeyCode key)
        {
            if (Mouse.current == null)
                return null;

            switch (key)
            {
                case KeyCode.Mouse0:
                    return Mouse.current.leftButton;

                case KeyCode.Mouse1:
                    return Mouse.current.rightButton;

                case KeyCode.Mouse2:
                    return Mouse.current.middleButton;

                case KeyCode.Mouse3:
                    return Mouse.current.forwardButton;

                case KeyCode.Mouse4:
                    return Mouse.current.backButton;

                default:
                    return null;
            }
        }

        private ButtonControl GetKeyboardButtonControl(KeyCode key)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return null;

            switch (key)
            {
                case KeyCode.A: return keyboard.aKey;
                case KeyCode.B: return keyboard.bKey;
                case KeyCode.C: return keyboard.cKey;
                case KeyCode.D: return keyboard.dKey;
                case KeyCode.E: return keyboard.eKey;
                case KeyCode.F: return keyboard.fKey;
                case KeyCode.G: return keyboard.gKey;
                case KeyCode.H: return keyboard.hKey;
                case KeyCode.I: return keyboard.iKey;
                case KeyCode.J: return keyboard.jKey;
                case KeyCode.K: return keyboard.kKey;
                case KeyCode.L: return keyboard.lKey;
                case KeyCode.M: return keyboard.mKey;
                case KeyCode.N: return keyboard.nKey;
                case KeyCode.O: return keyboard.oKey;
                case KeyCode.P: return keyboard.pKey;
                case KeyCode.Q: return keyboard.qKey;
                case KeyCode.R: return keyboard.rKey;
                case KeyCode.S: return keyboard.sKey;
                case KeyCode.T: return keyboard.tKey;
                case KeyCode.U: return keyboard.uKey;
                case KeyCode.V: return keyboard.vKey;
                case KeyCode.W: return keyboard.wKey;
                case KeyCode.X: return keyboard.xKey;
                case KeyCode.Y: return keyboard.yKey;
                case KeyCode.Z: return keyboard.zKey;

                case KeyCode.Alpha0: return keyboard.digit0Key;
                case KeyCode.Alpha1: return keyboard.digit1Key;
                case KeyCode.Alpha2: return keyboard.digit2Key;
                case KeyCode.Alpha3: return keyboard.digit3Key;
                case KeyCode.Alpha4: return keyboard.digit4Key;
                case KeyCode.Alpha5: return keyboard.digit5Key;
                case KeyCode.Alpha6: return keyboard.digit6Key;
                case KeyCode.Alpha7: return keyboard.digit7Key;
                case KeyCode.Alpha8: return keyboard.digit8Key;
                case KeyCode.Alpha9: return keyboard.digit9Key;

                case KeyCode.Keypad0: return keyboard.numpad0Key;
                case KeyCode.Keypad1: return keyboard.numpad1Key;
                case KeyCode.Keypad2: return keyboard.numpad2Key;
                case KeyCode.Keypad3: return keyboard.numpad3Key;
                case KeyCode.Keypad4: return keyboard.numpad4Key;
                case KeyCode.Keypad5: return keyboard.numpad5Key;
                case KeyCode.Keypad6: return keyboard.numpad6Key;
                case KeyCode.Keypad7: return keyboard.numpad7Key;
                case KeyCode.Keypad8: return keyboard.numpad8Key;
                case KeyCode.Keypad9: return keyboard.numpad9Key;

                case KeyCode.Space: return keyboard.spaceKey;
                case KeyCode.Escape: return keyboard.escapeKey;
                case KeyCode.Return: return keyboard.enterKey;
                case KeyCode.KeypadEnter: return keyboard.numpadEnterKey;
                case KeyCode.Tab: return keyboard.tabKey;
                case KeyCode.Backspace: return keyboard.backspaceKey;
                case KeyCode.Delete: return keyboard.deleteKey;

                case KeyCode.LeftShift: return keyboard.leftShiftKey;
                case KeyCode.RightShift: return keyboard.rightShiftKey;
                case KeyCode.LeftControl: return keyboard.leftCtrlKey;
                case KeyCode.RightControl: return keyboard.rightCtrlKey;
                case KeyCode.LeftAlt: return keyboard.leftAltKey;
                case KeyCode.RightAlt: return keyboard.rightAltKey;

                case KeyCode.UpArrow: return keyboard.upArrowKey;
                case KeyCode.DownArrow: return keyboard.downArrowKey;
                case KeyCode.LeftArrow: return keyboard.leftArrowKey;
                case KeyCode.RightArrow: return keyboard.rightArrowKey;

                default:
                    return null;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private bool IsPressedLegacyInput(InputBinding binding)
        {
            foreach (KeyCode key in binding.keyboardKeys)
            {
                if (Input.GetKey(key))
                    return true;
            }

            return false;
        }

        private bool IsPressedDownLegacyInput(InputBinding binding)
        {
            foreach (KeyCode key in binding.keyboardKeys)
            {
                if (Input.GetKeyDown(key))
                    return true;
            }

            return false;
        }

        private bool IsPressedUpLegacyInput(InputBinding binding)
        {
            foreach (KeyCode key in binding.keyboardKeys)
            {
                if (Input.GetKeyUp(key))
                    return true;
            }

            return false;
        }
#endif

        private void EnsureBindingsExist()
        {
            if (moveUp == null) moveUp = new InputBinding();
            if (moveDown == null) moveDown = new InputBinding();
            if (moveLeft == null) moveLeft = new InputBinding();
            if (moveRight == null) moveRight = new InputBinding();

            if (sprint == null) sprint = new InputBinding();
            if (jump == null) jump = new InputBinding();
            if (crouch == null) crouch = new InputBinding();

            if (interaction1 == null) interaction1 = new InputBinding();
            if (interaction2 == null) interaction2 = new InputBinding();

            if (inventory == null) inventory = new InputBinding();
            if (useItem == null) useItem = new InputBinding();

            if (crafting == null) crafting = new InputBinding();

            if (cancel == null) cancel = new InputBinding();
            if (pause == null) pause = new InputBinding();
        }

        private void Reset()
        {
#if ENABLE_INPUT_SYSTEM
            moveUp = new InputBinding(KeyCode.W, GamepadButton.DpadUp);
            moveDown = new InputBinding(KeyCode.S, GamepadButton.DpadDown);
            moveLeft = new InputBinding(KeyCode.A, GamepadButton.DpadLeft);
            moveRight = new InputBinding(KeyCode.D, GamepadButton.DpadRight);

            sprint = new InputBinding(KeyCode.LeftShift, GamepadButton.LeftStick);
            jump = new InputBinding(KeyCode.Space, GamepadButton.South);
            crouch = new InputBinding(KeyCode.LeftControl, GamepadButton.East);

            interaction1 = new InputBinding(KeyCode.Mouse0, GamepadButton.West);
            interaction2 = new InputBinding(KeyCode.F, GamepadButton.North);

            inventory = new InputBinding(KeyCode.I, GamepadButton.Start);
            useItem = new InputBinding(KeyCode.E, GamepadButton.RightTrigger);

            crafting = new InputBinding(KeyCode.C, GamepadButton.Select);

            cancel = new InputBinding(KeyCode.Escape, GamepadButton.East);
            pause = new InputBinding(KeyCode.Escape, GamepadButton.Start);
#else
            moveUp = new InputBinding(KeyCode.W);
            moveDown = new InputBinding(KeyCode.S);
            moveLeft = new InputBinding(KeyCode.A);
            moveRight = new InputBinding(KeyCode.D);

            sprint = new InputBinding(KeyCode.LeftShift);
            jump = new InputBinding(KeyCode.Space);
            crouch = new InputBinding(KeyCode.LeftControl);

            interaction1 = new InputBinding(KeyCode.Mouse0);
            interaction2 = new InputBinding(KeyCode.F);

            inventory = new InputBinding(KeyCode.I);
            useItem = new InputBinding(KeyCode.E);

            crafting = new InputBinding(KeyCode.C);

            cancel = new InputBinding(KeyCode.Escape);
            pause = new InputBinding(KeyCode.Escape);
#endif
        }
    }

    [System.Serializable]
    public class InputBinding
    {
        public List<KeyCode> keyboardKeys = new List<KeyCode>();

#if ENABLE_INPUT_SYSTEM
        public List<GamepadButton> gamepadButtons = new List<GamepadButton>();
#endif

        public InputBinding()
        {
            keyboardKeys = new List<KeyCode>();

#if ENABLE_INPUT_SYSTEM
            gamepadButtons = new List<GamepadButton>();
#endif
        }

        public InputBinding(params KeyCode[] keys)
        {
            keyboardKeys = new List<KeyCode>(keys);

#if ENABLE_INPUT_SYSTEM
            gamepadButtons = new List<GamepadButton>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        public InputBinding(KeyCode key, params GamepadButton[] buttons)
        {
            keyboardKeys = new List<KeyCode> { key };
            gamepadButtons = new List<GamepadButton>(buttons);
        }
#endif
    }
}