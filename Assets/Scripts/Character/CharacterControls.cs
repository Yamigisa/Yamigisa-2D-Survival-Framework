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

        [Header("Inventory / Items")]
        public List<KeyCode> inventoryKey;
        public List<KeyCode> useItemKey;

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
