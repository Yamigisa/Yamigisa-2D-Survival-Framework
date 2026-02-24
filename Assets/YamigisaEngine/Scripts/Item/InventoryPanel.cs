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
            // If sortButton exists and is under inventoryContent, preserve it.
            Transform sortT = (sortButton != null) ? sortButton.transform : null;

            for (int i = inventoryContent.childCount - 1; i >= 0; i--)
            {
                Transform child = inventoryContent.GetChild(i);

                // Keep sort button (or anything that contains it)
                if (sortT != null && (child == sortT || sortT.IsChildOf(child)))
                    continue;

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