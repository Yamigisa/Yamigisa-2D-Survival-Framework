using System;
using System.Collections.Generic;
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

        public GroupData groupData;
        public List<ActionBase> itemActions;

        // Consumable Effects
        public int increaseHealth = 0;
        public int increaseHunger = 0;
        public int increaseThirst = 0;

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