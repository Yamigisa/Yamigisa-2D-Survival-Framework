using UnityEngine;

namespace Yamigisa
{
    public class EquipmentSlot : MonoBehaviour
    {
        [SerializeField] private EquipmentSlotType slotType;
        private ItemSlot itemSlot;

        public EquipmentSlotType SlotType => slotType;

        private void Awake()   // ← CHANGE FROM Start TO Awake
        {
            itemSlot = GetComponent<ItemSlot>();
        }

        public bool CanEquip(ItemData item)
        {
            if (item == null) return false;
            if (item.itemType != ItemType.Equipment) return false;

            return item.equipmentSlotType == slotType;
        }

        public void Equip(ItemData item)
        {
            TrySetItem(item);
        }

        public ItemData GetEquippedItem()
        {
            if (itemSlot == null) return null;   // safety
            if (!itemSlot.HasItem) return null;
            return itemSlot.ItemData;
        }

        public void Unequip()
        {
            if (itemSlot == null) return;
            itemSlot.ResetSlot();
        }

        public bool TrySetItem(ItemData item)
        {
            if (!CanEquip(item))
                return false;

            itemSlot.SetItem(item, 1);
            return true;
        }
    }
}