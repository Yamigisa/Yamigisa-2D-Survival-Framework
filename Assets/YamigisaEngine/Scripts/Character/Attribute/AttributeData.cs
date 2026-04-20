using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "AttributeData", menuName = "Yamigisa/AttributeData")]
    public class AttributeData : ScriptableObject
    {
        public AttributeType type;

        [Space(3)]
        [Header("Attribute Settings")]
        public float MaxValue = 100f;
        public float CurrentValue = 100f;
        [HideInInspector] public float BaseMaxValue;

        [Tooltip("Set to 0 to disable depleting")]
        public float DepleteValuePerMinute = 0f;

        [Tooltip("Set to 0 to disable regeneration")]
        public float RegenerateValuePerMinute = 0f;

        [Space(3)]
        [Header("Effects while THIS attribute is depleted (<= 0)")]
        public bool triggerGameOver = false;
        public List<DepletedAttributeModifier> DepletedModifiers = new();

        [Tooltip("Movement speed multiplier loss when depleted")]
        public float MoveSpeedLossValue = -0.2f;

        [Header("For Slider UI")]
        public Sprite FillImage;
        public Sprite BackgroundImage;

        public AttributeType GetAttributeType() => type;

        private void OnEnable()
        {
            BaseMaxValue = MaxValue;
        }
    }

    [System.Serializable]
    public class DepletedAttributeModifier
    {
        public AttributeType targetType;

        [Tooltip("Extra regen per minute applied to target while THIS attribute is depleted")]
        public float regenAddition = 0f;

        [Tooltip("Extra deplete per minute applied to target while THIS attribute is depleted")]
        public float depleteAddition = 0f;
    }
}