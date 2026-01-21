using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "Open Storage", menuName = "Yamigisa/Actions/OpenStorage", order = 50)]
    public class ActionOpenStorage : ActionBase
    {
        public override void DoAction(Character character, Component context)
        {
            InteractiveObject InteractiveObject = context as InteractiveObject;

            Storage storage = InteractiveObject.gameObject.GetComponent<Storage>();

            storage.OpenStorage();
            Inventory.Instance.ShowInventory();
        }
    }
}