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

        [Header("Screen Offset")]
        [SerializeField] private Vector3 screenOffset = new Vector3(0f, 50f, 0f);

        public static TextTooltip Instance { get; private set; }

        private Transform currentTarget;
        private Camera mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            mainCamera = Camera.main;
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
            if (currentTarget == null) return;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(currentTarget.position);

            if (screenPos.z < 0f)
            {
                InteractiveObjectTextGameObject.SetActive(false);
                return;
            }

            InteractiveObjectTextGameObject.SetActive(true);
            InteractiveObjectTextGameObject.transform.position = screenPos + screenOffset;
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

                    // Use GetActionName instead of raw title (more flexible)
                    string actionName = action.GetActionName(interactiveObject);

                    InteractiveObjectTexts[i].text =
                        inputText + " to " +
                        actionName + " " +
                        interactiveObject.name;

                    // 🔥 CHECK CanDoAction()
                    bool canDo = action.CanDoAction(interactiveObject);

                    InteractiveObjectTexts[i].color = canDo ? normalColor : disabledColor;
                }
                else
                {
                    InteractiveObjectTexts[i].gameObject.SetActive(false);
                }
            }
        }

        private string GetReadableBindingName(InputBinding binding)
        {
            // If controller is connected, prefer showing gamepad button
            if (Character.instance.characterControls.gamepad != null && binding.gamepadButtons.Count > 0)
            {
                return binding.gamepadButtons[0].ToString();
            }

            // Otherwise show keyboard
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
