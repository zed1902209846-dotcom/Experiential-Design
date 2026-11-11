using NUnit.Framework;

namespace SolarSystemAR.Tests
{
    public class GravityGuessRoundTests
    {
        private const float Earth = 9.8f, Mars = 3.7f, Mercury = 3.7f;
        private GravityGuessRound round;

        [SetUp]
        public void SetUp()
        {
            round = new GravityGuessRound();
            round.Reset();
        }

        [Test]
        public void CannotJumpBeforeGuessing()
        {
            Assert.IsFalse(round.Begin());
            Assert.AreEqual(GravityGuessPhase.Guessing, round.Phase);
        }

        [Test]
        public void NoneIsNotAValidGuess()
        {
            Assert.IsFalse(round.Choose(GravityGuess.None));
            Assert.AreEqual(GravityGuessPhase.Guessing, round.Phase);
        }

        [Test]
        public void GuessCanBeChangedUntilTheJumpStarts()
        {
            Assert.IsTrue(round.Choose(GravityGuess.First));
            Assert.IsTrue(round.Choose(GravityGuess.Second));
            Assert.AreEqual(GravityGuess.Second, round.Prediction);
            Assert.IsTrue(round.Begin());
            Assert.IsFalse(round.Choose(GravityGuess.First));
            Assert.AreEqual(GravityGuess.Second, round.Prediction);
        }

        [Test]
        public void ResultStaysHiddenUntilBothLand()
        {
            round.Choose(GravityGuess.Second);
            round.Begin();
            Assert.AreEqual(GravityGuess.None, round.Answer);
            Assert.IsFalse(round.IsCorrect);
            round.Finish(Earth, Mars);
            Assert.AreEqual(GravityGuessPhase.Result, round.Phase);
            Assert.AreEqual(GravityGuess.Second, round.Answer);
            Assert.IsTrue(round.IsCorrect);
        }

        [Test]
        public void LowerGravityPlanetIsTheAnswer()
        {
            round.Choose(GravityGuess.Second);
            round.Begin();
            round.Finish(Mars, Earth);
            Assert.AreEqual(GravityGuess.First, round.Answer);
            Assert.IsFalse(round.IsCorrect);
        }

        [Test]
        public void EqualGravityIsATie()
        {
            round.Choose(GravityGuess.Same);
            round.Begin();
            round.Finish(Mercury, Mars);
            Assert.AreEqual(GravityGuess.Same, round.Answer);
            Assert.IsTrue(round.IsCorrect);
        }

        [Test]
        public void FinishIsIgnoredOutsideAJump()
        {
            round.Choose(GravityGuess.First);
            round.Finish(Mars, Earth);
            Assert.AreEqual(GravityGuessPhase.Ready, round.Phase);
            Assert.AreEqual(GravityGuess.None, round.Answer);
        }

        [Test]
        public void ResetStartsAFreshPrediction()
        {
            round.Choose(GravityGuess.First);
            round.Begin();
            round.Finish(Mars, Earth);
            round.Reset();
            Assert.AreEqual(GravityGuessPhase.Guessing, round.Phase);
            Assert.AreEqual(GravityGuess.None, round.Prediction);
            Assert.AreEqual(GravityGuess.None, round.Answer);
            Assert.IsFalse(round.IsCorrect);
        }
    }
}
