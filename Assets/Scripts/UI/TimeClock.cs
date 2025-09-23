using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Yamigisa
{
    public class TimeClock : MonoBehaviour
    {
        public event System.Action OnMinuteChanged;
        public event System.Action OnHourChanged;
        public event System.Action OnDayChanged;

        [Header("Time Settings")]
        [SerializeField] private float minuteToRealTimeSeconds = 2f;
        [Range(0, 59)][SerializeField] private int startingMinute = 0;
        [Range(0, 23)][SerializeField] private int startingHour = 0;
        [Min(1)][SerializeField] private int startingDay = 1;

        [Header("UI (Legacy Text)")]
        [SerializeField] private Text timeText;
        [SerializeField] private Text dayText;
        [SerializeField] private Image clockFill;

        [Header("Day-Night")]
        [SerializeField] private Light2D dayNightLight;
        [SerializeField] private Gradient dayNightGradient;
        [SerializeField] private bool changeEveryMinute = true;

        public int Minute { get; private set; }
        public int Hour { get; private set; }
        public int Day { get; private set; }

        public static TimeClock Instance { get; private set; }

        Coroutine ticking;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Start()
        {
            StartTime(startingMinute, startingHour, startingDay);

            if (changeEveryMinute)
                OnMinuteChanged += ApplyLightingByMinute;
            else
                OnHourChanged += ApplyLightingByHour;

            OnMinuteChanged += UpdateUIAndFill;
            OnHourChanged += UpdateUIAndFill;
            OnDayChanged += UpdateUIAndFill;

            UpdateUIAndFill();
            if (changeEveryMinute) ApplyLightingByMinute(); else ApplyLightingByHour();
        }

        void OnDisable()
        {
            if (Instance != this) return;

            if (changeEveryMinute)
                OnMinuteChanged -= ApplyLightingByMinute;
            else
                OnHourChanged -= ApplyLightingByHour;

            OnMinuteChanged -= UpdateUIAndFill;
            OnHourChanged -= UpdateUIAndFill;
            OnDayChanged -= UpdateUIAndFill;
        }

        public void StartTime(int minute, int hour, int day = 1)
        {
            Minute = Mathf.Clamp(minute, 0, 59);
            Hour = Mathf.Clamp(hour, 0, 23);
            Day = Mathf.Max(1, day);

            if (ticking != null) StopCoroutine(ticking);
            ticking = StartCoroutine(Tick());
        }

        public void StopTime()
        {
            if (ticking != null)
            {
                StopCoroutine(ticking);
                ticking = null;
            }
        }

        IEnumerator Tick()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(minuteToRealTimeSeconds);
                AdvanceMinute();
            }
        }

        void AdvanceMinute()
        {
            Minute++;
            OnMinuteChanged?.Invoke();

            if (Minute >= 60)
            {
                Minute = 0;
                Hour++;
                OnHourChanged?.Invoke();

                if (Hour >= 24)
                {
                    Hour = 0;
                    Day++;
                    OnDayChanged?.Invoke();
                }
            }
        }

        void UpdateUIAndFill()
        {
            if (timeText != null) timeText.text = $"{Hour:00}:{Minute:00}";
            if (dayText != null) dayText.text = $"Day {Day}";
            UpdateClockFill();
        }

        void UpdateClockFill()
        {
            if (clockFill == null) return;

            float dayTime = Hour + (Minute / 60f);
            bool clockwise = dayTime <= 12f;
            clockFill.fillClockwise = clockwise;

            if (clockwise)
            {
                float value = dayTime / 12f;
                clockFill.fillAmount = Mathf.Clamp01(value);
            }
            else
            {
                float value = (dayTime - 12f) / 12f;
                clockFill.fillAmount = Mathf.Clamp01(1f - value);
            }
        }

        void ApplyLightingByMinute()
        {
            if (dayNightLight == null || dayNightGradient == null) return;
            float t = (Hour * 60f + Minute) / 1440f;
            dayNightLight.color = dayNightGradient.Evaluate(t);
        }

        void ApplyLightingByHour()
        {
            if (dayNightLight == null || dayNightGradient == null) return;
            float t = Hour / 24f;
            dayNightLight.color = dayNightGradient.Evaluate(t);
        }
    }
}
