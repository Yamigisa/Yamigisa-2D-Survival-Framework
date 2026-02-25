using UnityEngine;

namespace Yamigisa
{
    public class CraftingPlaceable : MonoBehaviour
    {
        [SerializeField] private GroupData additionalCraftGroup;

        private CraftingInterface craftingInterface;

        private bool isOpened = false;
        void Start()
        {
            craftingInterface = FindAnyObjectByType<CraftingInterface>();
        }

        private void Update()
        {
            if (Character.instance.characterControls.IsPressedDown(
                Character.instance.characterControls.cancel) && Character.instance.CharacterIsBusy() && isOpened)
            {
                isOpened = false;
                Character.instance.SetCharacterBusy(false);
                craftingInterface.CloseAllCraftingInterfaces();
            }
        }

        public void ActivateCrafting()
        {
            if (!craftingInterface || !additionalCraftGroup) return;

            GameManager.instance.SetCanPause(false);
            isOpened = true;
            Character.instance.DisableMovements();
            Character.instance.SetCharacterBusy(true);
            // EXACTLY like pressing a craft group button first, then add additional
            craftingInterface.OpenCraftingFromPlaceable(additionalCraftGroup);
        }
    }
}
