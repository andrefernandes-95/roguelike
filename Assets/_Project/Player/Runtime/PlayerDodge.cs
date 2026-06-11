using AF.Core;
using UnityEngine;

namespace AF.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerDodge : MonoBehaviour
    {
        [SerializeField] PlayerLocomotionSettings settings;

        PlayerCameraRig cameraRig;


        PlayerMotor motor;
        PlayerInputAdapter input;

        float dodgeTimeRemaining;
        float cooldownRemaining;
        Vector3 dodgeVelocity;
        bool isEnabled = true;

        public bool IsDodging => dodgeTimeRemaining > 0f;

        void Awake()
        {
            cameraRig = FindAnyObjectByType<PlayerCameraRig>(FindObjectsInactive.Include);
            motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputAdapter>();
        }

        void Update()
        {
            if (!isEnabled || settings == null)
            {
                return;
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
            }

            if (dodgeTimeRemaining > 0f)
            {
                TickDodge();
                return;
            }

            if (input.Intent.Dodge && cooldownRemaining <= 0f && !motor.IsLocomotionBusy)
            {
                TryStartDodge(input.Intent.Move);
            }
        }

        public bool TryStartDodge(Vector3 moveInput)
        {
            if (!isEnabled || settings == null || dodgeTimeRemaining > 0f || cooldownRemaining > 0f)
            {
                return false;
            }

            bool backstep = moveInput.sqrMagnitude < 0.01f;
            Vector3 direction;

            if (backstep)
            {
                direction = -transform.forward;
                dodgeTimeRemaining = settings.backstepDuration;
                dodgeVelocity = direction * settings.backstepSpeed;
            }
            else
            {
                direction = LocomotionMath.CameraRelativeMove(moveInput, cameraRig.YawDegrees);
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = transform.forward;
                }

                dodgeTimeRemaining = settings.dodgeDuration;
                dodgeVelocity = direction.normalized * settings.dodgeSpeed;
            }

            motor.SetLocomotionBusy(true);
            cooldownRemaining = settings.dodgeCooldown;
            return true;
        }

        void TickDodge()
        {
            motor.ApplyDodgeDisplacement(dodgeVelocity);
            dodgeTimeRemaining -= Time.deltaTime;

            if (dodgeTimeRemaining <= 0f)
            {
                motor.SetLocomotionBusy(false);
            }
        }

        public void SetDodgeEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (!enabled)
            {
                dodgeTimeRemaining = 0f;
                motor.SetLocomotionBusy(false);
            }
        }
    }
}
