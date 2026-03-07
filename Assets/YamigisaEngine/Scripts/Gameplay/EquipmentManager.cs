using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class EquipmentManager : MonoBehaviour, ISavable
    {
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private List<EquipmentSlot> equipmentSlots; // armor, accessory etc
        [SerializeField] private List<EquipmentSlot> bagSlots; // bag equipment

        private Dictionary<EquipmentSlotType, EquipmentSlot> slotLookup;


        private void Awake()
        {
            slotLookup = new Dictionary<EquipmentSlotType, EquipmentSlot>();

            foreach (var slot in equipmentSlots)
            {
                slotLookup[slot.SlotType] = slot;
            }

            // ❌ DO NOT ADD bagSlots TO slotLookup

            equipmentPanel.SetActive(false);
        }
        private bool IsBag(ItemData item)
        {
            if (item == null) return false;
            return item.equipmentSlotType == EquipmentSlotType.Bag;
        }

        public bool Equip(ItemData item)
        {
            if (item == null || item.itemType != ItemType.Equipment)
                return false;

            EquipmentSlot slot = null;

            if (item.equipmentSlotType == EquipmentSlotType.Bag)
            {
                foreach (var s in bagSlots)
                {
                    if (s.GetEquippedItem() == null)
                    {
                        slot = s;
                        break;
                    }
                }

                if (slot == null)
                    return false;
            }
            else
            {
                if (!slotLookup.TryGetValue(item.equipmentSlotType, out slot))
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

        public bool Unequip(EquipmentSlotType type)
        {
            EquipmentSlot slot = null;

            if (type == EquipmentSlotType.Bag)
            {
                foreach (var s in bagSlots)
                {
                    if (s.GetEquippedItem() != null)
                    {
                        slot = s;
                        break;
                    }
                }

                if (slot == null)
                    return false;
            }
            else
            {
                if (!slotLookup.TryGetValue(type, out slot))
                    return false;
            }

            ItemData equipped = slot.GetEquippedItem();
            if (equipped == null)
                return false;

            // BAG CAPACITY CHECK
            if (IsBag(equipped))
            {
                int bagSize = equipped.bagSizeIncrease;

                int itemsInInventory = Inventory.Instance.TotalItemCount();
                int newCapacity = Inventory.Instance.GetBaseCapacity() + (GetCurrentBagBonus() - bagSize);

                if (itemsInInventory > newCapacity)
                {
                    return false;
                }
            }

            // REMOVE FROM SLOT
            slot.Unequip();

            // ADD BACK TO INVENTORY (THIS WAS MISSING)
            Inventory.Instance.AddItem(equipped, 1);

            // UPDATE BAG SIZE
            RecalculateBagSize();

            // UPDATE STATS
            RecalculateStats();

            return true;
        }

        private int GetCurrentBagBonus()
        {
            int bonus = 0;

            foreach (var slot in bagSlots)
            {
                var item = slot.GetEquippedItem();
                if (item != null)
                    bonus += item.bagSizeIncrease;
            }

            return bonus;
        }

        public bool TryUnequip(EquipmentSlotType type)
        {
            EquipmentSlot slot = null;

            if (type == EquipmentSlotType.Bag)
            {
                foreach (var s in bagSlots)
                {
                    if (s.GetEquippedItem() != null)
                    {
                        slot = s;
                        break;
                    }
                }

                if (slot == null)
                    return false;
            }
            else
            {
                if (!slotLookup.TryGetValue(type, out slot))
                    return false;
            }

            ItemData item = slot.GetEquippedItem();
            if (item == null)
                return false;

            if (item.equipmentSlotType == EquipmentSlotType.Bag)
            {
                int bagBonus = item.bagSizeIncrease;

                int itemsUsed = Inventory.Instance.GetUsedSlotCount();
                int newCapacity = Inventory.Instance.GetCurrentCapacity() - bagBonus;

                if (itemsUsed > newCapacity)
                {
                    return false;
                }
            }

            slot.Unequip();

            // 🔥 ADD BACK TO INVENTORY
            Inventory.Instance.AddItem(item, 1);

            // 🔥 FORCE BAG SIZE UPDATE
            RecalculateBagSize();

            // 🔥 RECALCULATE STATS
            RecalculateStats();


            return true;
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

            foreach (var slot in bagSlots)
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


            RecalculateBagSize();
        }

        public void Save(ref SaveGameData data)
        {
            if (!data.saveManager.SaveEquipment)
                return;
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

        private void RecalculateBagSize()
        {
            int bagBonus = 0;

            foreach (var slot in bagSlots)
            {
                var item = slot.GetEquippedItem();
                if (item == null) continue;

                bagBonus += item.bagSizeIncrease;
            }

            if (Inventory.Instance != null)
            {
                Inventory.Instance.SetEquipmentBagBonus(bagBonus);
                Debug.Log("Bag bonus recalculated: " + bagBonus);
            }
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