using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class AttributeUI : MonoBehaviour
    {
        [SerializeField] private Transform AttributeUIContainer;
        [SerializeField] private AttributeBar attributeBarPrefab;
        public List<AttributeBar> attributeBars = new List<AttributeBar>();

        public void InitializeAttributeBar(AttributeData _AttributeData)
        {
            // 🔎 Check if already exists
            foreach (AttributeBar bar in attributeBars)
            {
                if (bar.AttributeData.type == _AttributeData.type)
                {
                    // Already exists → just refresh data
                    bar.SetAttributeBar(_AttributeData);
                    return;
                }
            }

            // ❌ Not found → create new one
            AttributeBar attributeBarInstance = Instantiate(attributeBarPrefab, AttributeUIContainer);
            attributeBarInstance.SetAttributeBar(_AttributeData);
            attributeBars.Add(attributeBarInstance);
        }

        public AttributeBar GetAttributeBar(AttributeData _AttributeData)
        {
            foreach (AttributeBar bar in attributeBars)
            {
                if (bar.AttributeData.type == _AttributeData.type)
                    return bar;
            }
            return null;
        }
    }
}
