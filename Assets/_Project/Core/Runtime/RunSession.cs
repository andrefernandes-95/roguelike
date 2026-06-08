namespace AF.Core
{
    /// <summary>
    /// Mutable data for the current run.
    /// Lives for one attempt.
    /// </summary>
    public sealed class RunSession
    {
        public int Seed { get; private set; }
        public int FloorIndex { get; private set; }

        public void Begin(int seed)
        {
            Seed = seed;
            FloorIndex = 0;
        }

        public void NextFloor()
        {
            FloorIndex++;
        }
    }
}
