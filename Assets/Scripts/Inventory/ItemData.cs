using UnityEngine;

namespace Yamigisa
{
    [CreateAssetMenu(menuName = "Yamigisa/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public Sprite iconWorld;
        public Sprite iconInventory;
        public string description;
        public ItemType itemType;
        public int maxAmount = 99;
        public bool isDroppable = true;
        public bool isStackable = true;
    }

    public enum ItemType
    {
        Resource,
        Equipment,
        Consumable,
    }
}