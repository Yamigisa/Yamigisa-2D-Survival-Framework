using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    [RequireComponent(typeof(Collider2D))]
    public class Selectable : MonoBehaviour
    {
        [Header("Actions")]
        [SerializeField] private List<ActionBase> actions = new();
        public IReadOnlyList<ActionBase> Actions => actions;

        [Header("Action Buttons")]
        [SerializeField] private ButtonSelectable buttonSelectablePrefab;
        [SerializeField] private Transform buttonTransform;

        [Header("Item Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private ItemData itemData;
        [SerializeField][Min(1)] private int amount = 1;

        [Header("Outline")]
        [SerializeField] private GameObject outlineObject;

        [Header("Auto Hide")]
        [SerializeField] private float autoHideDistance = 4f;

        private static Selectable currentOpen;

        private Character character;
        private Camera cam;
        private bool panelVisible;

        public ItemData ItemData => itemData;
        public int Amount => amount;

        private void Awake()
        {
            if (character == null) character = FindObjectOfType<Character>();
            cam = Camera.main;
            spriteRenderer.sprite = itemData ? itemData.iconWorld : null;
        }

        private void Start()
        {
            SetOutline(false);
            InitializeButton();
            ShowSelectableButtons(false);
        }

        private void Update()
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

                var ray = cam ? cam.ScreenPointToRay(Input.mousePosition) : new Ray();
                RaycastHit2D hit = cam ? Physics2D.GetRayIntersection(ray) : default;

                var clickedSelectable = hit.collider ? hit.collider.GetComponentInParent<Selectable>() : null;
                if (clickedSelectable != this)
                {
                    ShowSelectableButtons(false);
                }
            }
        }

        private void OnMouseEnter() => SetOutline(true);
        private void OnMouseExit() => SetOutline(false);

        private void OnMouseDown()
        {
            ShowSelectableButtons(true);
        }

        public void SetOutline(bool on)
        {
            if (outlineObject) outlineObject.SetActive(on);
        }

        public void InitializeButton()
        {
            if (!buttonSelectablePrefab || !buttonTransform) return;

            for (int i = buttonTransform.childCount - 1; i >= 0; i--)
                Destroy(buttonTransform.GetChild(i).gameObject);

            foreach (ActionBase action in actions)
            {
                if (action == null) continue;

                ButtonSelectable btn = Instantiate(buttonSelectablePrefab, buttonTransform);
                btn.SetText(action.title);
                btn.Button.onClick.AddListener(() => PerformAction(action));
            }
        }

        public void ShowSelectableButtons(bool show)
        {
            if (!buttonTransform) return;

            if (show)
            {
                if (currentOpen && currentOpen != this)
                    currentOpen.ShowSelectableButtons(false);

                buttonTransform.gameObject.SetActive(true);
                panelVisible = true;
                currentOpen = this;
                SetOutline(true);
            }
            else
            {
                buttonTransform.gameObject.SetActive(false);
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


        public ItemData GetItemData()
        {
            return itemData;
        }

        private bool PointerOverOurPanel()
        {
            if (!buttonTransform) return false;
            var rt = buttonTransform as RectTransform;
            if (!rt) return false;

            var canvas = rt.GetComponentInParent<Canvas>();
            var eventCam = canvas ? canvas.worldCamera : cam;

            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, eventCam);
        }
    }
}
