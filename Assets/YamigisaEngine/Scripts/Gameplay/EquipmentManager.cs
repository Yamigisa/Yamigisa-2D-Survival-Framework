using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class EquipmentManager : MonoBehaviour, ISavable
    {
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private List<EquipmentSlot> equipmentSlots;

        private Dictionary<EquipmentSlotType, EquipmentSlot> slotLookup;

        public static EquipmentManager instance { get; private set; }

        private void Awake()
        {
            instance = this;

            slotLookup = new Dictionary<EquipmentSlotType, EquipmentSlot>();

            foreach (var slot in equipmentSlots)
            {
                slotLookup[slot.SlotType] = slot;
            }

            equipmentPanel.SetActive(false);
        }

        public bool Equip(ItemData item)
        {
            if (item == null || item.itemType != ItemType.Equipment)
                return false;

            if (!slotLookup.TryGetValue(item.equipmentSlotType, out var slot))
            {
                return false;
            }

            ItemData previous = slot.GetEquippedItem();

            if (previous != null)
            {
                Inventory.Instance.AddItem(previous, 1);
            }

            slot.Equip(item);
            RecalculateStats();

            return true;
        }

        public void Unequip(EquipmentSlotType type)
        {
            if (!slotLookup.TryGetValue(type, out var slot))
                return;

            slot.Unequip();
            RecalculateStats();
        }

        public ItemData GetEquipped(EquipmentSlotType type)
        {
            if (!slotLookup.TryGetValue(type, out var slot))
                return null;

            return slot.GetEquippedItem();
        }

        public IEnumerable<ItemData> GetAllEquippedItems()
        {
            foreach (var slot in equipmentSlots)
            {
                var item = slot.GetEquippedItem();
                if (item != null)
                    yield return item;
            }
        }

        private void RecalculateStats()
        {
            float damageAdditive = 0f;
            float damagePercent = 0f;

            float moveAdditive = 0f;
            float movePercent = 0f;

            Dictionary<AttributeType, float> attributeAdditive = new();
            Dictionary<AttributeType, float> attributePercent = new();

            foreach (var item in GetAllEquippedItems())
            {
                if (item.equipmentStats == null)
                    continue;

                foreach (var mod in item.equipmentStats)
                {
                    switch (mod.statType)
                    {
                        case StatType.Damage:

                            if (mod.valueType == StatValueType.Additive)
                                damageAdditive += mod.value;

                            else if (mod.valueType == StatValueType.Percent)
                                damagePercent += mod.value;

                            break;

                        case StatType.MovementSpeed:

                            if (mod.valueType == StatValueType.Additive)
                                moveAdditive += mod.value;

                            else if (mod.valueType == StatValueType.Percent)
                                movePercent += mod.value;

                            break;

                        case StatType.Attribute:

                            if (!attributeAdditive.ContainsKey(mod.attributeType))
                                attributeAdditive[mod.attributeType] = 0f;

                            if (!attributePercent.ContainsKey(mod.attributeType))
                                attributePercent[mod.attributeType] = 0f;

                            if (mod.valueType == StatValueType.Additive)
                                attributeAdditive[mod.attributeType] += mod.value;

                            else if (mod.valueType == StatValueType.Percent)
                                attributePercent[mod.attributeType] += mod.value;

                            break;
                    }
                }
            }

            var character = Character.instance;
            if (character == null)
                return;

            // ===== DAMAGE =====
            float baseDamage = character.characterCombat.handDamage;

            float finalDamage = baseDamage + damageAdditive;
            finalDamage += baseDamage * damagePercent;

            character.characterCombat.SetEquipmentDamage(Mathf.RoundToInt(finalDamage));

            // ===== MOVEMENT SPEED =====
            character.characterMovement.SetEquipmentMoveSpeedBonus(moveAdditive, movePercent);

            // ===== ATTRIBUTES (MAX ONLY) =====
            character.characterAttribute.SetEquipmentModifiers(
                attributeAdditive,
                attributePercent
            );
        }

        public void Save(ref SaveGameData data)
        {
            data.equippedItems.Clear();

            foreach (var slot in equipmentSlots)
            {
                var item = slot.GetEquippedItem();
                if (item == null) continue;

                data.equippedItems.Add(new EquipmentSaveData
                {
                    slotType = slot.SlotType,
                    itemId = item.Id
                });
            }
        }

        public void Load(SaveGameData data)
        {
            if (data.equippedItems == null) return;

            // Clear all equipment first
            foreach (var slot in equipmentSlots)
                slot.Unequip();

            foreach (var saved in data.equippedItems)
            {
                if (!slotLookup.TryGetValue(saved.slotType, out var slot))
                    continue;

                ItemData item = ItemDatabase.Get(saved.itemId);
                if (item == null) continue;

                slot.Equip(item);
            }

            RecalculateStats();
        }

        public void ShowEquipmentPanel()
        {
            equipmentPanel.SetActive(true);
        }

        public void HideEquipmentPanel()
        {
            equipmentPanel.SetActive(false);
        }
    }
    [System.Serializable]
    public class EquipmentSaveData
    {
        public EquipmentSlotType slotType;
        public string itemId;
    }
}