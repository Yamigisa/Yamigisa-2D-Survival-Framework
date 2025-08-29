using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        private List<InventoryItemData> inventoryItems = new();

        private CharacterControls controls;
        private CharacterAttribute characterAttribute;
        public static Inventory Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            controls = FindObjectOfType<CharacterControls>();
            characterAttribute = FindObjectOfType<CharacterAttribute>();

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
                quickInventoryItemSlots.Add(quickSlot);
            }
        }

        private void Update()
        {
            if (controls == null) return;

            if (controls.IsAnyKeyPressedDown(controls.inventoryKey))
            {
                if (inventoryPanel.activeSelf)
                    HideInventory();
                else
                    ShowInventory();
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
                if (i < 9 && Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    UseQuickSlot(i);
                }
                else if (i == 9 && Input.GetKeyDown(KeyCode.Alpha0))
                {
                    UseQuickSlot(i);
                }
            }
        }

        public void ShowInventory()
        {
            inventoryPanel.SetActive(true);
        }

        public void HideInventory()
        {
            inventoryPanel.SetActive(false);
        }

        public void AddItem(ItemData data, int amountToAdd = 1)
        {
            if (InventoryFull() && QuickInventoryFull())
                return;

            if (data.isStackable)
            {
                InventoryItem existingQuick = quickInventoryItemSlots.Find(i => i.HasItem && i.ItemInstance.itemData == data);
                if (existingQuick != null)
                {
                    existingQuick.ItemInstance.amount += amountToAdd;
                    existingQuick.Initialize(existingQuick.ItemInstance);
                    return;
                }

                InventoryItem existingMain = inventoryItemSlots.Find(i => i.HasItem && i.ItemInstance.itemData == data);
                if (existingMain != null)
                {
                    existingMain.ItemInstance.amount += amountToAdd;
                    existingMain.Initialize(existingMain.ItemInstance);
                    return;
                }

                InventoryItemData newItem = new InventoryItemData
                {
                    itemData = data,
                    amount = amountToAdd
                };

                if (!QuickInventoryFull())
                    AddItemToQuickInventory(newItem);
                else
                    AddItemToMainInventory(newItem);
            }
            else
            {
                for (int i = 0; i < amountToAdd; i++)
                {
                    InventoryItemData newItem = new InventoryItemData
                    {
                        itemData = data,
                        amount = 1
                    };

                    if (!QuickInventoryFull())
                        AddItemToQuickInventory(newItem);
                    else if (!InventoryFull())
                        AddItemToMainInventory(newItem);
                }
            }
        }

        private void AddItemToMainInventory(InventoryItemData itemData)
        {
            for (int i = 0; i < inventoryItemSlots.Count; i++)
            {
                InventoryItem slot = inventoryItemSlots[i];
                if (!slot.HasItem)
                {
                    slot.Initialize(itemData);
                    return;
                }
            }
        }

        public void RemoveItem(InventoryItem itemSlot)
        {
            if (quickInventoryItemSlots.Contains(itemSlot))
            {
                quickInventoryItemSlots.Remove(itemSlot);
                return;
            }

            if (inventoryItemSlots.Contains(itemSlot))
            {
                inventoryItemSlots.Remove(itemSlot);
                return;
            }
        }


        private void UseQuickSlot(int index)
        {
            if (index < 0 || index >= quickInventoryItemSlots.Count) return;

            InventoryItem quickSlot = quickInventoryItemSlots[index];

            if (quickSlot != null && quickSlot.HasItem)
            {
                ItemData itemData = quickSlot.ItemInstance.itemData;

                UseItem(itemData);

                RemoveItem(quickSlot);
            }
        }

        public void AddItemToQuickInventory(InventoryItemData itemData)
        {
            for (int i = 0; i < quickInventoryItemSlots.Count; i++)
            {
                InventoryItem quickSlot = quickInventoryItemSlots[i];

                if (!quickSlot.HasItem)
                {
                    quickSlot.Initialize(itemData);
                    return;
                }
            }
        }

        public bool InventoryFull()
        {
            for (int i = 0; i < inventoryItemSlots.Count; i++)
            {
                if (!inventoryItemSlots[i].HasItem)
                {
                    return false;
                }
            }
            return true;
        }

        private bool QuickInventoryFull()
        {
            for (int i = 0; i < quickInventoryItemSlots.Count; i++)
            {
                InventoryItem quickSlot = quickInventoryItemSlots[i];

                if (!quickSlot.HasItem)
                {
                    return false;
                }
            }
            return true;
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

        public void UseItem(ItemData data)
        {
            data.ApplyEffect(characterAttribute);
        }
    }
}