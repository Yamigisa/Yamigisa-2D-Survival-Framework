using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Animations;

namespace Yamigisa
{
    public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Item UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Text amountText;
        [SerializeField] private Button slotButton;
        [SerializeField] private Transform buttonContainer;

        [Header("Slot Flags")]
        private bool isQuickSlot = false;

        [Header("Highlight")]
        [SerializeField] private Color selectedColor = Color.red;
        [SerializeField] private Color normalColor = Color.white;

        public ItemData ItemData;
        public int Amount;
        private bool hasItem;
        public bool HasItem => hasItem;

        private bool blockNextClick;

        private static ItemSlot currentlyOpenSlot;

        public Storage OwnerStorage { get; private set; }

        public void SetOwnerStorage(Storage storage)
        {
            OwnerStorage = storage;
        }

        private void Start()
        {
            if (!hasItem)
                amountText.text = "";

            slotButton.onClick.AddListener(ShowButton);
            buttonContainer.gameObject.SetActive(false);
        }

        public void MarkAsQuickSlot(bool value)
        {
            isQuickSlot = value;
        }

        public void SetSelectedVisual(bool selected)
        {
            if (slotButton == null || slotButton.image == null) return;

            slotButton.image.enabled = true;
            slotButton.image.color = selected ? selectedColor : normalColor;
        }

        public void SetItem(ItemData data, int _amount = 1)
        {
            ItemData = data;
            hasItem = true;

            icon.enabled = true;
            icon.sprite = data.iconInventory;
            icon.color = normalColor;

            if (!data.isStackable)
            {
                Amount = 1;
                amountText.text = "";
            }
            else
            {
                int cap = Mathf.Max(1, data.maxAmount);
                Amount = Mathf.Clamp(_amount, 1, cap);
                amountText.text = _amount > 1 ? $"{Amount}" : "";
            }

            int index = 0;

            for (int i = 0; i < buttonContainer.childCount; i++)
                buttonContainer.GetChild(i).gameObject.SetActive(false);

            if (data.itemActions == null) return;

            foreach (ActionBase action in data.itemActions)
            {
                InitializeAction(action, index);
                index++;
            }
        }

        private void InitializeAction(ActionBase action, int index)
        {
            if (action == null) return;
            if (index < 0 || index >= buttonContainer.childCount) return;

            Transform child = buttonContainer.GetChild(index);
            child.gameObject.SetActive(true);

            ButtonInteractiveObject interactiveButton = child.GetComponent<ButtonInteractiveObject>();
            if (interactiveButton == null) return;

            interactiveButton.SetText(action.title);

            // 🔥 CHECK CanDoAction
            bool canDo = action.CanDoAction(this);

            interactiveButton.Button.interactable = canDo;

            interactiveButton.Button.onClick.RemoveAllListeners();

            if (canDo)
            {
                interactiveButton.Button.onClick.AddListener(() =>
                {
                    action.DoAction(Character.instance.GetCharacter(), this);
                    HideButton();
                });
            }
        }

        public void ShowButton()
        {
            if (blockNextClick) return;
            if (ItemData == null) return;

            // STORAGE → INVENTORY
            if (OwnerStorage != null)
            {
                Inventory.Instance.AddItem(ItemData, Amount);
                OwnerStorage.RemoveStoredItem(ItemData, Amount);
                Inventory.Instance.ReduceSlotAmount(this, Amount);
                return;
            }

            // INVENTORY → STORAGE
            if (Inventory.Instance.currentStorage != null)
            {
                Inventory.Instance.AddItem(
                    ItemData,
                    Amount,
                    Inventory.Instance.currentStorage.inventoryStorage
                );

                Inventory.Instance.ReduceSlotAmount(this, Amount);
                return;
            }

            // NORMAL ITEM ACTION UI
            if (currentlyOpenSlot != null && currentlyOpenSlot != this)
                currentlyOpenSlot.HideButton();

            if (buttonContainer.gameObject.activeSelf)
            {
                HideButton();
                return;
            }

            buttonContainer.gameObject.SetActive(true);
            currentlyOpenSlot = this;

            int count = ItemData.itemActions != null
                ? ItemData.itemActions.Count
                : 0;

            for (int i = 0; i < buttonContainer.childCount; i++)
                buttonContainer.GetChild(i).gameObject.SetActive(i < count);
        }

        private void HideButton()
        {
            buttonContainer.gameObject.SetActive(false);

            if (currentlyOpenSlot == this)
                currentlyOpenSlot = null;
        }

        public void DropItem(Vector3 spawnPoint, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                Instantiate(ItemData.itemPrefab, spawnPoint, Quaternion.identity);
            }

            ResetSlot();
        }

        public void ResetSlot()
        {
            ItemData = null;
            Amount = 0;
            hasItem = false;

            icon.sprite = null;
            icon.enabled = false;
            icon.color = normalColor;

            amountText.text = "";

            if (slotButton && slotButton.image)
                slotButton.image.color = normalColor;

            for (int i = 0; i < buttonContainer.childCount; i++)
                buttonContainer.GetChild(i).gameObject.SetActive(false);

            HideButton();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ItemData == null) return;
            //   Inventory.Instance.ShowTooltip(ItemData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //Inventory.Instance.HideTooltip();
        }
    }
}
