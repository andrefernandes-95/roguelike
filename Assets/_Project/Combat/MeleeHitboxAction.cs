using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "MeleeHitboxAction", menuName = "AF/Combat/Melee Hitbox Action")]
    public sealed class MeleeHitboxAction : CombatAction
    {
        [Header("Melee")]
        public int damage = 15;
        public float duration = 0.25f;

        [Header("Presentation")]
        public string animatorTrigger;

        public override void Begin(CombatExecution ctx)
        {
            if (ctx.Hitbox == null)
            {
                return;
            }

            ctx.Hitbox.ConfigureDamage(damage);
            ctx.Hitbox.BeginSwing();
            ctx.Controller.SetActionTimer(duration);
        }

        public override void Tick(CombatExecution ctx, float deltaTime) { }

        public override void End(CombatExecution ctx)
        {
            ctx.Hitbox?.EndSwing();
        }
    }
}
