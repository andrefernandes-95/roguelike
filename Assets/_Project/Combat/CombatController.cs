using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] CombatActor actor;
        [SerializeField] Hitbox hitbox;

        CombatExecution execution;
        CombatAction activeAction;
        float actionTimer;

        void Awake()
        {
            IActionAnimator actionAnimator = GetComponent<IActionAnimator>();
            ILocomotionReadout locomotionReadout = GetComponent<ILocomotionReadout>();

            execution = new CombatExecution(this, actor, hitbox, actionAnimator, locomotionReadout);
        }

        void Update()
        {
            if (!IsBusy)
            {
                return;
            }

            actionTimer -= Time.deltaTime;
            activeAction.Tick(execution, Time.deltaTime);

            if (actionTimer <= 0)
            {
                EndActiveAction();
            }
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
            return true;
        }

        public void SetActionTimer(float duration)
        {
            actionTimer = duration;
        }

        void EndActiveAction()
        {
            activeAction?.End(execution);
            activeAction = null;
            actionTimer = 0f;
        }

        public bool IsBusy => activeAction != null;
    }
}
