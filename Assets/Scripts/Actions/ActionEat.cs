using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Eat", order = 50)]
    public class ActionEat : ActionBase
    {
        public override void DoAction(Character character, InventoryItem slot)
        {
            character.ConsumeItem(slot.ItemData);
            Inventory.Instance.UseSlot(slot);
        }

        public override void DoAction(Character character, Component context)
        {
            if (context is Selectable selectable && selectable.ItemData != null)
            {
                character.ConsumeItem(selectable.ItemData);
                Destroy(selectable.gameObject);
            }
        }
    }
}