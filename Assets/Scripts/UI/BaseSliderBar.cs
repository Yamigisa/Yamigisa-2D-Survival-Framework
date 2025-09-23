using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class BaseSliderBar : MonoBehaviour
    {
        [Header("Drag component to inspector")]
        public Slider SliderObject;
        public Image SliderFillImage;
        public Image SliderBackgroundImage;
        public Text SliderText;

        public void SetMaxValue(float _maxValue)
        {
            SliderObject.maxValue = _maxValue;
        }

        public void SetMinValue(float _minValue)
        {
            SliderObject.minValue = _minValue;
            SliderText.text = SliderObject.minValue.ToString();
        }

        public void SetCurrentValue(float _currentValue)
        {
            SliderObject.value = _currentValue;
            SliderText.text = SliderObject.value.ToString();
        }
    }
}