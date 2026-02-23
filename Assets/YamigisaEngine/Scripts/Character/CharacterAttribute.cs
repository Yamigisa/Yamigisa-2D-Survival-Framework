using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yamigisa
{
    public class CharacterAttribute : MonoBehaviour
    {
        [SerializeField] private List<AttributeData> AttributeData;
        private AttributeUI attributeUI;

        private Dictionary<AttributeType, float> biomeRegenAdditions = new();
        private Dictionary<AttributeType, float> biomeDepleteAdditions = new();

        private readonly Dictionary<AttributeType, float> depletedRegenAdds = new();
        private readonly Dictionary<AttributeType, float> depletedDepleteAdds = new();

        // ✅ Equipment modifiers (MAX + PERCENT ONLY)
        private Dictionary<AttributeType, float> equipmentMax = new();
        private Dictionary<AttributeType, float> equipmentPercent = new();

        void Start()
        {
            attributeUI = FindAnyObjectByType<AttributeUI>();
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

            RebuildDepletedModifiers();

            foreach (AttributeData a in AttributeData)
            {
                float delta = a.DepleteValuePerMinute;

                if (biomeDepleteAdditions.TryGetValue(a.type, out float biomeAdd))
                    delta += biomeAdd;

                if (depletedDepleteAdds.TryGetValue(a.type, out float depletedAdd))
                    delta += depletedAdd;

                a.CurrentValue += delta;

                if (a.CurrentValue < 0)
                    a.CurrentValue = 0;

                var bar = attributeUI.GetAttributeBar(a);
                if (bar != null)
                    bar.SetCurrentValue(a.CurrentValue);

                if (a.CurrentValue <= 0 && a.triggerGameOver)
                {
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

            RebuildDepletedModifiers();

            foreach (AttributeData a in AttributeData)
            {
                if (a.type == AttributeType.Health && a.CurrentValue <= 0)
                    continue;

                if (a.CurrentValue >= a.MaxValue)
                    continue;

                float delta = a.RegenerateValuePerMinute;

                if (biomeRegenAdditions.TryGetValue(a.type, out float biomeAdd))
                    delta += biomeAdd;

                if (depletedRegenAdds.TryGetValue(a.type, out float depletedAdd))
                    delta += depletedAdd;

                a.CurrentValue += delta;

                if (a.CurrentValue > a.MaxValue)
                    a.CurrentValue = a.MaxValue;

                var bar = attributeUI.GetAttributeBar(a);
                if (bar != null)
                    bar.SetCurrentValue(a.CurrentValue);
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
                    if (bar != null)
                        bar.SetMaxValue(a.MaxValue);
                }
            }
        }

        // ✅ Consumables ONLY affect current value
        public void AddCurrentAttributeValue(AttributeType type, float value)
        {
            if (attributeUI == null) return;

            foreach (AttributeData a in AttributeData)
            {
                if (a.type == type)
                {
                    a.CurrentValue += value;

                    if (a.CurrentValue > a.MaxValue)
                        a.CurrentValue = a.MaxValue;

                    var bar = attributeUI.GetAttributeBar(a);
                    if (bar != null)
                        bar.SetCurrentValue(a.CurrentValue);
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
                    baseMax = a.BaseMaxValue // save BASE only
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

                    // restore BASE max
                    a.BaseMaxValue = saved.baseMax;

                    // reset max to base first (equipment will re-apply later)
                    a.MaxValue = a.BaseMaxValue;

                    // restore current
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

            // IMPORTANT:
            // If equipment is already loaded (order varies), apply its modifiers on top of base.
            RecalculateAttributes();
        }

        private void RebuildDepletedModifiers()
        {
            depletedRegenAdds.Clear();
            depletedDepleteAdds.Clear();

            foreach (AttributeData source in AttributeData)
            {
                if (source.CurrentValue > 0)
                    continue;

                if (source.DepletedModifiers == null)
                    continue;

                foreach (var mod in source.DepletedModifiers)
                {
                    if (!depletedRegenAdds.ContainsKey(mod.targetType))
                        depletedRegenAdds[mod.targetType] = 0f;

                    if (!depletedDepleteAdds.ContainsKey(mod.targetType))
                        depletedDepleteAdds[mod.targetType] = 0f;

                    depletedRegenAdds[mod.targetType] += mod.regenAddition;
                    depletedDepleteAdds[mod.targetType] += mod.depleteAddition;
                }
            }
        }

        public void EditorInitializeUI()
        {
            attributeUI = FindAnyObjectByType<AttributeUI>();

            if (attributeUI == null)
            {
                return;
            }

            for (int i = attributeUI.attributeBars.Count - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(attributeUI.attributeBars[i].gameObject);
                else
                    Destroy(attributeUI.attributeBars[i].gameObject);
#else
                Destroy(attributeUI.attributeBars[i].gameObject);
#endif
            }

            attributeUI.attributeBars.Clear();

            foreach (AttributeData a in AttributeData)
            {
                attributeUI.InitializeAttributeBar(a);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(attributeUI);
#endif
        }

        // ✅ Equipment modifies MAX only
        public void SetEquipmentModifiers(
            Dictionary<AttributeType, float> max,
            Dictionary<AttributeType, float> percent)
        {
            equipmentMax = max ?? new();
            equipmentPercent = percent ?? new();

            RecalculateAttributes();
        }

        private void RecalculateAttributes()
        {
            foreach (AttributeData a in AttributeData)
            {
                float baseMax = a.BaseMaxValue;

                float flat = equipmentMax.TryGetValue(a.type, out float m) ? m : 0f;
                float percent = equipmentPercent.TryGetValue(a.type, out float p) ? p : 0f;

                float finalMax = baseMax + flat;
                finalMax += baseMax * percent;

                a.MaxValue = finalMax;

                if (a.CurrentValue > finalMax)
                    a.CurrentValue = finalMax;

                if (a.CurrentValue < 0)
                    a.CurrentValue = 0;

                if (attributeUI != null)
                {
                    var bar = attributeUI.GetAttributeBar(a);
                    if (bar != null)
                    {
                        bar.SetMaxValue(finalMax);
                        bar.SetCurrentValue(a.CurrentValue);
                    }
                }
            }
        }
    }
}