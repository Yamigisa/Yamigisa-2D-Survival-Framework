using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

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

        public Gamepad gamepad { get; private set; }

        private void Update()
        {
            gamepad = Gamepad.current;
        }

        public bool IsPressed(InputBinding binding)
        {
            // Keyboard
            foreach (var key in binding.keyboardKeys)
            {
                if (Input.GetKey(key))
                    return true;
            }

            // Gamepad
            if (gamepad != null)
            {
                foreach (var button in binding.gamepadButtons)
                {
                    if (gamepad[button].isPressed)
                        return true;
                }
            }

            return false;
        }

        public bool IsPressedDown(InputBinding binding)
        {
            // Keyboard
            foreach (var key in binding.keyboardKeys)
            {
                if (Input.GetKeyDown(key))
                    return true;
            }

            // Gamepad
            if (gamepad != null)
            {
                foreach (var button in binding.gamepadButtons)
                {
                    if (gamepad[button].wasPressedThisFrame)
                        return true;
                }
            }

            return false;
        }

        public bool IsPressedUp(InputBinding binding)
        {
            foreach (var key in binding.keyboardKeys)
            {
                if (Input.GetKeyUp(key))
                    return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                foreach (var button in binding.gamepadButtons)
                {
                    if (gamepad[button].wasReleasedThisFrame)
                        return true;
                }
            }

            return false;
        }

        public Ray GetMouseCameraRay()
        {
            return Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        private void Reset()
        {
            // MOVEMENT
            moveUp = new InputBinding(KeyCode.W, GamepadButton.DpadUp);
            moveDown = new InputBinding(KeyCode.S, GamepadButton.DpadDown);
            moveLeft = new InputBinding(KeyCode.A, GamepadButton.DpadLeft);
            moveRight = new InputBinding(KeyCode.D, GamepadButton.DpadRight);

            sprint = new InputBinding(KeyCode.LeftShift, GamepadButton.LeftStick);
            jump = new InputBinding(KeyCode.Space, GamepadButton.South);
            crouch = new InputBinding(KeyCode.LeftControl, GamepadButton.East);

            // INTERACTION
            interaction1 = new InputBinding(KeyCode.E, GamepadButton.West);
            interaction2 = new InputBinding(KeyCode.F, GamepadButton.North);

            // INVENTORY
            inventory = new InputBinding(KeyCode.I, GamepadButton.Start);
            useItem = new InputBinding(KeyCode.Mouse0, GamepadButton.RightTrigger);

            // CRAFTING
            crafting = new InputBinding(KeyCode.C, GamepadButton.Select);

            // CANCEL
            cancel = new InputBinding(KeyCode.Escape, GamepadButton.B);

            // PAUSE
            pause = new InputBinding(KeyCode.Escape, GamepadButton.Start);
        }
    }

    [System.Serializable]
    public class InputBinding
    {
        public List<KeyCode> keyboardKeys = new();
        public List<GamepadButton> gamepadButtons = new();

        public InputBinding(params KeyCode[] keys)
        {
            keyboardKeys = new List<KeyCode>(keys);
        }

        public InputBinding(KeyCode key, params GamepadButton[] buttons)
        {
            keyboardKeys = new List<KeyCode> { key };
            gamepadButtons = new List<GamepadButton>(buttons);
        }
    }
}
