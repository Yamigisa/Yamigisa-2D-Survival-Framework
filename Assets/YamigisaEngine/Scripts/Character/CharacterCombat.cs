using UnityEngine;

namespace Yamigisa
{
    public class CharacterCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        public float handDamage = 3f;
        public float attackRange = 1f;
        public float attackCooldown = 0.4f;

        private Character character;
        private CharacterMovement characterMovement;

        private Destroyable currentTarget;
        private int currentDamage;
        private float cooldownTimer;
        private bool isAttacking;

        public bool IsAttacking => isAttacking;

        private void Awake()
        {
            character = GetComponent<Character>();
            characterMovement = GetComponent<CharacterMovement>();
        }

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

            int dmg = currentDamage;
            if (dmg <= 0)
                dmg = Mathf.Max(1, Mathf.RoundToInt(handDamage));

            currentTarget.TakeDamage(dmg);
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
    }
}
