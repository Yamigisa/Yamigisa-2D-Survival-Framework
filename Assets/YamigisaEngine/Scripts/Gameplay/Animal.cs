using UnityEngine;
using Yamigisa;

[RequireComponent(typeof(Rigidbody2D))]
public class Animal : MonoBehaviour
{
    [Header("Animal Data")]
    public AnimalData animalData;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Animation Parameters")]
    private Vector3 startPosition;

    private Rigidbody2D rb;

    private float wanderTimer;
    private Vector3 wanderTarget;

    private bool isWanderStopped;
    private float stopTimer;

    private bool isAttacked;
    private float attackCooldownTimer;

    private float reactionTimer;
    private bool isDetectedLocked;

    // NEW: attack state
    private bool isAttacking;
    private float attackTimer;
    private bool attackFailed;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        PickNewWanderTarget();
    }

    private void Update()
    {
        if (animalData == null || Character.instance == null) return;

        attackCooldownTimer -= Time.deltaTime;

        // Always use detectRange so leaving detectRange stops reacting immediately.
        bool inVisionNow = DetectCharacter(animalData.detectRange);

        if (!inVisionNow)
        {
            // ✅ CHANGE: DO NOT cancel the attack.
            // Let it finish its attack windup (staying still), then once done it will wander again.
            if (isAttacking)
            {
                // Keep progressing the attack timer even when player is outside detectRange.
                // AttackCharacter() will keep the animal still and will resolve success/fail at attackDuration.
                AttackCharacter();
                return;
            }

            reactionTimer = 0f;
            isDetectedLocked = false;
            Wander();
            return;
        }

        if (!isDetectedLocked)
        {
            reactionTimer += Time.deltaTime;

            if (reactionTimer < animalData.reactionTime)
            {
                Wander();
                return;
            }

            if (DetectCharacter(animalData.detectRange))
            {
                isDetectedLocked = true;
            }
            else
            {
                reactionTimer = 0f;
                isDetectedLocked = false;
                Wander();
                return;
            }
        }

        ReactDetected();
    }

    private void FixedUpdate()
    {
        // If currently attacking, force stop movement during attack duration
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero; // use rb.velocity if your Unity doesn't have linearVelocity
        }
    }

    private bool DetectCharacter(float range)
    {
        if (animalData == null || Character.instance == null) return false;

        Transform target = Character.instance.transform;

        Vector2 toTarget = (Vector2)(target.position - transform.position);

        float r = range;
        if (toTarget.sqrMagnitude > r * r) return false;

        float angle = Mathf.Clamp(animalData.detectAngle, 0f, 360f);
        if (angle >= 360f) return true;

        Vector2 forward = transform.right;
        float halfAngle = angle * 0.5f;
        float angleToTarget = Vector2.Angle(forward, toTarget);

        return angleToTarget <= halfAngle;
    }

    private void ReactDetected()
    {
        switch (animalData.behaviour)
        {
            case AnimalBehaviour.Passive:
                EscapeFromCharacter();
                break;

            case AnimalBehaviour.Aggressive:
                AttackCharacter();
                break;

            case AnimalBehaviour.EscapeAttacked:
                if (isAttacked) EscapeFromCharacter();
                else Wander();
                break;

            case AnimalBehaviour.DefenseAttacked:
                if (isAttacked) AttackCharacter();
                else Wander();
                break;
        }
    }

    private void EscapeFromCharacter()
    {
        // If attacking, do not move
        if (isAttacking) return;

        Transform target = Character.instance.transform;

        Vector2 awayDir = ((Vector2)transform.position - (Vector2)target.position).normalized;

        if (awayDir.sqrMagnitude < 0.0001f)
            awayDir = Random.insideUnitCircle.normalized;

        Vector2 nextPos = rb.position + awayDir * animalData.runSpeed * Time.fixedDeltaTime;
        float max = animalData.wanderRange;
        Vector2 offset = nextPos - (Vector2)startPosition;
        if (offset.magnitude > max)
            nextPos = (Vector2)startPosition + offset.normalized * max;

        rb.MovePosition(nextPos);
        FaceDirection(awayDir);
    }

    private void AttackCharacter()
    {
        Transform target = Character.instance.transform;

        Vector2 toTarget = (Vector2)(target.position - transform.position);
        float dist = toTarget.magnitude;

        // If already in an attack sequence: stay still, check fail condition, and resolve when duration ends
        if (isAttacking)
        {
            attackTimer += Time.deltaTime;

            // If player leaves range while attack is charging/animating -> fail
            if (!attackFailed && dist > animalData.attackRange)
            {
                attackFailed = true;
                Debug.Log("[Animal] Attack FAILED: player left attack range during attackDuration");
            }

            // Keep facing target during attack (optional)
            if (toTarget.sqrMagnitude > 0.0001f)
                FaceDirection(toTarget.normalized);

            // End of attack duration -> apply damage only if not failed
            if (attackTimer >= animalData.attackDuration)
            {
                if (!attackFailed)
                {
                    Debug.Log($"[Animal] Attack SUCCESS: dealing {animalData.attackDamage} damage");
                    Character.instance.TakeDamage(animalData.attackDamage);
                }
                else
                {
                    Debug.Log("[Animal] Attack ended with FAIL: no damage applied");
                }

                // reset attack state
                isAttacking = false;
                attackTimer = 0f;
                attackFailed = false;

                Debug.Log("[Animal] Attack finished/reset");
            }

            // While attacking, do not move
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Not attacking yet: move closer if not in attack range
        if (dist > animalData.attackRange)
        {
            Vector2 dir = toTarget.normalized;
            Vector2 nextPos = rb.position + dir * animalData.runSpeed * Time.fixedDeltaTime;
            rb.MovePosition(nextPos);
            FaceDirection(dir);
            return;
        }

        // In range -> start attack if cooldown allows
        FaceDirection(toTarget.normalized);

        if (attackCooldownTimer <= 0f)
        {
            attackCooldownTimer = animalData.attackCooldown;

            // Start attack sequence
            isAttacking = true;
            attackTimer = 0f;
            attackFailed = false;

            // Freeze immediately
            rb.linearVelocity = Vector2.zero;

            Debug.Log($"[Animal] Attack START: holding still for {animalData.attackDuration}s (range={animalData.attackRange})");
        }
    }

    private void Wander()
    {
        // If attacking, do not wander
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (animalData == null) return;

        if (!isWanderStopped)
        {
            wanderTimer += Time.deltaTime;

            Vector2 current = rb.position;
            Vector2 targetPos = (Vector2)wanderTarget;

            float step = animalData.wanderSpeed * Time.fixedDeltaTime;
            Vector2 next = Vector2.MoveTowards(current, targetPos, step);
            rb.MovePosition(next);

            Vector2 dir = (targetPos - current);
            if (dir.sqrMagnitude > 0.0001f)
                FaceDirection(dir);

            if (Vector2.Distance(next, targetPos) < 0.1f)
                PickNewWanderTarget();

            if (wanderTimer >= animalData.wanderInterval)
            {
                isWanderStopped = true;
                stopTimer = 0f;
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            stopTimer += Time.deltaTime;

            rb.linearVelocity = Vector2.zero;

            if (stopTimer >= animalData.continueWander)
            {
                isWanderStopped = false;
                wanderTimer = 0f;
                PickNewWanderTarget();
            }
        }
    }

    private void PickNewWanderTarget()
    {
        wanderTimer = 0f;

        float r = animalData != null ? animalData.wanderRange : 10f;
        Vector2 offset = Random.insideUnitCircle * r;
        wanderTarget = startPosition + new Vector3(offset.x, offset.y, 0f);
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    public void OnAttacked()
    {
        isAttacked = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (animalData == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, animalData.detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, animalData.detectedRange);

        float angle = Mathf.Clamp(animalData.detectAngle, 0f, 360f);
        if (angle < 360f)
        {
            Vector3 forward = transform.right;
            float half = angle * 0.5f;

            Vector3 left = Quaternion.Euler(0f, 0f, -half) * forward;
            Vector3 right = Quaternion.Euler(0f, 0f, half) * forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + left.normalized * animalData.detectRange);
            Gizmos.DrawLine(transform.position, transform.position + right.normalized * animalData.detectRange);
        }
    }
#endif
}
