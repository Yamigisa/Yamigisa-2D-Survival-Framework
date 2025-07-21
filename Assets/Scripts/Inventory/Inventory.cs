using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class Inventory : MonoBehaviour
    {
        [Header("Item Prefab")]
        [SerializeField] private InventoryItem inventoryItemPrefab;

        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryPanel;
        public GameObject InventoryPanel => inventoryPanel;

        [Header("Description Panel")]
        [SerializeField] private bool showDescriptionPanel = true;
        [SerializeField] private GameObject descriptionPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;
        [SerializeField] private Image itemIcon;

        private List<InventoryItemData> inventoryItems = new();

        private CharacterControls controls;

        public static Action OnInventoryToggle;
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


            itemNameText.text = "";
            itemDescriptionText.text = "";
            itemIcon.sprite = null;
        }

        // private void Update()
        // {
        //     if (controls == null) return;

        //     controls.IsAnyKeyPressedDown(controls.inventoryKey);
        //     foreach (KeyCode key in controls.inventoryKey)
        //     {
        //         if (Input.GetKeyDown(key))
        //         {
        //             if (inventoryPanel.activeSelf)
        //                 HideInventory();
        //             else
        //                 ShowInventory();
        //             break;
        //         }
        //     }
        // }

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
            if (data.isStackable)
            {
                var existingItem = inventoryItems.Find(i => i.itemData == data);
                if (existingItem != null)
                {
                    existingItem.amount += amountToAdd;
                }
                else
                {
                    inventoryItems.Add(new InventoryItemData
                    {
                        itemData = data,
                        amount = amountToAdd
                    });
                }
            }
            else
            {
                for (int i = 0; i < amountToAdd; i++)
                {
                    inventoryItems.Add(new InventoryItemData
                    {
                        itemData = data,
                        amount = 1
                    });
                }
            }

            RefreshUI();
        }

        public void RemoveItem(InventoryItemData itemToRemove, int amountToRemove = 1)
        {
            if (itemToRemove == null || !inventoryItems.Contains(itemToRemove))
                return;

            if (itemToRemove.itemData.isStackable)
            {
                itemToRemove.amount -= amountToRemove;

                if (itemToRemove.amount <= 0)
                {
                    inventoryItems.Remove(itemToRemove);
                }
            }
            else
            {
                inventoryItems.Remove(itemToRemove);
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            foreach (Transform child in inventoryContent)
                Destroy(child.gameObject);

            foreach (var item in inventoryItems)
            {
                InventoryItem uiItem = Instantiate(inventoryItemPrefab, inventoryContent);
                uiItem.Initialize(item);
            }
        }

        public void ShowDescription(InventoryItemData itemData)
        {
            if (showDescriptionPanel)
            {
                descriptionPanel.SetActive(true);
                itemNameText.text = itemData.itemData.itemName;
                itemDescriptionText.text = itemData.itemData.description;
                itemIcon.sprite = itemData.itemData.iconInventory;
            }
        }

        public void HideDescription()
        {
            descriptionPanel.SetActive(false);
            itemNameText.text = "";
            itemDescriptionText.text = "";
            itemIcon.sprite = null;
        }
    }
}