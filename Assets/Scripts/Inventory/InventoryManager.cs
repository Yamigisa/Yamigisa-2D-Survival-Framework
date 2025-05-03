using UnityEngine;

namespace Yamigisa.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private InventoryUI inventoryUI;

        [SerializeField] private int inventorySize = 10;
        private InputSystem_Actions inputActions;

        private void Awake()
        {
            #region Enable Input System

            inputActions = new InputSystem_Actions();
            inputActions.Player.Enable();

            inputActions.Player.Inventory.performed += ctx => ToggleInventory();
            #endregion

            inventoryUI.InitializeInventory(inventorySize);
        }

        private void ToggleInventory()
        {
            if (inventoryUI.InventoryPanel.activeSelf)
            {
                inventoryUI.HideInventory();
            }
            else
            {
                inventoryUI.ShowInventory();
            }
        }
    }
}