using UnityEngine;

namespace Yamigisa
{
    public class CollectibleItem : CollectibleBase
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private int amount = 1;

        private void Start()
        {
            spriteRenderer.sprite = itemData.iconInventory;
        }

        // For dropping Items
        public void Initialize(ItemData data, int amt)
        {
            itemData = data;
            amount = amt;
            spriteRenderer.sprite = itemData.iconInventory;
        }

        public override void Collect()
        {
            Inventory.Instance.AddItem(itemData, amount);
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