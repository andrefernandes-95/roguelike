namespace AF.Stats
{
    public readonly struct DamageRequest
    {
        public int Amount { get; }

        public DamageRequest(int amount)
        {
            Amount = amount;
        }
    }

    public readonly struct DamageResult
    {
        public int DamageDealt { get; }
        public int Remaining { get; }
        public bool Depleted { get; }

        public DamageResult(int damageDealt, int remaining, bool depleted)
        {
            DamageDealt = damageDealt;
            Remaining = remaining;
            Depleted = depleted;
        }

        public static DamageResult None => new(0, -1, false);
    }
}
