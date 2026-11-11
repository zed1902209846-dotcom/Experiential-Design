using NUnit.Framework;

namespace SolarSystemAR.Tests
{
    public class PlanetGravityTests
    {
        [Test]
        public void EarthJumpPeaksAtHalfAMetre()
        {
            Assert.IsTrue(PlanetGravity.TryGet("earth", out float gravity));
            Assert.AreEqual(0.50f, PlanetGravity.Peak(gravity), 0.005f);
        }

        [Test]
        public void LowerGravityJumpsHigherAndLonger()
        {
            PlanetGravity.TryGet("earth", out float earth);
            PlanetGravity.TryGet("mars", out float mars);
            Assert.AreEqual(1.32f, PlanetGravity.Peak(mars), 0.01f);
            Assert.Greater(PlanetGravity.Peak(mars), PlanetGravity.Peak(earth));
            Assert.Greater(PlanetGravity.FlightTime(mars), PlanetGravity.FlightTime(earth));
        }

        [Test]
        public void HeightStartsAndEndsOnTheGround()
        {
            PlanetGravity.TryGet("jupiter", out float gravity);
            float flight = PlanetGravity.FlightTime(gravity);
            Assert.AreEqual(0f, PlanetGravity.Height(gravity, -1f));
            Assert.AreEqual(0f, PlanetGravity.Height(gravity, 0f));
            Assert.AreEqual(PlanetGravity.Peak(gravity), PlanetGravity.Height(gravity, flight * 0.5f), 0.0001f);
            Assert.AreEqual(0f, PlanetGravity.Height(gravity, flight), 0.0001f);
            Assert.AreEqual(0f, PlanetGravity.Height(gravity, flight * 3f));
        }

        [TestCase("sun")]
        [TestCase("solarsystem")]
        [TestCase("pluto")]
        [TestCase(null)]
        public void BodiesOutsideTheComparisonHaveNoGravityValue(string id)
        {
            Assert.IsFalse(PlanetGravity.TryGet(id, out _));
        }

        [Test]
        public void OnlyTheFourOuterPlanetsAreGiants()
        {
            foreach (var id in new[] { "jupiter", "saturn", "uranus", "neptune" }) Assert.IsTrue(PlanetGravity.IsGiant(id), id);
            foreach (var id in new[] { "mercury", "venus", "earth", "mars", "sun" }) Assert.IsFalse(PlanetGravity.IsGiant(id), id);
        }
    }
}
