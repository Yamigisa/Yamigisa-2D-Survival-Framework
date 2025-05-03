using UnityEngine;

namespace Yamigisa
{
    public class PlayerAttributeUser : MonoBehaviour
    {
        //[SerializeField] private PlayerAttribute playerAttribute;

        private void OnEnable()
        {
            PassingTime.OnMinuteChanged += SetDepletingAttributes;
        }

        private void OnDisable()
        {
            PassingTime.OnMinuteChanged -= SetDepletingAttributes;
        }

        private void SetDepletingAttributes()
        {
            // foreach (var attributeData in playerAttribute.AttributeDatas)
            // {
            //     if (attributeData.DepleteValuePerMinute != 0f)
            //     {
            //         attributeData.CurrentValue += attributeData.DepleteValuePerMinute;
            //         Debug.Log(attributeData + "is delepting");
            //     }
            // }
        }
    }
}