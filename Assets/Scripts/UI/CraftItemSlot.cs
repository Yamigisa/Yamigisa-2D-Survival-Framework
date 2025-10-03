using UnityEngine;
using UnityEngine.UI;
using System;

namespace Yamigisa
{
    public class CraftItemSlot : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Button button;

        private ItemData itemData;
        private Action<ItemData> onCraftRequest; // callback to CraftingInterface.TryCraft

        public void Bind(ItemData data, Action<ItemData> onCraft)
        {
            itemData = data;
            onCraftRequest = onCraft;

            if (icon) icon.sprite = itemData != null ? itemData.iconInventory : null;

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }

            RefreshInteractable();
        }

        public void RefreshInteractable()
        {
            if (!button || itemData == null || Inventory.Instance == null)
            {
                if (button) button.interactable = false;
                return;
            }

            // interactable iff the player currently meets all requirements
            button.interactable = Inventory.Instance.CanCraft(itemData);
        }

        private void OnClick()
        {
            if (!button || !button.interactable) return;
            onCraftRequest?.Invoke(itemData);
        }
    }
}
