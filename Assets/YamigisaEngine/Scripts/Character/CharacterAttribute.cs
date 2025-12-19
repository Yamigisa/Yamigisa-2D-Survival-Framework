using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class CharacterAttribute : MonoBehaviour
    {
        [SerializeField] private List<AttributeData> AttributeData;
        private AttributeUI attributeUI;

        void Start()
        {
            attributeUI = FindObjectOfType<AttributeUI>();
            if (attributeUI == null) return;

            foreach (AttributeData a in AttributeData)
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
            if (attributeUI == null) return;

            foreach (var a in AttributeData)
            {
                a.CurrentValue += a.DepleteValuePerMinute;
                if (a.CurrentValue < 0) a.CurrentValue = 0;

                var bar = attributeUI.GetAttributeBar(a);
                if (bar != null) bar.SetCurrentValue(a.CurrentValue);
            }
        }

        private void SetRegeneratingAttributes()
        {
            if (attributeUI == null) return;

            foreach (var a in AttributeData)
            {
                if (a.CurrentValue >= a.MaxValue) return;
                a.CurrentValue += a.RegenerateValuePerMinute;

                var bar = attributeUI.GetAttributeBar(a);
                if (bar != null) bar.SetCurrentValue(a.CurrentValue);
            }
        }

        public void AddMaxAttributeValue(AttributeType type, float value)
        {
            if (attributeUI == null) return;

            foreach (var a in AttributeData)
            {
                if (a.type == type)
                {
                    a.MaxValue += value;
                    var bar = attributeUI.GetAttributeBar(a);
                    if (bar != null) bar.SetMaxValue(a.MaxValue);
                }
            }
        }

        public void AddCurrentAttributeValue(AttributeType type, float value)
        {
            if (attributeUI == null) return;

            foreach (var a in AttributeData)
            {
                if (a.type == type)
                {
                    a.CurrentValue += value;
                    if (a.CurrentValue > a.MaxValue) a.CurrentValue = a.MaxValue;

                    var bar = attributeUI.GetAttributeBar(a);
                    if (bar != null) bar.SetCurrentValue(a.CurrentValue);
                }
            }
        }
    }
}
