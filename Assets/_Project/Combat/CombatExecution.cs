namespace AF.Combat
{
    public sealed class CombatExecution
    {
        public CombatController Controller { get; }
        public CombatActor Actor { get; }
        public Hitbox Hitbox { get; }

        public CombatExecution(CombatController controller, CombatActor actor, Hitbox hitbox)
        {
            Controller = controller;
            Actor = actor;
            Hitbox = hitbox;
        }
    }
}