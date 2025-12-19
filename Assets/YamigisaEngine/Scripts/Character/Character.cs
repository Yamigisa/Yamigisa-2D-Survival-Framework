using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    public class Character : MonoBehaviour
    {
        [HideInInspector] public CharacterAnimation characterAnimation;
        [HideInInspector] public CharacterAttribute characterAttribute;
        [HideInInspector] public CharacterMovement characterMovement;

        [Header("Starting Items")]
        public List<ItemData> startingItems;

        private void Awake()
        {
            characterAnimation = GetComponent<CharacterAnimation>();
            characterAttribute = GetComponent<CharacterAttribute>();
            characterMovement = GetComponent<CharacterMovement>();
        }

        public void ConsumeItem(ItemData itemData)
        {
            characterAttribute.AddCurrentAttributeValue(AttributeType.Health, itemData.increaseHealth);
            characterAttribute.AddCurrentAttributeValue(AttributeType.Hunger, itemData.increaseHunger);
            characterAttribute.AddCurrentAttributeValue(AttributeType.Thirst, itemData.increaseThirst);
            Debug.Log("Consumed " + itemData.itemName);
        }
    }
}