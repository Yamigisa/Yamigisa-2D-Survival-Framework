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
            if (interactiveObject == null) return false;
            if (interactiveObject.IsRegrowing) return false;

            Destroyable destroyable = interactiveObject.GetComponent<Destroyable>();
            if (destroyable == null) return false;

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

            return canAttackBareHand || canAttackWithTool;
        }
    }
}