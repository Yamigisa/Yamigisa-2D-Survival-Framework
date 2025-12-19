using UnityEngine;
using UnityEngine.UI;
using System;

namespace Yamigisa
{
    public class CraftGroupSlot : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Button button;

        private GroupData groupCraft;
        private Action onClick;

        public GroupData Group => groupCraft;

        public void Bind(GroupData group, Action onClick)
        {
            groupCraft = group;
            this.onClick = onClick;

            if (icon) icon.sprite = group != null ? group.icon : null;

            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => this.onClick?.Invoke());
            }
        }
    }
}
