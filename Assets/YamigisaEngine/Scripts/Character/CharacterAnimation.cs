using UnityEngine;

namespace Yamigisa
{
    public class CharacterAnimation : MonoBehaviour
    {
        [Header("Animator Parameters")]
        [SerializeField] private string idleFront = "IdleFront";
        [SerializeField] private string idleBack = "IdleBack";
        [SerializeField] private string idleSide = "IdleSide";
        [SerializeField] private string walkFront = "WalkFront";
        [SerializeField] private string walkBack = "WalkBack";
        [SerializeField] private string walkSide = "WalkSide";

        private Animator animator;
        private CharacterMovement movement;
        private Transform spriteTransform;

        void Start()
        {
            animator = GetComponent<Animator>();
            movement = GetComponent<CharacterMovement>();
            spriteTransform = GetComponent<Transform>();
        }

        void Update()
        {
            Vector2 dir = movement.LastDirection;
            bool isWalking = movement.IsWalking;

            // Reset all bools first
            animator.SetBool(idleFront, false);
            animator.SetBool(idleBack, false);
            animator.SetBool(idleSide, false);
            animator.SetBool(walkFront, false);
            animator.SetBool(walkBack, false);
            animator.SetBool(walkSide, false);

            if (isWalking)
            {
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    animator.SetBool(walkSide, true);

                    // flip sprite for left/right
                    if (dir.x < 0)
                        spriteTransform.localScale = new Vector3(-1, 1, 1);
                    else
                        spriteTransform.localScale = new Vector3(1, 1, 1);
                }
                else if (dir.y > 0)
                {
                    animator.SetBool(walkBack, true);
                }
                else
                {
                    animator.SetBool(walkFront, true);
                }
            }
            else
            {
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    animator.SetBool(idleSide, true);

                    // flip sprite for left/right
                    if (dir.x < 0)
                        spriteTransform.localScale = new Vector3(-1, 1, 1);
                    else
                        spriteTransform.localScale = new Vector3(1, 1, 1);
                }
                else if (dir.y > 0)
                {
                    animator.SetBool(idleBack, true);
                }
                else
                {
                    animator.SetBool(idleFront, true);
                }
            }
        }
    }
}