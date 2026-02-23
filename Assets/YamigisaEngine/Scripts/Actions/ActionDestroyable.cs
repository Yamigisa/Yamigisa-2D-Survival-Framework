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

            InteractiveObject interactiveObject = context as InteractiveObject;
            if (interactiveObject == null) return;

            Destroyable destroyable = interactiveObject.GetComponent<Destroyable>();
            if (destroyable == null) return;

            CharacterCombat combat = character.GetComponent<CharacterCombat>();
            if (combat == null) return;

            ItemData equippedQuickItem = Inventory.Instance != null
                ? Inventory.Instance.GetSelectedQuickItemData()
                : null;

            bool canAttackBareHand =
                destroyable.requiredItems == null ||
                destroyable.requiredItems.Count == 0;

            bool canAttackWithTool =
                equippedQuickItem != null &&
                equippedQuickItem.groups != null &&
                destroyable.requiredItems != null &&
                equippedQuickItem.groups.Any(g => destroyable.requiredItems.Contains(g));

            if (!canAttackBareHand && !canAttackWithTool)
                return;

            // Damage is now handled internally by CharacterCombat
            combat.StartAutoAttack(destroyable);
        }
    }
}