using System.Collections;
using System.Linq;
using UnityEngine;

namespace Yamigisa
{
    public class Character : MonoBehaviour, ISavable
    {
        public CharacterAnimation characterAnimation { get; private set; }
        public CharacterAttribute characterAttribute { get; private set; }
        public CharacterMovement characterMovement { get; private set; }
        public CharacterCombat characterCombat { get; private set; }
        public CharacterControls characterControls { get; private set; }
        public bool IsBusy { get; private set; }

        private InteractiveObject pendingInteraction;
        private int pendingActionIndex = -1;

        public LayerMask interactObjectLayer = 1 << 7;
        public static Character instance { get; private set; }

        private Coroutine busyDelayRoutine;

        private void Awake()
        {
            instance = this;

            characterAnimation = GetComponent<CharacterAnimation>();
            characterAttribute = GetComponent<CharacterAttribute>();
            characterMovement = GetComponent<CharacterMovement>();
            characterCombat = GetComponent<CharacterCombat>();
            characterControls = GetComponent<CharacterControls>();
        }

        public Character GetCharacter()
        {
            return this;
        }

        private void Update()
        {
            HandleMouseInteraction();

            if (pendingInteraction == null)
                return;

            if (pendingInteraction.IsCharacterInRange(this))
            {
                ExecutePendingInteraction();
            }
        }

        private void ExecutePendingInteraction()
        {
            if (pendingInteraction == null)
                return;

            if (pendingInteraction.Actions == null || pendingInteraction.Actions.Count == 0)
            {
                ClearPendingInteraction();
                return;
            }

            int actionIndex = pendingActionIndex;

            if (actionIndex < 0 || actionIndex >= pendingInteraction.Actions.Count)
                actionIndex = 0;

            ActionBase action = pendingInteraction.Actions[actionIndex];
            if (action == null)
            {
                ClearPendingInteraction();
                return;
            }

            if (!action.CanDoAction(pendingInteraction))
            {
                ClearPendingInteraction();
                return;
            }

            action.DoAction(this, pendingInteraction);
            TextTooltip.Instance.CloseInteractiveObjectTexts();

            ClearPendingInteraction();
        }

        private void ClearPendingInteraction()
        {
            pendingInteraction = null;
            pendingActionIndex = -1;
        }

        public bool SetCharacterBusy(bool _isBusy, float delayIfFalse = 0.1f)
        {
            if (_isBusy)
            {
                if (busyDelayRoutine != null)
                {
                    StopCoroutine(busyDelayRoutine);
                    busyDelayRoutine = null;
                }

                DisableMovements();
                IsBusy = true;
                return true;
            }
            else
            {
                if (delayIfFalse > 0f)
                {
                    if (busyDelayRoutine != null)
                        StopCoroutine(busyDelayRoutine);

                    busyDelayRoutine = StartCoroutine(DelayedUnbusy(delayIfFalse));
                }
                else
                {
                    EnableMovements();
                    IsBusy = false;
                }

                return false;
            }
        }

        private IEnumerator DelayedUnbusy(float delay)
        {
            yield return new WaitForSeconds(delay);

            EnableMovements();
            IsBusy = false;
            busyDelayRoutine = null;
        }

        public bool CharacterIsBusy()
        {
            return IsBusy;
        }

        public void SetPendingInteraction(InteractiveObject obj, int actionIndex = -1)
        {
            pendingInteraction = obj;
            pendingActionIndex = actionIndex;

            if (obj == null) return;
            if (characterCombat == null || characterMovement == null) return;

            Destroyable destroyable = obj.GetComponent<Destroyable>();
            if (destroyable == null) return;

            ItemData equipped = Inventory.Instance != null ? Inventory.Instance.GetSelectedQuickItemData() : null;

            bool canAttackBareHand = (destroyable.requiredItems == null || destroyable.requiredItems.Count == 0);

            bool canAttackWithTool =
                equipped != null &&
                equipped.groups != null &&
                destroyable.requiredItems != null &&
                equipped.groups.Any(g => destroyable.requiredItems.Contains(g));

            if (!canAttackBareHand && !canAttackWithTool) return;

            characterCombat.StartAutoAttack(destroyable);

            float stopDist = Mathf.Max(0.05f, characterCombat.attackRange * 0.9f);
            characterMovement.MoveTo(destroyable.transform.position, stopDist);

            ClearPendingInteraction();
        }

        public void ConsumeItem(ItemData itemData)
        {
            foreach (var effect in itemData.consumableEffects)
            {
                switch (effect.effectType)
                {
                    case ConsumableEffectType.Instant:
                        characterAttribute.AddCurrentAttributeValue(
                            effect.attributeType,
                            effect.instantAmount
                        );
                        break;

                    case ConsumableEffectType.OverTime:
                        StartCoroutine(ApplyOverTime(effect));
                        break;

                    case ConsumableEffectType.DurationBuff:
                        StartCoroutine(ApplyBuff(effect));
                        break;
                }
            }
        }

