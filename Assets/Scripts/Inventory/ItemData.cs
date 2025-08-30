using System;
using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(menuName = "Yamigisa/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public Sprite iconWorld;
        public Sprite iconInventory;
        [TextArea] public string description;
        public ItemType itemType;
        public int maxAmount = 99;
        public bool isDroppable = true;
        public bool isStackable = true;

        // Equipment Effects
        public int increaseMaxHealth = 10;
        public int increaseMaxHunger = 5;
        public int increaseMaxThirst = 5;

        // Consumable Effects
        public int increaseHealth = 10;
        public int increaseHunger = 5;
        public int increaseThirst = 5;

        public void ApplyEffect(CharacterAttribute target)
        {
            if (!target) return;

            switch (itemType)
            {

                case ItemType.Equipment:
                    target.AddMaxAttributeValue(AttributeType.Health, increaseMaxHealth);
                    target.AddMaxAttributeValue(AttributeType.Hunger, increaseMaxHunger);
                    target.AddMaxAttributeValue(AttributeType.Thirst, increaseMaxThirst);
                    break;
                case ItemType.Consumable:
                    target.AddCurrentAttributeValue(AttributeType.Health, increaseHealth);
                    target.AddCurrentAttributeValue(AttributeType.Hunger, increaseHunger);
                    target.AddCurrentAttributeValue(AttributeType.Thirst, increaseThirst);
                    break;
                default:
                    break;
            }
        }

        public void ChangeDroppableState(bool state)
        {
            isDroppable = state;
        }
    }

    public enum ItemType
    {
        Resource,
        Equipment,
        Consumable,
    }
}