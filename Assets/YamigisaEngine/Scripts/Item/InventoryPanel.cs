using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class InventoryPanel : MonoBehaviour
    {
        [Header("UI")]
        public GameObject inventoryPanelGameObject;
        public Transform inventoryContent;
        public Button sortButton;

        [HideInInspector]
        public Inventory inventoryOwner;

        private void OnEnable()
        {
            sortButton.onClick.RemoveAllListeners();
            sortButton.onClick.AddListener(() =>
            {
                inventoryOwner.SortPanel(this);
            });
        }
    }

}