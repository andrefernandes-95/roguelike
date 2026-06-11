using AF.Player;
using UnityEngine;

namespace AF.Character
{
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class CharacterLocomotionView : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] CharacterMotor motor;
        [SerializeField] CharacterAnimationDriver driver;
        [SerializeField] float dampTime = 0.1f;

        void Update()
        {
            if (animator == null || motor == null)
            {
                return;
            }

            bool canDrive = driver == null || !driver.IsBusy;

            animator.SetBool(HumanoidAnimationHashes.Grounded, motor.IsGrounded);
            animator.SetBool(HumanoidAnimationHashes.FreeFall, !motor.IsGrounded && motor.VerticalVelocity <= 0.1f);

            float speed = canDrive ? motor.HorizontalSpeed : 0f;
            animator.SetFloat(HumanoidAnimationHashes.Speed, speed, dampTime, Time.deltaTime);
        }

        public void NotifyJumpTriggered()
        {
            if (animator != null)
            {
                animator.SetTrigger(HumanoidAnimationHashes.Jump);
            }
        }
    }
}
