using AF.Core;
using UnityEngine;

namespace AF.Character
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField] CharacterLocomotionSettings settings;
        [SerializeField] CharacterAnimationDriver animationDriver;
        [SerializeField] CharacterController controller;
        [SerializeField] CharacterLocomotionView locomotionView;

        Vector3 worldMoveDirection;
        float moveMagnitude;
        float verticalVelocity;
        float jumpTimeoutDelta;
        bool jumpRequested;
        bool isEnabled = true;

        public Vector2 MoveInput => new Vector2(worldMoveDirection.x, worldMoveDirection.z);
        public bool IsGrounded => controller != null && controller.isGrounded;
        public float HorizontalSpeed => moveMagnitude * (settings != null ? settings.moveSpeed : 0f);
        public float VerticalVelocity => verticalVelocity;

        void Update()
        {
            if (!isEnabled || settings == null)
            {
                return;
            }

            if (animationDriver != null && animationDriver.IsBusy)
            {
                ApplyGravityOnly();
                return;
            }

            UpdateJumpTimeout();
            HandleJump();
            ApplyGravity();

            Vector3 horizontal = worldMoveDirection * moveMagnitude * settings.moveSpeed;
            if (horizontal.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    settings.rotationSpeed * Time.deltaTime);
            }

            Vector3 velocity = horizontal;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        /// <summary>Called by PlayerLocomotionInput or AI steering.</summary>
        public void SetWorldMove(Vector3 direction, float magnitude)
        {
            worldMoveDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
            moveMagnitude = Mathf.Clamp01(magnitude);
        }

        public void RequestJump()
        {
            jumpRequested = true;
        }

        void HandleJump()
        {
            if (!jumpRequested)
            {
                return;
            }

            jumpRequested = false;
            if (!IsGrounded || jumpTimeoutDelta > 0f)
            {
                return;
            }

            verticalVelocity = LocomotionMath.ComputeJumpVelocity(settings.jumpHeight, settings.gravity);
            jumpTimeoutDelta = settings.jumpTimeout;
            locomotionView?.NotifyJumpTriggered();
        }

        void UpdateJumpTimeout()
        {
            if (jumpTimeoutDelta > 0f)
            {
                jumpTimeoutDelta -= Time.deltaTime;
            }
        }

        void ApplyGravity()
        {
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = settings.groundedStickVelocity;
            }

            verticalVelocity += settings.gravity * Time.deltaTime;
        }

        void ApplyGravityOnly()
        {
            ApplyGravity();
            controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        }

        public void SetMotorEnabled(bool enabled) => isEnabled = enabled;
    }
}
