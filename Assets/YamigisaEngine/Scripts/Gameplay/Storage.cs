using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class Storage : MonoBehaviour, ISavable
    {
        [SerializeField] private string storageId;

        [Header("Storage Settings")]
        [SerializeField] private List<Inventory.StartingItem> startingItems;
        [SerializeField] private int storageSize = 20;

        private List<ItemSlot> itemSlots = new();
        private List<StoredItem> storedItems = new();

        [HideInInspector] public InventoryPanel inventoryStorage;

        private bool initialized = false;

        private bool isOpened = false;

        private void Awake()
        {
            if (string.IsNullOrEmpty(storageId))
            {
                storageId = $"{gameObject.scene.name}:{gameObject.name}:{transform.position}";
            }
        }

        public void OpenStorage()
        {
            if (Inventory.Instance.currentStorage != null && isOpened) return;

            isOpened = true;

            // 🔴 CRITICAL FIX
            if (!initialized && storedItems.Count == 0)
            {
                InitializeStorage();
                initialized = true;
            }

            Inventory.Instance.ShowInventory();
            Inventory.Instance.currentStorage = this;

            inventoryStorage = Inventory.Instance.CreateInventoryPanel();
            inventoryStorage.sortButton.gameObject.SetActive(true);

            itemSlots.Clear();

            inventoryStorage.ClearPanel();

            for (int i = 0; i < storageSize; i++)
            {
                ItemSlot slot =
                    Inventory.Instance.CreateItemSlot(inventoryStorage.inventoryContent);
                slot.SetOwnerStorage(this);
                itemSlots.Add(slot);
            }

            foreach (var stored in storedItems)
            {
                if (stored.index < 0 || stored.index >= itemSlots.Count)
                    continue;

                itemSlots[stored.index].SetItem(stored.item, stored.amount);
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

        public void RemoveStoredItem(ItemData item, int amount)
        {
            for (int i = storedItems.Count - 1; i >= 0; i--)
            {
                if (storedItems[i].item == item)
                {
                    storedItems[i].amount -= amount;

                    if (storedItems[i].amount <= 0)
                    {
                        storedItems.RemoveAt(i);
                    }

                    return;
                }
            }
        }

        public void CloseStorage()
        {
            isOpened = false;
            Character.instance.SetCharacterBusy(false);

            // 🔴 IMPORTANT: sync before destroying UI
            SyncStoredItemsFromUI();

            if (inventoryStorage != null)
            {
                Destroy(inventoryStorage.gameObject);
                inventoryStorage = null;
            }

            itemSlots.Clear();

            Character.instance.EnableMovements();
            Inventory.Instance.currentStorage = null;
            Inventory.Instance.HideInventory();
        }


        public List<ItemSlot> GetSlots()
        {
            return itemSlots;
        }

        private void SyncStoredItemsFromUI()
        {
            storedItems.Clear();

            for (int i = 0; i < itemSlots.Count; i++)
            {
                ItemSlot slot = itemSlots[i];
                if (slot == null || !slot.HasItem) continue;

                storedItems.Add(new StoredItem
                {
                    item = slot.ItemData,
                    amount = slot.Amount,
                    index = i
                });
            }
        }

        public void Save(ref SaveGameData data)
        {
            if (data.storages == null)
                data.storages = new List<StorageSaveData>();

            // REMOVE OLD ENTRY
            data.storages.RemoveAll(s => s.storageId == storageId);

            StorageSaveData save = new StorageSaveData
            {
                storageId = storageId
            };

            foreach (var item in storedItems)
            {
                save.items.Add(new InventoryItemSaveData
                {
                    itemId = item.item.Id,
                    amount = item.amount,
                    isQuick = false,
                    index = item.index
                });
            }

            data.storages.Add(save);
        }

        public void Load(SaveGameData data)
        {
            if (data.storages == null)
            {
                return;
            }

            StorageSaveData save =
                data.storages.Find(s => s.storageId == storageId);

            if (save == null)
            {
                return;
            }

            storedItems.Clear();

            foreach (var saved in save.items)
            {
                ItemData item = ItemDatabase.Get(saved.itemId);
                if (item == null)
                {

                    continue;
                }

                storedItems.Add(new StoredItem
                {
                    item = item,
                    amount = saved.amount,
                    index = saved.index
                });
            }

            initialized = true;
        }

        [System.Serializable]
        public class StoredItem
        {
            public ItemData item;
            public int amount;
            public int index; // ✅ REQUIRED
        }
    }
}