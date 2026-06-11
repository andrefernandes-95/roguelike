using AF.Core;
using UnityEngine;

namespace AF.Character
{
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class CharacterAnimationDriver : MonoBehaviour, IActionAnimator
    {
        [SerializeField] Animator animator;
        [SerializeField] int actionLayer;

        [Header("Settings")]
        [SerializeField] float crossFade = 0.1f;

        public bool IsBusy { get; private set; }
        public bool IsRootMotionActive { get; private set; }

        public bool TryPlayState(int stateHash, bool useRootMotion)
        {
            if (IsBusy)
            {
                return false;
            }

            IsBusy = true;
            IsRootMotionActive = useRootMotion;
            animator.applyRootMotion = useRootMotion;
            animator.CrossFadeInFixedTime(stateHash, crossFade, actionLayer);
            return true;
        }

        public void OnActionComplete()
        {
            IsBusy = false;
            IsRootMotionActive = false;
            animator.applyRootMotion = false;
        }
    }
}
