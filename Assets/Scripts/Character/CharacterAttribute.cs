using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [ExecuteAlways]
    public class CharacterAttribute : MonoBehaviour
    {
        [SerializeField] private List<AttributeInfo> attributeInfo;
        private AttributeUI attributeUI;

        void OnEnable()
        {
            attributeUI = FindObjectOfType<AttributeUI>();
            if (attributeUI != null)
            {
                foreach (var a in attributeInfo)
                    attributeUI.InitializeAttributeBar(a);
            }

            if (Application.isPlaying && TimeClock.Instance != null)
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
            foreach (var a in attributeInfo)
            {
                if (a.DepleteValuePerMinute != 0f)
                {
                    a.CurrentValue += a.DepleteValuePerMinute;
                    var bar = attributeUI.GetAttributeBar(a);
                    bar.SetCurrentValue(a.CurrentValue);
                }
            }
        }

        private void SetRegeneratingAttributes()
        {
            foreach (var a in attributeInfo)
            {
                if (a.RegenerateValuePerMinute != 0f)
                {
                    if (a.CurrentValue > a.MaxValue) return;
                    a.CurrentValue += a.RegenerateValuePerMinute;
                    var bar = attributeUI.GetAttributeBar(a);
                    bar.SetCurrentValue(a.CurrentValue);
                }
            }
        }

        public void AddMaxAttributeValue(AttributeType type, float value)
        {
            foreach (var a in attributeInfo)
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
            foreach (var a in attributeInfo)
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
