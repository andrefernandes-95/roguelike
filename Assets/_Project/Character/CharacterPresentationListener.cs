using AF.Animation;
using AF.Core;
using UnityEngine;

namespace AF.Character
{
    /// <summary>
    /// Character-wide presentation events: action completion and footsteps.
    /// </summary>
    public sealed class CharacterPresentationListener : MonoBehaviour, IAnimationPresentationListener
    {
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

        public void OnAnimationPresentationEvent(string eventName)
        {
            switch (eventName)
            {
                case PresentationEventNames.ActionComplete:
                    actionAnimator?.OnActionComplete();
                    presentationComplete?.OnActionPresentationComplete();
                    break;
                case PresentationEventNames.FootstepLeft:
                    PlayFootstep(leftFootstep);
                    break;
                case PresentationEventNames.FootstepRight:
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
