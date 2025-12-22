using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Pick", order = 50)]
    public class ActionPick : ActionBase
    {
        public override bool CanDoAction(Component context = null)
        {
            InteractiveObject InteractiveObject = context as InteractiveObject;
            return InteractiveObject && InteractiveObject.ItemData != null;
        }

        public override void DoAction(Character character, Component context)
        {
            NewInteractiveObject InteractiveObject = context as NewInteractiveObject;

            Item item = InteractiveObject.gameObject.GetComponent<Item>();

            Inventory.Instance?.AddItem(item.itemData, item.quantity);

            Destroy(InteractiveObject.gameObject);
        }
    }
}
