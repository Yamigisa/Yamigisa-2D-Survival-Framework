using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(fileName = "AttributeData", menuName = "Yamigisa/AttributeData")]
    public class AttributeInfo : ScriptableObject
    {
        public AttributeType type;
        [Space(3)]
        [Header("Attribute Settings")]
        public float MaxValue = 100f;
        public float CurrentValue = 100f;
        [Tooltip("Set to 0 to disable depleting")]
        public float DepleteValuePerMinute = -10f;
        [Tooltip("Set to 0 to disable regeneration")]
        public float RegenerateValuePerMinute = 1f;

        [Space(3)]

        [Header("Effects if depleted")]
        [Tooltip("Health loss per minute in game time when depleted")]
        public float HealthLossValue = -1f;
        [Tooltip("Movement speed multiplier loss when depleted")]
        public float MoveSpeedLossValue = -0.2f;

        [Header("For Slider UI")]
        public Sprite FillImage;
        public Sprite BackgroundImage;

        public AttributeType GetAttributeType()
        {
            return type;
        }
    }

    [System.Serializable]
    public enum AttributeType
    {
        Health,
        Hunger,
        Thirst,
    }
}