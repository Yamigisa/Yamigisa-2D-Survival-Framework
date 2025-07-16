using System.Collections.Generic;
using UnityEngine;
using Yamigisa;

namespace Yamigisa.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterControls))]
    public class PlayerMovement : MonoBehaviour
    {
        #region Player Components
        [Header("Player Components")]
        [SerializeField] private Rigidbody2D rb;
        private CharacterControls characterControls;
        #endregion

        #region Movement Variables
        [Header("Movement Variables")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private bool playerCanMove = true;
        private bool isWalking = false;
        #endregion

        #region Sprint
        [Header("Sprint")]
        [SerializeField] private bool playerCanSprint = true;
        [SerializeField] private bool unlimitedSprint = false;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float sprintDuration = 5f;
        [SerializeField] private float sprintCooldown = .5f;

        private float sprintRemaining;
        private float sprintCooldownReset;
        private bool isSprintCooldown = false;
        #endregion

        private void Awake()
        {
            characterControls = GetComponent<CharacterControls>();

            if (!unlimitedSprint)
            {
                sprintRemaining = sprintDuration;
                sprintCooldownReset = sprintCooldown;
            }
        }

        private void Update()
        {
            HandleSprint();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (!playerCanMove) return;

            Vector2 inputDirection = Vector2.zero;

            if (characterControls.IsAnyKeyPressed(characterControls.moveUpKey)) inputDirection.y += 1;
            if (characterControls.IsAnyKeyPressed(characterControls.moveDownKey)) inputDirection.y -= 1;
            if (characterControls.IsAnyKeyPressed(characterControls.moveRightKey)) inputDirection.x += 1;
            if (characterControls.IsAnyKeyPressed(characterControls.moveLeftKey)) inputDirection.x -= 1;

            inputDirection = inputDirection.normalized;

            bool isPressingSprint = characterControls.IsAnyKeyPressed(characterControls.sprintKey);
            bool canSprint = playerCanSprint && !isSprintCooldown && (unlimitedSprint || sprintRemaining > 0f);
            float currentSpeed = (isPressingSprint && canSprint) ? sprintSpeed : walkSpeed;

            if (isPressingSprint && canSprint && !unlimitedSprint)
                sprintRemaining -= Time.deltaTime;

            Vector2 targetVelocity = inputDirection * currentSpeed;
            isWalking = targetVelocity != Vector2.zero;

            Vector2 velocity = rb.linearVelocity;
            Vector2 velocityChange = targetVelocity - velocity;
            rb.AddForce(velocityChange, ForceMode2D.Impulse);
        }

        private void HandleSprint()
        {
            if (!playerCanSprint || unlimitedSprint) return;

            if (sprintRemaining <= 0f && !isSprintCooldown)
            {
                isSprintCooldown = true;
            }

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
                {
                    sprintRemaining = Mathf.Clamp(sprintRemaining + Time.deltaTime, 0, sprintDuration);
                }
            }
        }
    }
}
