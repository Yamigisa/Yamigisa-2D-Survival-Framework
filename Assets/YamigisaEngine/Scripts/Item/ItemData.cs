using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(menuName = "Yamigisa/Item")]
    public class ItemData : ScriptableObject
    {
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

        // ===== CRAFTING =====
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

    // ===== New Loot entry =====
    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Min(1)] public int amount = 1;
        [Range(0f, 100f)] public float dropChancePercent = 100f; // 100 = always drops
    }
}
