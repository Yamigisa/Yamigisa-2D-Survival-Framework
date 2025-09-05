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

        [Header("Action Buttons")]
        [SerializeField] private ButtonSelectable buttonSelectablePrefab;
        [SerializeField] private Transform buttonTransform;
        [SerializeField] private float screenClampPadding = 8f;

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

                // NEW: ensure the panel is inside the camera view when it appears
                ClampButtonsToCamera();
            }
            else
            {
                buttonTransform.gameObject.SetActive(false);
                panelVisible = false;
                if (currentOpen == this) currentOpen = null;
            }
        }

        private void ClampButtonsToCamera()
        {
            if (!buttonTransform) return;

            RectTransform rt = buttonTransform as RectTransform;
            if (!rt) return;

            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Camera eventCam = null;

            // For World Space / Screen Space - Camera, use the canvas camera if set; otherwise fall back to scene camera.
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCam = canvas.worldCamera != null ? canvas.worldCamera : cam;
            else
                eventCam = cam;

            if (eventCam == null) return;

            // Get panel corners in screen space
            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector3 sp = eventCam.WorldToScreenPoint(worldCorners[i]);
                if (sp.x < minX) minX = sp.x;
                if (sp.y < minY) minY = sp.y;
                if (sp.x > maxX) maxX = sp.x;
                if (sp.y > maxY) maxY = sp.y;
            }

            float pad = screenClampPadding;
            float screenW = Screen.width;
            float screenH = Screen.height;

            float dx = 0f;
            float dy = 0f;

            if (minX < pad) dx += (pad - minX);
            if (maxX > screenW - pad) dx -= (maxX - (screenW - pad));
            if (minY < pad) dy += (pad - minY);
            if (maxY > screenH - pad) dy -= (maxY - (screenH - pad));

            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f)) return;

            // Move the panel by adjusting its pivot screen position, then convert back to world
            Vector3 pivotScreen = eventCam.WorldToScreenPoint(rt.position);
            Vector3 targetScreen = new Vector3(pivotScreen.x + dx, pivotScreen.y + dy, pivotScreen.z);

            Vector3 world;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, targetScreen, eventCam, out world))
            {
                rt.position = world;
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
