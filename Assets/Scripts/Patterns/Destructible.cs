using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(Selectable))]
    public class Destructible : MonoBehaviour
    {
        [Header("Stats")]
        public int hp = 100;

        [Header("Group Items Required")]
        [SerializeField] private List<GroupData> requiredItems;

        [Header("Loot")]
        [SerializeField] private List<ItemData> loots;
        private Selectable select;

        private void Awake()
        {
            select = GetComponent<Selectable>();
        }

        
        public void TakeDamage(int damage)
        {
            hp -= damage;

            if (hp <= 0)
            {
                Kill();
            }
        }

        private void Kill()
        {
            GetLoot();
        }

        private void GetLoot()
        {
            foreach (ItemData item in loots)
            {
                Inventory.Instance.AddItem(item);
            }
        }
    }
}