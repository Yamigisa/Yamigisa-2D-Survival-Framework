using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Split", menuName = "Yamigisa/Actions/Split", order = 50)]
    public class ActionSplit : ActionBase
    {
        public override void DoAction(Character character, ItemSlot slot)
        {
            if (slot == null || !slot.HasItem) return;

            ItemData data = slot.ItemData;
            if (data == null || !data.isStackable) return;

            int amount = slot.Amount;
            if (amount < 2) return;

            Inventory inv = Inventory.Instance;
            if (inv == null) return;

            int splitAmount = amount / 2;
            int remainAmount = amount - splitAmount;

            if (!inv.TryPlaceSplit(data, splitAmount))
                return;

            slot.SetItem(data, remainAmount);
        }
    }
}
