using NUnit.Framework;

namespace SolarSystemAR.Tests
{
    public class PlanetLabTests
    {
        private static float Value(LabExperiment experiment, string id)
        {
            Assert.IsTrue(PlanetLab.TryGet(experiment, id, out float value), experiment + " " + id);
            return value;
        }

        [Test]
        public void EveryPlanetHasAValueForEveryExperiment()
        {
            foreach (var experiment in PlanetLab.All)
                foreach (var id in new[] { "mercury", "venus", "earth", "mars", "jupiter", "saturn", "uranus", "neptune" })
                    Assert.Greater(Value(experiment, id), 0f);
        }

        [Test]
        public void SunAndSolarSystemDoNotTakePart()
        {
            foreach (var experiment in PlanetLab.All)
            {
                Assert.IsFalse(PlanetLab.TryGet(experiment, "sun", out _));
                Assert.IsFalse(PlanetLab.TryGet(experiment, "solarsystem", out _));
            }
        }

        [Test]
        public void JupiterHasTheShorterDayButTheLongerYear()
        {
            Assert.AreEqual(GravityGuess.Second, PlanetLab.Answer(LabExperiment.Day,
                Value(LabExperiment.Day, "earth"), Value(LabExperiment.Day, "jupiter")));
            Assert.AreEqual(GravityGuess.First, PlanetLab.Answer(LabExperiment.Year,
                Value(LabExperiment.Year, "earth"), Value(LabExperiment.Year, "jupiter")));
        }

        [Test]
        public void BiggerDiameterWinsTheSizeExperiment()
        {
            Assert.AreEqual(GravityGuess.Second, PlanetLab.Answer(LabExperiment.Size,
                Value(LabExperiment.Size, "earth"), Value(LabExperiment.Size, "jupiter")));
            Assert.AreEqual(GravityGuess.First, PlanetLab.Answer(LabExperiment.Size,
                Value(LabExperiment.Size, "earth"), Value(LabExperiment.Size, "mars")));
        }

        [Test]
        public void LowerGravityStillWinsTheJump()
        {
            Assert.AreEqual(GravityGuess.Second, PlanetLab.Answer(LabExperiment.Jump,
                Value(LabExperiment.Jump, "earth"), Value(LabExperiment.Jump, "mars")));
            Assert.AreEqual(GravityGuess.Same, PlanetLab.Answer(LabExperiment.Jump,
                Value(LabExperiment.Jump, "mercury"), Value(LabExperiment.Jump, "mars")));
        }

        [Test]
        public void ValuesReadTheSameAsTheFactCards()
        {
            Assert.AreEqual("0.50 m", PlanetLab.Format(LabExperiment.Jump, Value(LabExperiment.Jump, "earth")));
            Assert.AreEqual("23 h 56 m", PlanetLab.Format(LabExperiment.Day, Value(LabExperiment.Day, "earth")));
            Assert.AreEqual("9 h 56 m", PlanetLab.Format(LabExperiment.Day, Value(LabExperiment.Day, "jupiter")));
            Assert.AreEqual("10 h 42 m", PlanetLab.Format(LabExperiment.Day, Value(LabExperiment.Day, "saturn")));
            Assert.AreEqual("58.6 days", PlanetLab.Format(LabExperiment.Day, Value(LabExperiment.Day, "mercury")));
            Assert.AreEqual("243 days", PlanetLab.Format(LabExperiment.Day, Value(LabExperiment.Day, "venus")));
            Assert.AreEqual("365 days", PlanetLab.Format(LabExperiment.Year, Value(LabExperiment.Year, "earth")));
            Assert.AreEqual("11.9 years", PlanetLab.Format(LabExperiment.Year, Value(LabExperiment.Year, "jupiter")));
            Assert.AreEqual("164.8 years", PlanetLab.Format(LabExperiment.Year, Value(LabExperiment.Year, "neptune")));
            Assert.AreEqual("12,742 km", PlanetLab.Format(LabExperiment.Size, Value(LabExperiment.Size, "earth")));
        }

        [Test]
        public void FactNamesTheWinnerAndTheRatio()
        {
            Assert.AreEqual("Jupiter is 11.0x wider", PlanetLab.Fact(LabExperiment.Size,
                "Earth", Value(LabExperiment.Size, "earth"), "Jupiter", Value(LabExperiment.Size, "jupiter")));
            Assert.AreEqual("Mars jumps 2.6x higher", PlanetLab.Fact(LabExperiment.Jump,
                "Earth", Value(LabExperiment.Jump, "earth"), "Mars", Value(LabExperiment.Jump, "mars")));
            Assert.AreEqual("IT'S A TIE", PlanetLab.Fact(LabExperiment.Jump, "Mercury", 3.7f, "Mars", 3.7f));
        }

        [Test]
        public void OnlyVenusSpinsBackwards()
        {
            Assert.IsTrue(PlanetLab.SpinsBackwards("venus"));
            Assert.IsFalse(PlanetLab.SpinsBackwards("earth"));
            StringAssert.Contains("Venus", PlanetLab.Note(LabExperiment.Day, "earth", "venus"));
        }

        [Test]
        public void RoundAcceptsAnAnswerDecidedByTheExperiment()
        {
            var round = new GravityGuessRound();
            round.Reset();
            round.Choose(GravityGuess.Second);
            round.Finish(GravityGuess.Second);
            Assert.AreEqual(GravityGuessPhase.Ready, round.Phase, "不能跳过演示直接揭晓");
            round.Begin();
            round.Finish(GravityGuess.Second);
            Assert.IsTrue(round.IsCorrect);
        }
    }
}
