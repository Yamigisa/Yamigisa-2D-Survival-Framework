using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Eat", order = 50)]
    public class ActionEat : ActionBase
    {
        public override void DoItemAction(Character character, InventoryItem slot)
        {
            character.ConsumeItem(slot.ItemData);
        }
    }
}