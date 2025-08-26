using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Action", menuName = "Yamigisa/Actions/Pick", order = 50)]
    public class ActionPick : ActionSelectable
    {
        public override void DoAction(GameObject caller)
        {
            CollectibleItem collectible = caller.GetComponent<CollectibleItem>();
            if (collectible != null)
            {
                Inventory.Instance.AddItem(collectible.ItemData, collectible.Amount);
                GameObject.Destroy(caller);
            }
        }

        public override bool CanDoAction(GameObject caller)
        {
            return Inventory.Instance.InventoryFull();
        }
    }

}