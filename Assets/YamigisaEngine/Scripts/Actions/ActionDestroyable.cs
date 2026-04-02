using System.Linq;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Destroy", menuName = "Yamigisa/Actions/Destroy", order = 50)]
    public class ActionDestroyable : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            if (character == null) return;
            if (!CanDoAction(context)) return;

            InteractiveObject interactiveObject = context as InteractiveObject;
            if (interactiveObject == null) return;

            Destroyable destroyable = interactiveObject.GetComponent<Destroyable>();
            if (destroyable == null) return;

            CharacterCombat combat = character.GetComponent<CharacterCombat>();
            if (combat == null) return;

            combat.StartAutoAttack(destroyable);
        }

        public override bool CanDoAction(Component context)
        {
            InteractiveObject interactiveObject = context as InteractiveObject;
            if (interactiveObject == null)
            {
                return false;
            }

            if (interactiveObject.IsRegrowing)
            {
                return false;
            }

            Destroyable destroyable = interactiveObject.GetComponent<Destroyable>();
            if (destroyable == null)
            {
                return false;
            }

            ItemData weaponItem = GetCurrentWeaponItem();

            bool canAttackBareHand =
                destroyable.requiredItems == null ||
                destroyable.requiredItems.Count == 0;

            bool hasMatchingGroup =
                weaponItem != null &&
                weaponItem.groups != null &&
                destroyable.requiredItems != null &&
                weaponItem.groups.Any(weaponGroup =>
                    weaponGroup != null &&
                    destroyable.requiredItems.Any(requiredGroup =>
                        requiredGroup != null && requiredGroup == weaponGroup));

            bool canAttackWithTool =
                weaponItem != null &&
                weaponItem.itemType == ItemType.Equipment &&
                weaponItem.equipmentSlotType == EquipmentSlotType.Weapon &&
                hasMatchingGroup;


            return canAttackBareHand || canAttackWithTool;
        }

        private ItemData GetCurrentWeaponItem()
        {
            Character player = Character.instance;
            if (player == null) return null;

            EquipmentManager equipmentManager = Inventory.Instance.equipmentManager;
            if (equipmentManager == null) return null;

            EquipmentSlot weaponSlot = equipmentManager.GetSlot(EquipmentSlotType.Weapon);
            if (weaponSlot == null) return null;

            ItemData item = weaponSlot.GetEquippedItem();

            return item;
        }
    }
}