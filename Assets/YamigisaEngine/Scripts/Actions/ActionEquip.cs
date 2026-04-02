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

            var equipmentManager = Inventory.Instance?.equipmentManager;
            if (equipmentManager == null)
                return equipTitle;

            var equippedItem = equipmentManager.GetEquipped(slot.ItemData.equipmentSlotType);

            if (equippedItem == slot.ItemData)
                return unequipTitle;

            return equipTitle;
        }

        public override void DoAction(Character character, ItemSlot slot)
        {
            if (slot == null || slot.ItemData == null)
                return;

            ItemData clickedItem = slot.ItemData;

            if (clickedItem.itemType != ItemType.Equipment)
                return;

            var inventory = Inventory.Instance;
            if (inventory == null || inventory.equipmentManager == null)
                return;

            var equipmentManager = inventory.equipmentManager;
            var equippedItem = equipmentManager.GetEquipped(clickedItem.equipmentSlotType);

            // ===== UNEQUIP =====
            if (equippedItem == clickedItem)
            {
                bool removed = equipmentManager.Unequip(clickedItem.equipmentSlotType);

                if (removed)
                {
                    inventory.AddItem(clickedItem, 1);
                }

                return;
            }

            // ===== EQUIP / SWAP =====
            bool equipped = equipmentManager.Equip(clickedItem);

            if (equipped)
            {
                // remove ONLY the clicked item from inventory
                inventory.ReduceSlotAmount(slot, 1);

                // DO NOT add previous item here
                // EquipmentManager.Equip() already returns old item to inventory
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