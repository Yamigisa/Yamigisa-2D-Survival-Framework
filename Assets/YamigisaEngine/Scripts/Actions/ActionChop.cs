using System.Linq;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Chop", menuName = "Yamigisa/Actions/Chop", order = 50)]
    public class ActionChop : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            NewInteractiveObject InteractiveObject = context as NewInteractiveObject;

            ItemData equipped = Inventory.Instance.GetSelectedQuickItemData();
            Destroyable destroyable = InteractiveObject.GetComponent<Destroyable>();

            if (equipped.groups.Any(g => destroyable.requiredItems.Contains(g)))
            {
                int dmg = Mathf.Max(1, equipped.damage);
                destroyable.TakeDamage(dmg);
            }
            else if (destroyable.requiredItems == null || destroyable.requiredItems.Count == 0)
            {
                destroyable.Kill();
            }
        }
    }
}
