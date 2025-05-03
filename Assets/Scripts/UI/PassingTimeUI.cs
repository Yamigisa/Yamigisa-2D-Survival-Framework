using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class PassingTimeUI : MonoBehaviour
    {
        [SerializeField] private Text timeText;
        private void OnEnable()
        {
            PassingTime.OnMinuteChanged += UpdateTime;
            PassingTime.OnHourChanged += UpdateTime;
        }

        private void OnDisable()
        {
            PassingTime.OnMinuteChanged -= UpdateTime;
            PassingTime.OnHourChanged -= UpdateTime;
        }

        private void UpdateTime()
        {
            timeText.text = $"{PassingTime.Hour:00}:{PassingTime.Minute:00}";
        }
    }
}