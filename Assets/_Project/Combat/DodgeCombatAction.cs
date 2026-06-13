using AF.Animation;
using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "DodgeCombatAction", menuName = "AF/Combat/Dodge Action")]
    public sealed class DodgeCombatAction : CombatAction
    {
        [Header("Animation")]
        public string rollStateName = "Roll";
        public string backstepStateName = "Backstep";
        public AnimationPresentationMap rollPresentationMap;
        public AnimationPresentationMap backstepPresentationMap;

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
                ctx.Controller.CancelActiveAction();
                return;
            }

            bool backstep = ctx.Locomotion.MoveInput.sqrMagnitude < 0.01f;
            string stateName = backstep ? backstepStateName : rollStateName;
            int state = Animator.StringToHash(stateName);

            if (!ctx.Animator.TryPlayState(state, useRootMotion: true))
            {
                ctx.Controller.CancelActiveAction();
                return;
            }

            AnimationPresentationMap map = backstep ? backstepPresentationMap : rollPresentationMap;
            ctx.Presentation?.StartMap(map);
        }

        public override void Tick(CombatExecution ctx, float deltaTime) { }

        public override void End(CombatExecution ctx)
        {
            ctx.Presentation?.StopMap();
        }
    }
}
