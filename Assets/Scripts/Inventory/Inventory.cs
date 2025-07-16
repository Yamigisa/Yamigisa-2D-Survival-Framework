using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class Inventory : MonoBehaviour
    {
        [Header("Item Prefab")]
        [SerializeField] private InventoryItem inventoryItemPrefab;

        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryPanel;
        public GameObject InventoryPanel => inventoryPanel;

        private List<InventoryItemData> inventoryItems = new();

        public static Inventory Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowInventory()
        {
            inventoryPanel.SetActive(true);
        }

        public void HideInventory()
        {
            inventoryPanel.SetActive(false);
        }

        public void AddItem(ItemData data, int amountToAdd = 1)
        {
            InventoryItemData existingItem = inventoryItems.Find(i => i.itemData == data);

            if (existingItem != null)
            {
                existingItem.amount += amountToAdd;
            }
            else
            {
                InventoryItemData newItem = new InventoryItemData
                {
                    itemData = data,
                    amount = amountToAdd
                };
                inventoryItems.Add(newItem);
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            foreach (Transform child in inventoryContent)
                Destroy(child.gameObject);

            foreach (var item in inventoryItems)
            {
                InventoryItem uiItem = Instantiate(inventoryItemPrefab, inventoryContent);
                uiItem.Initialize(item.itemData);
            }
        }
    }
}