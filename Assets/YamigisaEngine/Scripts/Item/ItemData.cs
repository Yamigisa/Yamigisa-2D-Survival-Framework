using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(menuName = "Yamigisa/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public Sprite iconWorld;
        public Sprite iconInventory;
        [TextArea] public string description;
        public ItemType itemType;
        public int maxAmount = 99;
        public bool isDroppable = true;
        public bool isStackable = true;

        public List<GroupData> groups = new List<GroupData>();
        public List<ActionBase> itemActions;

        // Consumable Effects
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

        // ===== DESTRUCTIBLE (Resource only) =====
        [Header("Destructible (Resource only)")]
        public bool destructible = false;
        [Min(1)] public int destructibleHP = 100;
        public List<GroupData> destructibleRequiredGroups = new List<GroupData>(); // MUST have at least one
        public List<LootEntry> destructibleLoots = new List<LootEntry>();          // per-entry amount + chance

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
