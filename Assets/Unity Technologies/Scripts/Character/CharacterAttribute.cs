using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [ExecuteAlways]
    public class CharacterAttribute : MonoBehaviour
    {
        [SerializeField] private List<AttributeData> AttributeData;
        private AttributeUI attributeUI;

        void Start()
        {
            attributeUI = FindObjectOfType<AttributeUI>();

            foreach (var a in AttributeData)
                attributeUI.InitializeAttributeBar(a);

            if (TimeClock.Instance != null)
            {
                TimeClock.Instance.OnMinuteChanged += SetDepletingAttributes;
                TimeClock.Instance.OnMinuteChanged += SetRegeneratingAttributes;
            }
        }

        void OnDisable()
        {
            if (Application.isPlaying && TimeClock.Instance != null)
            {
                TimeClock.Instance.OnMinuteChanged -= SetDepletingAttributes;
                TimeClock.Instance.OnMinuteChanged -= SetRegeneratingAttributes;
            }
        }

        private void SetDepletingAttributes()
        {
            foreach (var a in AttributeData)
            {
                a.CurrentValue += a.DepleteValuePerMinute;
                if (a.CurrentValue < 0) a.CurrentValue = 0;
                AttributeBar bar = attributeUI.GetAttributeBar(a);
                bar.SetCurrentValue(a.CurrentValue);
            }
        }

        private void SetRegeneratingAttributes()
        {
            foreach (var a in AttributeData)
            {
                if (a.CurrentValue >= a.MaxValue) return;
                a.CurrentValue += a.RegenerateValuePerMinute;
                AttributeBar bar = attributeUI.GetAttributeBar(a);
                bar.SetCurrentValue(a.CurrentValue);
            }
        }

        public void AddMaxAttributeValue(AttributeType type, float value)
        {
            foreach (var a in AttributeData)
            {
                if (a.type == type)
                {
                    a.MaxValue += value;
                    attributeUI.GetAttributeBar(a).SetMaxValue(a.MaxValue);
                }
            }
        }

        public void AddCurrentAttributeValue(AttributeType type, float value)
        {
            foreach (var a in AttributeData)
            {
                if (a.type == type)
                {
                    a.CurrentValue += value;
                    if (a.CurrentValue > a.MaxValue) a.CurrentValue = a.MaxValue;
                    attributeUI.GetAttributeBar(a).SetCurrentValue(a.CurrentValue);
                }
            }
        }
    }
}
