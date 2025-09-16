using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class CraftSlot : MonoBehaviour
    {
        [Header("Header UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Text title;
        [SerializeField] private Button craftButton;

        [Header("Requirements UI")]
        [SerializeField] private Transform requirementsRoot;
        [SerializeField] private CraftRequirementUI requirementUIPrefab;

        private Inventory inventory;
        private ItemData output;

        public void Init(ItemData item, Inventory inv)
        {
            output = item;
            inventory = inv;

            if (title != null) title.text = item.itemName;
            if (icon != null) icon.sprite = item.iconInventory != null ? item.iconInventory : item.iconWorld;

            if (craftButton != null)
            {
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(OnCraftClicked);
            }

            RebuildRequirementsUI();
            Refresh(); // initial state
        }

        private void OnEnable()
        {
            Inventory.OnChanged += Refresh;
        }

        private void OnDisable()
        {
            Inventory.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            if (inventory == null || output == null) return;

            // Refresh interactable
            if (craftButton != null)
            {
                bool canCraft = inventory.CanCraft(output);
                craftButton.interactable = canCraft;
            }

            // Refresh counts/colors on existing requirement rows
            if (requirementsRoot != null)
            {
                for (int i = 0; i < requirementsRoot.childCount; i++)
                {
                    Transform child = requirementsRoot.GetChild(i);
                    CraftRequirementUI row = child.GetComponent<CraftRequirementUI>();
                    if (row == null) continue;

                    if (row.IsItemRequirement)
                    {
                        int have = inventory.CountOf(row.ItemKey);
                        int need = row.RequiredAmount;
                        UpdateRowText(row, have, need);
                    }
                    else if (row.IsGroupRequirement)
                    {
                        int have = inventory.CountOfGroup(row.GroupKey);
                        int need = row.RequiredAmount;
                        UpdateRowText(row, have, need);
                    }
                }
            }
        }

        private void OnCraftClicked()
        {
            if (inventory == null || output == null) return;

            if (inventory.Craft(output))
            {
                // Will also refresh via event, but keep it snappy
                Refresh();
            }
        }

        private void RebuildRequirementsUI()
        {
            if (requirementsRoot == null || requirementUIPrefab == null || output == null) return;

            // Remove ONLY existing requirement rows, leave other children (e.g., buttons) intact.
            for (int i = requirementsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = requirementsRoot.GetChild(i);
                CraftRequirementUI existingRow = child.GetComponent<CraftRequirementUI>();
                if (existingRow != null)
                {
                    Destroy(child.gameObject);
                }
            }

            // Specific-item requirements
            if (output.craftItemsNeeded != null)
            {
                for (int i = 0; i < output.craftItemsNeeded.Count; i++)
                {
                    CraftItemData req = output.craftItemsNeeded[i];
                    if (req == null || req.itemData == null) continue;

                    CraftRequirementUI row = Instantiate(requirementUIPrefab, requirementsRoot);
                    row.BindAsItem(req.itemData, Mathf.Max(1, req.Amount));

                    Sprite s = req.itemData.iconInventory != null ? req.itemData.iconInventory : req.itemData.iconWorld;
                    if (row.icon != null) row.icon.sprite = s;

                    int have = inventory != null ? inventory.CountOf(req.itemData) : 0;
                    UpdateRowText(row, have, row.RequiredAmount);
                }
            }

            // Group requirements
            if (output.craftGroupsNeeded != null)
            {
                for (int i = 0; i < output.craftGroupsNeeded.Count; i++)
                {
                    CraftGroupData req = output.craftGroupsNeeded[i];
                    if (req == null || req.GroupData == null) continue;

                    CraftRequirementUI row = Instantiate(requirementUIPrefab, requirementsRoot);
                    row.BindAsGroup(req.GroupData, Mathf.Max(1, req.Amount));

                    if (row.icon != null) row.icon.sprite = req.GroupData.icon;

                    int have = inventory != null ? inventory.CountOfGroup(req.GroupData) : 0;
                    UpdateRowText(row, have, row.RequiredAmount);
                }
            }
        }


        private void UpdateRowText(CraftRequirementUI row, int have, int need)
        {
            if (row.amountText == null) return;

            row.amountText.text = have.ToString() + "/" + need.ToString();

            if (have >= need) row.amountText.color = Color.white;
            else row.amountText.color = new Color(1f, 0.4f, 0.4f);
        }
    }
}
