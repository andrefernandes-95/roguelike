using UnityEngine;

namespace AF.Combat
{
    public abstract class CombatAction : ScriptableObject
    {
        [Header("Costs")]
        public int staminaCost;

        [Header("Combo")]
        public CombatAction next;

        public virtual bool CanExecute(CombatExecution ctx)
        {
            return ctx != null && ctx.Controller != null && !ctx.Controller.IsBusy;
        }

        public abstract void Begin(CombatExecution ctx);
        public abstract void Tick(CombatExecution ctx, float deltaTime);
        public abstract void End(CombatExecution ctx);
    }
}
