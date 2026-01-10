using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Build", menuName = "Yamigisa/Actions/Build", order = 50)]
    public class ActionBuild : ActionBase
    {
        public override void DoAction(Character character, ItemSlot slot)
        {
            GridBuildingSystem.instance.InitializeBuilding(slot.ItemData.itemPrefab);
            Inventory.Instance.ReduceSlotAmount(slot);
        }
    }
}