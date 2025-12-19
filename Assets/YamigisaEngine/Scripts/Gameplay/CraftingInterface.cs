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
        [SerializeField] private Transform contentRoot;  // container for group slots

        [Header("Crafting Item Selection")]
        [SerializeField] private GameObject craftingItemSelectionPanel;
        [SerializeField] private Transform itemListRoot; // container for item slots
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
                slot.Bind(
                    group,
                    onClick: () => OpenCraftingItemSelectionPanel(slot)
                );
                groupSlots.Add(slot);
            }
        }

        public void OpenCraftingItemSelectionPanel(CraftGroupSlot slot)
        {
            if (!slot) return;
            if (craftingItemSelectionPanel) craftingItemSelectionPanel.SetActive(true);
            BuildItemsForGroup(slot.Group);
            RefreshAllItemSlotsInteractable();
        }

        private void BuildItemsForGroup(GroupData selectedGroup)
        {
            if (!itemListRoot || !itemSelectionSlotPrefab) return;
            for (int i = itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(itemListRoot.GetChild(i).gameObject);
            itemSlots.Clear();

            string path = CleanResourcesPath(itemResourcesSubfolder);
            ItemData[] allItems = Resources.LoadAll<ItemData>(path);

            foreach (var item in allItems)
            {
                if (!item || !item.isCraftable) continue;

                bool belongsToGroup = item.groups != null && selectedGroup != null && item.groups.Contains(selectedGroup);
                if (!belongsToGroup) continue;

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
            craftingItemPanel.SetActive(true);
            itemCraftingIcon.sprite = item.iconInventory;
            itemCraftingDescriptionText.text = item.description;
            itemCraftingNameText.text = item.itemName;
            itemCraftingCraftButton.onClick.RemoveAllListeners();
            itemCraftingCraftButton.onClick.AddListener(() => TryCraft(item));

            foreach (Transform child in itemCraftingRequirementsTransform)
            {
                Destroy(child.gameObject);
            }

            if (item.craftGroupsNeeded.Count > 0)
            {

                foreach (GroupData craftGroup in item.craftGroupsNeeded.ConvertAll(cg => cg.GroupData))
                {
                    CraftItemSlot slot = Instantiate(itemSelectionSlotPrefab, itemCraftingRequirementsTransform);
                    slot.BindGroup(craftGroup, false);
                }
            }

            if (item.craftItemsNeeded.Count > 0)
            {
                foreach (ItemData craftItem in item.craftItemsNeeded.ConvertAll(ci => ci.itemData))
                {
                    CraftItemSlot slot = Instantiate(itemSelectionSlotPrefab, itemCraftingRequirementsTransform);
                    slot.BindItem(craftItem, false);
                }
            }
        }
        private void TryCraft(ItemData recipe)
        {
            if (!recipe || Inventory.Instance == null) return;

            if (!Inventory.Instance.CanCraft(recipe)) return;

            bool ok = Inventory.Instance.Craft(recipe);
            if (ok)
            {
                RefreshAllItemSlotsInteractable();
            }
        }

        private void OnInventoryChanged()
        {
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
