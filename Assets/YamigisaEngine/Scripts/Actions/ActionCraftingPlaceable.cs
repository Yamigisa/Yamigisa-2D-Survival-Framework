using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Craft", menuName = "Yamigisa/Actions/Craft", order = 50)]
    public class ActionCraftingPlaceable : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            InteractiveObject InteractiveObject = context as InteractiveObject;

            CraftingPlaceable craftingPlaceable = InteractiveObject.GetComponent<CraftingPlaceable>();

            if (craftingPlaceable == null) return;

            Debug.Log("Did i click this shit");
            craftingPlaceable.ActivateCrafting();
        }
    }
}