using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Pick", order = 50)]
    public class ActionPick : ActionBase
    {
        public override bool CanDoAction(Component context = null)
        {
            Selectable selectable = context as Selectable;
            return selectable && selectable.ItemData != null;
        }

        public override void DoAction(Character character, Component context)
        {
            Selectable selectable = context as Selectable;

            ItemData item = selectable.ItemData;

            Inventory.Instance?.AddItem(item, selectable.Amount);

            Destroy(selectable.gameObject);
        }
    }
}
