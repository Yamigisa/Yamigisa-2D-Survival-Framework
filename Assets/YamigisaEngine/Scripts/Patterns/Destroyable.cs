using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(InteractiveObject))]
    public class Destroyable : MonoBehaviour
    {
        [Header("Stats")]
        public int hp = 100;

        [Header("Group Items Required")]
        public GroupData requiredItem;

        [Header("Loot")]
        [SerializeField] private List<ItemData> loots;

        private InteractiveObject select;

        private void Awake()
        {
            select = GetComponent<InteractiveObject>();
        }

        public void TakeDamage(int damage)
        {
            hp -= damage;
            Debug.Log("tkaing dmg");
            if (hp <= 0)
            {
                Kill();
            }
        }

        private void Kill()
        {
            GetLoot();
            Destroy(gameObject);
        }

        private void GetLoot()
        {
            if (loots == null || loots.Count == 0) return;
            if (Inventory.Instance == null) return;

            for (int i = 0; i < loots.Count; i++)
            {
                ItemData item = loots[i];
                if (item != null)
                    Inventory.Instance.AddItem(item);
            }
        }
    }
}
