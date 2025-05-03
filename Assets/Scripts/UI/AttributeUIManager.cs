using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class AttributeUIManager : MonoBehaviour
    {
        [SerializeField] private Slider sliderPrefab;
        [SerializeField] private Transform sliderContainer;
        [SerializeField] private Text currentValueText;

        public void CreateAttributeSlider(AttributeInfo attributeData)
        {
            Slider sliderGO = Instantiate(sliderPrefab, sliderContainer);

            sliderGO.minValue = attributeData.CurrentValue;

        }
    }
}