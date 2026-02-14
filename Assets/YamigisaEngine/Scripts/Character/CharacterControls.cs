using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class CharacterControls : MonoBehaviour
    {
        [Header("Movement")]
        public List<KeyCode> moveUpKey;
        public List<KeyCode> moveDownKey;
        public List<KeyCode> moveLeftKey;
        public List<KeyCode> moveRightKey;
        public List<KeyCode> sprintKey;
        public List<KeyCode> jumpKey;
        public List<KeyCode> crouchKey;

        [Header("Interaction Inputs")]
        public KeyCode interaction1 = KeyCode.Mouse0; // LMB
        public KeyCode interaction2 = KeyCode.Mouse1; // RMB


        [Header("Inventory / Items")]
        public List<KeyCode> inventoryKey;
        public List<KeyCode> useItemKey;

        [Header("Crafting")]
        public List<KeyCode> craftingKey;

        [Header("Cancel")]
        public List<KeyCode> cancelKey;

        [Header("Pause")]
        public List<KeyCode> pauseKey;

        public bool IsAnyKeyPressed(List<KeyCode> keyList)
        {
            foreach (KeyCode key in keyList)
            {
                if (Input.GetKey(key))
                    return true;
            }
            return false;
        }

        public bool IsAnyKeyPressedDown(List<KeyCode> keyList)
        {
            foreach (KeyCode key in keyList)
            {
                if (Input.GetKeyDown(key))
                    return true;
            }
            return false;
        }

        public Ray GetMouseCameraRay()
        {
            return Camera.main.ScreenPointToRay(Input.mousePosition);
        }
    }
}
