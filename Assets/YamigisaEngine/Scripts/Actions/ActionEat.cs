using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Eat", menuName = "Yamigisa/Actions/Eat", order = 50)]
    public class ActionEat : ActionBase
    {
        public override void DoAction(Character character, ItemSlot slot)
        {
            character.ConsumeItem(slot.ItemData);
            Inventory.Instance.ReduceSlotAmount(slot);
        }

        public override bool CanDoAction(Component context)
        {
            ItemSlot slot = context as ItemSlot;
            return slot.ItemData.itemType == ItemType.Consumable;
        }
    }
}