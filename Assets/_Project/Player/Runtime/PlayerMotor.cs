using AF.Core;
using UnityEngine;

namespace AF.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] PlayerLocomotionSettings settings;

        PlayerCameraRig cameraRig;
        CharacterController controller;
        PlayerInputAdapter input;

        float verticalVelocity;
        float jumpTimeoutDelta;
        bool isEnabled = false;

        public bool IsGrounded => controller != null && controller.isGrounded;
        public bool IsLocomotionBusy
        {
            get;
            private set;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputAdapter>();
            cameraRig = FindAnyObjectByType<PlayerCameraRig>(FindObjectsInactive.Include);
        }

        void Update()
        {
            if (!isEnabled || settings == null)
            {
                return;
            }

            if (IsLocomotionBusy)
            {
                ApplyGravity();
                controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
                return;
            }

            UpdateJumpTimeout();
            HandleJump();
            ApplyGravity();

            Vector3 horizontal = LocomotionMath.CameraRelativeMove(
                input.Intent.Move,
                cameraRig.YawDegrees);

            if (horizontal.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    settings.rotationSpeed * Time.deltaTime
                );
            }

            Vector3 velocity = horizontal * settings.moveSpeed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        void HandleJump()
        {
            if (!input.Intent.Jump)
            {
                return;
            }

            if (!IsGrounded || jumpTimeoutDelta > 0f)
            {
                return;
            }

            verticalVelocity = LocomotionMath.ComputeJumpVelocity(settings.jumpHeight, settings.gravity);
            jumpTimeoutDelta = settings.jumpTimeout;
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

        public void ApplyDodgeDisplacement(Vector3 worldVelocity)
        {
            if (!isEnabled)
            {
                return;
            }

            worldVelocity.y = verticalVelocity;
            controller.Move(worldVelocity * Time.deltaTime);
        }

        public void SetLocomotionBusy(bool busy)
        {
            IsLocomotionBusy = busy;
        }

        public void SetMotorEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
    }
}