        public IEnumerator ApplyOverTime(ConsumableEffect effect)
        {
            float elapsed = 0f;

            while (elapsed < effect.duration)
            {
                characterAttribute.AddCurrentAttributeValue(effect.attributeType, effect.amountPerTick);

                yield return new WaitForSeconds(effect.tickInterval);
                elapsed += effect.tickInterval;
            }
        }

        private IEnumerator ApplyBuff(ConsumableEffect effect)
        {
            switch (effect.buffType)
            {
                case BuffType.MovementSpeedMultiplier:
                    characterMovement.AddSpeedBuff(effect.buffAmount);
                    yield return new WaitForSeconds(effect.duration);
                    characterMovement.RemoveSpeedBuff(effect.buffAmount);
                    break;

                case BuffType.DamageMultiplier:
                    characterCombat.AddDamageBuff(effect.buffAmount);
                    yield return new WaitForSeconds(effect.duration);
                    characterCombat.RemoveDamageBuff(effect.buffAmount);
                    break;
            }
        }

        public void TakeDamage(int damage)
        {
            characterAttribute.AddCurrentAttributeValue(AttributeType.Health, -damage);

            AttributeData health = characterAttribute.GetAttributeData(AttributeType.Health);

            if (health != null && health.CurrentValue <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            DisableMovements();
            SetCharacterBusy(true);
            GameManager.instance.OnCharacterDeath();
        }

        public void DisableMovements()
        {
            characterMovement.DisableAllMovements();
        }

        public void EnableMovements()
        {
            characterMovement.EnableAllMovements();
        }

        private void LateUpdate()
        {
            HandleWorldInteractionClick();
        }

        private static readonly Collider2D[] interactHits = new Collider2D[16];

        private void HandleWorldInteractionClick()
        {
            if (IsBusy) return;

            int requestedActionIndex = characterControls.GetPressedInteractionActionIndexDown();
            if (requestedActionIndex < 0)
                return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = interactObjectLayer;
            filter.useTriggers = true;

            int count = Physics2D.OverlapPoint(mouseWorld, filter, interactHits);

            if (count <= 0)
                return;

            InteractiveObject best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D col = interactHits[i];
                if (col == null) continue;

                InteractiveObject obj = col.GetComponent<InteractiveObject>();
                if (obj == null)
                    obj = col.GetComponentInParent<InteractiveObject>();

                if (obj == null) continue;

                if (obj.Actions == null) continue;
                if (requestedActionIndex >= obj.Actions.Count) continue;
                if (obj.Actions[requestedActionIndex] == null) continue;

                float dist = Vector2.Distance(mouseWorld, col.ClosestPoint(mouseWorld));
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = obj;
                }
            }

            if (best != null)
                SetPendingInteraction(best, requestedActionIndex);
        }

        private void HandleMouseInteraction()
        {
            if (IsBusy) return;

            int requestedActionIndex = characterControls.GetPressedInteractionActionIndexDown();
            if (requestedActionIndex < 0)
                return;

            if (PlaceableSystem.instance != null)
            {
                if (PlaceableSystem.instance.IsInBuildMode) return;
                if (PlaceableSystem.instance.IsInteractionBlocked) return;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);

            if (hits == null || hits.Length == 0)
                return;

            InteractiveObject bestObject = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null) continue;
                if (!hit.enabled) continue;

                if (PlaceableSystem.instance != null &&
                    PlaceableSystem.instance.IsPlacingObject &&
                    hit.GetComponentInParent<Placeable>() != null &&
                    !hit.GetComponentInParent<Placeable>().Placed)
                {
                    continue;
                }

                InteractiveObject obj = hit.GetComponent<InteractiveObject>();
                if (obj == null)
                    obj = hit.GetComponentInParent<InteractiveObject>();

                if (obj == null)
                    continue;

                if (obj.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                if (obj.Actions == null) continue;
                if (requestedActionIndex >= obj.Actions.Count) continue;
                if (obj.Actions[requestedActionIndex] == null) continue;

                Vector2 closest = hit.ClosestPoint(mouseWorld);
                float dist = (mouseWorld - closest).sqrMagnitude;

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestObject = obj;
                }
            }

            if (bestObject != null)
            {
                if (bestObject.IsCharacterInRange(this))
                {
                    pendingInteraction = bestObject;
                    pendingActionIndex = requestedActionIndex;
                    ExecutePendingInteraction();
                }
                else
                {
                    characterMovement.MoveTo(bestObject.transform.position, bestObject.interactRange);
                    SetPendingInteraction(bestObject, requestedActionIndex);
                }
            }
        }

        public void Save(ref SaveGameData data)
        {
            if (!data.saveManager.SavePlayer)
                return;

            data.player = new CharacterData
            {
                position = transform.position,
                rotation = transform.rotation,
                attributes = characterAttribute.GetSaveData()
            };
        }

        public void Load(SaveGameData data)
        {
            transform.position = data.player.position;
            transform.rotation = data.player.rotation;

            characterAttribute.LoadFromSaveData(data.player.attributes);
        }
    }
}