using UnityEngine;

namespace Yamigisa
{
    public class AttributeBar : BaseSliderBar
    {
        public AttributeData AttributeData;

        public void SetAttributeBar(AttributeData _AttributeData)
        {
            AttributeData = _AttributeData;

            SliderFillImage.sprite = AttributeData.FillImage;
            SliderBackgroundImage.sprite = AttributeData.BackgroundImage;

            SetMaxValue(AttributeData.MaxValue);
            SetMinValue(0);
            SetCurrentValue(AttributeData.CurrentValue);
        }
    }
}