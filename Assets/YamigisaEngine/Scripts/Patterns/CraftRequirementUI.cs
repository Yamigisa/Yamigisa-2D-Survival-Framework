using UnityEngine;
using UnityEngine.UI;

namespace Yamigisa
{
    public class CraftRequirementUI : MonoBehaviour
    {
        public Image icon;
        public Text amountText;

        [System.NonSerialized] public bool IsItemRequirement;
        [System.NonSerialized] public bool IsGroupRequirement;
        [System.NonSerialized] public ItemData ItemKey;
        [System.NonSerialized] public GroupData GroupKey;
        [System.NonSerialized] public int RequiredAmount;

        public void BindAsItem(ItemData item, int amount)
        {
            IsItemRequirement = true;
            IsGroupRequirement = false;
            ItemKey = item;
            GroupKey = null;
            RequiredAmount = amount;
        }

        public void BindAsGroup(GroupData group, int amount)
        {
            IsItemRequirement = false;
            IsGroupRequirement = true;
            ItemKey = null;
            GroupKey = group;
            RequiredAmount = amount;
        }
    }
}