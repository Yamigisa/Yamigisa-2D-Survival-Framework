using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(NewInteractiveObject))]
    public class Destroyable : MonoBehaviour
    {
        [Header("Stats")]
        public int hp = 100;

        [Header("Group Items Required")]
        public List<GroupData> requiredItems;

        [Header("Loot")]
        [SerializeField] private List<DestroyableLoot> loots;
        [Tooltip("If true, the destroyable will drop loot upon being killed. IF false, loot will instantly go into inventory.")]
        [SerializeField] private bool dropLootOnKill = true;

        private NewInteractiveObject select;

        private void Awake()
        {
            select = GetComponent<NewInteractiveObject>();
        }

        public void TakeDamage(int damage)
        {
            hp -= damage;
            if (hp <= 0)
            {
                Kill();
            }
        }

        public void Kill()
        {
            GetLoot();
            Destroy(gameObject);
        }

        private void GetLoot()
        {
            if (dropLootOnKill)
            {
                foreach (DestroyableLoot loot in loots)
                {
                    for (int i = 0; i < loot.quantity; i++)
                    {
                        Instantiate(loot.itemLoot.itemPrefab, transform.position, Quaternion.identity);
                    }
                }
            }
            else
            {
                foreach (DestroyableLoot loot in loots)
                {
                    Inventory.Instance?.AddItem(loot.itemLoot, loot.quantity);
                }
            }
        }
    }

    [System.Serializable]
    public class DestroyableLoot
    {
        public ItemData itemLoot;
        public int quantity;
    }
}
