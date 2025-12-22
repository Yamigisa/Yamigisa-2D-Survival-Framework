using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class TextTooltip : MonoBehaviour
    {
        [Header("Interactive Object")]
        [SerializeField] private GameObject interactiveObjectTextGameObject;
        [SerializeField] private List<Text> interactiveObjectTexts;

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
            interactiveObjectTextGameObject.SetActive(false);
            foreach (Text text in interactiveObjectTexts)
            {
                text.gameObject.SetActive(false);
            }
        }

        public void ShowInteractiveObjectText(List<ActionBase> action)
        {
            interactiveObjectTextGameObject.SetActive(true);
            for (int i = 0; i < interactiveObjectTexts.Count; i++)
            {
                if (i < action.Count)
                {
                    interactiveObjectTexts[i].gameObject.SetActive(true);
                    interactiveObjectTexts[i].text = action[i].title;
                }
            }
        }

        public void CloseInteractiveObjectTexts()
        {
            foreach (Text text in interactiveObjectTexts)
            {
                text.gameObject.SetActive(false);
            }

            interactiveObjectTextGameObject.SetActive(false);
        }
    }
}