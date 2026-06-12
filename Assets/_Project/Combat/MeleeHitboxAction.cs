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
        [Tooltip("Animator state name — same for all clip variations.")]
        public string animationStateName = "Action_LightAttack_01";

        [Tooltip("Per-clip frame cues for every light attack 01 variation.")]
        public AnimationPresentationMap presentationMap;

        public override void Begin(CombatExecution ctx)
        {
            if (ctx.Hitbox != null)
            {
                ctx.Hitbox.ConfigureDamage(damage);
            }

            if (ctx.Animator == null
                || !ctx.Animator.TryPlayState(Animator.StringToHash(animationStateName), useRootMotion: true))
            {
                ctx.Controller.CancelActiveAction();
                return;
            }

            ctx.Presentation?.StartMap(presentationMap);
        }

        public override void Tick(CombatExecution ctx, float deltaTime) { }

        public override void End(CombatExecution ctx)
        {
            ctx.Presentation?.StopMap();
            ctx.Hitbox?.EndSwing();
        }
    }
}
