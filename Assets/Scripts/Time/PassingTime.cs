using System.Collections;
using UnityEngine;

namespace Yamigisa
{
    public class PassingTime : MonoBehaviour
    {
        public event System.Action OnMinuteChanged;
        public event System.Action OnHourChanged;

        [Header("Time Settings")]
        [SerializeField] private float minuteToRealTimeSeconds = 2f;
        [Range(0, 59)][SerializeField] private int startingMinute = 0;
        [Range(0, 23)][SerializeField] private int startingHour = 0;

        public int Minute { get; private set; }
        public int Hour { get; private set; }

        public static PassingTime Instance { get; private set; }

        private Coroutine coroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartTime(startingMinute, startingHour);
        }

        public IEnumerator StartPassTime()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(minuteToRealTimeSeconds);
                AdvanceTimeNormal();
            }
        }

        public void StopTime()
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        public void StartTime(int _minute, int _hour)
        {
            Minute = _minute;
            Hour = _hour;
            coroutine = StartCoroutine(StartPassTime());
        }

        private void AdvanceTimeNormal()
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
