using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Item Prefab")]
        [SerializeField] private InventoryItem itemPrefab;

        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryPanel;
        public GameObject InventoryPanel => inventoryPanel;

        private List<InventoryItem> itemList = new List<InventoryItem>();

        public void InitializeInventory(int inventorySize)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                InventoryItem item = Instantiate(itemPrefab, inventoryContent);
                itemList.Add(item);
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
    }
}
