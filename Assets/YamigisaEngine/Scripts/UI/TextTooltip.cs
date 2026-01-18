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

            for (int i = 0; i < InteractiveObjectTexts.Count; i++)
            {
                if (i < interactiveObject.Actions.Count)
                {
                    InteractiveObjectTexts[i].gameObject.SetActive(true);
                    InteractiveObjectTexts[i].text =
                        interactiveObject.Actions[i].title + " " + interactiveObject.name;
                }
                else
                {
                    InteractiveObjectTexts[i].gameObject.SetActive(false);
                }
            }
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
