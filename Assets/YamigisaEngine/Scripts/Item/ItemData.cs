using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(menuName = "Yamigisa/Item")]
    public class ItemData : ScriptableObject
    {
        // ===================== SAVE ID =====================
        [Header("Save ID (DO NOT CHANGE)")]
        [SerializeField] private string id;
        public string Id => id;

        // ===================== STATIC DATABASE =====================
        private static Dictionary<string, ItemData> lookup;

        public static ItemData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (lookup == null)
                BuildLookup();

            lookup.TryGetValue(id, out ItemData item);
            return item;
        }

        private static void BuildLookup()
        {
            lookup = new Dictionary<string, ItemData>();

            ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData item = allItems[i];
                if (item == null || string.IsNullOrEmpty(item.id)) continue;

                if (!lookup.ContainsKey(item.id))
                    lookup.Add(item.id, item);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        // ===================== EXISTING DATA =====================

        [Header("Item Prefab")]
        public GameObject itemPrefab;

        [Header("Item Information")]
        public string itemName;
        public Sprite iconWorld;
        public Sprite iconInventory;
        [TextArea] public string description;

        [Header("Item Properties")]
        public ItemType itemType;
        public int maxAmount = 99;
        public bool isDroppable = true;
        public bool isStackable = true;

        [Header("Item Groups")]
        public List<GroupData> groups = new List<GroupData>();

        [Header("Item Actions")]
        public List<ActionBase> itemActions;

        [Header("Consumable Effect")]
        public int increaseHealth = 0;
        public int increaseHunger = 0;
        public int increaseThirst = 0;

        // Equipment Effect
        public int damage = 0;

        [Header("Crafting (Recipe)")]
        public bool isCraftable = false;
        public List<CraftGroupData> craftGroupsNeeded = new List<CraftGroupData>();
        public List<CraftItemData> craftItemsNeeded = new List<CraftItemData>();
        [Min(1)] public int craftResultAmount = 1;

        public void ChangeDroppableState(bool state) => isDroppable = state;
    }

    public enum ItemType
    {
        Resource,
        Equipment,
        Consumable,
        Placeable,
    }

    [System.Serializable]
    public class CraftGroupData
    {
        public GroupData GroupData;
        public int Amount;
    }

    [System.Serializable]
    public class CraftItemData
    {
        public ItemData itemData;
        public int Amount;
    }

    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
        [Range(0f, 100f)] public float dropChancePercent = 100f;
    }
}
