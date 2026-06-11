using AF.Stats;
using NUnit.Framework;

namespace AF.Tests.Stats
{
    public class ResourcePoolTests
    {
        [Test]
        public void ApplyDamage_ReducesCurrent()
        {
            ResourcePool pool = new ResourcePool(100);
            DamageResult result = pool.ApplyDamage(30);
            Assert.AreEqual(30, result.DamageDealt);
            Assert.AreEqual(70, result.Remaining);
            Assert.IsFalse(result.Depleted);
        }

        [Test]
        public void ApplyDamage_ToZero_SetsDepleted()
        {
            ResourcePool pool = new ResourcePool(100);
            DamageResult result = pool.ApplyDamage(100);
            Assert.IsTrue(result.Depleted);
            Assert.IsTrue(pool.IsEmpty);
        }

        [Test]
        public void RefreshMax_ClampsCurrent()
        {
            ResourcePool pool = new ResourcePool(100);
            pool.ApplyDamage(40); // current pool becomes 60
            pool.RefreshMax(50); // change max pool to 50

            Assert.AreEqual(50, pool.Max);
            Assert.AreEqual(50, pool.Current);
        }

        [Test]
        public void TrySpend_FailsWhenInsufficient()
        {
            ResourcePool pool = new ResourcePool(10);

            Assert.IsFalse(pool.TrySpend(15));
            Assert.AreEqual(10, pool.Current);
        }
    }
}
