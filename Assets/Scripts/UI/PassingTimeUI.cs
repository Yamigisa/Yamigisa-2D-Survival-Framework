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
            PassingTime.Instance.OnMinuteChanged += UpdateTime;
            PassingTime.Instance.OnHourChanged += UpdateTime;
        }

        private void OnDisable()
        {
            PassingTime.Instance.OnMinuteChanged -= UpdateTime;
            PassingTime.Instance.OnHourChanged -= UpdateTime;
        }

        private void UpdateTime()
        {
            timeText.text = $"{PassingTime.Instance.Hour:00}:{PassingTime.Instance.Minute:00}";
        }
    }
}