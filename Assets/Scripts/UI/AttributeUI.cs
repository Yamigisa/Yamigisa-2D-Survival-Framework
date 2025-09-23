using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class AttributeUI : MonoBehaviour
    {
        [SerializeField] private Transform AttributeUIContainer;
        [SerializeField] private AttributeBar attributeBarPrefab;
        [HideInInspector] public List<AttributeBar> attributeBars = new List<AttributeBar>();

        public void InitializeAttributeBar(AttributeInfo _attributeInfo)
        {
            AttributeBar attributeBarInstance = Instantiate(attributeBarPrefab, AttributeUIContainer);
            attributeBarInstance.SetAttributeBar(_attributeInfo);
            attributeBars.Add(attributeBarInstance);
        }

        public AttributeBar GetAttributeBar(AttributeInfo _attributeInfo)
        {
            foreach (var bar in attributeBars)
            {
                if (bar.AttributeInfo == _attributeInfo)
                {
                    return bar;
                }
            }
            return null;
        }
    }
}