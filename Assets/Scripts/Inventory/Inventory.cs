using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class Inventory : MonoBehaviour
    {
        [Header("Item Prefab")]
        public int maxItems = 20;

        [Header("Item Prefab")]
        [SerializeField] private InventoryItem inventoryItemPrefab;

        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryPanel;
        public GameObject InventoryPanel => inventoryPanel;

        [Header("Tooltip Panel")]
        [SerializeField] private bool showTooltipPanel = true;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        private List<InventoryItemData> inventoryItems = new();

        private CharacterControls controls;
        private CharacterAttribute characterAttribute;
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

        private void OnEnable()
        {
            closeButton.onClick.AddListener(HideInventory);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(HideInventory);
        }

        private void Start()
        {
            controls = FindObjectOfType<CharacterControls>();
            characterAttribute = FindObjectOfType<CharacterAttribute>();
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
                    null, // if canvas is Overlay; replace with canvas.worldCamera if Screen Space - Camera
                    out pos);

                // offset upwards instead of downwards
                tooltipPanel.GetComponent<RectTransform>().anchoredPosition = pos + new Vector2(0f, 30f);
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

        public bool InventoryFull()
        {
            return inventoryItems.Count >= maxItems;
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

        public void ShowTooltip(InventoryItemData itemData)
        {
            if (showTooltipPanel)
            {
                tooltipPanel.SetActive(true);
                itemNameText.text = itemData.itemData.itemName;
                itemDescriptionText.text = itemData.itemData.description;
                //itemIcon.sprite = itemData.itemData.iconInventory;
            }
        }

        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
            itemNameText.text = "";
            itemDescriptionText.text = "";
            //itemIcon.sprite = null;
        }

        public void UseItem(ItemData data)
        {
            data.ApplyEffect(characterAttribute);
        }
    }
}