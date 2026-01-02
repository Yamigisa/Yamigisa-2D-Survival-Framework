using System.Linq;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Destroy", menuName = "Yamigisa/Actions/Destroy", order = 50)]
    public class ActionDestroyable : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            NewInteractiveObject InteractiveObject = context as NewInteractiveObject;

            ItemData equipped = Inventory.Instance.GetSelectedQuickItemData();
            Destroyable destroyable = InteractiveObject.GetComponent<Destroyable>();
            CharacterCombat combat = character != null ? character.GetComponent<CharacterCombat>() : null;

            if (destroyable == null) return;

            if (equipped != null && equipped.groups != null && destroyable.requiredItems != null &&
                equipped.groups.Any(g => destroyable.requiredItems.Contains(g)))
            {
                int dmg = Mathf.Max(1, equipped.damage);
                destroyable.TakeDamage(dmg);
                combat?.StartAutoAttack(destroyable, dmg);
            }
            else if (destroyable.requiredItems == null || destroyable.requiredItems.Count == 0)
            {
                combat?.StartAutoAttack(destroyable, 0);
            }
        }
    }
}
