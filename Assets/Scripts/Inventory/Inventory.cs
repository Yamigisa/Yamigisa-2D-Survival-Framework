using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class Inventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        public int maxItems = 32;

        [Header("Inventory Items")]
        [SerializeField] private InventoryItem inventoryItemPrefab;

        private List<InventoryItem> inventoryItemSlots = new List<InventoryItem>();

        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryPanel;

        [Header("Quick Inventory")]
        [SerializeField] private int quickSlotCount = 8;
        [SerializeField] private Transform quickInventoryContent;
        private List<InventoryItem> quickInventoryItemSlots = new List<InventoryItem>();

        [Header("Tooltip Panel")]
        [SerializeField] private bool showTooltipPanel = true;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;

        [Header("Drag & Drop")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GraphicRaycaster raycaster;
        [SerializeField] private float holdToPickSeconds = 0.25f;

        private int selectedQuickIndex = -1;

        private bool isDragging;
        private InventoryItem dragOrigin;
        private ItemData dragData;
        private int dragAmount;
        private RectTransform dragIconRT;
        private Image dragIconImage;
        private Text dragIconAmount;

        private bool pendingPickActive;
        private float pendingPickTimer;
        private InventoryItem pendingPickSlot;

        public bool IsDragging { get { return isDragging; } }
        public bool IsInventoryOpen { get { return inventoryPanel != null && inventoryPanel.activeSelf; } }

        private CharacterControls controls;
        public Character Character;

        private bool isUsingSlot;

        // ===== Inventory change event (for Crafting UI etc.) =====
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

            // Ensure we can read starting items
            if (Character == null) Character = FindObjectOfType<Character>();
        }

        private void Start()
        {
            if (controls == null) controls = FindObjectOfType<CharacterControls>();
            if (rootCanvas == null && inventoryPanel != null)
                rootCanvas = inventoryPanel.GetComponentInParent<Canvas>();
            if (raycaster == null && rootCanvas != null)
                raycaster = rootCanvas.GetComponent<GraphicRaycaster>();

            // Build main inventory slots
            inventoryItemSlots.Clear();
            for (int i = 0; i < maxItems; i++)
            {
                InventoryItem newSlot = Instantiate(inventoryItemPrefab, inventoryContent);
                inventoryItemSlots.Add(newSlot);
            }

            // Build quick slots
            quickInventoryItemSlots.Clear();
            for (int i = 0; i < quickSlotCount; i++)
            {
                InventoryItem quickSlot = Instantiate(inventoryItemPrefab, quickInventoryContent);
                quickSlot.MarkAsQuickSlot(true);
                quickInventoryItemSlots.Add(quickSlot);
            }

            // Seed starting items
            SetStartingItems();

            // Select first quick slot
            if (quickInventoryItemSlots.Count > 0)
            {
                selectedQuickIndex = 0;
                SelectQuickSlot(selectedQuickIndex);
                UpdateQuickIndicators();
            }

            // Force a late broadcast next frame so any late-subscribing UIs (e.g. CraftingInterface)
            // will refresh counts including starting items.
            StartCoroutine(BroadcastInventoryChangedNextFrame());
        }

        private IEnumerator BroadcastInventoryChangedNextFrame()
        {
            yield return null; // wait one frame (after all Start calls)
            NotifyChanged();
        }

        private void Update()
        {
            if (controls == null) return;

            // Toggle Inventory
            if (controls.IsAnyKeyPressedDown(controls.inventoryKey))
            {
                if (inventoryPanel != null && inventoryPanel.activeSelf) HideInventory();
                else ShowInventory();
            }

            // Quick slot number keys (work even when inventory is closed)
            for (int i = 0; i < quickInventoryItemSlots.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    SelectQuickSlot(i);
            }

            // Use selected quick slot (e.g., E)
            if (!isDragging && controls.IsAnyKeyPressedDown(controls.useItemKey) && selectedQuickIndex >= 0)
                UseQuickSlot(selectedQuickIndex);

            // Stop here if inventory closed (drag/tooltip only when open)
            if (!IsInventoryOpen)
            {
                CancelPendingPick();
                if (isDragging) CancelDrag();
                return;
            }

            // Drag
            if (isDragging)
            {
                UpdateDragIconPosition();
                if (Input.GetMouseButtonUp(0)) CompleteDragAtCursor();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                pendingPickSlot = RaycastInventoryItemAtMouse();
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

            if (Input.GetMouseButtonUp(0)) CancelPendingPick();

            // Tooltip follows cursor when open
            if (showTooltipPanel && tooltipPanel != null && tooltipPanel.activeSelf)
            {
                Vector2 pos;
                RectTransform canvasRT = inventoryPanel != null
                    ? inventoryPanel.GetComponentInParent<Canvas>().transform as RectTransform
                    : null;

                if (canvasRT != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT,
                        Input.mousePosition,
                        null,
                        out pos);
                    RectTransform tprt = tooltipPanel.GetComponent<RectTransform>();
                    if (tprt != null) tprt.anchoredPosition = pos + new Vector2(0f, 30f);
                }
            }
        }

        private void CancelPendingPick()
        {
            pendingPickActive = false;
            pendingPickTimer = 0f;
            pendingPickSlot = null;
        }

        private void SelectQuickSlot(int index)
        {
            if (index < 0 || index >= quickInventoryItemSlots.Count) return;
            selectedQuickIndex = index;
            UpdateQuickIndicators();
        }

        private void UpdateQuickIndicators()
        {
            for (int i = 0; i < quickInventoryItemSlots.Count; i++)
                quickInventoryItemSlots[i].SetSelectedVisual(i == selectedQuickIndex);
        }

        public void ShowInventory() { if (inventoryPanel != null) inventoryPanel.SetActive(true); }
        public void HideInventory()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            CancelPendingPick();
            if (isDragging) CancelDrag();
        }

        // ===================== ADD / REMOVE =====================

        public void AddItem(ItemData data, int amountToAdd = 1)
        {
            if (data == null || amountToAdd <= 0) return;

            // Try stack in quick
            if (data.isStackable && TryStackInList(quickInventoryItemSlots, data, amountToAdd))
            {
                UpdateQuickIndicators();
                NotifyChanged();
                return;
            }
            // Try stack in inventory
            if (data.isStackable && TryStackInList(inventoryItemSlots, data, amountToAdd))
            {
                UpdateQuickIndicators();
                NotifyChanged();
                return;
            }
            // Empty in quick
            if (TryPlaceInEmptySlot(quickInventoryItemSlots, data, amountToAdd))
            {
                UpdateQuickIndicators();
                NotifyChanged();
                return;
            }
            // Empty in inventory
            if (TryPlaceInEmptySlot(inventoryItemSlots, data, amountToAdd))
            {
                UpdateQuickIndicators();
                NotifyChanged();
                return;
            }
        }

        private bool TryStackInList(List<InventoryItem> list, ItemData data, int amountToAdd)
        {
            for (int i = 0; i < list.Count; i++)
            {
                InventoryItem slot = list[i];
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

        private bool TryPlaceInEmptySlot(List<InventoryItem> list, ItemData data, int amountToAdd)
        {
            for (int i = 0; i < list.Count; i++)
            {
                InventoryItem slot = list[i];
                if (!slot.HasItem)
                {
                    slot.SetItem(data, amountToAdd);
                    return true;
                }
            }
            return false;
        }

        // ===================== USE =====================

        public void UseSlot(InventoryItem slot)
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
                        action.DoAction(Character, slot);
                    }
                }

                // Only auto-consume stack for Consumables
                if (slot.ItemData.itemType == ItemType.Consumable)
                {
                    ReduceSlotAmount(slot, 1);
                }

                UpdateQuickIndicators();
                NotifyChanged();
            }
            finally
            {
                isUsingSlot = false;
            }
        }

        private void UseQuickSlot(int index)
        {
            if (index < 0 || index >= quickInventoryItemSlots.Count) return;
            InventoryItem quickSlot = quickInventoryItemSlots[index];
            if (quickSlot.HasItem) UseSlot(quickSlot);
        }

        public void ReduceSlotAmount(InventoryItem slot, int amount = 1)
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
            if (inventoryPanel == null || !inventoryPanel.activeSelf) return;

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

        // ===================== DRAG & DROP =====================

        public void BeginDrag(InventoryItem origin)
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
            InventoryItem target = RaycastInventoryItemAtMouse();
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
            if (rootCanvas == null && inventoryPanel != null)
                rootCanvas = inventoryPanel.GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;

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

        private InventoryItem RaycastInventoryItemAtMouse()
        {
            if (raycaster == null && rootCanvas != null) raycaster = rootCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) return null;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                InventoryItem slot = results[i].gameObject.GetComponentInParent<InventoryItem>();
                if (slot != null) return slot;
            }
            return null;
        }

        // ===================== QUICK SELECT ACCESSORS =====================

        public InventoryItem GetSelectedQuickSlot()
        {
            if (selectedQuickIndex < 0) return null;
            if (selectedQuickIndex >= quickInventoryItemSlots.Count) return null;

            InventoryItem slot = quickInventoryItemSlots[selectedQuickIndex];
            return slot;
        }

        public ItemData GetSelectedQuickItemData()
        {
            InventoryItem slot = GetSelectedQuickSlot();
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
            for (int i = 0; i < quickInventoryItemSlots.Count; i++)
            {
                var slot = quickInventoryItemSlots[i];
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
            if (Character == null) Character = FindObjectOfType<Character>();
            if (Character == null) return;
            if (Character.startingItems == null || Character.startingItems.Count == 0) return;

            for (int i = 0; i < Character.startingItems.Count; i++)
            {
                ItemData data = Character.startingItems[i];
                if (data == null) continue;
                AddItem(data, 1);
            }
        }

        // ===================== CRAFTING HELPERS =====================

        private IEnumerable<InventoryItem> AllSlots()
        {
            for (int i = 0; i < quickInventoryItemSlots.Count; i++)
                yield return quickInventoryItemSlots[i];
            for (int i = 0; i < inventoryItemSlots.Count; i++)
                yield return inventoryItemSlots[i];
        }

        public int CountOf(ItemData data)
        {
            if (data == null) return 0;
            int total = 0;
            foreach (InventoryItem slot in AllSlots())
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
            foreach (InventoryItem slot in AllSlots())
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

            // Consume specific items
            if (output.craftItemsNeeded != null)
            {
                for (int i = 0; i < output.craftItemsNeeded.Count; i++)
                {
                    CraftItemData req = output.craftItemsNeeded[i];
                    ConsumeByItem(req.itemData, Mathf.Max(1, req.Amount));
                }
            }

            // Consume groups (any items matching group)
            if (output.craftGroupsNeeded != null)
            {
                for (int i = 0; i < output.craftGroupsNeeded.Count; i++)
                {
                    CraftGroupData req = output.craftGroupsNeeded[i];
                    ConsumeByGroup(req.GroupData, Mathf.Max(1, req.Amount));
                }
            }

            // Grant result
            int amount = Mathf.Max(1, output.craftResultAmount);
            AddItem(output, amount);

            NotifyChanged();
            return true;
        }

        private void ConsumeByItem(ItemData data, int amount)
        {
            if (data == null || amount <= 0) return;

            int remaining = amount;

            foreach (InventoryItem slot in AllSlots())
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

            foreach (InventoryItem slot in AllSlots())
            {
                if (remaining <= 0) break;
                if (slot == null || !slot.HasItem || slot.ItemData == null || slot.ItemData.groups == null) continue;
                if (!slot.ItemData.groups.Contains(group)) continue;

                int take = Mathf.Min(remaining, slot.Amount);
                ReduceSlotAmount(slot, take);
                remaining -= take;
            }
        }
    }
}
