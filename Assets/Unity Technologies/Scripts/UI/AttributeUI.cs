using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class AttributeUI : MonoBehaviour
    {
        [SerializeField] private Transform AttributeUIContainer;
        [SerializeField] private AttributeBar attributeBarPrefab;
        [HideInInspector] public List<AttributeBar> attributeBars = new List<AttributeBar>();

        public void InitializeAttributeBar(AttributeData _AttributeData)
        {
            if (GetAttributeBar(_AttributeData) != null) return;
            AttributeBar attributeBarInstance = Instantiate(attributeBarPrefab, AttributeUIContainer);
            attributeBarInstance.SetAttributeBar(_AttributeData);
            attributeBars.Add(attributeBarInstance);
        }

        public AttributeBar GetAttributeBar(AttributeData _AttributeData)
        {
            foreach (var bar in attributeBars)
            {
                if (bar.AttributeData == _AttributeData)
                {
                    return bar;
                }
            }
            return null;
        }
    }
}