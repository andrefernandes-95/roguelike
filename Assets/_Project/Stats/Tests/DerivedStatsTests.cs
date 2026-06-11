using AF.Stats;
using NUnit.Framework;

namespace AF.Tests.Stats
{
    public class DerivedStatsTests
    {
        [Test]
        public void MaxHealth_ScalesWithVitality()
        {
            StatSheet sheet = new StatSheet(
                new StatProfile { Vitality = 10, Endurance = 0 }
            );

            Assert.AreEqual(100, DerivedStats.MaxHealth(sheet));
        }

        [Test]
        public void MaxHealth_IncludesEquipmentModifier()
        {
            StatSheet sheet = new StatSheet(
                new StatProfile { Vitality = 10, Endurance = 0 }
            );
            sheet.AddModifiers("gear", new[] { new StatModifier(StatId.Vitality, 2) });

            Assert.AreEqual(120, DerivedStats.MaxHealth(sheet));
        }
    }
}
