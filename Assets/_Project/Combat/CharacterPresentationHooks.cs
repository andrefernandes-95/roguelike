using AF.Animation;
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class CharacterPresentationHooks : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] Hitbox attackHitbox;

        [Header("Footsteps")]
        [SerializeField] AudioSource footstepSource;
        [SerializeField] AudioClip leftFootstep;
        [SerializeField] AudioClip rightFootstep;

        IActionAnimator actionAnimator;
        IActionPresentationComplete presentationComplete;

        void Awake()
        {
            actionAnimator = GetComponent<IActionAnimator>();
            presentationComplete = GetComponent<IActionPresentationComplete>();
        }

        public void Fire(PresentationCueType type)
        {
            switch (type)
            {
                case PresentationCueType.HitboxOpen:
                    attackHitbox?.BeginSwing();
                    break;
                case PresentationCueType.HitboxClose:
                    attackHitbox?.EndSwing();
                    break;
                case PresentationCueType.DodgeIframesBegin:
                    break;
                case PresentationCueType.DodgeIframesEnd:
                    break;
                case PresentationCueType.ActionComplete:
                    actionAnimator?.OnActionComplete();
                    presentationComplete?.OnActionPresentationComplete();
                    break;
                case PresentationCueType.FootstepLeft:
                    PlayFootstep(leftFootstep);
                    break;
                case PresentationCueType.FootstepRight:
                    PlayFootstep(rightFootstep);
                    break;
            }
        }

        void PlayFootstep(AudioClip clip)
        {
            if (footstepSource != null && clip != null)
            {
                footstepSource.PlayOneShot(clip);
            }
        }
    }
}
