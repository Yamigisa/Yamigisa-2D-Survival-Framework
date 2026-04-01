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
                if (slot != null)
                    slotLookup[slot.SlotType] = slot;
            }

            // keep bagSlots separate on purpose
            if (equipmentPanel != null)
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
                // find empty bag slot first
                foreach (var s in bagSlots)
                {
                    if (s != null && s.GetEquippedItem() == null)
                    {
                        slot = s;
                        break;
                    }
                }

                // if all occupied, replace first occupied bag slot
                if (slot == null)
                {
                    foreach (var s in bagSlots)
                    {
                        if (s != null && s.GetEquippedItem() != null)
                        {
                            slot = s;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (!slotLookup.TryGetValue(item.equipmentSlotType, out slot) || slot == null)
                    return false;
            }

            if (slot == null)
                return false;

            ItemData oldItem = slot.GetEquippedItem();

            // empty slot
            if (oldItem == null)
            {
                if (!slot.TrySetItem(item))
                    return false;

                RecalculateBagSize();
                RecalculateStats();
                return true;
            }

            // same item reference
            if (oldItem == item)
                return false;

            // before replacing, make sure old equipped item can go back into inventory
            if (Inventory.Instance == null)
                return false;

            if (!Inventory.Instance.CanAddItem(oldItem, 1))
            {
                return false;
            }

            // bag replacement safety
            if (IsBag(oldItem))
            {
                int oldBagBonus = oldItem.bagSizeIncrease;

                int totalCurrentBagBonus = GetCurrentBagBonus();
                int newBagBonusTotal = totalCurrentBagBonus - oldBagBonus;

                if (IsBag(item))
                    newBagBonusTotal += item.bagSizeIncrease;

                int newCapacity = Inventory.Instance.GetBaseCapacity() + newBagBonusTotal;
                int usedSlots = Inventory.Instance.GetUsedSlotCount();

                if (usedSlots > newCapacity)
                {
                    return false;
                }
            }

            // clear old slot first
            slot.Unequip();

            // return old item to inventory
            Inventory.Instance.AddItem(oldItem, 1);

            // place new item
            if (!slot.TrySetItem(item))
            {
                return false;
            }

            RecalculateBagSize();
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
                    if (s != null && s.GetEquippedItem() != null)
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

            if (Inventory.Instance == null)
                return false;

            if (!Inventory.Instance.CanAddItem(equipped, 1))
            {
                return false;
            }

            // bag capacity check
            if (IsBag(equipped))
            {
                int bagSize = equipped.bagSizeIncrease;
                int usedSlots = Inventory.Instance.GetUsedSlotCount();
                int newCapacity = Inventory.Instance.GetBaseCapacity() + (GetCurrentBagBonus() - bagSize);

                if (usedSlots > newCapacity)
                {
                    return false;
                }
            }

            slot.Unequip();
            //Inventory.Instance.AddItem(equipped, 1);

            RecalculateBagSize();
            RecalculateStats();
            return true;
        }

        public bool TryUnequip(EquipmentSlotType type)
        {
            return Unequip(type);
        }

        private int GetCurrentBagBonus()
        {
            int bonus = 0;

            foreach (var slot in bagSlots)
            {
                if (slot == null) continue;

                var item = slot.GetEquippedItem();
                if (item != null)
                    bonus += item.bagSizeIncrease;
            }

            return bonus;
        }

        public ItemData GetEquipped(EquipmentSlotType type)
        {
            if (type == EquipmentSlotType.Bag)
            {
                foreach (var slot in bagSlots)
                {
                    if (slot == null) continue;

                    var item = slot.GetEquippedItem();
                    if (item != null)
                        return item;
                }

                return null;
            }

            if (!slotLookup.TryGetValue(type, out var normalSlot))
                return null;

            return normalSlot.GetEquippedItem();
        }

        public IEnumerable<ItemData> GetAllEquippedItems()
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot == null) continue;

                var item = slot.GetEquippedItem();
                if (item != null)
                    yield return item;
            }

            foreach (var slot in bagSlots)
            {
                if (slot == null) continue;

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

            float baseDamage = character.characterCombat.handDamage;

            float finalDamage = baseDamage + damageAdditive;
            finalDamage += baseDamage * damagePercent;

            character.characterCombat.SetEquipmentDamage(Mathf.RoundToInt(finalDamage));
            character.characterMovement.SetEquipmentMoveSpeedBonus(moveAdditive, movePercent);
            character.characterAttribute.SetEquipmentModifiers(attributeAdditive, attributePercent);
        }

        public EquipmentSlot GetSlot(EquipmentSlotType slotType)
        {
            if (slotType == EquipmentSlotType.Bag)
            {
                foreach (var slot in bagSlots)
                {
                    if (slot != null)
                        return slot;
                }

                return null;
            }

            if (slotLookup == null)
                return null;

            slotLookup.TryGetValue(slotType, out EquipmentSlot slotFound);
            return slotFound;
        }

        public void Save(ref SaveGameData data)
        {
            if (!data.saveManager.SaveEquipment)
                return;

            data.equippedItems.Clear();

            foreach (var slot in equipmentSlots)
            {
                if (slot == null) continue;

                var item = slot.GetEquippedItem();
                if (item == null) continue;

                data.equippedItems.Add(new EquipmentSaveData
                {
                    slotType = slot.SlotType,
                    itemId = item.Id
                });
            }

            foreach (var slot in bagSlots)
            {
                if (slot == null) continue;

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
            if (data.equippedItems == null)
                return;

            foreach (var slot in equipmentSlots)
            {
                if (slot != null)
                    slot.Unequip();
            }

            foreach (var slot in bagSlots)
            {
                if (slot != null)
                    slot.Unequip();
            }

            foreach (var saved in data.equippedItems)
            {
                ItemData item = ItemDatabase.Get(saved.itemId);
                if (item == null) continue;

                if (saved.slotType == EquipmentSlotType.Bag)
                {
                    foreach (var bagSlot in bagSlots)
                    {
                        if (bagSlot == null) continue;
                        if (bagSlot.GetEquippedItem() != null) continue;

                        bagSlot.Equip(item);
                        break;
                    }
                }
                else
                {
                    if (!slotLookup.TryGetValue(saved.slotType, out var slot))
                        continue;

                    slot.Equip(item);
                }
            }

            RecalculateBagSize();
            RecalculateStats();
        }

        private void RecalculateBagSize()
        {
            int bagBonus = 0;

            foreach (var slot in bagSlots)
            {
                if (slot == null) continue;

                var item = slot.GetEquippedItem();
                if (item == null) continue;

                bagBonus += item.bagSizeIncrease;
            }

            if (Inventory.Instance != null)
                Inventory.Instance.SetEquipmentBagBonus(bagBonus);
        }

        public void ShowEquipmentPanel()
        {
            if (equipmentPanel != null)
                equipmentPanel.SetActive(true);
        }

        public void HideEquipmentPanel()
        {
            if (equipmentPanel != null)
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