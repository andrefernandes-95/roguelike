using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "MeleeHitboxAction", menuName = "AF/Combat/Melee Hitbox Action")]
    public sealed class MeleeHitboxAction : CombatAction
    {
        [Header("Melee")]
        public int damage = 15;

        [Header("Presentation")]
        public string animationName = "LightAttack1";

        public override void Begin(CombatExecution ctx)
        {
            if (ctx.Hitbox == null)
            {
                return;
            }

            if (ctx.Animator == null
                || !ctx.Animator.TryPlayState(Animator.StringToHash(animationName), useRootMotion: true))
            {
                return;
            }

            ctx.Hitbox.ConfigureDamage(damage);
            ctx.Hitbox.BeginSwing();
            ctx.Controller.SetActionTimer(0f);
        }

        public override void Tick(CombatExecution ctx, float deltaTime) { }

        public override void End(CombatExecution ctx)
        {
            ctx.Hitbox?.EndSwing();
        }
    }
}
