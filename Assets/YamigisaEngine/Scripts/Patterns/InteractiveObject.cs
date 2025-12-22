using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yamigisa
{
    [RequireComponent(typeof(Collider2D))]
    public class InteractiveObject : MonoBehaviour
    {
        [Header("Actions")]
        private List<ActionBase> actions = new();
        public List<ActionBase> Actions => actions;

        [Header("Item Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private ItemData itemData;
        [SerializeField, Min(1)] private int amount = 1;

        [Header("Outline")]
        [SerializeField] private GameObject outlineObject;

        [Header("Auto Hide")]
        [SerializeField] private float autoHideDistance = 4f;

        private static InteractiveObject currentOpen;

        private Character character;
        private Camera cam; 
        private bool panelVisible;
        private Collider2D col2D;

        // runtime destructible state
        private int currentHP;
        private bool isDestructibleInstance;

        public ItemData ItemData => itemData;
        public int Amount => amount;

        void Awake()
        {
            if (!character) character = FindObjectOfType<Character>();
            cam = Camera.main;
            col2D = GetComponent<Collider2D>();

            if (spriteRenderer)
                spriteRenderer.sprite = itemData ? itemData.iconWorld : null;

            if (ItemData != null && ItemData.itemActions != null)
                actions.AddRange(ItemData.itemActions);

            isDestructibleInstance =
                (ItemData != null &&
                 ItemData.itemType == ItemType.Resource &&
                 ItemData.destructible);

            if (isDestructibleInstance)
                currentHP = Mathf.Max(1, ItemData.destructibleHP);
        }

        void Start()
        {
            SetOutline(false);
            ShowInteractiveObjectButtons(false);
        }

        void Update()
        {
            if (!panelVisible) return;

            if (character && autoHideDistance > 0f)
            {
                float d = Vector2.Distance(character.transform.position, transform.position);
                if (d > autoHideDistance)
                {
                    ShowInteractiveObjectButtons(false);
                    return;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverAnyUI()) return;
                if (PointerOverOurPanel()) return;

                Ray ray = cam ? cam.ScreenPointToRay(Input.mousePosition) : new Ray();
                RaycastHit2D hit = cam ? Physics2D.GetRayIntersection(ray) : default;
                var clickedInteractiveObject = hit.collider ? hit.collider.GetComponentInParent<InteractiveObject>() : null;
                if (clickedInteractiveObject != this)
                {
                    ShowInteractiveObjectButtons(false);
                }
            }
        }

        void OnMouseEnter() => SetOutline(true);
        void OnMouseExit() => SetOutline(false);

        private void OnMouseDown()
        {
            if (IsPointerOverAnyUI()) return;

            if (panelVisible) ShowInteractiveObjectButtons(false);
            else ShowInteractiveObjectButtons(true);
        }

        public void SetOutline(bool on)
        {
            if (outlineObject) outlineObject.SetActive(on);
        }

        public void ShowInteractiveObjectButtons(bool show)
        {
            if (show)
            {
                if (currentOpen && currentOpen != this)
                    currentOpen.ShowInteractiveObjectButtons(false);

                if (ButtonActionsUI.Instance != null)
                    ButtonActionsUI.Instance.InitializeButton(this);

                panelVisible = true;
                currentOpen = this;
                SetOutline(true);
            }
            else
            {
                if (ButtonActionsUI.Instance != null)
                    ButtonActionsUI.Instance.HideButtonActions();

                panelVisible = false;
                if (currentOpen == this) currentOpen = null;
            }
        }

        public void PerformAction(ActionBase action)
        {
            if (action == null) return;
            if (!action.CanDoAction(this)) return;

            action.DoAction(character, this);
            ShowInteractiveObjectButtons(false);
        }

        public ItemData GetItemData() => itemData;

        public Vector3 GetTopWorldPoint()
        {
            float topY = transform.position.y;

            if (spriteRenderer && spriteRenderer.sprite)
                topY = Mathf.Max(topY, spriteRenderer.bounds.max.y);
            if (col2D)
                topY = Mathf.Max(topY, col2D.bounds.max.y);

            const float worldLift = 0.1f;
            return new Vector3(transform.position.x, topY + worldLift, transform.position.z);
        }

        private bool PointerOverOurPanel()
        {
            if (ButtonActionsUI.Instance == null || ButtonActionsUI.Instance.buttonTransform == null)
                return false;

            var rt = ButtonActionsUI.Instance.buttonTransform as RectTransform;
            if (!rt) return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
        }

        private static bool IsPointerOverAnyUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // ===== Destructible handling =====

        public void TakeDamage(int damage)
        {
            if (!isDestructibleInstance) return;
            if (damage <= 0) return;

            if (!HasAnyRequiredGroup())
                return;

            currentHP -= damage;
            if (currentHP <= 0) Kill();
        }

        private bool HasAnyRequiredGroup()
        {
            var req = ItemData.destructibleRequiredGroups;
            if (req == null || req.Count == 0) return false; // MUST: list cannot be empty

            if (Inventory.Instance == null) return false;

            // Require: player has at least one of the required groups
            // Adjust this if your design needs "ALL groups" instead.
            for (int i = 0; i < req.Count; i++)
            {
                var g = req[i];
                if (g != null && Inventory.Instance.HasGroup(g))
                    return true;
            }
            return false;
        }

        private void Kill()
        {
            DropLoot();
            Destroy(gameObject);
        }

        private void DropLoot()
        {
            if (ItemData == null || ItemData.destructibleLoots == null || ItemData.destructibleLoots.Count == 0) return;
            if (Inventory.Instance == null) return;

            // Each LootEntry has: item, amount, dropChancePercent
            for (int i = 0; i < ItemData.destructibleLoots.Count; i++)
            {
                var entry = ItemData.destructibleLoots[i];
                if (entry == null || entry.item == null) continue;

                // Roll chance (0..100)
                float roll = Random.value * 100f;
                if (roll <= Mathf.Clamp(entry.dropChancePercent, 0f, 100f))
                {
                    int amt = Mathf.Max(1, entry.amount);
                    Inventory.Instance.AddItem(entry.item, amt);
                }
            }
        }

    }
}
