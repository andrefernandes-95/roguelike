using UnityEngine;

namespace AF.Player
{
    [CreateAssetMenu(fileName = "PlayerLocomotionSettings", menuName = "AF/Player/Locomotion Settings")]
    public sealed class PlayerLocomotionSettings : ScriptableObject
    {
        [Header("Move")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 12f;

        [Header("Jump")]
        public float jumpHeight = 1.2f;
        public float gravity = -20f;
        public float jumpTimeout = 0.25f;
        public float groundedStickVelocity = -2f;

        [Header("Dodge")]
        public float dodgeSpeed = 8f;
        public float dodgeDuration = 0.4f;
        public float dodgeCooldown = 0.5f;
        public float backstepSpeed = 6f;
        public float backstepDuration = 0.35f;
    }
}
