using UnityEngine;

namespace Yamigisa
{
    public class ButtonActionsUI : MonoBehaviour
    {
        [SerializeField] private ButtonSelectable buttonSelectablePrefab;
        public Transform buttonTransform;

        [Header("Overlay Positioning (pixels)")]
        [SerializeField] private float offsetX = 0f;   // new: horizontal adjust
        [SerializeField] private float offsetY = 16f;  // vertical adjust
        [SerializeField] private float screenClampPadding = 8f;

        [Header("Scene Camera (for world->screen)")]
        [SerializeField] private Camera sceneCamera; // fallback to Camera.main if null

        public static ButtonActionsUI Instance { get; private set; }

        Canvas canvas;
        RectTransform rt;
        RectTransform parentRT;

        void Awake()
        {
            Instance = this;

            rt = buttonTransform as RectTransform;
            canvas = rt ? rt.GetComponentInParent<Canvas>() : null;

            if (!rt || !canvas)
            {
                Debug.LogError("[ButtonActionsUI] buttonTransform must be a RectTransform under a Canvas.");
                return;
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning("[ButtonActionsUI] Canvas is not ScreenSpace-Overlay. This script assumes OVERLAY.");
            }

            parentRT = rt.parent as RectTransform;

            // Normalized anchors/pivot for stable anchored positioning
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f); // bottom-center -> grows upward

            buttonTransform.gameObject.SetActive(false);

            if (!sceneCamera) sceneCamera = Camera.main;
        }

        public void InitializeButton(Selectable selectable)
        {
            if (!rt || !canvas || selectable == null) return;

            // Clear children
            for (int i = buttonTransform.childCount - 1; i >= 0; i--)
                Destroy(buttonTransform.GetChild(i).gameObject);

            // Build buttons
            foreach (ActionBase action in selectable.Actions)
            {
                ButtonSelectable selectableButton = Instantiate(buttonSelectablePrefab, buttonTransform);
                selectableButton.SetText(action.title);
                selectableButton.Button.onClick.AddListener(() => selectable.PerformAction(action));
            }

            buttonTransform.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();

            PositionAboveSelectable_Overlay(selectable);
        }

        public void HideButtonActions()
        {
            if (buttonTransform) buttonTransform.gameObject.SetActive(false);
        }

        void PositionAboveSelectable_Overlay(Selectable selectable)
        {
            // 1) Get the object's top world point
            Vector3 worldTop = selectable.GetTopWorldPoint();

            // 2) Convert to screen pixels
            Vector3 sp = sceneCamera ? sceneCamera.WorldToScreenPoint(worldTop)
                                     : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            // 3) Apply offsets
            sp.x += offsetX;
            sp.y += offsetY;

            // 4) Convert screen -> local anchored (Overlay => null camera)
            if (!parentRT) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, sp, null, out Vector2 local);
            rt.anchoredPosition = local;

            // 5) Clamp to screen
            ClampInsideScreen_Overlay();
        }

        void ClampInsideScreen_Overlay()
        {
            Vector3[] wc = new Vector3[4];
            rt.GetWorldCorners(wc);

            Vector3[] sc = new Vector3[4];
            for (int i = 0; i < 4; i++)
                sc[i] = RectTransformUtility.WorldToScreenPoint(null, wc[i]);

            float minX = sc[0].x, minY = sc[0].y, maxX = sc[0].x, maxY = sc[0].y;
            for (int i = 1; i < 4; i++)
            {
                if (sc[i].x < minX) minX = sc[i].x;
                if (sc[i].y < minY) minY = sc[i].y;
                if (sc[i].x > maxX) maxX = sc[i].x;
                if (sc[i].y > maxY) maxY = sc[i].y;
            }

            float pad = screenClampPadding;
            float dx = 0f, dy = 0f;

            if (minX < pad) dx += pad - minX;
            if (maxX > Screen.width - pad) dx -= maxX - (Screen.width - pad);
            if (minY < pad) dy += pad - minY;
            if (maxY > Screen.height - pad) dy -= maxY - (Screen.height - pad);

            if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f)) return;

            Vector3 pivotScreen = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            Vector3 targetScreen = new Vector3(pivotScreen.x + dx, pivotScreen.y + dy, 0f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, targetScreen, null, out Vector2 local);
            rt.anchoredPosition = local;
        }
    }
}
