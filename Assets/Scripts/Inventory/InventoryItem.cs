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

        private InventoryItemData itemInstance;

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
                dropButton.onClick.AddListener(DropItem);
            }
            else
            {
                dropButton.gameObject.SetActive(false);
            }

            if (itemData.itemData.itemType == ItemType.Consumable)
            {
                useButton.gameObject.SetActive(true);
                useButton.onClick.AddListener(UseItem);
            }
            else
            {
                useButton.gameObject.SetActive(false);
            }
        }

        private void ToggleDropdown()
        {
            buttonsPanel.SetActive(!buttonsPanel.activeSelf);
        }

        private void DropItem()
        {
            buttonsPanel.SetActive(false);

            Vector2 dropPosition = Vector2.zero; // placeholder position

            // Create an empty GameObject
            GameObject drop = new GameObject(itemInstance.itemData.itemName);

            // Position it in the world
            drop.transform.position = dropPosition;

            // Add visual representation (sprite)
            SpriteRenderer renderer = drop.AddComponent<SpriteRenderer>();
            renderer.sprite = itemInstance.itemData.iconWorld;

            // Optional: Add 2D collider (so it can be detected)
            CircleCollider2D collider = drop.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;

            // Optional: Add Rigidbody2D (if you want physics interaction)
            drop.AddComponent<Rigidbody2D>().gravityScale = 0;

            // Add the collectible behavior
            CollectibleItem runtimeCollectible = drop.AddComponent<CollectibleItem>();

            // Initialize item data
            runtimeCollectible.Initialize(itemInstance.itemData, itemInstance.amount);

            // Remove from inventory
            Inventory.Instance.RemoveItem(itemInstance);
        }

        private void UseItem()
        {
            buttonsPanel.SetActive(false);
            Inventory.Instance.UseItem(itemInstance.itemData);
            Inventory.Instance.RemoveItem(itemInstance);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (buttonsPanel.activeSelf)
                return;

            Inventory.Instance.ShowTooltip(itemInstance);
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