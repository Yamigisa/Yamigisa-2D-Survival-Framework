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
        public List<KeyCode> inventoryKey;

        public LayerMask selectable_layer = ~0;
        private HashSet<Selectable> raycast_list = new HashSet<Selectable>();

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

        public void RaycastSelectable()
        {
            RaycastHit[] hits = Physics.RaycastAll(GetMouseCameraRay(), 99f, selectable_layer.value);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null)
                {
                    Selectable select = hit.collider.GetComponentInParent<Selectable>();
                    if (select != null)
                    {
                        raycast_list.Add(select);
                        select.OnHover(true);
                    }
                }
            }
        }
    }
}
