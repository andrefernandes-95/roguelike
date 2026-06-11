using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class CombatAnimationEvents : MonoBehaviour
    {
        [SerializeField] Hitbox attackHitbox;

        IActionAnimator actionAnimator;
        IActionPresentationComplete presentationComplete;

        void Awake()
        {
            actionAnimator = GetComponentInParent<IActionAnimator>();
            presentationComplete = GetComponentInParent<IActionPresentationComplete>();
        }

        public void OnHitboxOpen() => attackHitbox?.BeginSwing();

        public void OnHitboxClose() => attackHitbox?.EndSwing();

        public void OnDodgeIframesBegin() { }

        public void OnDodgeIframesEnd() { }

        public void OnActionComplete()
        {
            actionAnimator?.OnActionComplete();
            presentationComplete?.OnActionPresentationComplete();
        }
    }
}
