using System;

namespace AF.Stats
{
    public sealed class ResourcePool
    {
        public int Max { get; private set; }
        public int Current { get; private set; }
        public bool IsEmpty => Current <= 0;

        public ResourcePool(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            Max = max;
            Current = max;
        }

        public void RefreshMax(int newMax)
        {
            if (newMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newMax));
            }

            Max = newMax;
            Current = Math.Min(Current, Max);
        }

        public void Fill()
        {
            Current = Max;
        }

        public bool TrySpend(int amount)
        {
            if (IsEmpty || amount <= 0 || Current < amount)
            {
                return false;
            }

            Current -= amount;
            return true;
        }

        public DamageResult ApplyDamage(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                return DamageResult.None;
            }

            int dealt = Math.Min(amount, Current);
            Current -= dealt;
            return new DamageResult(dealt, Current, Current <= 0);
        }
    }
}
