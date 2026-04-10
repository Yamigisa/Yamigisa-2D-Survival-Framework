using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class CraftItemSlot : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText; // Legacy Text inspectorable
        public Button button;

        private ItemData itemData;
        private GroupData groupData;

        public void BindItem(ItemData data, bool isInteractable = true, string amount = "")
        {
            itemData = data;
            groupData = null;

            if (icon)
                icon.sprite = itemData != null ? itemData.iconInventory : null;

            SetAmountText(amount);

            if (isInteractable)
                RefreshInteractable();
            else if (button)
                button.interactable = false;
        }

        public void BindGroup(GroupData data, bool isInteractable = true, string amount = "")
        {
            groupData = data;
            itemData = null;

            if (icon)
                icon.sprite = groupData != null ? groupData.icon : null;

            SetAmountText(amount);

            if (button)
                button.interactable = isInteractable;
        }

        private void SetAmountText(string amount)
        {
            if (!amountText)
                return;

            bool hasAmount = !string.IsNullOrWhiteSpace(amount);

            amountText.gameObject.SetActive(hasAmount);
            amountText.text = hasAmount ? amount : "";
        }

        public void RefreshInteractable()
        {
            if (!button || itemData == null || Inventory.Instance == null)
            {
                if (button)
                    button.interactable = false;
                return;
            }

            button.interactable = Inventory.Instance.CanCraft(itemData);
        }
    }
}