using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(InteractiveObject))]
    public class Destroyable : MonoBehaviour, ISavable
    {
        [Header("Save ID (DO NOT CHANGE)")]
        [SerializeField] private string id;

        [Header("Stats")]
        public int hp = 100;
        [SerializeField] private int maxHp = 100;

        [Header("Group Items Required")]
        public List<GroupData> requiredItems;

        [Header("Loot")]
        [SerializeField] private List<DestroyableLoot> loots;
        [SerializeField] private bool dropLootOnKill = true;

        [Header("Loot Scatter")]
        [SerializeField] private float lootScatterRadius = 0.5f;

        [Header("Damage Feedback")]
        [SerializeField] private bool flashOnDamage = true;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private float damageFlashDuration = 0.1f;

        public event Action<Destroyable> OnKilled;

        private bool isDying;
        private bool deathAnimFinished;

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine flashRoutine;
        private InteractiveObject interactiveObject;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (maxHp < 1)
                maxHp = 1;

            if (hp > maxHp)
                hp = maxHp;
        }
#endif

        private void Awake()
        {
            interactiveObject = GetComponent<InteractiveObject>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;

            if (maxHp <= 0)
                maxHp = Mathf.Max(1, hp);

            hp = Mathf.Clamp(hp, 0, maxHp);
        }

        public void TakeDamage(int damage)
        {
            if (isDying) return;

            hp -= damage;

            TriggerDamageFlash();

            if (hp <= 0)
                Kill();
        }

        private void TriggerDamageFlash()
        {
            if (!flashOnDamage) return;
            if (spriteRenderer == null) return;

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(DamageFlashRoutine());
        }

        private IEnumerator DamageFlashRoutine()
        {
            spriteRenderer.color = damageFlashColor;

            yield return new WaitForSeconds(damageFlashDuration);

            spriteRenderer.color = originalColor;
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

            float timeout = 10f;
            while (OnKilled != null && !deathAnimFinished && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            GetLoot();

            if (interactiveObject != null)
            {
                hp = maxHp;
                isDying = false;
                deathAnimFinished = false;

                interactiveObject.HandleHarvested();
            }
            else
            {
                gameObject.SetActive(false);
            }

            Character.instance.characterCombat.StopAttack();
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

        public void ResetDestroyableStateAfterRegrow()
        {
            hp = maxHp;
            isDying = false;
            deathAnimFinished = false;

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }

        public void Save(ref SaveGameData data)
        {
            if (!data.saveManager.SaveDestroyables)
                return;

            if (data.destroyables == null)
                data.destroyables = new Dictionary<string, DestroyableSaveData>();

            data.destroyables[id] = new DestroyableSaveData
            {
                hp = hp,
                destroyed = !gameObject.activeSelf
            };
        }

        public void Load(SaveGameData data)
        {
            if (data.destroyables == null) return;
            if (!data.destroyables.TryGetValue(id, out var saved)) return;

            hp = saved.hp;

            if (saved.destroyed)
            {
                Debug.Log("gameobject saved :" + saved.destroyed);
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                isDying = false;
                deathAnimFinished = false;
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