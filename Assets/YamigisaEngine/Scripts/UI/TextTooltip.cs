using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class TextTooltip : MonoBehaviour
    {
        [Header("Interactive Object")]
        [SerializeField] private GameObject InteractiveObjectTextGameObject;
        [SerializeField] private List<Text> InteractiveObjectTexts;

        [Header("Text Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.4f);

        [Header("Position")]
        [SerializeField] private float topMargin = 16f;
        [SerializeField] private Vector3 extraScreenOffset = Vector3.zero;

        public static TextTooltip Instance { get; private set; }

        private Transform currentTarget;
        private Camera mainCamera;
        private RectTransform tooltipRect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            mainCamera = Camera.main;

            if (InteractiveObjectTextGameObject != null)
                tooltipRect = InteractiveObjectTextGameObject.GetComponent<RectTransform>();
        }

        private void Start()
        {
            InteractiveObjectTextGameObject.SetActive(false);

            foreach (Text text in InteractiveObjectTexts)
            {
                text.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (currentTarget == null)
                return;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (tooltipRect == null && InteractiveObjectTextGameObject != null)
                tooltipRect = InteractiveObjectTextGameObject.GetComponent<RectTransform>();

            Vector3 worldPos = currentTarget.position;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0f)
            {
                InteractiveObjectTextGameObject.SetActive(false);
                return;
            }

            InteractiveObjectTextGameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            float tooltipHeight = tooltipRect != null ? tooltipRect.rect.height : 0f;

            // geser tooltip ke atas sebesar setengah tinggi panel + margin
            Vector3 finalPos = screenPos
                + new Vector3(0f, tooltipHeight * 0.5f + topMargin, 0f)
                + extraScreenOffset;

            InteractiveObjectTextGameObject.transform.position = finalPos;
        }

        public void ShowInteractiveObjectText(InteractiveObject interactiveObject)
        {
            if (Character.instance.IsBusy)
                return;

            currentTarget = interactiveObject.transform;

            InteractiveObjectTextGameObject.SetActive(true);

            CharacterControls controls = Character.instance.characterControls;

            for (int i = 0; i < InteractiveObjectTexts.Count; i++)
            {
                if (i < interactiveObject.Actions.Count && i < 4)
                {
                    InteractiveObjectTexts[i].gameObject.SetActive(true);

                    string inputText = "";

                    switch (i)
                    {
                        case 0:
                            inputText = GetReadableBindingName(controls.interaction1);
                            break;

                        case 1:
                            inputText = GetReadableBindingName(controls.interaction2);
                            break;
                    }

                    ActionBase action = interactiveObject.Actions[i];
                    string actionName = action.GetActionName(interactiveObject);

                    InteractiveObjectTexts[i].text =
                        inputText + " to " +
                        actionName + " " +
                        interactiveObject.name;

                    bool canDo = action.CanDoAction(interactiveObject);
                    InteractiveObjectTexts[i].color = canDo ? normalColor : disabledColor;
                }
                else
                {
                    InteractiveObjectTexts[i].gameObject.SetActive(false);
                }
            }

            Canvas.ForceUpdateCanvases();

            if (tooltipRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }

        private string GetReadableBindingName(InputBinding binding)
        {
            if (Character.instance.characterControls.gamepad != null && binding.gamepadButtons.Count > 0)
            {
                return binding.gamepadButtons[0].ToString();
            }

            if (binding.keyboardKeys.Count > 0)
            {
                return GetReadableKeyName(binding.keyboardKeys[0]);
            }

            return "";
        }

        private string GetReadableKeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Mouse0: return "LMB";
                case KeyCode.Mouse1: return "RMB";
                case KeyCode.Mouse2: return "MMB";
            }

            return key.ToString();
        }

        public void CloseInteractiveObjectTexts()
        {
            currentTarget = null;

            foreach (Text text in InteractiveObjectTexts)
            {
                text.gameObject.SetActive(false);
            }

            InteractiveObjectTextGameObject.SetActive(false);
        }
    }
}