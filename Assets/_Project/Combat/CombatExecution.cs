using AF.Animation;
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
        public PresentationScheduler Presentation { get; }

        public CombatExecution(
            CombatController controller,
            CombatActor actor,
            Hitbox hitbox,
            IActionAnimator actionAnimator,
            ILocomotionReadout locomotionReadout,
            PresentationScheduler presentation)
        {
            Controller = controller;
            Actor = actor;
            Hitbox = hitbox;
            Animator = actionAnimator;
            Locomotion = locomotionReadout;
            Presentation = presentation;
        }
    }
}
