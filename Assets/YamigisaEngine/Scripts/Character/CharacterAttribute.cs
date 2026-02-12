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

                if (a.type == AttributeType.Health && a.CurrentValue <= 0)
                {
                    a.CurrentValue = 0;

                    if (bar != null)
                        bar.SetCurrentValue(0);

                    Character.instance.Die();
                }
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
                if (a.type == AttributeType.Health && a.CurrentValue <= 0)
                    continue;

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

        public AttributeData GetAttributeData(AttributeType type)
        {
            foreach (AttributeData a in AttributeData)
            {
                if (a.type == type)
                    return a;
            }
            return null;
        }

        public List<AttributeSaveData> GetSaveData()
        {
            List<AttributeSaveData> result = new();

            foreach (AttributeData a in AttributeData)
            {
                result.Add(new AttributeSaveData
                {
                    type = a.type,
                    current = a.CurrentValue,
                    max = a.MaxValue
                });
            }

            return result;
        }

        public void LoadFromSaveData(List<AttributeSaveData> data)
        {
            foreach (var saved in data)
            {
                foreach (AttributeData a in AttributeData)
                {
                    if (a.type != saved.type) continue;

                    a.MaxValue = saved.max;
                    a.CurrentValue = Mathf.Clamp(saved.current, 0, a.MaxValue);

                    if (attributeUI != null)
                    {
                        var bar = attributeUI.GetAttributeBar(a);
                        if (bar != null)
                        {
                            bar.SetMaxValue(a.MaxValue);
                            bar.SetCurrentValue(a.CurrentValue);
                        }
                    }
                }
            }
        }

    }
}
