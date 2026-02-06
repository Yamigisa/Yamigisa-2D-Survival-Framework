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

        public void ClearPanel()
        {
            foreach (Transform child in inventoryContent)
            {
                Destroy(child.gameObject);
            }
        }
        private void OnEnable()
        {
            if (sortButton == null) return;

            sortButton.onClick.RemoveAllListeners();
            sortButton.onClick.AddListener(() =>
            {
                inventoryOwner.SortPanel(this);
            });
        }
    }

}