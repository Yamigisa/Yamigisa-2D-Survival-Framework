using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class CraftingInterface : MonoBehaviour
    {
        [Header("Build From Resources")]
        [Tooltip("Optional subfolder inside a Resources/… path. Leave blank to scan all Resources.")]
        [SerializeField] private string resourcesSubfolder = "";

        [Header("UI")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private CraftSlot slotPrefab;

        private Inventory inventory;

        private CharacterControls controls;
        private readonly List<CraftSlot> slots = new List<CraftSlot>();

        private void OnEnable()
        {
            inventory = Inventory.Instance != null ? Inventory.Instance : FindObjectOfType<Inventory>();

            // Subscribe first so we catch the very first NotifyChanged after start
            Inventory.OnChanged += RefreshAll;

            Rebuild();     // build slots
            RefreshAll();  // immediate pull (in case inventory already has items)
        }

        private void OnDisable()
        {
            Inventory.OnChanged -= RefreshAll;
        }

        private void Awake()
        {
            if (controls == null) controls = FindObjectOfType<CharacterControls>();
        }

        void Update()
        {
            if (controls == null) return;

            if (controls.IsAnyKeyPressedDown(controls.craftingKey))
            {
                ToggleCraftingInterface();
            }
        }

        public void Rebuild()
        {
            if (contentRoot == null || slotPrefab == null) return;

            // Clear
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
            slots.Clear();

            // Load all craftable outputs from Resources
            ItemData[] all = Resources.LoadAll<ItemData>(resourcesSubfolder);
            for (int i = 0; i < all.Length; i++)
            {
                ItemData item = all[i];
                if (item == null) continue;

                bool hasReqs =
                    (item.craftItemsNeeded != null && item.craftItemsNeeded.Count > 0) ||
                    (item.craftGroupsNeeded != null && item.craftGroupsNeeded.Count > 0);

                if (!hasReqs) continue;

                CraftSlot slot = Instantiate(slotPrefab, contentRoot);
                slot.Init(item, inventory);
                slots.Add(slot);
            }
        }

        private void ToggleCraftingInterface()
        {
            bool isActive = contentRoot != null && contentRoot.gameObject.activeSelf;
            contentRoot.gameObject.SetActive(!isActive);
            Rebuild();
        }

        private void RefreshAll()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                CraftSlot s = slots[i];
                if (s != null) s.Refresh();
            }
        }
    }
}
