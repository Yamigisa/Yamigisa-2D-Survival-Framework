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
    [SerializeField] private string idleFront = "IdleFront";
    [SerializeField] private string idleBack = "IdleBack";
    [SerializeField] private string idleSide = "IdleSide";
    [SerializeField] private string wanderFront = "WanderFront";
    [SerializeField] private string wanderBack = "WanderBack";
    [SerializeField] private string wanderSide = "WanderSide";
    [SerializeField] private string runFront = "RunFront";
    [SerializeField] private string runBack = "RunBack";
    [SerializeField] private string runSide = "RunSide";
    [SerializeField] private string attackFront = "AttackFront";
    [SerializeField] private string attackBack = "AttackBack";
    [SerializeField] private string attackSide = "AttackSide";

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

    private bool isAttacking;
    private float attackTimer;
    private bool attackFailed;

    private Vector2 lastAnimDir = Vector2.down;

    private enum AnimMode { Idle, Wander, Run, Attack }

    private float defaultWanderRange;
    private bool ignoreWanderClamp;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        defaultWanderRange = animalData != null ? animalData.wanderRange : 10f;

        PickNewWanderTarget();
    }

    private void Update()
    {
        if (animalData == null || Character.instance == null) return;

        attackCooldownTimer -= Time.deltaTime;

        float activeRange = isDetectedLocked ? animalData.detectedRange : animalData.detectRange;
        bool inVisionNow = DetectCharacter(activeRange);

        if (!inVisionNow)
        {
            if (isAttacking)
            {
                AttackCharacter();
                return;
            }

            reactionTimer = 0f;
            isDetectedLocked = false;

            ignoreWanderClamp = false;
            animalData.wanderRange = defaultWanderRange;
            PickNewWanderTarget();

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
                ignoreWanderClamp = true;
                animalData.wanderRange = Mathf.Infinity;
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
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
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
        if (isAttacking) return;

        Transform target = Character.instance.transform;

        Vector2 awayDir = ((Vector2)transform.position - (Vector2)target.position).normalized;

        if (awayDir.sqrMagnitude < 0.0001f)
            awayDir = Random.insideUnitCircle.normalized;

        Vector2 nextPos = rb.position + awayDir * animalData.runSpeed * Time.fixedDeltaTime;

        if (!ignoreWanderClamp)
        {
            float max = animalData.wanderRange;
            Vector2 offset = nextPos - (Vector2)startPosition;
            if (offset.magnitude > max)
                nextPos = (Vector2)startPosition + offset.normalized * max;
        }

        rb.MovePosition(nextPos);

        SetAnimation(AnimMode.Run, awayDir);
        FaceDirection(awayDir);
    }

    private void AttackCharacter()
    {
        Transform target = Character.instance.transform;

        Vector2 toTarget = (Vector2)(target.position - transform.position);
        float dist = toTarget.magnitude;

        if (isAttacking)
        {
            attackTimer += Time.deltaTime;

            if (!attackFailed && dist > animalData.attackRange)
            {
                attackFailed = true;
                Debug.Log("[Animal] Attack FAILED: player left attack range during attackDuration");
            }

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                SetAnimation(AnimMode.Attack, toTarget.normalized);
                FaceDirection(toTarget.normalized);
            }
            else
            {
                SetAnimation(AnimMode.Attack, lastAnimDir);
            }

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

                isAttacking = false;
                attackTimer = 0f;
                attackFailed = false;

                Debug.Log("[Animal] Attack finished/reset");
            }

            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (dist > animalData.attackRange)
        {
            Vector2 dir = toTarget.normalized;
            Vector2 nextPos = rb.position + dir * animalData.runSpeed * Time.fixedDeltaTime;
            rb.MovePosition(nextPos);

            SetAnimation(AnimMode.Run, dir);
            FaceDirection(dir);
            return;
        }

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            SetAnimation(AnimMode.Attack, toTarget.normalized);
            FaceDirection(toTarget.normalized);
        }
        else
        {
            SetAnimation(AnimMode.Attack, lastAnimDir);
        }

        if (attackCooldownTimer <= 0f)
        {
            attackCooldownTimer = animalData.attackCooldown;

            isAttacking = true;
            attackTimer = 0f;
            attackFailed = false;

            rb.linearVelocity = Vector2.zero;

            Debug.Log($"[Animal] Attack START: holding still for {animalData.attackDuration}s (range={animalData.attackRange})");
        }
    }

    private void Wander()
    {
        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            SetAnimation(AnimMode.Attack, lastAnimDir);
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
            {
                SetAnimation(AnimMode.Wander, dir);
                FaceDirection(dir);
            }
            else
            {
                SetAnimation(AnimMode.Idle, lastAnimDir);
            }

            if (Vector2.Distance(next, targetPos) < 0.1f)
                PickNewWanderTarget();

            if (wanderTimer >= animalData.wanderInterval)
            {
                isWanderStopped = true;
                stopTimer = 0f;
                rb.linearVelocity = Vector2.zero;

                SetAnimation(AnimMode.Idle, lastAnimDir);
            }
        }
        else
        {
            stopTimer += Time.deltaTime;

            rb.linearVelocity = Vector2.zero;

            SetAnimation(AnimMode.Idle, lastAnimDir);

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
        lastAnimDir = dir.normalized;
    }

    private void SetAnimation(AnimMode mode, Vector2 dir)
    {
        if (animator == null) return;

        if (animalData != null &&
            (animalData.behaviour == AnimalBehaviour.Passive ||
             animalData.behaviour == AnimalBehaviour.EscapeAttacked))
        {
            if (mode == AnimMode.Attack)
                mode = AnimMode.Run;
        }

        if (dir.sqrMagnitude > 0.0001f) lastAnimDir = dir.normalized;
        Vector2 d = (dir.sqrMagnitude > 0.0001f) ? dir : lastAnimDir;

        animator.SetBool(idleFront, false);
        animator.SetBool(idleBack, false);
        animator.SetBool(idleSide, false);
        animator.SetBool(wanderFront, false);
        animator.SetBool(wanderBack, false);
        animator.SetBool(wanderSide, false);
        animator.SetBool(runFront, false);
        animator.SetBool(runBack, false);
        animator.SetBool(runSide, false);
        animator.SetBool(attackFront, false);
        animator.SetBool(attackBack, false);
        animator.SetBool(attackSide, false);

        bool isSide = Mathf.Abs(d.x) > Mathf.Abs(d.y);

        if (spriteRenderer != null)
        {
            if (isSide)
                spriteRenderer.flipX = (d.x > 0f);
            else
                spriteRenderer.flipX = false;
        }

        string param = null;

        if (isSide)
        {
            param = mode switch
            {
                AnimMode.Idle => idleSide,
                AnimMode.Wander => wanderSide,
                AnimMode.Run => runSide,
                AnimMode.Attack => attackSide,
                _ => idleSide
            };
        }
        else if (d.y > 0f)
        {
            param = mode switch
            {
                AnimMode.Idle => idleBack,
                AnimMode.Wander => wanderBack,
                AnimMode.Run => runBack,
                AnimMode.Attack => attackBack,
                _ => idleBack
            };
        }
        else
        {
            param = mode switch
            {
                AnimMode.Idle => idleFront,
                AnimMode.Wander => wanderFront,
                AnimMode.Run => runFront,
                AnimMode.Attack => attackFront,
                _ => idleFront
            };
        }

        animator.SetBool(param, true);
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
