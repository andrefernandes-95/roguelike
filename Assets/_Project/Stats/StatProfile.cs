using System;

namespace AF.Stats
{
    [Serializable]
    public struct StatProfile
    {
        public int Vitality;
        public int Endurance;

        public static StatProfile Default => new()
        {
            Vitality = 0,
            Endurance = 0
        };
    }
}
