using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks;

namespace Yamigisa
{
    public class TimeClock : MonoBehaviour, ISavable
    {
        public static TimeClock Instance { get; private set; }

        public event System.Action OnMinuteChanged;
        public event System.Action OnHourChanged;
        public event System.Action OnDayChanged;

        [Header("Time Settings")]
        [SerializeField] private float minuteToRealTimeSeconds = 2f;
        [Range(0, 59)][SerializeField] private int startingMinute = 0;
        [Range(0, 23)][SerializeField] private int startingHour = 0;
        [Min(1)][SerializeField] private int startingDay = 1;

        [Header("UI")]
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

        private Coroutine ticking;
        private bool wired;

        // ===================== SINGLETON ONLY =====================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // ===================== MANUAL LIFECYCLE =====================

        /// <summary>
        /// Called ONCE by GameManager before loading save data.
        /// Wires events and sets default values.
        /// </summary>
        public void Setup()
        {
            if (!wired)
            {
                if (changeEveryMinute)
                    OnMinuteChanged += ApplyLightingByMinute;
                else
                    OnHourChanged += ApplyLightingByHour;

                OnMinuteChanged += UpdateUIAndFill;
                OnHourChanged += UpdateUIAndFill;
                OnDayChanged += UpdateUIAndFill;

                wired = true;
            }

            SetTime(startingMinute, startingHour, startingDay);
            RefreshVisuals();
        }

        /// <summary>
        /// Called by GameManager AFTER Load().
        /// This is when time actually starts moving.
        /// </summary>
        public void StartSystem()
        {
            if (ticking != null)
                StopCoroutine(ticking);

            ticking = StartCoroutine(Tick());
        }

        public void StopSystem()
        {
            if (ticking != null)
            {
                StopCoroutine(ticking);
                ticking = null;
            }
        }

        // ===================== CORE LOGIC =====================

        public void SetTime(int minute, int hour, int day)
        {
            Minute = Mathf.Clamp(minute, 0, 59);
            Hour = Mathf.Clamp(hour, 0, 23);
            Day = Mathf.Max(1, day);
        }

        private IEnumerator Tick()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(minuteToRealTimeSeconds);
                AdvanceMinute();
            }
        }

        private void AdvanceMinute()
        {
            Minute++;
            OnMinuteChanged?.Invoke();

            if (changeEveryMinute)
                ApplyLightingByMinute();

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

        // ===================== VISUALS =====================

        private void UpdateUIAndFill()
        {
            if (timeText) timeText.text = $"{Hour:00}:{Minute:00}";
            if (dayText) dayText.text = $"Day {Day}";
            UpdateClockFill();
        }

        private void UpdateClockFill()
        {
            if (!clockFill) return;

            float dayTime = Hour + Minute / 60f;
            bool clockwise = dayTime <= 12f;
            clockFill.fillClockwise = clockwise;

            if (clockwise)
                clockFill.fillAmount = Mathf.Clamp01(dayTime / 12f);
            else
                clockFill.fillAmount = Mathf.Clamp01(1f - ((dayTime - 12f) / 12f));
        }

        private void RefreshVisuals()
        {
            UpdateUIAndFill();

            if (changeEveryMinute) ApplyLightingByMinute();
            else ApplyLightingByHour();
        }

        private void ApplyLightingByMinute()
        {
            if (!dayNightLight || dayNightGradient == null) return;
            float t = (Hour * 60f + Minute) / 1440f;
            dayNightLight.color = dayNightGradient.Evaluate(t);
        }

        private void ApplyLightingByHour()
        {
            if (!dayNightLight || dayNightGradient == null) return;
            float t = Hour / 24f;
            dayNightLight.color = dayNightGradient.Evaluate(t);
        }

        public void ForceRefreshVisual()
        {
            if (changeEveryMinute)
                ApplyLightingByMinute();
            else
                ApplyLightingByHour();

            UpdateUIAndFill();
        }
        // ===================== SAVE =====================

        public void Save(ref SaveGameData data)
        {
            if (!data.saveManager.SaveWorldTime)
                return;

            data.day = Day;
            data.hour = Hour;
            data.minute = Minute;
        }

        public void Load(SaveGameData data)
        {
            SetTime(data.minute, data.hour, data.day);

            // Force refresh lighting explicitly
            if (changeEveryMinute)
                ApplyLightingByMinute();
            else
                ApplyLightingByHour();

            UpdateUIAndFill();

            StartSystem();
        }
    }
}