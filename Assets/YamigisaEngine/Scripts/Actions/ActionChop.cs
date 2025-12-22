using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Chop", order = 50)]
    public class ActionChop : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            InteractiveObject InteractiveObject = context as InteractiveObject;

            ItemData equipped = Inventory.Instance.GetSelectedQuickItemData();
            if (equipped == null)
            {
                Debug.LogWarning("[ActionChop] No selected quick item. Select a tool first (1–8).");
                return;
            }

            var targetData = InteractiveObject.GetItemData();
            if (targetData == null)
            {
                Debug.LogWarning("[ActionChop] Target has no ItemData.");
                return;
            }

            // Only resources marked as destructible can be chopped
            if (!(targetData.itemType == ItemType.Resource && targetData.destructible))
            {
                Debug.Log("[ActionChop] Target is not destructible.");
                return;
            }

            // If the target requires groups, you must have at least one matching tool in QUICK SLOTS
            List<GroupData> req = targetData.destructibleRequiredGroups;
            if (req != null && req.Count > 0)
            {
                if (!Inventory.Instance.HasAnyGroup(req))
                {
                    Debug.Log("[ActionChop] You don't have a required tool in your quick slots.");
                    return;
                }
            }

            int dmg = Mathf.Max(1, equipped.damage);
            InteractiveObject.TakeDamage(dmg);
        }
    }
}
