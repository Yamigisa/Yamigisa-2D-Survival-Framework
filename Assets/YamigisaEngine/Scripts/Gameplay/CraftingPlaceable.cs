using UnityEngine;

namespace Yamigisa
{
    public class CraftingPlaceable : MonoBehaviour
    {
        [SerializeField] private GroupData additionalCraftGroup;

        private CraftingInterface craftingInterface;

        void Start()
        {
            craftingInterface = FindAnyObjectByType<CraftingInterface>();
        }

        public void ActivateCrafting()
        {
            if (!craftingInterface || !additionalCraftGroup) return;

            Debug.Log("acivagte crafting");
            craftingInterface.AddAdditionalCraftGroup(additionalCraftGroup);
        }
    }
}
