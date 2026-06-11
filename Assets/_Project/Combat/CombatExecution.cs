using AF.Core;

namespace AF.Combat
{
    public sealed class CombatExecution
    {
        public CombatController Controller { get; }
        public CombatActor Actor { get; }
        public Hitbox Hitbox { get; }
        public IActionAnimator Animator { get; }
        public ILocomotionReadout Locomotion { get; }

        public CombatExecution(
            CombatController controller,
            CombatActor actor,
            Hitbox hitbox,
            IActionAnimator actionAnimator,
            ILocomotionReadout locomotionReadout)
        {
            Controller = controller;
            Actor = actor;
            Hitbox = hitbox;
            Animator = actionAnimator;
            Locomotion = locomotionReadout;
        }
    }
}
