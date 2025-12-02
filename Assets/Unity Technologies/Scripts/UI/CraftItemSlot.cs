using UnityEngine;
using UnityEngine.UI;
using System;

namespace Yamigisa
{
    public class CraftItemSlot : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        public Button button;

        private ItemData itemData;
        private GroupData groupData;
        private string amount;

        public void BindItem(ItemData data, bool isInteractable = true)
        {
            itemData = data;

            if (icon) icon.sprite = itemData != null ? itemData.iconInventory : null;

            if (isInteractable)
                RefreshInteractable();
        }

        public void BindGroup(GroupData data, bool isInteractable = true)
        {
            groupData = data;

            if (icon) icon.sprite = groupData != null ? groupData.icon : null;

            if (isInteractable)
                RefreshInteractable();
        }

        public void RefreshInteractable()
        {
            if (!button || itemData == null || Inventory.Instance == null)
            {
                if (button) button.interactable = false;
                return;
            }

            button.interactable = Inventory.Instance.CanCraft(itemData);
        }
    }
}
