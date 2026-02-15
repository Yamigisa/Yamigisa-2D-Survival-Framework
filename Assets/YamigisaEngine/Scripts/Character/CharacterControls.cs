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
    }

    [System.Serializable]
    public class InputBinding
    {
        public List<KeyCode> keyboardKeys = new();
        public List<GamepadButton> gamepadButtons = new();
    }

}
