using UnityEngine;

namespace Yamigisa
{
    public enum AnimalState
    {
        Wander,
        Alerted,
        Escape,
        Attack,
        Dead
    }

    public enum AnimalBehaviour
    {
        Passive,
        EscapeAttacked,
        DefenseAttacked,
        Aggressive
    }

    [CreateAssetMenu(fileName = "Animal", menuName = "Yamigisa/Animal", order = 50)]
    public class AnimalData : ScriptableObject
    {
        [Header("Behaviour")]
        [Tooltip(
           "Passive: Escape when character is near.\n" +
           "EscapeAttacked: Escape when attacked. \n" +
           "DefenseAttacked: Attack when attacked. \n" +
           "Aggressive: Attack when character is near."
       )]
        public AnimalBehaviour behaviour = AnimalBehaviour.Passive;

        [Header("Move")]
        public float wanderSpeed = 2f;
        public float runSpeed = 3.5f;
        public float wanderRange = 10f;
        public float wanderInterval = 5f;
        public float continueWander = 1.5f;

        [Header("Vision")]
        public float detectRange = 2.5f;
        public float detectedRange = 10f;
        public float detectAngle = 360f;
        public float reactionTime = 1f;

        [Header("Attack")]
        public int attackDamage = 5;
        public float attackRange = 1.5f;
        public float attackDuration = 1f;
        public float attackCooldown = 2f;
    }
}