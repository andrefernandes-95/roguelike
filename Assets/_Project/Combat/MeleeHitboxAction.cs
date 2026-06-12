using AF.Animation;
using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "MeleeHitboxAction", menuName = "AF/Combat/Melee Hitbox Action")]
    public sealed class MeleeHitboxAction : CombatAction
    {
        [Header("Melee")]
        public int damage = 15;

        [Header("Animation")]
        [Tooltip("Animator state name, e.g. Action_LightAttack_01")]
        public string animationStateName = "Action_LightAttack_01";

        [Header("Components")]
        public AnimationPresentationSchedule presentation;

        public override void Begin(CombatExecution ctx)
        {
            if (ctx.Hitbox != null)
            {
                ctx.Hitbox.ConfigureDamage(damage);
            }

            if (ctx.Animator == null
                || !ctx.Animator.TryPlayState(Animator.StringToHash(animationStateName), useRootMotion: true))
            {

                ctx.Scheduler?.StartSchedule(presentation);
                ctx.Controller.CancelActiveAction();
                return;
            }

            // Hitbox open/close via CombatAnimationEvents clip events.
        }

        public override void Tick(CombatExecution ctx, float deltaTime) { }

        public override void End(CombatExecution ctx)
        {
            ctx.Scheduler?.StopSchedule();
            ctx.Hitbox?.EndSwing();
        }
    }
}
