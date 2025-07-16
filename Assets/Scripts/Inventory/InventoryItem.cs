using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yamigisa;

namespace Yamigisa
{
    public class InventoryItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private GameObject descriptionPanel;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public ItemData itemData;
        private int amount;

        public void Initialize(ItemData _item)
        {
            itemData = _item;
            icon.sprite = itemData.iconInventory;
            nameText.text = itemData.itemName;
            amountText.text = $"x{amount}";
            descriptionText.text = itemData.description;
        }

        // Optional hover UI
        public void OnPointerEnter() => descriptionPanel.SetActive(true);
        public void OnPointerExit() => descriptionPanel.SetActive(false);
    }
}

[System.Serializable]
public class InventoryItemData
{
    public ItemData itemData;
    public int amount;
}