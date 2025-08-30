using UnityEngine;

namespace Yamigisa
{
    public class Character : MonoBehaviour
    {
        public CharacterAnimation characterAnimation;
        public CharacterAttribute characterAttribute;
        public CharacterMovement characterMovement;

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
        }
    }
}