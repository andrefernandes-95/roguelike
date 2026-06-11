using AF.Stats;
using NUnit.Framework;

namespace AF.Tests.Stats
{
    public class DamageResolverTests
    {
        [Test]
        public void Resolve_DelegatesToPool()
        {
            ResourcePool pool = new ResourcePool(50);

            DamageResult result = DamageResolver.Resolve(pool, new DamageRequest(15));
            Assert.AreEqual(15, result.DamageDealt);
            Assert.AreEqual(35, pool.Current);
        }
    }
}
