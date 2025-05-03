using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Yamigisa
{
    [RequireComponent(typeof(PassingTime))]
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light2D dayNightLight;
        [SerializeField] private Gradient dayNightGradient;

        [Header("Day Night Settings")]
        [Tooltip("Disable if change every hour")]
        [SerializeField] private bool changeEveryMinute = true;

        private void OnEnable()
        {
            if (!changeEveryMinute)
            {
                PassingTime.OnHourChanged += UpdateDayNightHour;
                UpdateDayNightHour();
            }
        }

        private void OnDisable()
        {
            if (!changeEveryMinute)
                PassingTime.OnHourChanged -= UpdateDayNightHour;
        }

        private void Update()
        {
            if (changeEveryMinute)
            {
                float normalizedTime = (PassingTime.Hour * 60 + PassingTime.Minute) / 1440f;
                dayNightLight.color = dayNightGradient.Evaluate(normalizedTime);
            }
        }

        private void UpdateDayNightHour()
        {
            float normalizedTime = PassingTime.Hour / 24f;
            dayNightLight.color = dayNightGradient.Evaluate(normalizedTime);
        }
    }
}
