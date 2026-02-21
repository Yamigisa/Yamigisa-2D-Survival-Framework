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
        private int currentDamage;
        private float cooldownTimer;
        private bool isAttacking;

        public bool IsAttacking => isAttacking;

        private float damageBuff = 0f;

        private void Update()
        {
            if (!isAttacking) return;

            if (characterMovement != null && characterMovement.IsWalking && !characterMovement.IsAutoMoving)
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
                characterMovement?.MoveTo(currentTarget.transform.position, Mathf.Max(0.05f, attackRange * 0.9f));
                return;
            }

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f) return;

            cooldownTimer = attackCooldown;

            int baseDamage = currentDamage;

            if (baseDamage <= 0)
                baseDamage = Mathf.Max(1, Mathf.RoundToInt(handDamage));

            int finalDamage = GetFinalDamage(baseDamage);

            currentTarget.TakeDamage(finalDamage);
        }

        public void StartAutoAttack(Destroyable target, int damagePerHit = 0)
        {
            if (target == null) return;

            currentTarget = target;
            currentDamage = damagePerHit;
            isAttacking = true;
            cooldownTimer = 0f;
        }

        public void StopAttack()
        {
            isAttacking = false;
            currentTarget = null;
            currentDamage = 0;
            cooldownTimer = 0f;
            characterMovement?.StopAutoMoveExternal();
        }


        public void AddDamageBuff(float amount)
        {
            damageBuff += amount;
        }

        public void RemoveDamageBuff(float amount)
        {
            damageBuff -= amount;
        }

        public int GetFinalDamage(int baseDamage)
        {
            return Mathf.RoundToInt(baseDamage + damageBuff);
        }
    }
}
