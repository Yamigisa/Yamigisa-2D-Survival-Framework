using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Equip", menuName = "Yamigisa/Actions/Equip", order = 50)]
    public class ActionEquip : ActionBase
    {
        [Header("Action Names")]
        [SerializeField] private string equipTitle = "Equip";
        [SerializeField] private string unequipTitle = "Unequip";

        public override string GetActionName(Component context)
        {
            ItemSlot slot = context as ItemSlot;
            if (slot == null || slot.ItemData == null)
                return equipTitle;

            if (slot.ItemData.itemType != ItemType.Equipment)
                return equipTitle;

            var equippedItem = EquipmentManager.instance.GetEquipped(slot.ItemData.equipmentSlotType);

            // If same item already equipped → show Unequip
            if (equippedItem == slot.ItemData)
                return unequipTitle;

            return equipTitle;
        }

        public override void DoAction(Character character, ItemSlot slot)
        {
            if (slot == null || slot.ItemData == null)
                return;

            if (slot.ItemData.itemType != ItemType.Equipment)
                return;

            var equippedItem = EquipmentManager.instance.GetEquipped(slot.ItemData.equipmentSlotType);

            // ===== UNEQUIP =====
            // ===== UNEQUIP =====
            if (equippedItem == slot.ItemData)
            {
                ItemData itemToReturn = slot.ItemData; // cache BEFORE unequip
                EquipmentManager.instance.Unequip(slot.ItemData.equipmentSlotType);

                Inventory.Instance.AddItem(
                    itemToReturn,
                    1
                );
                return;
            }

            // ===== EQUIP =====
            ItemData previous = equippedItem;

            bool equipped = EquipmentManager.instance.Equip(slot.ItemData);

            if (equipped)
            {
                Inventory.Instance.ReduceSlotAmount(slot, 1);

                if (previous != null)
                {
                    Inventory.Instance.AddItem(previous, 1);
                }
            }
        }

        public override bool CanDoAction(Component context)
        {
            ItemSlot slot = context as ItemSlot;
            if (slot == null || slot.ItemData == null)
                return false;

            return slot.ItemData.itemType == ItemType.Equipment;
        }
    }
}