using AF.Animation;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// Handles combat-only presentation events (hitbox, dodge iframes).
    /// </summary>
    public sealed class CombatPresentationListener : MonoBehaviour, IAnimationPresentationListener
    {
        [SerializeField] Hitbox attackHitbox;

        public void OnAnimationPresentationEvent(string eventName)
        {
            switch (eventName)
            {
                case PresentationEventNames.HitboxOpen:
                    attackHitbox?.BeginSwing();
                    break;
                case PresentationEventNames.HitboxClose:
                    attackHitbox?.EndSwing();
                    break;
                case PresentationEventNames.DodgeIframesBegin:
                    break;
                case PresentationEventNames.DodgeIframesEnd:
                    break;
            }
        }
    }
}
