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

        public static TextTooltip Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InteractiveObjectTextGameObject.SetActive(false);
            foreach (Text text in InteractiveObjectTexts)
            {
                text.gameObject.SetActive(false);
            }
        }

        public void ShowInteractiveObjectText(InteractiveObject interactiveObject)
        {
            if (Character.instance.IsBusy)
                return;

            InteractiveObjectTextGameObject.SetActive(true);
            for (int i = 0; i < interactiveObject.Actions.Count; i++)
            {
                if (i < interactiveObject.Actions.Count)
                {
                    InteractiveObjectTexts[i].gameObject.SetActive(true);
                    InteractiveObjectTexts[i].text = interactiveObject.Actions[i].title + " " + interactiveObject.name;
                }
            }
        }

        public void CloseInteractiveObjectTexts()
        {
            foreach (Text text in InteractiveObjectTexts)
            {
                text.gameObject.SetActive(false);
            }

            InteractiveObjectTextGameObject.SetActive(false);
        }
    }
}