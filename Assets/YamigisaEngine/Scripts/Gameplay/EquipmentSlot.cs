using UnityEngine;

namespace Yamigisa
{
    public class EquipmentSlot : MonoBehaviour
    {
        [SerializeField] private EquipmentSlotType slotType;
        private ItemSlot itemSlot;

        public EquipmentSlotType SlotType => slotType;
        public ItemData ItemData => GetEquippedItem();

        private void Awake()
        {
            itemSlot = GetComponent<ItemSlot>();

            if (itemSlot == null)
                itemSlot = GetComponentInChildren<ItemSlot>();
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
            if (itemSlot == null)
            {
                return null;
            }

            if (!itemSlot.HasItem)
            {
                return null;
            }
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

            if (itemSlot == null)
                itemSlot = GetComponent<ItemSlot>();

            if (itemSlot == null)
                itemSlot = GetComponentInChildren<ItemSlot>();

            if (itemSlot == null)
                return false;

            itemSlot.ResetSlot();      // clear old visual/data first
            itemSlot.SetItem(item, 1); // equipment should always be 1
            return true;
        }
    }
}