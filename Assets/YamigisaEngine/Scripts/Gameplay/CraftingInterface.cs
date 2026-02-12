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
        [Tooltip("Additional craft group folder under Resources, e.g. 'Groups/CraftAdditional'")]
        [SerializeField] private string additionalCraftGroupResourcesSubfolder = "Groups/CraftAdditional";


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
        private readonly HashSet<GroupData> additionalCraftGroups = new();

        private ItemData currentRecipe;
        private bool isInitialized;

        private bool isOpened;
        private readonly HashSet<ItemData> spawnedItems = new();

        public void Setup()
        {
            if (isInitialized) return;

            // 1. Build all craft group data + UI
            BuildCraftSelection();

            // 2. Subscribe to inventory changes
            Inventory.OnChanged += OnInventoryChanged;

            // 3. Ensure UI starts closed
            if (craftingItemSelectionPanel)
                craftingItemSelectionPanel.SetActive(false);

            if (craftingItemPanel)
                craftingItemPanel.SetActive(false);

            currentRecipe = null;

            runtimeAdditionalGroups.Clear();
            activeShownGroups.Clear();

            isInitialized = true;
        }

        private void Update()
        {
            if (Character.instance.characterControls.IsAnyKeyPressedDown(
                Character.instance.characterControls.cancelKey) && Character.instance.CharacterIsBusy() && isOpened)
            {
                Character.instance.SetCharacterBusy(false);
                CloseAllCraftingInterfaces();
            }
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
            additionalCraftGroups.Clear(); // <-- ADD

            // === BASE CRAFT GROUPS ===
            string basePath = CleanResourcesPath(groupResourcesSubfolder);
            GroupData[] baseGroups = Resources.LoadAll<GroupData>(basePath);

            foreach (GroupData group in baseGroups)
            {
                if (!group) continue;

                groupCrafts.Add(group);

                var slot = Instantiate(slotGroupPrefab, contentRoot);
                slot.Bind(group, () => OpenCraftingItemSelectionPanel(slot));
                groupSlots.Add(slot);
            }

            // === ADDITIONAL CRAFT GROUPS (HIDDEN BY DEFAULT) ===
            string additionalPath = CleanResourcesPath(additionalCraftGroupResourcesSubfolder);
            GroupData[] additionalGroups = Resources.LoadAll<GroupData>(additionalPath);

            foreach (GroupData group in additionalGroups)
            {
                if (!group) continue;
                additionalCraftGroups.Add(group);
            }
        }


        public void OpenCraftingItemSelectionPanel(CraftGroupSlot slot)
        {
            if (craftingItemSelectionPanel.activeSelf)
            {
                CloseAllCraftingInterfaces();
                Character.instance.SetCharacterBusy(false);
                return;
            }

            if (!slot && Character.instance.CharacterIsBusy() && !GameManager.instance.IsPaused)
                return;

            isOpened = true;
            craftingItemSelectionPanel.SetActive(true);

            ClearItemList();
            activeShownGroups.Clear();

            Character.instance.SetCharacterBusy(true);
            AddItemsForGroup(slot.Group);
            RefreshAllItemSlotsInteractable();
        }

        private void ClearItemList()
        {
            for (int i = itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(itemListRoot.GetChild(i).gameObject);

            itemSlots.Clear();
            spawnedItems.Clear(); // <-- ADD THIS
        }


        private void AddItemsForGroup(GroupData selectedGroup)
        {
            if (!itemListRoot || !itemSelectionSlotPrefab) return;
            if (!selectedGroup) return;
            if (activeShownGroups.Contains(selectedGroup)) return;

            activeShownGroups.Add(selectedGroup);

            string path = CleanResourcesPath(itemResourcesSubfolder);
            List<ItemData> allItems = new List<ItemData>(Resources.LoadAll<ItemData>(path));
            allItems.Sort(CompareItemsAlphabetically);

            foreach (var item in allItems)
            {
                if (!item || !item.isCraftable) continue;
                if (item.groups == null || !item.groups.Contains(selectedGroup)) continue;

                bool selectedIsBase = groupCrafts.Contains(selectedGroup);

                // Hide ONLY if the item requires another craft group
                // that has NOT been unlocked yet
                if (selectedIsBase && HasLockedCraftGroup(item))
                    continue;

                bool hasReqs =
                    (item.craftItemsNeeded != null && item.craftItemsNeeded.Count > 0) ||
                    (item.craftGroupsNeeded != null && item.craftGroupsNeeded.Count > 0);

                if (!hasReqs) continue;

                // 🔒 PREVENT DUPLICATE ITEMS
                if (spawnedItems.Contains(item))
                    continue;

                spawnedItems.Add(item);

                CraftItemSlot slot = Instantiate(itemSelectionSlotPrefab, itemListRoot);
                slot.BindItem(item);
                slot.button.onClick.AddListener(() => OpenItemCraftingPanel(item));
                itemSlots.Add(slot);

            }
        }

        public void ForceInitialize()
        {
            if (groupCrafts.Count > 0 || additionalCraftGroups.Count > 0)
                return;

            BuildCraftSelection();
        }

        private void OpenItemCraftingPanel(ItemData item)
        {
            isOpened = true;
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

            ForceInitialize();

            // Open UI
            craftingItemSelectionPanel.SetActive(true);

            // If this is the first time opening crafting (list empty),
            // initialize base items first so they don't "disappear"
            bool listIsEmpty = itemListRoot == null || itemListRoot.childCount == 0;

            if (listIsEmpty)
            {
                ClearItemList();
                activeShownGroups.Clear();

                // Default: load first base craft group items
                GroupData baseGroup = GetFirstBaseCraftGroup();
                if (baseGroup)
                    AddItemsForGroup(baseGroup);
            }

            // Now unlock + append additional group items (without wiping existing list)
            runtimeAdditionalGroups.Add(group);
            AddItemsForGroup(group);

            RefreshAllItemSlotsInteractable();
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
            isOpened = false;
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

            // =========================
            // 🔒 RESET TEMP CRAFT STATE
            // =========================
            runtimeAdditionalGroups.Clear();
            activeShownGroups.Clear();
        }

        private bool HasLockedCraftGroup(ItemData item)
        {
            if (item == null || item.groups == null)
                return false;

            foreach (var g in item.groups)
            {
                if (!g) continue;

                // If item belongs to an ADDITIONAL craft group
                // and it has NOT been unlocked yet → LOCK IT
                if (additionalCraftGroups.Contains(g) && !runtimeAdditionalGroups.Contains(g))
                    return true;
            }

            return false;
        }

        private static int CompareItemsAlphabetically(ItemData a, ItemData b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return string.Compare(a.itemName, b.itemName, System.StringComparison.OrdinalIgnoreCase);
        }

        private GroupData GetFirstBaseCraftGroup()
        {
            for (int i = 0; i < groupCrafts.Count; i++)
            {
                if (groupCrafts[i]) return groupCrafts[i];
            }
            return null;
        }

        public void OpenCraftingFromPlaceable(GroupData additionalGroup)
        {
            if (!additionalGroup) return;

            isOpened = true;
            // Make sure groups are loaded even if Start() hasn't run yet
            ForceInitialize();

            // Same behavior as clicking a CraftGroup button:
            // open selection panel, reset list, show default base group items
            if (craftingItemSelectionPanel) craftingItemSelectionPanel.SetActive(true);
            if (craftingItemPanel) craftingItemPanel.SetActive(false);

            ClearItemList();
            activeShownGroups.Clear();

            // Load base items first (same default behavior as "first craft group")
            GroupData baseGroup = null;
            for (int i = 0; i < groupCrafts.Count; i++)
            {
                if (groupCrafts[i])
                {
                    baseGroup = groupCrafts[i];
                    break;
                }
            }

            if (baseGroup)
                AddItemsForGroup(baseGroup);

            // Then append the additional group items
            runtimeAdditionalGroups.Add(additionalGroup);
            AddItemsForGroup(additionalGroup);

            RefreshAllItemSlotsInteractable();
        }

    }
}
