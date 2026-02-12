using System.Linq;
using Unity.VisualScripting;
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

        public LayerMask interactObjectLayer;
        public static Character instance { get; private set; }

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
            if (pendingInteraction == null) return;

            if (pendingInteraction.IsCharacterInRange(this))
            {
                pendingInteraction.InteractObject(this);
                pendingInteraction = null;
            }
        }

        public bool SetCharacterBusy(bool _isBusy)
        {
            if (_isBusy == true)
                DisableMovements();
            else
                EnableMovements();

            return IsBusy = _isBusy;
        }

        public bool CharacterIsBusy()
        {
            return IsBusy;
        }

        public void SetPendingInteraction(InteractiveObject obj)
        {
            pendingInteraction = obj;

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

            int damagePerHit = 0;
            if (canAttackWithTool)
                damagePerHit = Mathf.Max(1, equipped.damage);

            characterCombat.StartAutoAttack(destroyable, damagePerHit);

            float stopDist = Mathf.Max(0.05f, characterCombat.attackRange * 0.9f);
            characterMovement.MoveTo(destroyable.transform.position, stopDist);

            pendingInteraction = null;
        }

        public void ConsumeItem(ItemData itemData)
        {
            characterAttribute.AddCurrentAttributeValue(AttributeType.Health, itemData.increaseHealth);
            characterAttribute.AddCurrentAttributeValue(AttributeType.Hunger, itemData.increaseHunger);
            characterAttribute.AddCurrentAttributeValue(AttributeType.Thirst, itemData.increaseThirst);
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

        public void Save(ref SaveGameData data)
        {
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
