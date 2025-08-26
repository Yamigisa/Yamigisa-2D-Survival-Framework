using UnityEngine;

namespace Yamigisa
{
    public class CollectibleItem : CollectibleBase
    {
        public ItemData ItemData;
        [SerializeField] private SpriteRenderer spriteRenderer;
        public int Amount = 1;

        private void Start()
        {
            spriteRenderer.sprite = ItemData.iconInventory;
        }

        // For dropping Items
        public void Initialize(ItemData data, int amt)
        {
            ItemData = data;
            Amount = amt;
            spriteRenderer.sprite = ItemData.iconInventory;
        }

        public override void Collect()
        {
            Inventory.Instance.AddItem(ItemData, Amount);
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Collect();
            }
        }
    }
}