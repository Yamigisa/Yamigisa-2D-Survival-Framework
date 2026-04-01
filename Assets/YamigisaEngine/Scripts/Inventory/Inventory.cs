using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    public class Inventory : MonoBehaviour, ISavable
    {
        [Header("Inventory Panels (Scene References)")]
        [SerializeField] private InventoryPanel mainInventoryPanel;
        [SerializeField] private InventoryPanel quickInventoryPanel;

        [Header("Inventory Prefabs")]
        [SerializeField] private ItemSlot ItemSlotPrefab;
        [SerializeField] private InventoryPanel inventoryPanelPrefab;

        [Header("Inventory Transform")]
        [SerializeField] private Transform mainInventoryTransform;
        [SerializeField] private Transform quickInventoryTransform;

        [Header("Quick Inventory")]
        [SerializeField] private int quickSlotCount = 8;
        [SerializeField] private Transform quickInventoryContent;

        [Header("Max Item Count")]
        [SerializeField] private int baseInventorySize = 32;

        [Header("Starting Items")]
        public List<StartingItem> startingItems;

        private List<ItemSlot> itemSlots = new List<ItemSlot>();

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

        // [Header("Tooltip Panel")]
        // [SerializeField] private bool showTooltipPanel = true;
        // [SerializeField] private GameObject tooltipPanel;
        // [SerializeField] private Text itemNameText;
        // [SerializeField] private Text itemDescriptionText;

        [Header("Drag & Drop")]
        [SerializeField] private float holdToPickSeconds = 0.25f;
        private Canvas rootCanvas;
        private GraphicRaycaster raycaster;

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
        public bool IsInventoryOpen { get { return mainInventoryPanel != null && mainInventoryPanel.gameObject.activeSelf; } }

        private CharacterControls controls;
        private Character Character;

        private bool isUsingSlot;
        [HideInInspector] public Storage currentStorage;

        public EquipmentManager equipmentManager { get; private set; }
        private int equipmentBagBonus = 0;
        private int currentInventorySize;
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

            equipmentManager = GetComponentInChildren<EquipmentManager>();
        }

        public void Setup()
        {
            Character = Character.instance.GetCharacter();
            controls = Character.characterControls;

            mainInventoryPanel.inventoryOwner = this;
            quickInventoryPanel.inventoryOwner = this;

            mainInventoryPanel.ClearPanel();
            quickInventoryPanel.ClearPanel();

            mainInventoryPanel.gameObject.SetActive(false);
            rootCanvas = GetComponentInParent<Canvas>();
            raycaster = rootCanvas.GetComponent<GraphicRaycaster>();

            itemSlots.Clear();

            // Main inventory slots
            currentInventorySize = baseInventorySize + equipmentBagBonus;

            for (int i = 0; i < currentInventorySize; i++)
            {
                ItemSlot slot = Instantiate(
                    ItemSlotPrefab,
                    mainInventoryPanel.inventoryContent

                );
                itemSlots.Add(slot);
                slot.MarkAsQuickSlot(false);
                slot.HideQuickSlotIndex();
            }

            // Quick inventory slots
            for (int i = 0; i < quickSlotCount; i++)
            {
                ItemSlot slot = Instantiate(
    ItemSlotPrefab,
    quickInventoryPanel.inventoryContent
);

                slot.MarkAsQuickSlot(true, i);
                quickItemSlot.Add(slot);
            }

            SetStartingItems();

            if (quickItemSlot.Count > 0)
            {
                selectedQuickIndex = 0;
                SelectQuickSlot(selectedQuickIndex);
                UpdateQuickIndicators();
            }

            StartCoroutine(BroadcastInventoryChangedNextFrame());

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
            // Inventory Toggle
            if (controls.IsPressedDown(controls.inventory))
            {
                if (currentStorage != null)
                {
                    currentStorage.CloseStorage();
                }
                else if (IsInventoryOpen)
                {
                    HideInventory();
                }
                else if (!Character.instance.CharacterIsBusy())
                {
                    ShowInventory();
                }
            }
            else if (controls.IsPressedDown(controls.cancel))
            {
                if (currentStorage != null)
                {
                    currentStorage.CloseStorage();
                }
                else if (IsInventoryOpen)
                {
                    HideInventory();
                }
            }

            for (int i = 0; i < quickItemSlot.Count && i < 10; i++)
            {
                KeyCode key;

                if (i == 9) // 10th slot → key 0
                    key = KeyCode.Alpha0;
                else
                    key = KeyCode.Alpha1 + i;

                if (Input.GetKeyDown(key))
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

            if (!isDragging && controls.IsPressed(controls.useItem) && selectedQuickIndex >= 0)
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
            Character.instance.SetCharacterBusy(true);
            GameManager.instance.SetCanPause(false);

            mainInventoryPanel.gameObject.SetActive(true);
            equipmentManager.ShowEquipmentPanel();
        }

        public void HideInventory()
        {
            Character.instance.SetCharacterBusy(false, 0.1f);
            GameManager.instance.SetCanPause(true);

            mainInventoryPanel.gameObject.SetActive(false);
            equipmentManager.HideEquipmentPanel();
        }


        // ===================== ADD / REMOVE =====================

        public void AddItem(ItemData data, int amountToAdd = 1, InventoryPanel panel = null)
        {
            if (data == null || amountToAdd <= 0) return;

            // 🔥 If no specific panel, use smart priority
            if (panel == null)
            {
                // 1️⃣ Try Quick Inventory first
                if (TryAddToPanel(quickInventoryPanel, data, amountToAdd))
                {
                    NotifyChanged();
                    return;
                }

                // 2️⃣ Then Main Inventory
                if (TryAddToPanel(mainInventoryPanel, data, amountToAdd))
                {
                    NotifyChanged();
                    return;
                }

                // No space
                return;
            }

            // If specific panel provided, use that only
            if (TryAddToPanel(panel, data, amountToAdd))
            {
                NotifyChanged();
                return;
            }
        }

        private bool TryAddToPanel(InventoryPanel panel, ItemData data, int amountToAdd)
        {
            List<ItemSlot> targetList = GetSlotListFromPanel(panel);
            if (targetList == null) return false;

            int remaining = amountToAdd;

            // Stack first
            if (data.isStackable)
            {
                for (int i = 0; i < targetList.Count; i++)
                {
                    ItemSlot slot = targetList[i];
                    if (slot.HasItem && slot.ItemData == data)
                    {
                        int cap = Mathf.Max(1, data.maxAmount);
                        int space = cap - slot.Amount;
                        if (space <= 0) continue;

                        int moved = Mathf.Min(space, remaining);
                        slot.SetItem(data, slot.Amount + moved);
                        remaining -= moved;

                        if (remaining <= 0)
                            return true;
                    }
                }
            }

            // Empty slots
            for (int i = 0; i < targetList.Count; i++)
            {
                ItemSlot slot = targetList[i];

                if (!slot.HasItem)
                {
                    if (data.isStackable)
                    {
                        slot.SetItem(data, remaining);
                        return true;
                    }
                    else
                    {
                        slot.SetItem(data, 1);
                        remaining--;

                        if (remaining <= 0)
                            return true;
                    }
                }
            }

            return false;
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
                if (data.isStackable && slot.HasItem && slot.ItemData == data)
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

        // public void ShowTooltip(ItemData itemData)
        // {
        //     if (!showTooltipPanel || itemData == null) return;
        //     //if (inventoryPanel == null || !inventoryPanel.activeSelf) return;

        //     if (tooltipPanel != null) tooltipPanel.SetActive(true);
        //     if (itemNameText != null) itemNameText.text = itemData.itemName;
        //     if (itemDescriptionText != null) itemDescriptionText.text = itemData.description;
        // }

        // public void HideTooltip()
        // {
        //     if (tooltipPanel != null) tooltipPanel.SetActive(false);
        //     if (itemNameText != null) itemNameText.text = "";
        //     if (itemDescriptionText != null) itemDescriptionText.text = "";
        // }

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
            EquipmentSlot originEquipSlot = dragOrigin.GetComponentInParent<EquipmentSlot>();
            ItemSlot target = RaycastItemSlotAtMouse();
            bool success = false;

            if (target != null && target != dragOrigin)
            {
                EquipmentSlot equipSlot = target.GetComponentInParent<EquipmentSlot>();

                // ================= EQUIPMENT SLOT =================
                if (equipSlot != null)
                {
                    // Must be equipment
                    if (dragData.itemType != ItemType.Equipment)
                    {
                        CancelDragState();
                        return;
                    }

                    // Must match slot type
                    if (!equipSlot.CanEquip(dragData))
                    {
                        CancelDragState();
                        return;
                    }

                    if (equipmentManager == null)
                    {
                        CancelDragState();
                        return;
                    }

                    // If slot already has equipment → return it to inventory
                    bool equipped = equipmentManager.Equip(dragData);

                    if (equipped)
                    {
                        dragOrigin.ResetSlot();
                        success = true;
                    }
                }

                // ================= NORMAL INVENTORY =================
                else
                {
                    if (!target.HasItem)
                    {
                        if (originEquipSlot != null)
                        {
                            bool removed = equipmentManager.Unequip(originEquipSlot.SlotType);
                            if (!removed)
                            {
                                CancelDragState();
                                return;
                            }

                            // Unequip already returns item to inventory
                            success = true;
                        }
                        else
                        {
                            target.SetItem(dragData, dragAmount);
                            dragOrigin.ResetSlot();
                            success = true;
                        }
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

                            if (left <= 0)
                                dragOrigin.ResetSlot();
                            else
                                dragOrigin.SetItem(dragData, left);

                            success = true;
                        }
                    }
                    else
                    {
                        // swap
                        ItemData tempData = target.ItemData;
                        int tempAmount = target.Amount;

                        target.SetItem(dragData, dragAmount);
                        dragOrigin.SetItem(tempData, tempAmount);

                        success = true;
                    }
                }
            }
            else
            {
                EquipmentSlot equipSlot = dragOrigin.GetComponentInParent<EquipmentSlot>();

                if (equipSlot != null)
                {
                    ItemData equippedItem = equipSlot.GetEquippedItem();
                    if (equippedItem == null)
                    {
                        CancelDragState();
                        return;
                    }

                    equipSlot.Unequip();
                    dragOrigin.DropItem(Character.transform.position, 1);
                    success = true;
                }
                else
                {
                    dragOrigin.DropItem(Character.transform.position, dragAmount);
                    dragOrigin.ResetSlot();
                    success = true;
                }
            }

            CancelDragState();

            if (success)
            {
                UpdateQuickIndicators();
                NotifyChanged();
            }
        }

        public int GetUsedSlotCount()
        {
            int count = 0;

            foreach (var slot in itemSlots)
                if (slot.HasItem)
                    count++;

            return count;
        }
        public int GetCurrentCapacity()
        {
            return baseInventorySize + equipmentBagBonus;
        }
        private void CancelDragState()
        {
            DestroyDragIcon();
            isDragging = false;
            dragOrigin = null;
            dragData = null;
            dragAmount = 0;
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
            //dragIconAmount.font = itemNameText != null ? itemNameText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
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
            if (raycaster == null && rootCanvas != null)
                raycaster = rootCanvas.GetComponent<GraphicRaycaster>();

            if (raycaster == null)
                return null;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(eventData, results);

            ItemSlot normalSlot = null;

            for (int i = 0; i < results.Count; i++)
            {
                ItemSlot slot = results[i].gameObject.GetComponentInParent<ItemSlot>();
                if (slot == null) continue;

                // PRIORITY: Equipment slot
                if (slot.GetComponentInParent<EquipmentSlot>() != null)
                    return slot;

                if (normalSlot == null)
                    normalSlot = slot;
            }

            return normalSlot;
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

        public void SetEquipmentBagBonus(int value)
        {
            equipmentBagBonus = value;
            RefreshInventorySize();
        }

        private void RefreshInventorySize()
        {
            int newSize = baseInventorySize + equipmentBagBonus;

            if (newSize == itemSlots.Count)
                return;

            // EXPAND inventory
            if (newSize > itemSlots.Count)
            {
                int toAdd = newSize - itemSlots.Count;

                for (int i = 0; i < toAdd; i++)
                {
                    ItemSlot slot = Instantiate(
                        ItemSlotPrefab,
                        mainInventoryPanel.inventoryContent
                    );

                    itemSlots.Add(slot);
                    slot.MarkAsQuickSlot(false);
                    slot.HideQuickSlotIndex();
                }
            }
            else
            {
                int targetSize = newSize;

                // STEP 1 — collect items from bag slots
                List<(ItemData data, int amount)> displacedItems = new();

                for (int i = targetSize; i < itemSlots.Count; i++)
                {
                    ItemSlot slot = itemSlots[i];

                    if (slot.HasItem)
                    {
                        displacedItems.Add((slot.ItemData, slot.Amount));
                    }
                }

                // STEP 2 — remove bag slots
                for (int i = itemSlots.Count - 1; i >= targetSize; i--)
                {
                    Destroy(itemSlots[i].gameObject);
                    itemSlots.RemoveAt(i);
                }

                // STEP 3 — reinsert displaced items
                foreach (var entry in displacedItems)
                {
                    ItemData data = entry.data;
                    int amount = entry.amount;

                    bool placed = false;

                    // 1️⃣ Quick inventory first
                    for (int q = 0; q < quickItemSlot.Count; q++)
                    {
                        ItemSlot qSlot = quickItemSlot[q];

                        if (!qSlot.HasItem)
                        {
                            qSlot.SetItem(data, amount);
                            placed = true;
                            break;
                        }
                    }

                    // 2️⃣ Main inventory
                    if (!placed)
                    {
                        for (int i = 0; i < itemSlots.Count; i++)
                        {
                            ItemSlot slot = itemSlots[i];

                            if (!slot.HasItem)
                            {
                                slot.SetItem(data, amount);
                                placed = true;
                                break;
                            }
                        }
                    }
                }
            }

            currentInventorySize = newSize;

            NotifyChanged();
        }

        public int TotalItemCount()
        {
            int count = 0;

            foreach (var slot in itemSlots)
            {
                if (slot.HasItem)
                    count++;
            }

            return count;
        }

        public int GetBaseCapacity()
        {
            return baseInventorySize;
        }
        public void Save(ref SaveGameData data)
        {
            if (!data.saveManager.SaveInventory)
                return;

            InventorySaveData inv = new InventorySaveData();
            inv.selectedQuickIndex = selectedQuickIndex;

            // Main inventory
            for (int i = 0; i < itemSlots.Count; i++)
            {
                ItemSlot slot = itemSlots[i];
                if (slot == null || !slot.HasItem || slot.ItemData == null) continue;

                inv.items.Add(new InventoryItemSaveData
                {
                    itemId = slot.ItemData.Id,
                    amount = slot.Amount,
                    isQuick = false,
                    index = i
                });
            }

            // Quick inventory
            for (int i = 0; i < quickItemSlot.Count; i++)
            {
                ItemSlot slot = quickItemSlot[i];
                if (slot == null || !slot.HasItem || slot.ItemData == null) continue;

                inv.items.Add(new InventoryItemSaveData
                {
                    itemId = slot.ItemData.Id,
                    amount = slot.Amount,
                    isQuick = true,
                    index = i
                });
            }

            data.inventory = inv;
        }

        public void Load(SaveGameData data)
        {
            if (itemSlots.Count == 0 || quickItemSlot.Count == 0)
            {
                return;
            }

            if (data.inventory == null) return;

            // Clear all slots
            foreach (ItemSlot slot in itemSlots)
                slot.ResetSlot();

            foreach (ItemSlot slot in quickItemSlot)
                slot.ResetSlot();

            // Restore items
            foreach (var saved in data.inventory.items)
            {
                ItemData item = ItemDatabase.Get(saved.itemId);
                if (item == null) continue;

                if (saved.isQuick)
                {
                    if (saved.index < 0 || saved.index >= quickItemSlot.Count) continue;
                    quickItemSlot[saved.index].SetItem(item, saved.amount);
                }
                else
                {
                    if (saved.index < 0 || saved.index >= itemSlots.Count) continue;
                    itemSlots[saved.index].SetItem(item, saved.amount);
                }
            }

            // Restore selected quick slot
            selectedQuickIndex = Mathf.Clamp(
                data.inventory.selectedQuickIndex,
                0,
                quickItemSlot.Count - 1
            );

            UpdateQuickIndicators();
            NotifyChanged();
        }

        public bool CanAddItem(ItemData data, int amount = 1)
        {
            if (data == null) return false;

            // Example simple logic:
            // 1. If stackable, check existing stacks
            // 2. If not stackable, check empty slot count

            if (data.isStackable)
            {
                foreach (var slot in itemSlots)
                {
                    if (slot.HasItem && slot.ItemData == data && slot.Amount < data.maxAmount)
                        return true;
                }
            }

            // Check empty slot
            foreach (var slot in itemSlots)
            {
                if (!slot.HasItem)
                    return true;
            }

            return false;
        }

        public InventoryPanel GetMainInventoryPanel()
        {
            return mainInventoryPanel;
        }

        [System.Serializable]
        public class StartingItem
        {
            public ItemData item;
            public int amount = 1;
        }
    }
    [System.Serializable]
    public class InventoryItemSaveData
    {
        public string itemId;
        public int amount;
        public bool isQuick;
        public int index;
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public List<InventoryItemSaveData> items = new();
        public int selectedQuickIndex;
    }
}
