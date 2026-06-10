using System;

namespace AF.Stats
{
    [Serializable]
    public struct StatProfile
    {
        public int Vitality;
        public int Endurance;

        public static StatProfile DefaultPlayer => new()
        {
            Vitality = 1,
            Endurance = 1
        };

        public static StatProfile DefaultEnemy => new()
        {
            Vitality = 1,
            Endurance = 1
        };
    }
}
