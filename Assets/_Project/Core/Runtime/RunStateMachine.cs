using System;

namespace AF.Core
{
    public sealed class RunStateMachine
    {
        public RunState State { get; private set; } = RunState.Boot;

        public event Action<RunState> StateEntered;

        public void GoTo(RunState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateEntered?.Invoke(next);
        }
    }
}
