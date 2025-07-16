using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(CharacterUI))]
    public class CharacterAttribute : MonoBehaviour
    {
        [SerializeField] private List<AttributeInfo> attributeInfo;

        private CharacterUI characterUI;

        private void OnEnable()
        {
            PassingTime.Instance.OnMinuteChanged += SetDepletingAttributes;
            PassingTime.Instance.OnMinuteChanged += SetRegeneratingAttributes;
        }

        private void OnDisable()
        {
            PassingTime.Instance.OnMinuteChanged -= SetDepletingAttributes;
            PassingTime.Instance.OnMinuteChanged -= SetRegeneratingAttributes;
        }

        void Start()
        {
            characterUI = GetComponent<CharacterUI>();

            foreach (AttributeInfo _attributeInfo in attributeInfo)
            {
                characterUI.InitializeAttributeBar(_attributeInfo);
            }
        }

        private void SetDepletingAttributes()
        {
            foreach (AttributeInfo _attributeInfo in attributeInfo)
            {
                if (_attributeInfo.DepleteValuePerMinute != 0f)
                {
                    _attributeInfo.CurrentValue += _attributeInfo.DepleteValuePerMinute;

                    AttributeBar bar = characterUI.GetAttributeBar(_attributeInfo);
                    bar.SetCurrentValue(_attributeInfo.CurrentValue);
                }
            }
        }

        private void SetRegeneratingAttributes()
        {
            foreach (AttributeInfo _attributeInfo in attributeInfo)
            {
                if (_attributeInfo.RegenerateValuePerMinute != 0f)
                {
                    if (_attributeInfo.CurrentValue > _attributeInfo.MaxValue)
                        return;

                    _attributeInfo.CurrentValue += _attributeInfo.RegenerateValuePerMinute;

                    AttributeBar bar = characterUI.GetAttributeBar(_attributeInfo);
                    bar.SetCurrentValue(_attributeInfo.CurrentValue);
                }
            }
        }
    }
}