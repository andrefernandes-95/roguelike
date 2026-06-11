using AF.Character;
using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "DodgeCombatAction", menuName = "AF/Combat/Dodge Action")]
    public sealed class DodgeCombatAction : CombatAction
    {
        [Header("Rules")]
        public float cooldown = 0.5f;

        public override bool CanExecute(CombatExecution ctx)
        {
            if (!base.CanExecute(ctx))
            {
                return false;
            }

            return ctx.Locomotion != null && ctx.Locomotion.IsGrounded;
        }

        public override void Begin(CombatExecution ctx)
        {
            if (ctx.Animator == null || ctx.Locomotion == null)
            {
                return;
            }

            bool backstep = ctx.Locomotion.MoveInput.sqrMagnitude < 0.01f;
            int state = backstep
                ? HumanoidAnimationHashes.StateBackStep
                : HumanoidAnimationHashes.StateRoll;
            if (!ctx.Animator.TryPlayState(state, useRootMotion: true))
            {
                return;
            }

            ctx.Controller.SetActionTimer(0f);
        }

        public override void Tick(CombatExecution ctx, float deltaTime)
        {
        }

        public override void End(CombatExecution ctx)
        {
        }
    }
}
