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

            Inventory.Instance.AddItem(item.itemData);

            InteractiveObject.MarkPickedUp();

            Destroy(InteractiveObject.gameObject);
        }

        public override bool CanDoAction(Component context)
        {
            if (context == null) return false;

            InteractiveObject interactiveObject = context as InteractiveObject;
            if (interactiveObject == null) return false;

            Item item = interactiveObject.GetComponent<Item>();
            if (item == null) return false;

            if (item.itemData == null) return false;

            if (Inventory.Instance == null) return false;

            // 🔥 Important: Check if inventory has space
            if (!Inventory.Instance.CanAddItem(item.itemData))
                return false;

            return true;
        }
    }
}
