using UnityEngine;

namespace AF.Character
{
    [CreateAssetMenu(fileName = "CharacterLocomotionSettings", menuName = "AF/Character/Locomotion Settings")]
    public sealed class CharacterLocomotionSettings : ScriptableObject
    {
        [Header("Move")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 12f;

        [Header("Jump")]
        public float jumpHeight = 1.2f;
        public float gravity = -20f;
        public float jumpTimeout = 0.25f;
        public float groundedStickVelocity = -2f;
    }
}
