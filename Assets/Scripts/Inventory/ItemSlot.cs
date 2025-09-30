using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Yamigisa
{
    public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Item UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private Button itemButton;

        [Header("Item Actions")]
        private List<ActionBase> itemActions;


        [Header("Slot Flags")]
        private bool isQuickSlot = false;

        [Header("Highlight")]
        [SerializeField] private Color selectedColor = Color.red;
        [SerializeField] private Color normalColor = Color.white;

        public ItemData ItemData;
        public int Amount;
        private bool hasItem;
        public bool HasItem => hasItem;

        private void Start()
        {
            amountText.text = "";
            if (itemButton && itemButton.image) itemButton.image.color = normalColor;
        }

        public void MarkAsQuickSlot(bool value)
        {
            isQuickSlot = value;
        }

        public void SetSelectedVisual(bool selected)
        {
            if (itemButton && itemButton.image) itemButton.image.color = selected ? selectedColor : normalColor;
            if (icon && icon.enabled) icon.color = selected ? selectedColor : normalColor;
        }

        public void SetItem(ItemData data, int _amount = 1)
        {
            ItemData = data;
            hasItem = true;

            icon.enabled = true;
            icon.sprite = data.iconInventory;

            if (!data.isStackable)
            {
                Amount = 1;
                amountText.text = "";
            }
            else
            {
                int cap = Mathf.Max(1, data.maxAmount);
                Amount = Mathf.Clamp(_amount, 1, cap);
                amountText.text = Amount <= 1 ? "" : $"{Amount}";
            }
        }

        private void DropItem()
        {
            ResetSlot();
        }

        public void ResetSlot()
        {
            ItemData = null;
            Amount = 0;
            hasItem = false;

            icon.sprite = null;
            icon.enabled = false;
            icon.color = normalColor;

            amountText.text = "";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ItemData == null) return;
            Inventory.Instance.ShowTooltip(ItemData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Inventory.Instance.HideTooltip();
        }
    }
}
