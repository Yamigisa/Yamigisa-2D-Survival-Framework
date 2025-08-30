using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Yamigisa
{
    public class InventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Item UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private Button itemButton;

        [Header("Item Actions")]
        public List<ActionBase> itemActions;

        [Header("Buttons")]
        [SerializeField] private GameObject buttonsPanel;
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;

        [Header("Slot Flags")]
        [SerializeField] private bool isQuickSlot = false;

        [Header("Highlight")]
        [SerializeField] private Color selectedColor = Color.red;
        [SerializeField] private Color normalColor = Color.white;

        public ItemData ItemData;
        public int Amount;
        private bool hasItem;
        public bool HasItem => hasItem;

        private void OnEnable()
        {
            itemButton.onClick.AddListener(ToggleDropdown);
        }

        private void OnDisable()
        {
            itemButton.onClick.RemoveListener(ToggleDropdown);
            buttonsPanel.SetActive(false);
            dropButton.onClick.RemoveAllListeners();
            useButton.onClick.RemoveAllListeners();
        }

        private void Start()
        {
            amountText.text = "";
            if (icon) icon.enabled = false;
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

            icon.sprite = data.iconInventory;
            icon.enabled = true;

            if (!data.isStackable)
            {
                Amount = 1;
                amountText.text = "1";
            }
            else
            {
                int cap = Mathf.Max(1, data.maxAmount);
                Amount = Mathf.Clamp(_amount, 1, cap);
                amountText.text = Amount <= 1 ? "" : $"{Amount}";
            }

            buttonsPanel.SetActive(false);
            dropButton.onClick.RemoveAllListeners();
            useButton.onClick.RemoveAllListeners();

            dropButton.onClick.RemoveAllListeners();
            useButton.onClick.RemoveAllListeners();

            if (ItemData.isDroppable)
            {
                dropButton.gameObject.SetActive(true);
                dropButton.interactable = true;
                dropButton.onClick.AddListener(DropItem);
            }
            else
            {
                dropButton.interactable = false;
            }

            if (ItemData.itemType == ItemType.Consumable)
            {
                useButton.gameObject.SetActive(true);
                useButton.interactable = true;
                useButton.onClick.AddListener(() => Inventory.Instance.UseSlot(this));
            }
            else
            {
                useButton.interactable = false;
            }
        }

        private void ToggleDropdown()
        {
            if (isQuickSlot) return;
            if (ItemData == null) return;
            buttonsPanel.SetActive(!buttonsPanel.activeSelf);
        }

        private void DropItem()
        {
            buttonsPanel.SetActive(false);
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
            buttonsPanel.SetActive(false);

            dropButton.onClick.RemoveAllListeners();
            useButton.onClick.RemoveAllListeners();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (buttonsPanel.activeSelf) return;
            if (ItemData == null) return;
            Inventory.Instance.ShowTooltip(ItemData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Inventory.Instance.HideTooltip();
        }
    }
}
