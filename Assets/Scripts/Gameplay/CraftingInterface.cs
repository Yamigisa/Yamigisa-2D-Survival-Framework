using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class CraftingInterface : MonoBehaviour
    {
        // ======== DO NOT REMOVE ORIGINAL COMMENT BLOCK ========
        // [Header("Build From Resources")]
        // [Tooltip("Optional subfolder inside a Resources/… path. Leave blank to scan all Resources.")]
        // [SerializeField] private string resourcesSubfolder = "";

        // [Header("UI")]
        // [SerializeField] private Transform contentRoot;
        // [SerializeField] private CraftGroupSlot slotPrefab;

        // private Inventory inventory;

        // private CharacterControls controls;
        // private readonly List<CraftGroupSlot> slots = new List<CraftGroupSlot>();

        // private void OnEnable()
        // {
        //     inventory = Inventory.Instance != null ? Inventory.Instance : FindObjectOfType<Inventory>();

        //     // Subscribe first so we catch the very first NotifyChanged after start
        //     Inventory.OnChanged += RefreshAll;

        //     Rebuild();     // build slots
        //     RefreshAll();  // immediate pull (in case inventory already has items)
        // }

        // private void OnDisable()
        // {
        //     Inventory.OnChanged -= RefreshAll;
        // }

        // private void Awake()
        // {
        //     if (controls == null) controls = FindObjectOfType<CharacterControls>();
        // }

        // void Update()
        // {
        //     if (controls == null) return;

        //     if (controls.IsAnyKeyPressedDown(controls.craftingKey))
        //     {
        //         ToggleCraftingInterface();
        //     }
        // }

        // public void Rebuild()
        // {
        //     if (contentRoot == null || slotPrefab == null) return;

        //     // Clear
        //     for (int i = contentRoot.childCount - 1; i >= 0; i--)
        //         Destroy(contentRoot.GetChild(i).gameObject);
        //     slots.Clear();

        //     // Load all craftable outputs from Resources
        //     ItemData[] all = Resources.LoadAll<ItemData>(resourcesSubfolder);
        //     for (int i = 0; i < all.Length; i++)
        //     {
        //         ItemData item = all[i];
        //         if (item == null) continue;

        //         bool hasReqs =
        //             (item.craftItemsNeeded != null && item.craftItemsNeeded.Count > 0) ||
        //             (item.craftGroupsNeeded != null && item.craftGroupsNeeded.Count > 0);

        //         if (!hasReqs) continue;

        //         CraftGroupSlot slot = Instantiate(slotPrefab, contentRoot);
        //         //slot.Init(item, inventory);
        //         slots.Add(slot);
        //     }
        // }

        // private void ToggleCraftingInterface()
        // {
        //     bool isActive = contentRoot != null && contentRoot.gameObject.activeSelf;
        //     contentRoot.gameObject.SetActive(!isActive);
        //     Rebuild();
        // }

        // private void RefreshAll()
        // {
        //     for (int i = 0; i < slots.Count; i++)
        //     {
        //         CraftGroupSlot s = slots[i];
        //         //if (s != null) s.Refresh();
        //     }
        // }
        // ======== END OF ORIGINAL COMMENT BLOCK ========

        [Header("Folders")]
        [Tooltip("Craft group folder under Resources (NO 'Resources/' prefix), e.g. 'Groups/Craft'")]
        [SerializeField] private string groupResourcesSubfolder = "Groups/Craft";

        [Tooltip("ItemData folder under Resources to scan for craftables, e.g. 'Items' or 'Items/Craftables'")]
        [SerializeField] private string itemResourcesSubfolder = "Items";

        [Header("Crafting Group Selection")]
        [SerializeField] private CraftGroupSlot slotPrefab;
        [SerializeField] private Transform contentRoot;  // container for group slots

        [Header("Crafting Interface")]
        [SerializeField] private GameObject craftingPanel;
        [SerializeField] private Transform itemListRoot; // container for item slots
        [SerializeField] private CraftItemSlot itemSlotPrefab;

        private readonly List<CraftGroupSlot> groupSlots = new();
        private readonly List<CraftItemSlot> itemSlots = new();
        private readonly List<GroupData> groupCrafts = new();

        private void Awake()
        {
            // live refresh when inventory changes
            Inventory.OnChanged += OnInventoryChanged;
        }

        private void OnDestroy()
        {
            Inventory.OnChanged -= OnInventoryChanged;
        }

        private void Start()
        {
            BuildCraftSelection();
            if (craftingPanel) craftingPanel.SetActive(false);
        }

        private static string CleanResourcesPath(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "";
            return p.Replace("\\", "/").Replace("Resources/", "").Trim().TrimStart('/');
        }

        private void BuildCraftSelection()
        {
            if (!contentRoot || !slotPrefab) return;

            // Clear old
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
            groupSlots.Clear();
            groupCrafts.Clear();

            string path = CleanResourcesPath(groupResourcesSubfolder);
            GroupData[] groups = Resources.LoadAll<GroupData>(path); // <<< FIXED: no "Resources/" prefix

            foreach (GroupData group in groups)
            {
                if (!group) continue;
                groupCrafts.Add(group);

                var slot = Instantiate(slotPrefab, contentRoot);
                slot.Bind(
                    group,
                    onClick: () => OpenCraftingInterface(slot)
                );
                groupSlots.Add(slot);
            }
        }

        public void OpenCraftingInterface(CraftGroupSlot slot)
        {
            if (!slot) return;
            if (craftingPanel) craftingPanel.SetActive(true);
            BuildItemsForGroup(slot.Group);
            RefreshAllItemSlotsInteractable();
        }

        private void BuildItemsForGroup(GroupData selectedGroup)
        {
            if (!itemListRoot || !itemSlotPrefab) return;

            // Clear item list
            for (int i = itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(itemListRoot.GetChild(i).gameObject);
            itemSlots.Clear();

            string path = CleanResourcesPath(itemResourcesSubfolder);
            ItemData[] allItems = Resources.LoadAll<ItemData>(path); // <<< FIXED path usage

            foreach (var item in allItems)
            {
                if (!item || !item.isCraftable) continue;

                bool belongsToGroup = item.groups != null && selectedGroup != null && item.groups.Contains(selectedGroup);
                if (!belongsToGroup) continue;

                bool hasReqs =
                    (item.craftItemsNeeded != null && item.craftItemsNeeded.Count > 0) ||
                    (item.craftGroupsNeeded != null && item.craftGroupsNeeded.Count > 0);
                if (!hasReqs) continue;

                var slot = Instantiate(itemSlotPrefab, itemListRoot);
                slot.Bind(item, TryCraft);
                itemSlots.Add(slot);
            }
        }

        private void TryCraft(ItemData recipe)
        {
            if (!recipe || Inventory.Instance == null) return;

            // Guard: only craft if requirements currently met
            if (!Inventory.Instance.CanCraft(recipe)) return;

            bool ok = Inventory.Instance.Craft(recipe); // consumes materials AND adds result (already fires OnChanged)
            if (ok)
            {
                // UI can still re-check interactables right away
                RefreshAllItemSlotsInteractable();
            }
        }

        private void OnInventoryChanged()
        {
            // Any change in the inventory should re-evaluate craft buttons
            RefreshAllItemSlotsInteractable();
        }

        private void RefreshAllItemSlotsInteractable()
        {
            for (int i = 0; i < itemSlots.Count; i++)
            {
                if (itemSlots[i]) itemSlots[i].RefreshInteractable();
            }
        }
    }
}
