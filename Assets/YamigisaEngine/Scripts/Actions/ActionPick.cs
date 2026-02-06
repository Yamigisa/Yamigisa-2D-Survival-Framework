using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Pick", menuName = "Yamigisa/Actions/Pick", order = 50)]
    public class ActionPick : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            InteractiveObject InteractiveObject = context as InteractiveObject;

            Item item = InteractiveObject.gameObject.GetComponent<Item>();

            Inventory.Instance.AddItem(item.itemData, item.quantity);

            InteractiveObject.MarkPickedUp();
            
            Destroy(InteractiveObject.gameObject);
        }
    }
}
