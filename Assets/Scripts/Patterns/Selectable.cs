using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(Collider2D))]
    public class Selectable : MonoBehaviour
    {
        [Header("Actions")]
        [SerializeField] private List<ActionBase> actions = new();
        public IReadOnlyList<ActionBase> Actions => actions;

        [Header("Item Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private ItemData itemData;
        [SerializeField, Min(1)] private int amount = 1;

        [Header("Outline")]
        [SerializeField] private GameObject outlineObject;

        [Header("Auto Hide")]
        [SerializeField] private float autoHideDistance = 4f;

        private static Selectable currentOpen;

        private Character character;
        private Camera cam; // used only for world picking (not UI)
        private bool panelVisible;
        private Collider2D col2D;

        public ItemData ItemData => itemData;
        public int Amount => amount;

        void Awake()
        {
            if (!character) character = FindObjectOfType<Character>();
            cam = Camera.main;
            col2D = GetComponent<Collider2D>();
            if (spriteRenderer) spriteRenderer.sprite = itemData ? itemData.iconWorld : null;
        }

        void Start()
        {
            SetOutline(false);
            ShowSelectableButtons(false);
        }

        void Update()
        {
            if (!panelVisible) return;

            if (character && autoHideDistance > 0f)
            {
                float d = Vector2.Distance(character.transform.position, transform.position);
                if (d > autoHideDistance)
                {
                    ShowSelectableButtons(false);
                    return;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (PointerOverOurPanel())
                    return;

                Ray ray = cam ? cam.ScreenPointToRay(Input.mousePosition) : new Ray();
                RaycastHit2D hit = cam ? Physics2D.GetRayIntersection(ray) : default;
                var clickedSelectable = hit.collider ? hit.collider.GetComponentInParent<Selectable>() : null;
                if (clickedSelectable != this)
                {
                    ShowSelectableButtons(false);
                }
            }
        }

        void OnMouseEnter() => SetOutline(true);
        void OnMouseExit() => SetOutline(false);

        private void OnMouseDown()
        {
            // If this selectable is already open, toggle it closed
            if (panelVisible)
            {
                ShowSelectableButtons(false);
            }
            else
            {
                ShowSelectableButtons(true);
            }
        }

        public void SetOutline(bool on)
        {
            if (outlineObject) outlineObject.SetActive(on);
        }

        public void ShowSelectableButtons(bool show)
        {
            if (show)
            {
                if (currentOpen && currentOpen != this)
                    currentOpen.ShowSelectableButtons(false);

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
            ShowSelectableButtons(false);
        }

        public ItemData GetItemData() => itemData;

        // ONLY used to get a geometric anchor; no camera/UI logic here
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

            // Overlay => null camera
            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
        }
    }
}
