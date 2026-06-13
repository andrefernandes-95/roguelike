using AF.Animation;
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// Shared combat executor for player and AI. Does not read input.
    /// Actions end via presentation ActionComplete or locomotion hub SMB — not timers.
    /// </summary>
    public sealed class CombatController : MonoBehaviour, IActionPresentationComplete, IAnimationPresentationListener
    {
        [SerializeField] CombatActor actor;
        [SerializeField] Hitbox hitbox;

        CombatExecution execution;
        CombatAction activeAction;

        void Awake()
        {
            IActionAnimator actionAnimator = GetComponent<IActionAnimator>();
            ILocomotionReadout locomotionReadout = GetComponent<ILocomotionReadout>();
            PresentationScheduler presentation = GetComponent<PresentationScheduler>();
            execution = new CombatExecution(
              this,
              actor,
              hitbox,
              actionAnimator,
              locomotionReadout,
              presentation);
        }

        void Update()
        {
            if (!IsBusy)
            {
                return;
            }

            activeAction.Tick(execution, Time.deltaTime);
        }

        public bool TryStart(CombatAction action)
        {
            if (action == null || IsBusy)
            {
                return false;
            }

            if (!action.CanExecute(execution))
            {
                return false;
            }

            activeAction = action;
            activeAction.Begin(execution);
            return IsBusy;
        }

        public void CancelActiveAction()
        {
            if (!IsBusy)
            {
                return;
            }

            EndActiveAction();
        }

        public void OnActionPresentationComplete()
        {
            if (!IsBusy)
            {
                return;
            }

            EndActiveAction();
        }

        public void OnAnimationPresentationEvent(string eventName)
        {
            switch (eventName)
            {
                case PresentationEventNames.HitboxOpen:
                    hitbox?.BeginSwing();
                    break;
                case PresentationEventNames.HitboxClose:
                    hitbox?.EndSwing();
                    break;
            }
        }

        void EndActiveAction()
        {
            activeAction?.End(execution);
            activeAction = null;
        }

        public bool IsBusy => activeAction != null;
    }
}
