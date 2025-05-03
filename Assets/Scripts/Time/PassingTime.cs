using System;
using UnityEngine;

namespace Yamigisa
{
    public class PassingTime : MonoBehaviour
    {
        public static event Action OnMinuteChanged;
        public static event Action OnHourChanged;

        [Header("Time Settings")]
        [SerializeField] private float minuteToRealTime = 2f;
        [Range(0, 59)][SerializeField] private int startingMinute = 0;
        [Range(0, 23)][SerializeField] private int startingHour = 0;

        private float elapsedTime;

        public static int Minute { get; private set; }
        public static int Hour { get; private set; }

        private void Start()
        {
            Minute = startingMinute;
            Hour = startingHour;

            elapsedTime = 0f;
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;

            while (elapsedTime >= minuteToRealTime)
            {
                elapsedTime -= minuteToRealTime;
                AdvanceMinute();
            }
        }

        private void AdvanceMinute()
        {
            Minute++;
            OnMinuteChanged?.Invoke();

            if (Minute >= 60)
            {
                Minute = 0;
                Hour++;
                if (Hour >= 24)
                {
                    Hour = 0;
                }
                OnHourChanged?.Invoke();
            }
        }
    }
}
