using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class CharacterAttribute : MonoBehaviour
    {
        [SerializeField] private List<AttributeData> AttributeData;
        private AttributeUI attributeUI;

        private Dictionary<AttributeType, float> biomeRegenAdditions = new();
        private Dictionary<AttributeType, float> biomeDepleteAdditions = new();

        [System.Obsolete]
        void Start()
        {
            attributeUI = FindObjectOfType<AttributeUI>();
            if (attributeUI == null) return;

            foreach (AttributeData a in AttributeData)
            {
                attributeUI.InitializeAttributeBar(a);
            }

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

            foreach (AttributeData a in AttributeData)
            {
                float delta = a.DepleteValuePerMinute;

                if (biomeDepleteAdditions.TryGetValue(a.type, out float add))
                    delta += add;

                a.CurrentValue += delta;
                if (a.CurrentValue < 0) a.CurrentValue = 0;

                var bar = attributeUI.GetAttributeBar(a);
                if (bar != null) bar.SetCurrentValue(a.CurrentValue);
            }
        }

        public void ApplyBiomeModifiers(List<AttributeModifier> modifiers)
        {
            biomeRegenAdditions.Clear();
            biomeDepleteAdditions.Clear();

            foreach (var mod in modifiers)
            {
                biomeRegenAdditions[mod.type] = mod.regenAddition;
                biomeDepleteAdditions[mod.type] = mod.depleteAddition;
            }
        }

        private void SetRegeneratingAttributes()
        {
            if (attributeUI == null) return;

            foreach (AttributeData a in AttributeData)
            {
                if (a.CurrentValue >= a.MaxValue) continue;

                float delta = a.RegenerateValuePerMinute;

                if (biomeRegenAdditions.TryGetValue(a.type, out float add))
                    delta += add;

                a.CurrentValue += delta;
                if (a.CurrentValue > a.MaxValue) a.CurrentValue = a.MaxValue;

                var bar = attributeUI.GetAttributeBar(a);
                if (bar != null) bar.SetCurrentValue(a.CurrentValue);
            }
        }

        public void AddMaxAttributeValue(AttributeType type, float value)
        {
            if (attributeUI == null) return;

            foreach (AttributeData a in AttributeData)
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

            foreach (AttributeData a in AttributeData)
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
