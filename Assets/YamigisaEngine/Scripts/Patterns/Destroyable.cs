using System;
using System.Collections;
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

        [Header("Loot Scatter")]
        [SerializeField] private float lootScatterRadius = 0.5f;

        public event Action<Destroyable> OnKilled;

        private NewInteractiveObject select;

        private bool isDying;
        private bool deathAnimFinished;

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
            if (isDying) return;
            isDying = true;
            deathAnimFinished = false;
            StartCoroutine(KillRoutine());
        }

        public void NotifyDeathAnimationFinished()
        {
            deathAnimFinished = true;
        }

        private IEnumerator KillRoutine()
        {
            OnKilled?.Invoke(this);

            if (OnKilled == null)
            {
                GetLoot();
                Destroy(gameObject);
                yield break;
            }

            float timeout = 10f;
            while (!deathAnimFinished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

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
                        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * lootScatterRadius;
                        Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

                        Instantiate(
                            loot.itemLoot.itemPrefab,
                            spawnPos,
                            Quaternion.identity
                        );
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
