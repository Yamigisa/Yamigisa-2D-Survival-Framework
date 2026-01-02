using System.Collections;
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
    [SerializeField] private string hurtFront = "HurtFront";
    [SerializeField] private string hurtBack = "HurtBack";
    [SerializeField] private string hurtSide = "HurtSide";

    [Header("Death Animation Parameters")]
    [SerializeField] private string deathFront = "DeathFront";
    [SerializeField] private string deathBack = "DeathBack";
    [SerializeField] private string deathSide = "DeathSide";

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

    private Destroyable destroyable;
    private bool isDead;

    private void OnEnable()
    {
        destroyable = GetComponent<Destroyable>();
        if (destroyable != null)
            destroyable.OnKilled += OnDestroyableKilled;
    }

    private void OnDisable()
    {
        if (destroyable != null)
            destroyable.OnKilled -= OnDestroyableKilled;
    }

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
        if (isDead) return;
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
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (rb != null && rb.linearVelocity.sqrMagnitude <= 0.0001f)
        {
            SetAnimation(AnimMode.Idle, lastAnimDir);
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

        if (!IsFinite(nextPos))
        {
            rb.linearVelocity = Vector2.zero;
            return;
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
                attackFailed = true;

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
                    Character.instance.TakeDamage(animalData.attackDamage);

                isAttacking = false;
                attackTimer = 0f;
                attackFailed = false;
            }

            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (dist > animalData.attackRange)
        {
            Vector2 dir = toTarget.normalized;
            Vector2 nextPos = rb.position + dir * animalData.runSpeed * Time.fixedDeltaTime;

            if (!IsFinite(nextPos))
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            rb.MovePosition(nextPos);
            SetAnimation(AnimMode.Run, dir);
            FaceDirection(dir);
            return;
        }

        if (attackCooldownTimer <= 0f)
        {
            attackCooldownTimer = animalData.attackCooldown;
            isAttacking = true;
            attackTimer = 0f;
            attackFailed = false;
            rb.linearVelocity = Vector2.zero;
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

        if (!isWanderStopped)
        {
            wanderTimer += Time.deltaTime;

            Vector2 current = rb.position;
            Vector2 targetPos = (Vector2)wanderTarget;

            if (!IsFinite(current) || !IsFinite(targetPos))
            {
                rb.linearVelocity = Vector2.zero;
                PickNewWanderTarget();
                return;
            }

            float step = animalData.wanderSpeed * Time.fixedDeltaTime;
            Vector2 next = Vector2.MoveTowards(current, targetPos, step);

            if (!IsFinite(next))
            {
                rb.linearVelocity = Vector2.zero;
                PickNewWanderTarget();
                return;
            }

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
        if (!IsFinite(r) || r <= 0f)
            r = defaultWanderRange > 0f ? defaultWanderRange : 10f;

        Vector2 offset = Random.insideUnitCircle * r;
        if (!IsFinite(offset))
            offset = Vector2.zero;

        Vector3 target = startPosition + new Vector3(offset.x, offset.y, 0f);
        if (!IsFinite(target))
            target = startPosition;

        wanderTarget = target;
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        lastAnimDir = dir.normalized;
    }

    private void SetAnimation(AnimMode mode, Vector2 dir)
    {
        if (animator == null) return;

        if (dir.sqrMagnitude > 0.0001f)
            lastAnimDir = dir.normalized;

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

        if (mode == AnimMode.Idle)
        {
            bool sideIdle = Mathf.Abs(lastAnimDir.x) > Mathf.Abs(lastAnimDir.y);

            if (spriteRenderer != null)
            {
                if (sideIdle)
                    spriteRenderer.flipX = lastAnimDir.x > 0f;
                else
                    spriteRenderer.flipX = false;
            }

            if (sideIdle)
                animator.SetBool(idleSide, true);
            else if (lastAnimDir.y > 0f)
                animator.SetBool(idleBack, true);
            else
                animator.SetBool(idleFront, true);

            return;
        }

        bool side = Mathf.Abs(lastAnimDir.x) > Mathf.Abs(lastAnimDir.y);

        if (spriteRenderer != null)
        {
            if (side)
                spriteRenderer.flipX = lastAnimDir.x > 0f;
            else
                spriteRenderer.flipX = false;
        }

        if (side)
            animator.SetBool(mode == AnimMode.Run ? runSide : attackSide, true);
        else if (lastAnimDir.y > 0f)
            animator.SetBool(mode == AnimMode.Run ? runBack : attackBack, true);
        else
            animator.SetBool(mode == AnimMode.Run ? runFront : attackFront, true);
    }

    public void OnAttacked()
    {
        isAttacked = true;

        if (animator == null) return;

        animator.SetBool(hurtFront, false);
        animator.SetBool(hurtBack, false);
        animator.SetBool(hurtSide, false);

        bool side = Mathf.Abs(lastAnimDir.x) > Mathf.Abs(lastAnimDir.y);

        if (side)
            animator.SetBool(hurtSide, true);
        else if (lastAnimDir.y > 0f)
            animator.SetBool(hurtBack, true);
        else
            animator.SetBool(hurtFront, true);
    }

    private void OnDestroyableKilled(Destroyable d)
    {
        if (isDead) return;
        isDead = true;

        isAttacking = false;
        attackTimer = 0f;
        attackFailed = false;
        isDetectedLocked = false;
        reactionTimer = 0f;

        rb.linearVelocity = Vector2.zero;

        if (animator == null)
        {
            d.NotifyDeathAnimationFinished();
            return;
        }

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
        animator.SetBool(hurtFront, false);
        animator.SetBool(hurtBack, false);
        animator.SetBool(hurtSide, false);

        animator.SetBool(deathFront, false);
        animator.SetBool(deathBack, false);
        animator.SetBool(deathSide, false);

        bool side = Mathf.Abs(lastAnimDir.x) > Mathf.Abs(lastAnimDir.y);

        if (side)
            animator.SetBool(deathSide, true);
        else if (lastAnimDir.y > 0f)
            animator.SetBool(deathBack, true);
        else
            animator.SetBool(deathFront, true);

        StartCoroutine(DeathAnimThenLoot(d));
    }

    private IEnumerator DeathAnimThenLoot(Destroyable d)
    {
        yield return null;

        float wait = 0.5f;
        if (animator != null)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.length > 0f) wait = st.length;
        }

        yield return new WaitForSeconds(wait);

        if (d != null)
            d.NotifyDeathAnimationFinished();
    }

    private bool IsFinite(float v)
    {
        return !(float.IsNaN(v) || float.IsInfinity(v));
    }

    private bool IsFinite(Vector2 v)
    {
        return IsFinite(v.x) && IsFinite(v.y);
    }

    private bool IsFinite(Vector3 v)
    {
        return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
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
