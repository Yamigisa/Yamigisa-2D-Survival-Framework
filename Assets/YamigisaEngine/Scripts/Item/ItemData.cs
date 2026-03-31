using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(menuName = "Yamigisa/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Save ID (DO NOT CHANGE)")]
        [HideInInspector][SerializeField] private string id;
        public string Id => id;

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
        [Header("Resource Regrowth")]
        [Tooltip("Only used if ItemType = Resource. If empty, the object is destroyed instantly when picked.")]
        public List<ResourceGrowthStage> growthStages = new();

        [Tooltip("Use in-game time or real seconds for stage duration.")]
        public GrowthTimeMode growthTimeMode = GrowthTimeMode.GameMinutes;

        public bool HasGrowthStages =>
            itemType == ItemType.Resource &&
            growthStages != null &&
            growthStages.Count > 0;

        [Header("Consumable Effects")]
        public List<ConsumableEffect> consumableEffects = new List<ConsumableEffect>();

        [Header("Equipment")]
        [Tooltip("Only used if ItemType = Equipment")]
        public EquipmentSlotType equipmentSlotType = EquipmentSlotType.None;

        [Header("Bag Settings")]
        [Tooltip("Only used if EquipmentSlotType = Bag")]
        public int bagSizeIncrease = 5;

        [Header("Equipment Effects")]
        public List<EquipmentStatModifier> equipmentStats = new();

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

    public enum GrowthTimeMode
    {
        GameMinutes,
        RealSeconds
    }

    [System.Serializable]
    public class ResourceGrowthStage
    {
        public Sprite sprite;

        [Min(0f)]
        [Tooltip("Duration to move from this stage to the next stage.")]
        public float duration = 5f;
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

    [System.Serializable]
    public class ConsumableEffect
    {
        public ConsumableEffectType effectType;

        [Header("Target")]
        public AttributeType attributeType;

        [Header("Instant")]
        public int instantAmount;

        [Header("Over Time")]
        public int amountPerTick;
        public float tickInterval;
        public float duration;

        [Header("Buff Settings")]
        public BuffType buffType;
        public float buffAmount;
    }

    public enum BuffType
    {
        None,
        MovementSpeedMultiplier,
        DamageMultiplier,
    }

    public enum ConsumableEffectType
    {
        Instant,
        OverTime,
        DurationBuff
    }

    public enum EquipmentSlotType
    {
        None,
        Head,
        Chest,
        Legs,
        Accessory,
        Bag
    }

    [System.Serializable]
    public class EquipmentStatModifier
    {
        public StatType statType;

        [Tooltip("Only used if statType == Attribute")]
        public AttributeType attributeType;

        public StatValueType valueType;
        public float value;
    }

    public enum StatType
    {
        Damage,
        MovementSpeed,
        Attribute
    }

    public enum StatValueType
    {
        Additive,
        Percent
    }
}