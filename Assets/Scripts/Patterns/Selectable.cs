using UnityEngine;
using UnityEngine.Events;

namespace Yamigisa
{
    [RequireComponent(typeof(Collider2D))]
    public class Selectable : MonoBehaviour
    {
        public SelectableType type;
        public float use_range = 1f;

        [Header("Action Selectable")]
        public ActionSelectable[] actions;

        [Header("Outline")]
        public GameObject outline;

        [Header("Mouse Events")]
        public UnityAction onHover;
        public UnityAction onSelect;

        [SerializeField] private CircleCollider2D triggerCollider;
        private Collider2D[] colliders;

        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>();

            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = use_range;
            }
        }

        private void Start()
        {
            OnHover(false);
        }

        public void OnHover(bool value)
        {
            if (outline != null)
            {
                outline.SetActive(value);
                onHover?.Invoke();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Debug.Log($"{name} entered interaction range of player!");
                OnHover(true);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Debug.Log($"{name} left interaction range of player!");
                OnHover(false);
            }
        }
    
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, use_range);
        }
    }

    public enum SelectableType
    {
        Interact = 0,
        InteractBound = 5,
        InteractSurface = 10,
        CantInteract = 20,
        CantSelect = 30,
    }
}
