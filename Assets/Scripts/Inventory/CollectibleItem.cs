using UnityEngine;

namespace Yamigisa
{
    public class CollectibleItem : CollectibleBase
    {
        [SerializeField] private ItemData itemData;

        public override void Collect()
        {
            if (LockInteraction) return;

            Inventory.Instance.AddItem(itemData);
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Collect();
            }
        }
    }
}