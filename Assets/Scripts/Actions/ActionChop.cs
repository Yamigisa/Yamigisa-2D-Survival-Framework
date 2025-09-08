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
            if (selectable == null)
            {
                Debug.LogWarning("[ActionChop] Context is not a Selectable.");
                return;
            }

            Destructible destructible = selectable.GetComponent<Destructible>();
            if (destructible == null)
            {
                Debug.LogWarning("[ActionChop] Target has no Destructible.");
                return;
            }

            if (Inventory.Instance == null)
            {
                Debug.LogWarning("[ActionChop] No Inventory.Instance.");
                return;
            }

            ItemData equipped = Inventory.Instance.GetSelectedQuickItemData();
            if (equipped == null)
            {
                Debug.LogWarning("[ActionChop] No selected quick item. Select a tool first (1–8).");
                return;
            }

            GroupData requiredGroup = destructible.requiredItem;

            // If the destructible has no required group, allow any tool.
            if (requiredGroup == null)
            {
                Debug.Log("[ActionChop] No required group on target. Applying damage.");
                destructible.TakeDamage(equipped.damage);
                return;
            }

            // Check if the equipped item declares this group.
            List<GroupData> equippedGroups = equipped.groups;
            bool matches = equippedGroups != null && equippedGroups.Contains(requiredGroup);

            if (matches)
            {
                Debug.Log("[ActionChop] Correct tool group found on equipped item. Applying damage.");
                destructible.TakeDamage(equipped.damage);
            }
            else
            {
                string eq = equippedGroups != null && equippedGroups.Count > 0 ? equippedGroups[0].name : "none";
                Debug.Log($"[ActionChop] Equipped item does not contain required group: {requiredGroup.name}. (Equipped first group: {eq})");
            }
        }
    }
}
