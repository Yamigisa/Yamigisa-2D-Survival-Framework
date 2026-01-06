using UnityEngine;

namespace Yamigisa
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterControls))]
    public class CharacterMovement : MonoBehaviour
    {
        [Header("Player Components")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D playerCollider;
        private CharacterControls characterControls;

        [Header("Movement Variables")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private bool playerCanMove = true;

        private Vector2 lastDirection = Vector2.down;
        private bool isWalking = false;

        [Header("Sprint")]
        [SerializeField] private bool playerCanSprint = true;
        [SerializeField] private bool unlimitedSprint = false;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float sprintDuration = 5f;
        [SerializeField] private float sprintCooldown = .5f;

        private float sprintRemaining;
        private float sprintCooldownReset;
        private bool isSprintCooldown = false;

        public bool IsWalking => isWalking;
        public Vector2 LastDirection => lastDirection;

        [Header("Jump")]
        [SerializeField] private bool canJump = true;
        [SerializeField] private float jumpForce = 0.4f;
        [SerializeField] private float jumpDuration = 0.35f;
        [SerializeField] private float jumpDistance = 1.2f;
        [SerializeField] private float obstacleCheckDistance = 1f;

        private float jumpTimer;
        private bool isJumping = false;

        private Vector3 baseSpriteOffset;
        private Vector2 landingPosition;
        private int defaultLayer;

        [Header("Crouch")]
        [SerializeField] private float crouchSpeedMultiplier = 0.45f;
        [SerializeField] private float crouchSpriteScale = 0.7f;

        private bool isCrouching = false;
        private Vector3 originalSpriteScale;

        private bool isAutoMoving = false;
        private Vector2 autoMoveTarget;
        private float autoMoveStoppingDistance = 0.1f;

        public bool IsAutoMoving => isAutoMoving;

        public void StopAutoMoveExternal()
        {
            StopAutoMove();
        }

        private void Start()
        {
            characterControls = Character.instance.characterControls;

            defaultLayer = gameObject.layer;
            baseSpriteOffset = spriteRenderer.transform.localPosition;
            originalSpriteScale = spriteRenderer.transform.localScale;

            if (!unlimitedSprint)
            {
                sprintRemaining = sprintDuration;
                sprintCooldownReset = sprintCooldown;
            }
        }

        private void Update()
        {
            HandleSprint();
            HandleCrouch();
            HandleJump();
        }

        private void FixedUpdate()
        {
            if (isJumping) return;

            if (isAutoMoving && HasManualMoveInput())
            {
                StopAutoMove();
            }

            if (isAutoMoving)
                AutoMove();
            else
                Move();
        }

        private void AutoMove()
        {
            Vector2 currentPos = rb.position;
            Vector2 direction = (autoMoveTarget - currentPos);

            if (direction.sqrMagnitude <= autoMoveStoppingDistance * autoMoveStoppingDistance)
            {
                StopAutoMove();
                return;
            }

            direction.Normalize();

            float currentSpeed = walkSpeed;
            Vector2 targetVelocity = direction * currentSpeed;

            isWalking = true;
            lastDirection = direction;

            Vector2 velocity = rb.linearVelocity;
            Vector2 velocityChange = targetVelocity - velocity;
            rb.AddForce(velocityChange, ForceMode2D.Impulse);
        }

        private void StopAutoMove()
        {
            isAutoMoving = false;
            rb.linearVelocity = Vector2.zero;
        }

        private void Move()
        {
            if (!playerCanMove) return;

            Vector2 inputDirection = Vector2.zero;

            if (characterControls.IsAnyKeyPressed(characterControls.moveUpKey)) inputDirection.y += 1;
            if (characterControls.IsAnyKeyPressed(characterControls.moveDownKey)) inputDirection.y -= 1;
            if (characterControls.IsAnyKeyPressed(characterControls.moveRightKey)) inputDirection.x += 1;
            if (characterControls.IsAnyKeyPressed(characterControls.moveLeftKey)) inputDirection.x -= 1;

            StopAutoMove();
            if (inputDirection.x != 0 && inputDirection.y != 0)
                inputDirection.y = 0;

            bool isPressingSprint = characterControls.IsAnyKeyPressed(characterControls.sprintKey);
            bool canSprint = playerCanSprint && !isSprintCooldown && (unlimitedSprint || sprintRemaining > 0f);

            float currentSpeed = (isPressingSprint && canSprint) ? sprintSpeed : walkSpeed;

            if (isCrouching)
                currentSpeed *= crouchSpeedMultiplier;

            if (isPressingSprint && canSprint && !unlimitedSprint)
                sprintRemaining -= Time.deltaTime;

            Vector2 targetVelocity = inputDirection * currentSpeed;
            isWalking = targetVelocity != Vector2.zero;

            if (isWalking)
                lastDirection = inputDirection;

            Vector2 velocity = rb.linearVelocity;
            Vector2 velocityChange = targetVelocity - velocity;
            rb.AddForce(velocityChange, ForceMode2D.Impulse);
        }

        public void MoveTo(Vector2 target, float stoppingDistance)
        {
            autoMoveTarget = target;
            autoMoveStoppingDistance = stoppingDistance;
            isAutoMoving = true;
        }

        private bool HasManualMoveInput()
        {
            return
                characterControls.IsAnyKeyPressed(characterControls.moveUpKey) ||
                characterControls.IsAnyKeyPressed(characterControls.moveDownKey) ||
                characterControls.IsAnyKeyPressed(characterControls.moveLeftKey) ||
                characterControls.IsAnyKeyPressed(characterControls.moveRightKey);
        }

        private void HandleJump()
        {
            if (!isJumping)
            {
                if (canJump && characterControls.IsAnyKeyPressed(characterControls.jumpKey) && !isCrouching)
                    TryJump();

                return;
            }

            jumpTimer -= Time.deltaTime;
            float t = 1f - (jumpTimer / jumpDuration);

            rb.position = Vector2.Lerp(rb.position, landingPosition, t);

            float arc = jumpForce * (1f - Mathf.Pow(2f * t - 1f, 2f));
            Vector3 offset = baseSpriteOffset;
            offset.y += arc;
            spriteRenderer.transform.localPosition = offset;

            if (jumpTimer <= 0f)
                EndJump();
        }

        private void TryJump()
        {
            if (isJumping) return;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, lastDirection, obstacleCheckDistance, LayerMask.GetMask("Obstacles"));

            if (hit.collider != null)
                landingPosition = hit.point - lastDirection * 0.2f;
            else
                landingPosition = rb.position + lastDirection * jumpDistance;

            gameObject.layer = LayerMask.NameToLayer("NoCollision");

            isJumping = true;
            playerCanMove = false;

            jumpTimer = jumpDuration;
            spriteRenderer.transform.localPosition = baseSpriteOffset;
        }

        private void EndJump()
        {
            isJumping = false;
            playerCanMove = true;

            spriteRenderer.transform.localPosition = baseSpriteOffset;
            gameObject.layer = defaultLayer;
        }

        private void HandleCrouch()
        {
            if (characterControls.IsAnyKeyPressed(characterControls.crouchKey))
            {
                if (!isCrouching && !isJumping)
                {
                    isCrouching = true;
                    spriteRenderer.transform.localScale = originalSpriteScale * crouchSpriteScale;
                    playerCollider.enabled = false;
                }
            }
            else
            {
                if (isCrouching)
                {
                    isCrouching = false;
                    spriteRenderer.transform.localScale = originalSpriteScale;
                    playerCollider.enabled = true;
                }
            }
        }

        private void HandleSprint()
        {
            if (!playerCanSprint || unlimitedSprint) return;

            if (sprintRemaining <= 0f && !isSprintCooldown)
                isSprintCooldown = true;

            if (isSprintCooldown)
            {
                sprintCooldown -= Time.deltaTime;
                if (sprintCooldown <= 0f)
                {
                    sprintCooldown = sprintCooldownReset;
                    sprintRemaining = sprintDuration;
                    isSprintCooldown = false;
                }
            }
            else
            {
                if (!characterControls.IsAnyKeyPressed(characterControls.sprintKey))
                    sprintRemaining = Mathf.Clamp(sprintRemaining + Time.deltaTime, 0, sprintDuration);
            }
        }
    }
}
