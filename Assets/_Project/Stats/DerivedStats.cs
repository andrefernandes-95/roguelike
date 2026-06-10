namespace AF.Stats
{
    public static class DerivedStats
    {
        public const int HealthPerVitality = 10;
        public const int StaminaPerEndurance = 5;

        public static int MaxHealth(StatSheet sheet)
        {
            return sheet.GetTotal(StatId.Vitality) * HealthPerVitality;
        }

        public static int MaxEndurance(StatSheet sheet)
        {
            return sheet.GetTotal(StatId.Endurance) * StaminaPerEndurance;
        }
    }
}
