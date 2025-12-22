using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Eat", order = 50)]
    public class ActionEat : ActionBase
    {
        public override void DoAction(Character character, ItemSlot slot)
        {
            character.ConsumeItem(slot.ItemData);
            Inventory.Instance.UseSlot(slot);
        }

        public override void DoAction(Character character, Component context)
        {
            if (context is InteractiveObject InteractiveObject && InteractiveObject.ItemData != null)
            {
                character.ConsumeItem(InteractiveObject.ItemData);
                Destroy(InteractiveObject.gameObject);
            }
        }
    }
}