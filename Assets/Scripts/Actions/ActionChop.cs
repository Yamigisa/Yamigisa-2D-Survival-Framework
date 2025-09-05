using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Chop", order = 50)]
    public class ActionChop : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            Selectable selectable = context as Selectable;

            Destructible destructible = selectable.GetComponent<Destructible>();

            ItemData equipped = Inventory.Instance.GetSelectedQuickItemData();

            GroupData equippedGroup = equipped.GroupData;
            GroupData requiredGroup = destructible.requiredItem;

            if (equippedGroup == requiredGroup)
            {
                Debug.Log("[ActionChop] Correct tool group equipped. Applying damage.");
                destructible.TakeDamage(equipped.damage);
                return;
            }
        }
    }
}
