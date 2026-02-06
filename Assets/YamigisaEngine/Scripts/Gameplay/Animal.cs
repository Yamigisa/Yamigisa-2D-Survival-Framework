using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yamigisa;

[RequireComponent(typeof(Rigidbody2D))]
public class Animal : MonoBehaviour
{
    [Header("Animal Data")]
    public AnimalData animalData;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [Header("Idle Animation Parameters")]
    [SerializeField] private string idleFront = "IdleFront";
    [SerializeField] private string idleBack = "IdleBack";
    [SerializeField] private string idleSide = "IdleSide";
    [Header("Wander Animation Parameters")]
    [SerializeField] private string wanderFront = "WanderFront";
    [SerializeField] private string wanderBack = "WanderBack";
    [SerializeField] private string wanderSide = "WanderSide";
    [Header("Run Animation Parameters")]
    [SerializeField] private string runFront = "RunFront";
    [SerializeField] private string runBack = "RunBack";
    [SerializeField] private string runSide = "RunSide";
    [Header("Attack Animation Parameters")]
    [SerializeField] private string attackFront = "AttackFront";
    [SerializeField] private string attackBack = "AttackBack";
    [SerializeField] private string attackSide = "AttackSide";
    [Header("Hurt Animation Parameters")]
    [SerializeField] private string hurtFront = "HurtFront";
    [SerializeField] private string hurtBack = "HurtBack";
    [SerializeField] private string hurtSide = "HurtSide";

    [Header("Death Animation Parameters")]
    [SerializeField] private string deathFront = "DeathFront";
    [SerializeField] private string deathBack = "DeathBack";
    [SerializeField] private string deathSide = "DeathSide";

    private Vector3 wanderCenter;
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

    private Destroyable destroyable;
    private bool isDead;

    private int lastKnownHp;

    private Transform characterTf;

    private int hIdleFront, hIdleBack, hIdleSide;
    private int hWanderFront, hWanderBack, hWanderSide;
    private int hRunFront, hRunBack, hRunSide;
    private int hAttackFront, hAttackBack, hAttackSide;
    private int hHurtFront, hHurtBack, hHurtSide;
    private int hDeathFront, hDeathBack, hDeathSide;

    private HashSet<int> boolParams;
    private int currentAnimHash = 0;
    private bool currentFlipX = false;

    private float aiTickTimer;
    private float aiTickInterval = 0.066f;
    private bool cachedInVision;
    private float cachedActiveRange;
    private float cachedCosHalfAngle;
    private float cachedAngle;
    private void OnEnable()
    {
        destroyable = GetComponent<Destroyable>();
        if (destroyable != null)
        {
            destroyable.OnKilled += OnDestroyableKilled;
            lastKnownHp = destroyable.hp;
        }
    }

