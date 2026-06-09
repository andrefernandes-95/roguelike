using UnityEngine;

namespace AF.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float gravity = -20f;

        CharacterController controller;
        PlayerInputAdapter input;
        float verticalVelocity;
        bool isEnabled = true;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputAdapter>();
        }

        void Update()
        {
            if (!isEnabled)
            {
                return;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            Vector2 moveInput = input.Intent.Move;
            Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y);

            // Normalize diagonal input so movement speed stays consistent (e.g., (1,0,1) would otherwise be √2 times faster than (1,0,0)).
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            if (direction.sqrMagnitude > 0.01f && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 forward = cam.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = cam.right;
                right.y = 0f;
                right.Normalize();

                direction = forward * direction.z + right * direction.x;
            }

            Vector3 velocity = direction * moveSpeed;
            verticalVelocity += gravity * Time.deltaTime;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        public void SetMotorEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
    }
}
