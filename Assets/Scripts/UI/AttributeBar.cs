using UnityEngine;

namespace Yamigisa
{
    public class AttributeBar : BaseSliderBar
    {
        public AttributeInfo AttributeInfo;

        public void SetAttributeBar(AttributeInfo _attributeInfo)
        {
            AttributeInfo = _attributeInfo;

            SliderFillImage.sprite = AttributeInfo.FillImage;
            SliderBackgroundImage.sprite = AttributeInfo.BackgroundImage;

            SetMaxValue(AttributeInfo.MaxValue);
            SetMinValue(0);
            SetCurrentValue(AttributeInfo.CurrentValue);
        }
    }
}