    private void OnDisable()
    {
        if (destroyable != null)
            destroyable.OnKilled -= OnDestroyableKilled;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        defaultWanderRange = animalData != null ? animalData.wanderRange : 0f;

        wanderCenter = transform.position;

        isWanderStopped = false;
        wanderTimer = 0f;
        stopTimer = 0f;

        PickNewWanderTarget();

        hIdleFront = Animator.StringToHash(idleFront);
        hIdleBack = Animator.StringToHash(idleBack);
        hIdleSide = Animator.StringToHash(idleSide);

        hWanderFront = Animator.StringToHash(wanderFront);
        hWanderBack = Animator.StringToHash(wanderBack);
        hWanderSide = Animator.StringToHash(wanderSide);

        hRunFront = Animator.StringToHash(runFront);
        hRunBack = Animator.StringToHash(runBack);
        hRunSide = Animator.StringToHash(runSide);

        hAttackFront = Animator.StringToHash(attackFront);
        hAttackBack = Animator.StringToHash(attackBack);
        hAttackSide = Animator.StringToHash(attackSide);

        hHurtFront = Animator.StringToHash(hurtFront);
        hHurtBack = Animator.StringToHash(hurtBack);
        hHurtSide = Animator.StringToHash(hurtSide);

        hDeathFront = Animator.StringToHash(deathFront);
        hDeathBack = Animator.StringToHash(deathBack);
        hDeathSide = Animator.StringToHash(deathSide);

        boolParams = new HashSet<int>(64);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var ps = animator.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].type == AnimatorControllerParameterType.Bool)
                    boolParams.Add(Animator.StringToHash(ps[i].name));
            }
        }

        aiTickTimer = Random.Range(0f, aiTickInterval);
        cachedActiveRange = -1f;
        cachedAngle = -1f;
        cachedCosHalfAngle = 0f;

        if (Character.instance != null)
            characterTf = Character.instance.transform;
    }

    private void Update()
    {
        if (isDead) return;
        if (animalData == null || Character.instance == null) return;

        if (characterTf == null || characterTf != Character.instance.transform)
            characterTf = Character.instance.transform;

        if (destroyable != null)
        {
            if (destroyable.hp < lastKnownHp)
                OnAttacked();
            lastKnownHp = destroyable.hp;
        }

        attackCooldownTimer -= Time.deltaTime;

        if (isAttacking)
        {
            AttackCharacter();
            return;
        }

        aiTickTimer -= Time.deltaTime;
        bool doAITick = aiTickTimer <= 0f;
        if (doAITick)
        {
            aiTickTimer = aiTickInterval;

            float activeRange = isDetectedLocked ? animalData.detectedRange : animalData.detectRange;
            cachedActiveRange = activeRange;
            cachedInVision = DetectCharacter(activeRange);
        }

        if (!doAITick && isDetectedLocked)
        {
            ReactDetected();
            return;
        }

        if (!cachedInVision)
        {
            reactionTimer = 0f;
            isDetectedLocked = false;

            isAttacked = false;

            animalData.wanderRange = defaultWanderRange;

            Wander();
            return;
        }

        if (!isDetectedLocked)
        {
            reactionTimer += aiTickInterval;

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
        if (isDead || isAttacking)
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
        if (characterTf == null) return false;

        Vector2 toTarget = (Vector2)characterTf.position - (Vector2)transform.position;

        float r = range;
        if (toTarget.sqrMagnitude > r * r) return false;

        float angle = animalData.detectAngle;
        if (!IsFinite(angle)) return false;

        angle = Mathf.Clamp(angle, 0f, 360f);
        if (angle >= 360f) return true;

        if (angle != cachedAngle)
        {
            cachedAngle = angle;
            float halfAngleRad = (angle * 0.5f) * Mathf.Deg2Rad;
            cachedCosHalfAngle = Mathf.Cos(halfAngleRad);
        }

        Vector2 forward = (Vector2)transform.right;
        float fSqr = forward.sqrMagnitude;
        if (fSqr <= 0.0001f) return true;

        float tSqr = toTarget.sqrMagnitude;
        if (tSqr <= 0.0001f) return true;

        float dot = Vector2.Dot(forward, toTarget);
        float cos = dot / Mathf.Sqrt(fSqr * tSqr);

        return cos >= cachedCosHalfAngle;
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
                Wander();
                if (isAttacked) EscapeFromCharacter();
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
        if (characterTf == null) return;

        Vector2 awayDir = ((Vector2)transform.position - (Vector2)characterTf.position).normalized;

        if (awayDir.sqrMagnitude < 0.0001f)
            awayDir = Random.insideUnitCircle.normalized;

        Vector2 nextPos = rb.position + awayDir * animalData.runSpeed * Time.fixedDeltaTime;

        float max = animalData.wanderRange;
        if (max > 0f)
        {
            Vector2 offset = nextPos - (Vector2)wanderCenter;
            if (offset.magnitude > max)
                nextPos = (Vector2)wanderCenter + offset.normalized * max;
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
        if (characterTf == null) return;

        Vector2 toTarget = (Vector2)characterTf.position - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        if (isAttacking)
        {
            attackTimer += Time.deltaTime;

            if (!attackFailed && dist > animalData.attackRange)
                attackFailed = true;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector2 d = toTarget.normalized;
                SetAnimation(AnimMode.Attack, d);
                FaceDirection(d);
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
            Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget / dist : Vector2.zero;
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
        if (!isWanderStopped)
        {
            wanderTimer += Time.deltaTime;

            Vector2 current = rb.position;
            Vector2 target = wanderTarget;

            Vector2 next = Vector2.MoveTowards(current, target, animalData.wanderSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);

            Vector2 dir = target - current;
            if (dir.sqrMagnitude > 0.0001f)
            {
                SetAnimation(AnimMode.Wander, dir);
                FaceDirection(dir);
            }
            else
            {
                SetAnimation(AnimMode.Idle, lastAnimDir);
            }

            if ((next - target).sqrMagnitude < 0.01f)
                PickNewWanderTarget();

            if (wanderTimer >= animalData.wanderInterval)
            {
                isWanderStopped = true;
                stopTimer = 0f;
                SetAnimation(AnimMode.Idle, lastAnimDir);
            }
        }
        else
        {
            stopTimer += Time.deltaTime;
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

        float r = animalData.wanderRange;
        if (!IsFinite(r) || r <= 0.01f)
            r = defaultWanderRange > 0f ? defaultWanderRange : 5f;

        Vector2 offset = Random.insideUnitCircle * r;

        if (!IsFinite(offset))
            offset = Vector2.zero;

        wanderTarget = wanderCenter + new Vector3(offset.x, offset.y, 0f);

        if (!IsFinite(wanderTarget))
            wanderTarget = wanderCenter;
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        lastAnimDir = dir.normalized;
    }

    private void SetAnimation(AnimMode mode, Vector2 dir)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        if (dir.sqrMagnitude > 0.0001f)
            lastAnimDir = dir.normalized;

        bool side = Mathf.Abs(lastAnimDir.x) > Mathf.Abs(lastAnimDir.y);

        if (spriteRenderer != null)
        {
            bool flip = side && lastAnimDir.x > 0f;
            if (flip != currentFlipX)
            {
                currentFlipX = flip;
                spriteRenderer.flipX = flip;
            }
        }

        int desired = 0;

        switch (mode)
        {
            case AnimMode.Idle:
                desired = side ? hIdleSide : (lastAnimDir.y > 0f ? hIdleBack : hIdleFront);
                break;

            case AnimMode.Wander:
                desired = side ? hWanderSide : (lastAnimDir.y > 0f ? hWanderBack : hWanderFront);
                break;

            case AnimMode.Run:
                desired = side ? hRunSide : (lastAnimDir.y > 0f ? hRunBack : hRunFront);
                break;

            case AnimMode.Attack:
                desired = side ? hAttackSide : (lastAnimDir.y > 0f ? hAttackBack : hAttackFront);
                break;
        }

        if (desired == 0) return;
        if (desired == currentAnimHash) return;

        if (currentAnimHash != 0 && boolParams != null && boolParams.Contains(currentAnimHash))
            animator.SetBool(currentAnimHash, false);

        currentAnimHash = desired;

        if (boolParams == null || boolParams.Contains(currentAnimHash))
            animator.SetBool(currentAnimHash, true);
    }

    private void SafeSetBool(string param, bool value)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(param)) return;

        int h = Animator.StringToHash(param);
        if (boolParams != null && !boolParams.Contains(h)) return;

        animator.SetBool(h, value);
    }

    public void OnAttacked()
    {
        isAttacked = true;

        if (animator == null) return;

        bool side = Mathf.Abs(lastAnimDir.x) > Mathf.Abs(lastAnimDir.y);
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

        if (currentAnimHash != 0 && boolParams != null && boolParams.Contains(currentAnimHash))
            animator.SetBool(currentAnimHash, false);
        currentAnimHash = 0;

        if (boolParams != null)
        {
            if (boolParams.Contains(hIdleFront)) animator.SetBool(hIdleFront, false);
            if (boolParams.Contains(hIdleBack)) animator.SetBool(hIdleBack, false);
            if (boolParams.Contains(hIdleSide)) animator.SetBool(hIdleSide, false);

            if (boolParams.Contains(hWanderFront)) animator.SetBool(hWanderFront, false);
            if (boolParams.Contains(hWanderBack)) animator.SetBool(hWanderBack, false);
            if (boolParams.Contains(hWanderSide)) animator.SetBool(hWanderSide, false);

            if (boolParams.Contains(hRunFront)) animator.SetBool(hRunFront, false);
            if (boolParams.Contains(hRunBack)) animator.SetBool(hRunBack, false);
            if (boolParams.Contains(hRunSide)) animator.SetBool(hRunSide, false);

            if (boolParams.Contains(hAttackFront)) animator.SetBool(hAttackFront, false);
            if (boolParams.Contains(hAttackBack)) animator.SetBool(hAttackBack, false);
            if (boolParams.Contains(hAttackSide)) animator.SetBool(hAttackSide, false);

            if (boolParams.Contains(hHurtFront)) animator.SetBool(hHurtFront, false);
            if (boolParams.Contains(hHurtBack)) animator.SetBool(hHurtBack, false);
            if (boolParams.Contains(hHurtSide)) animator.SetBool(hHurtSide, false);

            if (boolParams.Contains(hDeathFront)) animator.SetBool(hDeathFront, false);
            if (boolParams.Contains(hDeathBack)) animator.SetBool(hDeathBack, false);
            if (boolParams.Contains(hDeathSide)) animator.SetBool(hDeathSide, false);
        }

        bool side = Mathf.Abs(lastAnimDir.x) > Mathf.Abs(lastAnimDir.y);

        int death = side ? hDeathSide : (lastAnimDir.y > 0f ? hDeathBack : hDeathFront);

        if (boolParams == null || boolParams.Contains(death))
            animator.SetBool(death, true);

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
