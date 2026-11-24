using UnityEngine;

namespace SolarSystemAR
{
    public enum GravityGuess { None, First, Same, Second }
    public enum GravityGuessPhase { Guessing, Ready, Jumping, Result }

    /// One visitor's prediction. Results stay hidden until both jumpers have landed.
    public sealed class GravityGuessRound
    {
        public GravityGuessPhase Phase { get; private set; }
        public GravityGuess Prediction { get; private set; }
        public GravityGuess Answer { get; private set; }
        public bool IsCorrect => Phase == GravityGuessPhase.Result && Prediction == Answer;

        public void Reset()
        {
            Phase = GravityGuessPhase.Guessing;
            Prediction = GravityGuess.None;
            Answer = GravityGuess.None;
        }
        public bool Choose(GravityGuess guess)
        {
            if ((Phase != GravityGuessPhase.Guessing && Phase != GravityGuessPhase.Ready) ||
                (guess != GravityGuess.First && guess != GravityGuess.Same && guess != GravityGuess.Second)) return false;
            Prediction = guess;
            Phase = GravityGuessPhase.Ready;
            return true;
        }
        public bool Begin()
        {
            if (Phase != GravityGuessPhase.Ready) return false;
            Phase = GravityGuessPhase.Jumping;
            return true;
        }
        public void Finish(float firstGravity, float secondGravity)
        {
            Finish(Mathf.Approximately(firstGravity, secondGravity) ? GravityGuess.Same
                : firstGravity < secondGravity ? GravityGuess.First : GravityGuess.Second);
        }

        /// 其他实验（自转、公转、大小）自己判定谁获胜，再交给回合揭晓。
        public void Finish(GravityGuess answer)
        {
            if (Phase != GravityGuessPhase.Jumping || answer == GravityGuess.None) return;
            Answer = answer;
            Phase = GravityGuessPhase.Result;
        }
    }
}
