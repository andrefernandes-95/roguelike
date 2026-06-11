using AF.Character;
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    /// <summary>Maps player intent + camera yaw into CharacterMotor. AI uses its own steering adapter.</summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class PlayerLocomotionInput : MonoBehaviour, ILocomotionReadout
    {
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] CharacterMotor motor;

        PlayerCameraRig cameraRig;

        bool isEnabled;

        public Vector2 MoveInput => input != null ? input.Intent.Move : Vector2.zero;
        public bool IsGrounded => motor != null && motor.IsGrounded;

        void Awake()
        {
            cameraRig = FindAnyObjectByType<PlayerCameraRig>(FindObjectsInactive.Include);
        }

        void Update()
        {
            if (!isEnabled || input == null || motor == null)
            {
                return;
            }

            PlayerIntent intent = input.Intent;
            float yaw = cameraRig != null ? cameraRig.YawDegrees : 0f;
            Vector3 worldDir = LocomotionMath.CameraRelativeMove(intent.Move, yaw);
            motor.SetWorldMove(worldDir, intent.Move.magnitude);

            if (intent.Jump)
            {
                motor.RequestJump();
            }
        }

        public void SetLocomotionInputEnabled(bool enabled) => isEnabled = enabled;
    }
}
