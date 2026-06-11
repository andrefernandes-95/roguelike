using UnityEngine;

namespace AF.Character
{
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterRootMotionApplier : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] Transform characterRoot;
        [SerializeField] CharacterController characterController;
        [SerializeField] CharacterAnimationDriver driver;

        void OnAnimatorMove()
        {
            if (!animator.applyRootMotion || !driver.IsRootMotionActive)
            {
                return;
            }

            if (characterController != null)
            {
                characterController.Move(animator.deltaPosition);
                characterRoot.rotation *= animator.deltaRotation;
            }
            else if (characterRoot != null)
            {
                characterRoot.position += animator.deltaPosition;
                characterRoot.rotation *= animator.deltaRotation;
            }
        }
    }
}
