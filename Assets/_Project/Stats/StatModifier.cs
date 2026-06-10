namespace AF.Stats
{
    public readonly struct StatModifier
    {
        public StatId Stat { get; }
        public int FlatDelta { get; }

        public StatModifier(StatId stat, int flatDelta)
        {
            Stat = stat;
            FlatDelta = flatDelta;
        }
    }
}
