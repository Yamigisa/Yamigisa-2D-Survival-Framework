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

        private void Update()
        {
            if (Character.instance.characterControls.IsAnyKeyPressedDown(
                Character.instance.characterControls.cancelKey))
            {
                craftingInterface.CloseAllCraftingInterfaces();
            }
        }

        public void ActivateCrafting()
        {
            if (!craftingInterface || !additionalCraftGroup) return;

            Character.instance.DisableMovements();
            craftingInterface.AddAdditionalCraftGroup(additionalCraftGroup);
        }
    }
}
