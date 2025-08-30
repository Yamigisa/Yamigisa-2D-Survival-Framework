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

        private List<InventoryItem> inventoryItemSlots = new();

        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryPanel;

        [Header("Quick Inventory")]
        [SerializeField] private int quickSlotCount = 8;
        [SerializeField] private Transform quickInventoryContent;
        private List<InventoryItem> quickInventoryItemSlots = new();

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

        public bool IsDragging => isDragging;
        public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        private CharacterControls controls;
        public Character Character;

        public static Inventory Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            controls = FindObjectOfType<CharacterControls>();
            Character = FindObjectOfType<Character>();

            if (rootCanvas == null) rootCanvas = inventoryPanel.GetComponentInParent<Canvas>();
            if (raycaster == null && rootCanvas != null) raycaster = rootCanvas.GetComponent<GraphicRaycaster>();

            inventoryItemSlots.Clear();
            for (int i = 0; i < maxItems; i++)
            {
                InventoryItem newSlot = Instantiate(inventoryItemPrefab, inventoryContent);
                inventoryItemSlots.Add(newSlot);
            }

            quickInventoryItemSlots.Clear();
            for (int i = 0; i < quickSlotCount; i++)
            {
                InventoryItem quickSlot = Instantiate(inventoryItemPrefab, quickInventoryContent);
                quickSlot.MarkAsQuickSlot(true);
                quickInventoryItemSlots.Add(quickSlot);
            }

            if (quickInventoryItemSlots.Count > 0)
            {
                selectedQuickIndex = 0;
                SelectQuickSlot(selectedQuickIndex);
                UpdateQuickIndicators();
            }
        }

        private void Update()
        {
            if (controls == null) return;

            if (!IsInventoryOpen)
            {
                CancelPendingPick();
                if (isDragging) CancelDrag();
                return;
            }

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
                    return;
                }
            }

            if (Input.GetMouseButtonUp(0)) CancelPendingPick();

            if (controls.IsAnyKeyPressedDown(controls.inventoryKey))
            {
                if (inventoryPanel.activeSelf) HideInventory();
                else ShowInventory();
            }

            if (tooltipPanel.activeSelf)
            {
                Vector2 pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    inventoryPanel.GetComponentInParent<Canvas>().transform as RectTransform,
                    Input.mousePosition,
                    null,
                    out pos);
                tooltipPanel.GetComponent<RectTransform>().anchoredPosition = pos + new Vector2(0f, 30f);
            }

            for (int i = 0; i < quickInventoryItemSlots.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    SelectQuickSlot(i);
            }

            if (controls.IsAnyKeyPressedDown(controls.useItemKey) && selectedQuickIndex >= 0)
                UseQuickSlot(selectedQuickIndex);
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

        public void ShowInventory() { inventoryPanel.SetActive(true); }
        public void HideInventory()
        {
            inventoryPanel.SetActive(false);
            CancelPendingPick();
            if (isDragging) CancelDrag();
        }

        public void AddItem(ItemData data, int amountToAdd = 1)
        {
            if (data == null || amountToAdd <= 0) return;

            if (data.isStackable && TryStackInList(quickInventoryItemSlots, data, amountToAdd)) { UpdateQuickIndicators(); return; }
            if (data.isStackable && TryStackInList(inventoryItemSlots, data, amountToAdd)) { UpdateQuickIndicators(); return; }

            if (TryPlaceInEmptySlot(quickInventoryItemSlots, data, amountToAdd)) { UpdateQuickIndicators(); return; }
            if (TryPlaceInEmptySlot(inventoryItemSlots, data, amountToAdd)) { UpdateQuickIndicators(); return; }
        }

        private bool TryStackInList(List<InventoryItem> list, ItemData data, int amountToAdd)
        {
            for (int i = 0; i < list.Count; i++)
            {
                InventoryItem slot = list[i];
                if (slot.HasItem && slot.ItemData == data && data.isStackable)
                {
                    if (data.maxAmount < slot.Amount + amountToAdd)
                    {
                        int space = data.maxAmount - slot.Amount;
                        slot.SetItem(data, slot.Amount + space);
                        amountToAdd -= space;
                        if (amountToAdd > 0) continue;
                        return true;
                    }
                    slot.SetItem(data, slot.Amount + amountToAdd);
                    return true;
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

        public void UseSlot(InventoryItem slot)
        {
            if (slot == null || slot.ItemData == null) return;

            Debug.Log("Using item: " + slot.ItemData.itemName);

            // Prefer actions defined on the ItemData
            var actions = slot.ItemData.itemActions;
            if (actions != null && actions.Count > 0)
            {
                foreach (var action in actions)
                {
                    if (action == null) continue;
                    action.DoItemAction(Character, slot);
                }
            }
            else
            {
                // Fallback if no actions configured
                if (Character != null)
                {
                    if (slot.ItemData.itemType == ItemType.Consumable)
                    {
                        Character.ConsumeItem(slot.ItemData);
                    }
                    else if (slot.ItemData.itemType == ItemType.Equipment)
                    {
                        Character.characterAttribute.AddMaxAttributeValue(AttributeType.Health, slot.ItemData.increaseMaxHealth);
                        Character.characterAttribute.AddMaxAttributeValue(AttributeType.Hunger, slot.ItemData.increaseMaxHunger);
                        Character.characterAttribute.AddMaxAttributeValue(AttributeType.Thirst, slot.ItemData.increaseMaxThirst);
                    }
                }
            }

            slot.Amount--;
            if (slot.Amount <= 0) slot.ResetSlot();
            else slot.SetItem(slot.ItemData, slot.Amount);

            UpdateQuickIndicators();
        }

        private void UseQuickSlot(int index)
        {
            if (index < 0 || index >= quickInventoryItemSlots.Count) return;
            var quickSlot = quickInventoryItemSlots[index];
            if (quickSlot.HasItem) UseSlot(quickSlot);
        }

        public void ShowTooltip(ItemData itemData)
        {
            if (showTooltipPanel && itemData != null)
            {
                tooltipPanel.SetActive(true);
                itemNameText.text = itemData.itemName;
                itemDescriptionText.text = itemData.description;
            }
        }

        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
            itemNameText.text = "";
            itemDescriptionText.text = "";
        }

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
            if (success) UpdateQuickIndicators();
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
            if (rootCanvas == null) rootCanvas = inventoryPanel.GetComponentInParent<Canvas>();
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
    }
}
