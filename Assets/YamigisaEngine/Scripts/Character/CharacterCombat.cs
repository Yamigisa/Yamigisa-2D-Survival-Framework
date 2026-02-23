using UnityEngine;

namespace Yamigisa
{
    public class CharacterCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        public float handDamage = 3f;
        public float attackRange = 1f;
        public float attackCooldown = 0.4f;

        private CharacterMovement characterMovement;

        private Destroyable currentTarget;

        private float cooldownTimer;
        private bool isAttacking;

        public bool IsAttacking => isAttacking;

        private float damageBuff = 0f;
        private int equipmentDamage = 0;

        private void Awake()
        {
            characterMovement = GetComponent<CharacterMovement>();
        }

        private void Update()
        {
            if (!isAttacking) return;

            if (characterMovement != null &&
                characterMovement.IsWalking &&
                !characterMovement.IsAutoMoving)
            {
                StopAttack();
                return;
            }

            if (currentTarget == null)
            {
                StopAttack();
                return;
            }

            Vector2 toTarget = (Vector2)currentTarget.transform.position - (Vector2)transform.position;
            float rangeSqr = attackRange * attackRange;

            if (toTarget.sqrMagnitude > rangeSqr)
            {
                characterMovement?.MoveTo(
                    currentTarget.transform.position,
                    Mathf.Max(0.05f, attackRange * 0.9f)
                );
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f) return;

            cooldownTimer = attackCooldown;

            int baseDamage = CalculateBaseDamage();
            int finalDamage = GetFinalDamage(baseDamage);

            currentTarget.TakeDamage(finalDamage);
        }

        // ===============================
        // DAMAGE CALCULATION
        // ===============================

        private int CalculateBaseDamage()
        {
            // If equipment provides damage, use it
            if (equipmentDamage > 0)
                return equipmentDamage;

            // Otherwise fallback to hand damage
            return Mathf.Max(1, Mathf.RoundToInt(handDamage));
        }

        public int GetFinalDamage(int baseDamage)
        {
            return Mathf.RoundToInt(baseDamage + damageBuff);
        }

        // ===============================
        // ATTACK CONTROL
        // ===============================

        public void StartAutoAttack(Destroyable target)
        {
            if (target == null) return;

            currentTarget = target;
            isAttacking = true;
            cooldownTimer = 0f;
        }

        public void StopAttack()
        {
            isAttacking = false;
            currentTarget = null;
            cooldownTimer = 0f;

            characterMovement?.StopAutoMoveExternal();
        }

        // ===============================
        // BUFFS
        // ===============================

        public void AddDamageBuff(float amount)
        {
            damageBuff += amount;
        }

        public void RemoveDamageBuff(float amount)
        {
            damageBuff -= amount;
        }

        // ===============================
        // EQUIPMENT
        // ===============================

        public void SetEquipmentDamage(int value)
        {
            equipmentDamage = Mathf.Max(0, value);
        }
    }
}