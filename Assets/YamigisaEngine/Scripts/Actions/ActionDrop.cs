using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Drop", menuName = "Yamigisa/Actions/Drop", order = 50)]
    public class ActionDrop : ActionBase
    {
        public override void DoAction(Character character, ItemSlot slot)
        {
            slot.DropItem(character.transform.position, slot.Amount);
        }

        public override bool CanDoAction(Component context)
        {
            ItemSlot slot = context as ItemSlot;
            if (slot == null) return false;

            return slot.ItemData != null && slot.ItemData.isDroppable;
        }
    }
}