using UnityEngine;

namespace Yamigisa.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        #region Player Components
        [Header("Player Components")]
        [SerializeField] private Rigidbody2D rb;
        private InputSystem_Actions inputActions;
        #endregion

        #region Movement Variables
        [Header("Movement Variables")]
        [SerializeField] private float walkSpeed = 5f;

        [SerializeField] private bool playerCanMove = true;

        private bool isWalking = false;
        #endregion

        #region Sprint
        [SerializeField] private bool playerCanSprint = true;
        [SerializeField] private bool unlimitedSprint = false;

        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float sprintDuration = 5f;
        [SerializeField] private float sprintCooldown = .5f;

        private float sprintRemaining;
        private float sprintCooldownReset;

        private bool isSprintCooldown = false;
        private bool isSprinting = false;
        #endregion
        private void Awake()
        {
            #region Enable Input System

            inputActions = new InputSystem_Actions();
            inputActions.Player.Enable();

            inputActions.Player.Move.performed += ctx => HandleMovement(ctx.ReadValue<Vector2>());

            inputActions.Player.Sprint.performed += ctx => StartSprint();
            inputActions.Player.Sprint.canceled += ctx => StopSprint();

            #endregion

            if (!unlimitedSprint)
            {
                sprintRemaining = sprintDuration;
                sprintCooldownReset = sprintCooldown;
            }
        }

        private void Update()
        {
            #region Sprint
            if (playerCanSprint)
            {
                if (isSprinting)
                {
                    if (!unlimitedSprint)
                    {
                        sprintRemaining -= 1 * Time.deltaTime;
                        if (sprintRemaining <= 0)
                        {
                            isSprinting = false;
                            isSprintCooldown = true;
                        }
                    }
                }
                else
                {
                    // Regain sprint while not sprinting
                    sprintRemaining = Mathf.Clamp(sprintRemaining += 1 * Time.deltaTime, 0, sprintDuration);
                }
            }

            if (isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0)
                {
                    isSprintCooldown = false;
                }
            }
            else
            {
                sprintCooldown = sprintCooldownReset;
            }
            #endregion
        }
        private void FixedUpdate()
        {
            #region Calling Movement Method

            Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
            Vector2 inputDirection = new Vector2(input.x, input.y);
            HandleMovement(inputDirection);
            #endregion
        }
        private void HandleMovement(Vector2 inputDirection)
        {
            if (!playerCanMove) return;

            Vector2 targetVelocity = transform.TransformDirection(inputDirection);

            if (isSprinting && sprintRemaining > 0f && !isSprintCooldown)
            {
                targetVelocity *= sprintSpeed;
            }
            else
            {
                targetVelocity *= walkSpeed;
            }

            isWalking = (targetVelocity.x != 0 || targetVelocity.y != 0);

            Vector2 velocity = rb.linearVelocity;
            Vector2 velocityChange = targetVelocity - velocity;

            rb.AddForce(velocityChange, ForceMode2D.Impulse);
        }
        private void StartSprint()
        {
            isSprinting = true;
        }

        private void StopSprint()
        {
            isSprinting = false;
        }
    }
}