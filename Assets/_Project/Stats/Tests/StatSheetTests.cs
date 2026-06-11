using AF.Stats;
using NUnit.Framework;

namespace AF.Tests.Stats
{
    public class StatSheetTests
    {
        [Test]
        public void GetTotal_UsesBaseLevel()
        {
            StatSheet sheet = new StatSheet(
                new StatProfile
                {
                    Vitality = 10,
                    Endurance = 8
                }
            );

            Assert.AreEqual(10, sheet.GetTotal(StatId.Vitality));
            Assert.AreEqual(8, sheet.GetTotal(StatId.Endurance));
        }

        [Test]
        public void AddModifiers_IncreasesTotal()
        {
            StatSheet sheet = new StatSheet(StatProfile.Default);
            sheet.AddModifiers("ring_01", new[] { new StatModifier(StatId.Vitality, 3) });
            Assert.AreEqual(3, sheet.GetTotal(StatId.Vitality));
        }

        [Test]
        public void RemoveModifiers_RestoresTotal()
        {
            StatSheet sheet = new StatSheet(StatProfile.Default);
            sheet.AddModifiers("ring_01", new[] { new StatModifier(StatId.Vitality, 5) });
            Assert.AreEqual(5, sheet.GetTotal(StatId.Vitality));
            sheet.RemoveModifiers("ring_01");
            Assert.AreEqual(0, sheet.GetTotal(StatId.Vitality));
        }
    }
}
