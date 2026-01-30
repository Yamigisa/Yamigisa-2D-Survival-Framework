using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class CraftingInterface : MonoBehaviour
    {
        [Header("Folders")]
        [Tooltip("Craft group folder under Resources (NO 'Resources/' prefix), e.g. 'Groups/Craft'")]
        [SerializeField] private string groupResourcesSubfolder = "Groups/Craft";

        [Tooltip("ItemData folder under Resources to scan for craftables, e.g. 'Items' or 'Items/Craftables'")]
        [SerializeField] private string itemResourcesSubfolder = "Items";

        [Header("Crafting Group Selection")]
        [SerializeField] private CraftGroupSlot slotGroupPrefab;
        [SerializeField] private Transform contentRoot;

        [Header("Crafting Item Selection")]
        [SerializeField] private GameObject craftingItemSelectionPanel;
        [SerializeField] private Transform itemListRoot;
        [SerializeField] private CraftItemSlot itemSelectionSlotPrefab;

        [Header("Crafting Item Panel")]
        [SerializeField] private GameObject craftingItemPanel;
        [SerializeField] private Image itemCraftingIcon;
        [SerializeField] private Text itemCraftingNameText;
        [SerializeField] private Text itemCraftingDescriptionText;
        [SerializeField] private Transform itemCraftingRequirementsTransform;
        [SerializeField] private Button itemCraftingCraftButton;

        private readonly List<CraftGroupSlot> groupSlots = new();
        private readonly List<CraftItemSlot> itemSlots = new();
        private readonly List<GroupData> groupCrafts = new();

        private readonly HashSet<GroupData> runtimeAdditionalGroups = new();
        private readonly HashSet<GroupData> activeShownGroups = new();

        private ItemData currentRecipe;

        private void Awake()
        {
            Inventory.OnChanged += OnInventoryChanged;
        }

        private void OnDestroy()
        {
            Inventory.OnChanged -= OnInventoryChanged;
        }

        private void Start()
        {
            BuildCraftSelection();
            craftingItemSelectionPanel.SetActive(false);
            craftingItemPanel.SetActive(false);
        }

        private static string CleanResourcesPath(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "";
            return p.Replace("\\", "/").Replace("Resources/", "").Trim().TrimStart('/');
        }

        private void BuildCraftSelection()
        {
            if (!contentRoot || !slotGroupPrefab) return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);

            groupSlots.Clear();
            groupCrafts.Clear();

            string path = CleanResourcesPath(groupResourcesSubfolder);
            GroupData[] groups = Resources.LoadAll<GroupData>(path);

            foreach (GroupData group in groups)
            {
                if (!group) continue;

                groupCrafts.Add(group);

                var slot = Instantiate(slotGroupPrefab, contentRoot);
                slot.Bind(group, () => OpenCraftingItemSelectionPanel(slot));
                groupSlots.Add(slot);
            }
        }

        public void OpenCraftingItemSelectionPanel(CraftGroupSlot slot)
        {
            if (!slot) return;

            craftingItemSelectionPanel.SetActive(true);

            ClearItemList();
            activeShownGroups.Clear();

            AddItemsForGroup(slot.Group);
            RefreshAllItemSlotsInteractable();
        }

        private void ClearItemList()
        {
            for (int i = itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(itemListRoot.GetChild(i).gameObject);

            itemSlots.Clear();
        }

        private void AddItemsForGroup(GroupData selectedGroup)
        {
            if (!itemListRoot || !itemSelectionSlotPrefab) return;
            if (!selectedGroup) return;
            if (activeShownGroups.Contains(selectedGroup)) return;

            activeShownGroups.Add(selectedGroup);

            string path = CleanResourcesPath(itemResourcesSubfolder);
            ItemData[] allItems = Resources.LoadAll<ItemData>(path);

            foreach (var item in allItems)
            {
                if (!item || !item.isCraftable) continue;
                if (item.groups == null || !item.groups.Contains(selectedGroup)) continue;

                bool selectedIsBase = groupCrafts.Contains(selectedGroup);
                if (selectedIsBase && HasAnyNonBaseCraftGroup(item)) continue;

                bool hasReqs =
                    (item.craftItemsNeeded != null && item.craftItemsNeeded.Count > 0) ||
                    (item.craftGroupsNeeded != null && item.craftGroupsNeeded.Count > 0);

                if (!hasReqs) continue;

                CraftItemSlot slot = Instantiate(itemSelectionSlotPrefab, itemListRoot);
                slot.BindItem(item);
                slot.button.onClick.AddListener(() => OpenItemCraftingPanel(item));
                itemSlots.Add(slot);
            }
        }

        private void OpenItemCraftingPanel(ItemData item)
        {
            currentRecipe = item;

            craftingItemPanel.SetActive(true);
            itemCraftingIcon.sprite = item.iconInventory;
            itemCraftingNameText.text = item.itemName;
            itemCraftingDescriptionText.text = item.description;

            itemCraftingCraftButton.onClick.RemoveAllListeners();
            itemCraftingCraftButton.onClick.AddListener(() => TryCraft(item));

            // rebuild requirements
            for (int i = itemCraftingRequirementsTransform.childCount - 1; i >= 0; i--)
                Destroy(itemCraftingRequirementsTransform.GetChild(i).gameObject);

            if (item.craftGroupsNeeded != null)
            {
                foreach (var g in item.craftGroupsNeeded)
                {
                    if (g == null || g.GroupData == null) continue;
                    var s = Instantiate(itemSelectionSlotPrefab, itemCraftingRequirementsTransform);
                    s.BindGroup(g.GroupData, false);
                }
            }

            if (item.craftItemsNeeded != null)
            {
                foreach (var r in item.craftItemsNeeded)
                {
                    if (r == null || r.itemData == null) continue;
                    var s = Instantiate(itemSelectionSlotPrefab, itemCraftingRequirementsTransform);
                    s.BindItem(r.itemData, false);
                }
            }

            UpdateCraftButtonState();
        }

        private void TryCraft(ItemData recipe)
        {
            if (!recipe || Inventory.Instance == null) return;
            if (!Inventory.Instance.CanCraft(recipe)) return;

            if (Inventory.Instance.Craft(recipe))
            {
                RefreshAllItemSlotsInteractable();
                UpdateCraftButtonState();
            }
        }

        private void OnInventoryChanged()
        {
            RefreshAllItemSlotsInteractable();
            UpdateCraftButtonState();
        }

        private void RefreshAllItemSlotsInteractable()
        {
            foreach (var slot in itemSlots)
            {
                if (!slot) continue;
                if (slot.button) slot.button.interactable = true;
            }
        }

        public void AddAdditionalCraftGroup(GroupData group)
        {
            if (!group) return;

            runtimeAdditionalGroups.Add(group);
            craftingItemSelectionPanel.SetActive(true);
            AddItemsForGroup(group);
            RefreshAllItemSlotsInteractable();
        }

        private bool HasAnyNonBaseCraftGroup(ItemData item)
        {
            if (item == null || item.groups == null) return false;

            foreach (var g in item.groups)
            {
                if (g && !groupCrafts.Contains(g))
                    return true;
            }
            return false;
        }

        private void UpdateCraftButtonState()
        {
            if (!itemCraftingCraftButton)
                return;

            bool canCraft =
                Inventory.Instance != null &&
                currentRecipe != null &&
                Inventory.Instance.CanCraft(currentRecipe);

            itemCraftingCraftButton.interactable = canCraft;
        }

        public void CloseAllCraftingInterfaces()
        {
            // Hide panels
            if (craftingItemSelectionPanel)
                craftingItemSelectionPanel.SetActive(false);

            if (craftingItemPanel)
                craftingItemPanel.SetActive(false);

            // Clear current recipe
            currentRecipe = null;

            // Clear requirements UI
            if (itemCraftingRequirementsTransform)
            {
                for (int i = itemCraftingRequirementsTransform.childCount - 1; i >= 0; i--)
                    Destroy(itemCraftingRequirementsTransform.GetChild(i).gameObject);
            }

            // Disable craft button safely
            if (itemCraftingCraftButton)
                itemCraftingCraftButton.interactable = false;
        }
    }
}
