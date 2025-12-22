using UnityEngine;

namespace Yamigisa
{
    public class Item : MonoBehaviour
    {
        [Header("Item")]
        public ItemData itemData;
        public int quantity = 1;
        [Header("Settings")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private InteractiveObject interactiveObject;

        void Start()
        {
            spriteRenderer.sprite = itemData.iconWorld;
            interactiveObject = GetComponent<InteractiveObject>();
        }
    }
}