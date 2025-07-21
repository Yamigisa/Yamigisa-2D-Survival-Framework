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

        private void Start()
        {
            if (changeEveryMinute)
            {
                PassingTime.Instance.OnMinuteChanged += UpdateDayNightMinute;
                UpdateDayNightMinute();
            }
            else
            {
                PassingTime.Instance.OnHourChanged += UpdateDayNightHour;
                UpdateDayNightHour();
            }
        }

        private void OnDisable()
        {
            if (changeEveryMinute)
            {
                PassingTime.Instance.OnMinuteChanged -= UpdateDayNightMinute;
            }
            else
            {
                PassingTime.Instance.OnHourChanged -= UpdateDayNightHour;
            }
        }

        private void UpdateDayNightMinute()
        {
            float normalizedTime = (PassingTime.Instance.Hour * 60 + PassingTime.Instance.Minute) / 1440f;
            dayNightLight.color = dayNightGradient.Evaluate(normalizedTime);
        }

        private void UpdateDayNightHour()
        {
            float normalizedTime = PassingTime.Instance.Hour / 24f;
            dayNightLight.color = dayNightGradient.Evaluate(normalizedTime);
        }
    }
}
