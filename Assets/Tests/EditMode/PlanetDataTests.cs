using NUnit.Framework;

namespace SolarSystemAR.Tests
{
    public class PlanetDataTests
    {
        [TestCase("earth", "earth")]
        [TestCase("Earth", "earth")]
        [TestCase("  JUPITER ", "jupiter")]
        [TestCase("earth1", "earth")]      // Vuforia target names may carry a numeric suffix.
        [TestCase("venus1", "venus")]
        [TestCase("地球", "earth")]
        [TestCase("太阳系", "solarsystem")]
        public void FindResolvesTargetNames(string targetName, string expectedId)
        {
            var info = PlanetData.Find(targetName);
            Assert.IsNotNull(info, targetName);
            Assert.AreEqual(expectedId, info.id);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("pluto")]
        public void FindReturnsNullForUnknownTargets(string targetName)
        {
            Assert.IsNull(PlanetData.Find(targetName));
        }

        [Test]
        public void EveryBodyHasCompleteCopyAndSixStatRows()
        {
            Assert.AreEqual(10, PlanetData.All.Count);
            foreach (var info in PlanetData.All)
            {
                foreach (var text in new[] { info.display, info.tagline, info.what, info.special, info.life, info.fun })
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text), info.id);
                var stats = PlanetStats.Get(info.id);
                Assert.IsNotNull(stats, info.id);
                Assert.AreEqual(6, stats.Length, info.id);
            }
        }

        [Test]
        public void ExactlyTheEightPlanetsJoinTheGravityLab()
        {
            int count = 0;
            foreach (var info in PlanetData.All)
                if (PlanetGravity.TryGet(info.id, out float gravity))
                {
                    Assert.Greater(gravity, 0f, info.id);
                    count++;
                }
            Assert.AreEqual(8, count);
        }
    }
}
