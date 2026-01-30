using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class Storage : MonoBehaviour
    {
        [Header("Storage Settings")]
        [SerializeField] private List<Inventory.StartingItem> startingItems;
        [SerializeField] private int storageSize = 20;

        private List<ItemSlot> itemSlots = new();
        private List<StoredItem> storedItems = new();

        [HideInInspector] public InventoryPanel inventoryStorage;

        private bool initialized = false;

        public void OpenStorage()
        {
            if (Inventory.Instance.currentStorage != null) return;

            Character.instance.DisableMovements();
            // Seed once
            if (!initialized)
            {
                InitializeStorage();
                initialized = true;
            }

            Inventory.Instance.ShowInventory();
            Inventory.Instance.currentStorage = this;
            inventoryStorage = Inventory.Instance.CreateInventoryPanel();
            inventoryStorage.sortButton.gameObject.SetActive(true);

            itemSlots.Clear();

            for (int i = 0; i < storageSize; i++)
            {
                ItemSlot slot = Inventory.Instance.CreateItemSlot(inventoryStorage.inventoryContent);
                itemSlots.Add(slot);
            }

            // Populate UI from real data
            for (int i = 0; i < storedItems.Count; i++)
            {
                itemSlots[i].SetItem(storedItems[i].item, storedItems[i].amount);
            }
        }

        private void InitializeStorage()
        {
            storedItems.Clear();

            foreach (var entry in startingItems)
            {
                storedItems.Add(new StoredItem
                {
                    item = entry.item,
                    amount = Mathf.Max(1, entry.amount)
                });
            }
        }

        private void Update()
        {
            if (Character.instance.characterControls.IsAnyKeyPressedDown(
                Character.instance.characterControls.cancelKey))
            {
                CloseStorage();
            }
        }

        public void CloseStorage()
        {
            if (inventoryStorage != null)
            {
                Destroy(inventoryStorage.gameObject);
            }

            Character.instance.EnableMovements();
            Inventory.Instance.currentStorage = null;
            Inventory.Instance.HideInventory();
        }

        public List<ItemSlot> GetSlots()
        {
            return itemSlots;
        }

        [System.Serializable]
        public class StoredItem
        {
            public ItemData item;
            public int amount;
        }
    }
}