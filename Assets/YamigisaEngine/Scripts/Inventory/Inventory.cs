using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class Inventory : MonoBehaviour
    {
        [HideInInspector] public InventoryPanel mainInventoryPanel;
        [HideInInspector] public InventoryPanel quickInventoryPanel;

        [Header("Inventory Prefabs")]
        [SerializeField] private ItemSlot ItemSlotPrefab;
        [SerializeField] private InventoryPanel inventoryPanelPrefab;

        [Header("Inventory Transform")]
        [SerializeField] private Transform mainInventoryTransform;
        [SerializeField] private Transform quickInventoryTransform;

        [Header("Max Item Count")]
        public int maxItems = 32;

        [Header("Starting Items")]
        public List<StartingItem> startingItems;

        private List<ItemSlot> itemSlots = new List<ItemSlot>();

        [Header("Quick Inventory")]
        [SerializeField] private int quickSlotCount = 8;
        [SerializeField] private Transform quickInventoryContent;
        private List<ItemSlot> quickItemSlot = new List<ItemSlot>();

        [Header("Quick Slot Scroll")]
        [SerializeField] private bool CanScrollUsingScrollWheel = true;

        [Header("Inventory Sorting")]
        [SerializeField]
        private List<ItemType> inventorySortOrder = new List<ItemType>()
        {
            ItemType.Equipment,
            ItemType.Consumable,
            ItemType.Resource
        };

        [Header("Tooltip Panel")]
        [SerializeField] private bool showTooltipPanel = true;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;

        [Header("Drag & Drop")]
        [SerializeField] private float holdToPickSeconds = 0.25f;
        public Canvas rootCanvas;
        public GraphicRaycaster raycaster;

        private int selectedQuickIndex = -1;

        private bool isDragging;
        private ItemSlot dragOrigin;
        private ItemData dragData;
        private int dragAmount;
        private RectTransform dragIconRT;
        private Image dragIconImage;
        private Text dragIconAmount;

        private bool pendingPickActive;
        private float pendingPickTimer;
        private ItemSlot pendingPickSlot;

        public bool IsDragging { get { return isDragging; } }
        public bool IsInventoryOpen { get { return mainInventoryPanel != null && mainInventoryPanel.inventoryPanelGameObject.activeSelf; } }

        private CharacterControls controls;
        public Character Character;

        private bool isUsingSlot;
        public Storage currentStorage;

        public static event System.Action OnChanged;
        private static void NotifyChanged()
        {
            if (OnChanged != null) OnChanged();
        }

        public static Inventory Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            Character = Character.instance.GetCharacter();
            controls = Character.characterControls;

            mainInventoryPanel = CreateInventoryPanel();
            quickInventoryPanel = CreateInventoryPanel(quickInventoryTransform);

            mainInventoryPanel.gameObject.SetActive(false);
            rootCanvas = GetComponentInParent<Canvas>();
            raycaster = rootCanvas.GetComponent<GraphicRaycaster>();

            itemSlots.Clear();

            // Initialize Slots for main Inventory
            for (int i = 0; i < maxItems; i++)
            {
                ItemSlot newSlot = CreateItemSlot();
                itemSlots.Add(newSlot);
            }


            // Initialize Slots for quick Inventory
            quickItemSlot.Clear();
            for (int i = 0; i < quickSlotCount; i++)
            {
                ItemSlot quickSlot = CreateItemSlot(quickInventoryPanel.inventoryContent);
                quickItemSlot.Add(quickSlot);
            }

            SetStartingItems();

            if (quickItemSlot.Count > 0)
            {
                selectedQuickIndex = 0;
                SelectQuickSlot(selectedQuickIndex);
                UpdateQuickIndicators();
            }

            StartCoroutine(BroadcastInventoryChangedNextFrame());

            //mainInventoryPanel.sortButton.onClick.AddListener(() => { SortInventory(); });
            mainInventoryPanel.sortButton.gameObject.SetActive(true);
        }

        public InventoryPanel CreateInventoryPanel(Transform parent = null)
        {
            if (parent == null)
                parent = mainInventoryTransform;

            InventoryPanel panel = Instantiate(inventoryPanelPrefab, parent);
            panel.inventoryOwner = this;
            return panel;
        }

        public ItemSlot CreateItemSlot(Transform parent = null)
        {
            if (parent == null)
                parent = mainInventoryPanel.inventoryContent;

            return Instantiate(ItemSlotPrefab, parent);
        }

        public void SortPanel(InventoryPanel panel)
        {
            List<ItemSlot> slots = GetSlotListFromPanel(panel);
            if (slots == null) return;

            List<SlotSnapshot> items = new List<SlotSnapshot>();

            for (int i = 0; i < slots.Count; i++)
            {
                ItemSlot slot = slots[i];
                if (slot.HasItem && slot.ItemData != null)
                {
                    items.Add(new SlotSnapshot
                    {
                        data = slot.ItemData,
                        amount = slot.Amount
                    });
                }
            }

            for (int i = 0; i < slots.Count; i++)
                slots[i].ResetSlot();

            items.Sort((a, b) =>
            {
                int typeCompare =
                    GetItemTypePriority(a.data.itemType)
                    .CompareTo(GetItemTypePriority(b.data.itemType));

                if (typeCompare != 0)
                    return typeCompare;

                return string.Compare(
                    a.data.itemName,
                    b.data.itemName,
                    System.StringComparison.OrdinalIgnoreCase
                );
            });

            for (int i = 0; i < items.Count && i < slots.Count; i++)
                slots[i].SetItem(items[i].data, items[i].amount);

            NotifyChanged();
        }

        private IEnumerator BroadcastInventoryChangedNextFrame()
        {
            yield return null;
            NotifyChanged();
        }

        private void Update()
        {
            if (controls.IsAnyKeyPressedDown(controls.inventoryKey))
            {
                if (mainInventoryPanel != null && mainInventoryPanel.gameObject.activeSelf) HideInventory();
                else ShowInventory();
            }

            for (int i = 0; i < quickItemSlot.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    SelectQuickSlot(i);
            }

            if (CanScrollUsingScrollWheel && quickItemSlot.Count > 0)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0f)
                {
                    int dir = scroll > 0f ? 1 : -1;
                    int next = selectedQuickIndex + dir;

                    if (next < 0) next = quickItemSlot.Count - 1;
                    else if (next >= quickItemSlot.Count) next = 0;

                    SelectQuickSlot(next);
                }
            }

            if (!isDragging && controls.IsAnyKeyPressedDown(controls.useItemKey) && selectedQuickIndex >= 0)
                UseQuickSlot(selectedQuickIndex);

            if (!IsInventoryOpen)
            {
                CancelPendingPick();
                if (isDragging) CancelDrag();
                return;
            }

            // ===================== DRAG DRIVER (RESTORED) =====================

            if (isDragging)
            {
                UpdateDragIconPosition();

                if (Input.GetMouseButtonUp(0))
                    CompleteDragAtCursor();

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                pendingPickSlot = RaycastItemSlotAtMouse();
                if (pendingPickSlot != null && pendingPickSlot.HasItem)
                {
                    pendingPickActive = true;
                    pendingPickTimer = 0f;
                }
            }

            if (pendingPickActive && Input.GetMouseButton(0))
            {
                pendingPickTimer += Time.unscaledDeltaTime;
                if (pendingPickTimer >= holdToPickSeconds)
                {
                    BeginDrag(pendingPickSlot);
                    CancelPendingPick();
                }
            }

            if (Input.GetMouseButtonUp(0))
                CancelPendingPick();
        }

        private void CancelPendingPick()
        {
            pendingPickActive = false;
            pendingPickTimer = 0f;
            pendingPickSlot = null;
        }

        private void SelectQuickSlot(int index)
        {
            selectedQuickIndex = index;
            UpdateQuickIndicators();
        }

        private void UpdateQuickIndicators()
        {
            for (int i = 0; i < quickItemSlot.Count; i++)
                quickItemSlot[i].SetSelectedVisual(i == selectedQuickIndex);
        }

        public void ShowInventory()
        {
            mainInventoryPanel.gameObject.SetActive(true);
        }

        public void HideInventory()
        {
            mainInventoryPanel.gameObject.SetActive(false);
            CancelPendingPick();
            if (isDragging) CancelDrag();
        }


        // ===================== ADD / REMOVE =====================

        public void AddItem(ItemData data, int amountToAdd = 1, InventoryPanel panel = null)
        {
            if (data == null || amountToAdd <= 0) return;

            if (panel == null)
                panel = mainInventoryPanel;

            List<ItemSlot> targetList = GetSlotListFromPanel(panel);
            if (targetList == null) return;

            // stack
            if (data.isStackable && TryStackInList(targetList, data, amountToAdd))
            {
                NotifyChanged();
                return;
            }

            if (!data.isStackable && amountToAdd > 1)
            {
                for (int i = 0; i < amountToAdd; i++)
                {
                    AddItem(data, 1, panel);
                }
                return;
            }

            // empty
            if (TryPlaceInEmptySlot(targetList, data, amountToAdd))
            {
                NotifyChanged();
                return;
            }
        }

        private List<ItemSlot> GetSlotListFromPanel(InventoryPanel panel)
        {
            if (panel == mainInventoryPanel)
                return itemSlots;

            if (panel == quickInventoryPanel)
                return quickItemSlot;

            // storage
            if (currentStorage != null && panel == currentStorage.inventoryStorage)
                return currentStorage.GetSlots();

            return null;
        }

        private bool TryStackInList(List<ItemSlot> list, ItemData data, int amountToAdd)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ItemSlot slot = list[i];
                if (slot.HasItem && slot.ItemData == data && data.isStackable)
                {
                    int cap = Mathf.Max(1, data.maxAmount);
                    int space = cap - slot.Amount;
                    if (space <= 0) continue;

                    int moved = Mathf.Min(space, amountToAdd);
                    slot.SetItem(data, slot.Amount + moved);
                    amountToAdd -= moved;
                    if (amountToAdd <= 0) return true;
                }
            }
            return false;
        }

        private bool TryPlaceInEmptySlot(List<ItemSlot> list, ItemData data, int amountToAdd)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ItemSlot slot = list[i];
                if (!slot.HasItem)
                {
                    slot.SetItem(data, amountToAdd);
                    return true;
                }
            }
            return false;
        }

        // ===================== USE =====================

        public void UseSlot(ItemSlot slot)
        {
            if (slot == null || slot.ItemData == null) return;
            if (isUsingSlot) return;

            isUsingSlot = true;
            try
            {
                List<ActionBase> actions = slot.ItemData.itemActions;
                if (actions != null && actions.Count > 0)
                {
                    for (int i = 0; i < actions.Count; i++)
                    {
                        ActionBase action = actions[i];
                        if (action == null) continue;

                        if (action is ActionDrop || action is ActionSplit) continue;

                        action.DoAction(Character, slot);
                    }
                }

                UpdateQuickIndicators();
                NotifyChanged();
            }
            finally
            {
                isUsingSlot = false;
            }
        }

        public bool TryPlaceSplit(ItemData data, int amount)
        {
            if (data == null || amount <= 0) return false;

            // Try empty quick slots first
            for (int i = 0; i < quickItemSlot.Count; i++)
            {
                ItemSlot slot = quickItemSlot[i];
                if (slot != null && !slot.HasItem)
                {
                    slot.SetItem(data, amount);
                    UpdateQuickIndicators();
                    NotifyChanged();
                    return true;
                }
            }

            // Then try empty inventory slots
            for (int i = 0; i < itemSlots.Count; i++)
            {
                ItemSlot slot = itemSlots[i];
                if (slot != null && !slot.HasItem)
                {
                    slot.SetItem(data, amount);
                    NotifyChanged();
                    return true;
                }
            }

            return false;
        }

        private void UseQuickSlot(int index)
        {
            if (index < 0 || index >= quickItemSlot.Count) return;
            ItemSlot quickSlot = quickItemSlot[index];
            if (quickSlot.HasItem) UseSlot(quickSlot);
        }

        public void ReduceSlotAmount(ItemSlot slot, int amount = 1)
        {
            if (slot == null || slot.ItemData == null) return;

            int toRemove = Mathf.Max(1, amount);
            int newAmount = slot.Amount - toRemove;

            if (newAmount <= 0) slot.ResetSlot();
            else slot.SetItem(slot.ItemData, newAmount);

            NotifyChanged();
        }

        // ===================== TOOLTIP =====================

        public void ShowTooltip(ItemData itemData)
        {
            if (!showTooltipPanel || itemData == null) return;
            //if (inventoryPanel == null || !inventoryPanel.activeSelf) return;

            if (tooltipPanel != null) tooltipPanel.SetActive(true);
            if (itemNameText != null) itemNameText.text = itemData.itemName;
            if (itemDescriptionText != null) itemDescriptionText.text = itemData.description;
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
            if (itemNameText != null) itemNameText.text = "";
            if (itemDescriptionText != null) itemDescriptionText.text = "";
        }

        // ===================== SORT INVENTORY =====================

        private struct SlotSnapshot
        {
            public ItemData data;
            public int amount;
        }

        private int GetItemTypePriority(ItemType type)
        {
            if (inventorySortOrder == null) return int.MaxValue;
            int index = inventorySortOrder.IndexOf(type);
            return index >= 0 ? index : int.MaxValue;
        }

        public void SortInventory()
        {
            List<SlotSnapshot> items = new List<SlotSnapshot>();

            for (int i = 0; i < itemSlots.Count; i++)
            {
                ItemSlot slot = itemSlots[i];
                if (slot != null && slot.HasItem && slot.ItemData != null)
                {
                    items.Add(new SlotSnapshot
                    {
                        data = slot.ItemData,
                        amount = slot.Amount
                    });
                }
            }

            for (int i = 0; i < itemSlots.Count; i++)
                itemSlots[i].ResetSlot();

            items.Sort((a, b) =>
            {
                int typeCompare =
                    GetItemTypePriority(a.data.itemType)
                    .CompareTo(GetItemTypePriority(b.data.itemType));

                if (typeCompare != 0)
                    return typeCompare;

                return string.Compare(
                    a.data.itemName,
                    b.data.itemName,
                    System.StringComparison.OrdinalIgnoreCase
                );
            });

            for (int i = 0; i < items.Count && i < itemSlots.Count; i++)
                itemSlots[i].SetItem(items[i].data, items[i].amount);

            NotifyChanged();
        }

        // ===================== DRAG & DROP =====================

        public void BeginDrag(ItemSlot origin)
        {
            if (!IsInventoryOpen || isDragging || origin == null || !origin.HasItem) return;
            isDragging = true;
            dragOrigin = origin;
            dragData = origin.ItemData;
            dragAmount = origin.Amount;
            CreateDragIcon(dragData.iconInventory, dragAmount);
            UpdateDragIconPosition();
        }

        private void CompleteDragAtCursor()
        {
            ItemSlot target = RaycastItemSlotAtMouse();
            bool success = false;

            if (target != null && target != dragOrigin)
            {
                if (!target.HasItem)
                {
                    target.SetItem(dragData, dragAmount);
                    dragOrigin.ResetSlot();
                    success = true;
                }
                else if (target.ItemData == dragData && dragData.isStackable)
                {
                    int cap = Mathf.Max(1, dragData.maxAmount);
                    int space = cap - target.Amount;
                    if (space > 0)
                    {
                        int moved = Mathf.Min(space, dragAmount);
                        target.SetItem(dragData, target.Amount + moved);
                        int left = dragAmount - moved;
                        if (left <= 0) dragOrigin.ResetSlot();
                        else dragOrigin.SetItem(dragData, left);
                        success = true;
                    }
                }
                else
                {
                    ItemData td = target.ItemData;
                    int ta = target.Amount;
                    target.SetItem(dragData, dragAmount);
                    dragOrigin.SetItem(td, ta);
                    success = true;
                }
            }
            else
            {
                if (dragOrigin != null && dragData != null)
                {
                    dragOrigin.DropItem(Character.transform.position, dragAmount);
                    dragOrigin.ResetSlot();
                    success = true;
                }
            }

            DestroyDragIcon();
            isDragging = false;
            dragOrigin = null;
            dragData = null;
            dragAmount = 0;

            if (success)
            {
                UpdateQuickIndicators();
                NotifyChanged();
            }
        }

        private void CancelDrag()
        {
            DestroyDragIcon();
            isDragging = false;
            dragOrigin = null;
            dragData = null;
            dragAmount = 0;
        }

        private void CreateDragIcon(Sprite sprite, int amount)
        {
            GameObject go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(rootCanvas.transform, false);
            dragIconRT = go.GetComponent<RectTransform>();
            dragIconImage = go.GetComponent<Image>();
            dragIconImage.sprite = sprite;
            dragIconImage.raycastTarget = false;
            dragIconRT.sizeDelta = new Vector2(64f, 64f);

            GameObject textGO = new GameObject("Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            dragIconAmount = textGO.GetComponent<Text>();
            dragIconAmount.raycastTarget = false;
            dragIconAmount.alignment = TextAnchor.LowerRight;
            dragIconAmount.font = itemNameText != null ? itemNameText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            dragIconAmount.fontSize = 20;
            dragIconAmount.text = amount > 1 ? amount.ToString() : "";
            RectTransform tr = textGO.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
        }

        private void UpdateDragIconPosition()
        {
            if (dragIconRT == null || rootCanvas == null) return;

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                dragIconRT.position = Input.mousePosition;
            }
            else
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    Input.mousePosition,
                    rootCanvas.worldCamera,
                    out local);
                dragIconRT.localPosition = local;
            }
        }

        private void DestroyDragIcon()
        {
            if (dragIconRT != null) Destroy(dragIconRT.gameObject);
            dragIconRT = null;
            dragIconImage = null;
            dragIconAmount = null;
        }

        private ItemSlot RaycastItemSlotAtMouse()
        {
            if (raycaster == null && rootCanvas != null) raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) return null;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                ItemSlot slot = results[i].gameObject.GetComponentInParent<ItemSlot>();
                if (slot != null) return slot;
            }
            return null;
        }

        // ===================== QUICK SELECT ACCESSORS =====================

        public ItemSlot GetSelectedQuickSlot()
        {
            if (selectedQuickIndex < 0) return null;
            if (selectedQuickIndex >= quickItemSlot.Count) return null;

            ItemSlot slot = quickItemSlot[selectedQuickIndex];
            return slot;
        }

        public ItemData GetSelectedQuickItemData()
        {
            ItemSlot slot = GetSelectedQuickSlot();
            if (slot == null) return null;
            if (!slot.HasItem) return null;

            return slot.ItemData;
        }

        // ===================== GROUP CHECKS (QUICK SLOTS ONLY) =====================

        /// <summary>
        /// Returns true if ANY filled quick slot contains an item that belongs to the given group.
        /// </summary>
        public bool HasGroup(GroupData group)
        {
            if (group == null) return false;
            for (int i = 0; i < quickItemSlot.Count; i++)
            {
                var slot = quickItemSlot[i];
                if (slot == null || !slot.HasItem || slot.ItemData == null) continue;
                var groups = slot.ItemData.groups;
                if (groups != null && groups.Contains(group))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if ANY of the provided groups is present in ANY filled quick slot.
        /// </summary>
        public bool HasAnyGroup(IList<GroupData> groups)
        {
            if (groups == null || groups.Count == 0) return false;
            for (int i = 0; i < groups.Count; i++)
            {
                if (HasGroup(groups[i])) return true;
            }
            return false;
        }

        // ===================== STARTING ITEMS =====================

        private void SetStartingItems()
        {
            for (int i = 0; i < startingItems.Count; i++)
            {
                StartingItem entry = startingItems[i];
                if (entry == null || entry.item == null) continue;

                int amount = Mathf.Max(1, entry.amount);
                AddItem(entry.item, amount);
            }
        }

        // ===================== CRAFTING HELPERS =====================

        private IEnumerable<ItemSlot> AllSlots()
        {
            for (int i = 0; i < quickItemSlot.Count; i++)
                yield return quickItemSlot[i];
            for (int i = 0; i < itemSlots.Count; i++)
                yield return itemSlots[i];
        }

        public int CountOf(ItemData data)
        {
            if (data == null) return 0;
            int total = 0;
            foreach (ItemSlot slot in AllSlots())
            {
                if (slot != null && slot.HasItem && slot.ItemData == data)
                    total += Mathf.Max(0, slot.Amount);
            }
            return total;
        }

        public int CountOfGroup(GroupData group)
        {
            if (group == null) return 0;
            int total = 0;
            foreach (ItemSlot slot in AllSlots())
            {
                if (slot != null && slot.HasItem && slot.ItemData != null && slot.ItemData.groups != null)
                {
                    if (slot.ItemData.groups.Contains(group))
                        total += Mathf.Max(0, slot.Amount);
                }
            }
            return total;
        }

        public bool CanCraft(ItemData output)
        {
            if (output == null) return false;

            bool hasReqs =
                (output.craftItemsNeeded != null && output.craftItemsNeeded.Count > 0) ||
                (output.craftGroupsNeeded != null && output.craftGroupsNeeded.Count > 0);

            if (!hasReqs) return false;

            // Specific items
            if (output.craftItemsNeeded != null)
            {
                for (int i = 0; i < output.craftItemsNeeded.Count; i++)
                {
                    CraftItemData req = output.craftItemsNeeded[i];
                    if (req == null || req.itemData == null) return false;
                    if (CountOf(req.itemData) < Mathf.Max(1, req.Amount)) return false;
                }
            }

            // Grouped items
            if (output.craftGroupsNeeded != null)
            {
                for (int i = 0; i < output.craftGroupsNeeded.Count; i++)
                {
                    CraftGroupData req = output.craftGroupsNeeded[i];
                    if (req == null || req.GroupData == null) return false;
                    if (CountOfGroup(req.GroupData) < Mathf.Max(1, req.Amount)) return false;
                }
            }

            return true;
        }

        public bool Craft(ItemData output)
        {
            if (!CanCraft(output)) return false;

            if (output.craftItemsNeeded != null)
            {
                for (int i = 0; i < output.craftItemsNeeded.Count; i++)
                {
                    CraftItemData req = output.craftItemsNeeded[i];
                    ConsumeByItem(req.itemData, Mathf.Max(1, req.Amount));
                }
            }

            if (output.craftGroupsNeeded != null)
            {
                for (int i = 0; i < output.craftGroupsNeeded.Count; i++)
                {
                    CraftGroupData req = output.craftGroupsNeeded[i];
                    ConsumeByGroup(req.GroupData, Mathf.Max(1, req.Amount));
                }
            }

            int amount = Mathf.Max(1, output.craftResultAmount);
            AddItem(output, amount);

            NotifyChanged();
            return true;
        }

        private void ConsumeByItem(ItemData data, int amount)
        {
            if (data == null || amount <= 0) return;

            int remaining = amount;

            foreach (ItemSlot slot in AllSlots())
            {
                if (remaining <= 0) break;
                if (slot == null || !slot.HasItem || slot.ItemData != data) continue;

                int take = Mathf.Min(remaining, slot.Amount);
                ReduceSlotAmount(slot, take);
                remaining -= take;
            }
        }

        private void ConsumeByGroup(GroupData group, int amount)
        {
            if (group == null || amount <= 0) return;

            int remaining = amount;

            foreach (ItemSlot slot in AllSlots())
            {
                if (remaining <= 0) break;
                if (slot == null || !slot.HasItem || slot.ItemData == null || slot.ItemData.groups == null) continue;
                if (!slot.ItemData.groups.Contains(group)) continue;

                int take = Mathf.Min(remaining, slot.Amount);
                ReduceSlotAmount(slot, take);
                remaining -= take;
            }
        }

        [System.Serializable]
        public class StartingItem
        {
            public ItemData item;
            public int amount = 1;
        }
    }
}
