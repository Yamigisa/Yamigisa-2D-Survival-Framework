using UnityEngine;
using UnityEngine.UI;
using Yamigisa;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class InventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Item UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private Button itemButton;

        [Header("Buttons")]
        [SerializeField] private GameObject buttonsPanel;
        [SerializeField] private Button useButton;
        [SerializeField] private Button dropButton;

        [SerializeField] private InventoryItemData itemInstance;
        public InventoryItemData ItemInstance => itemInstance;
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
        }

        public void Initialize(InventoryItemData itemData)
        {
            itemInstance = itemData;

            icon.sprite = itemData.itemData.iconInventory;
            if (itemData.amount <= 1)
            {
                amountText.text = "";
            }
            else
            {
                amountText.text = $"x{itemData.amount}";
            }

            buttonsPanel.SetActive(false);
            dropButton.onClick.RemoveAllListeners();
            useButton.onClick.RemoveAllListeners();

            if (itemData.itemData.isDroppable)
            {
                dropButton.gameObject.SetActive(true);
                dropButton.interactable = true;
                dropButton.onClick.AddListener(DropItem);
            }
            else
            {
                dropButton.interactable = false;
            }

            if (itemData.itemData.itemType == ItemType.Consumable)
            {
                useButton.gameObject.SetActive(true);
                useButton.interactable = true;
                useButton.onClick.AddListener(UseItem);
            }
            else
            {
                useButton.interactable = false;
            }
        }

        private void ToggleDropdown()
        {
            if (itemInstance == null)
                return;

            buttonsPanel.SetActive(!buttonsPanel.activeSelf);
        }

        private void DropItem()
        {
            buttonsPanel.SetActive(false);

            Inventory.Instance.RemoveItem(this);
        }

        private void UseItem()
        {
            buttonsPanel.SetActive(false);
            Inventory.Instance.UseItem(itemInstance.itemData);
        }

        public void ReduceAmount(int amount)
        {
            itemInstance.amount -= amount;

            if (itemInstance.amount <= 0)
            {
                ResetSlot();
                Inventory.Instance.RemoveItem(this);
            }
        }

        public void ResetSlot()
        {
            itemInstance = null;
            icon.sprite = null;
            amountText.text = "";
            buttonsPanel.SetActive(false);

            dropButton.onClick.RemoveAllListeners();
            useButton.onClick.RemoveAllListeners();

            Debug.Log("Reset Slot");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (buttonsPanel.activeSelf)
                return;

            if (itemInstance == null || itemInstance.itemData == null)
                return;

            Inventory.Instance.ShowTooltip(itemInstance.itemData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Inventory.Instance.HideTooltip();
        }
    }
}

[System.Serializable]
public class InventoryItemData
{
    public ItemData itemData;
    public int amount;
}