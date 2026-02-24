using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Build", menuName = "Yamigisa/Actions/Build", order = 50)]
    public class ActionBuild : ActionBase
    {
        public override bool CanDoAction(Component context = null)
        {
            if (PlaceableSystem.instance == null)
                return false;

            if (PlaceableSystem.instance.IsInBuildMode)
                return false;

            if (context is ItemSlot slot)
            {
                if (slot.ItemData == null)
                    return false;

                if (slot.ItemData.itemPrefab == null)
                    return false;
            }

            return true;
        }

        public override void DoAction(Character character, ItemSlot slot)
        {
            if (!CanDoAction(slot))
                return;

            PlaceableSystem.instance.InitializeBuilding(slot.ItemData.itemPrefab);
            Inventory.Instance.ReduceSlotAmount(slot);
        }
    }
}