using System.Globalization;
using UnityEngine;

namespace SolarSystemAR
{
    public enum LabExperiment { Jump, Day, Year, Size }

    /// 「先猜再揭晓」的四个实验：每个实验给每颗行星一个数值，并规定数值小还是大的一方获胜。
    /// 数据来自 NASA Planetary Fact Sheet，与资料卡 PlanetStats 保持一致。
    public static class PlanetLab
    {
        public static readonly LabExperiment[] All =
            { LabExperiment.Jump, LabExperiment.Day, LabExperiment.Year, LabExperiment.Size };

        public static string Tab(LabExperiment experiment)
        {
            switch (experiment)
            {
                case LabExperiment.Day: return "DAY";
                case LabExperiment.Year: return "YEAR";
                case LabExperiment.Size: return "SIZE";
                default: return "JUMP";
            }
        }

        public static string Question(LabExperiment experiment)
        {
            switch (experiment)
            {
                case LabExperiment.Day: return "WHOSE DAY IS SHORTER?";
                case LabExperiment.Year: return "WHOSE YEAR IS SHORTER?";
                case LabExperiment.Size: return "WHICH IS BIGGER?";
                default: return "WHO JUMPS HIGHER?";
            }
        }

        /// 主按钮在已选好答案时显示的动作。
        public static string Action(LabExperiment experiment)
        {
            switch (experiment)
            {
                case LabExperiment.Day: return "SPIN";
                case LabExperiment.Year: return "ORBIT";
                case LabExperiment.Size: return "COMPARE";
                default: return "JUMP";
            }
        }

        /// Jump：重力 m/s²；Day：自转周期（小时，取绝对值）；Year：公转周期（地球日）；Size：直径 km。
        public static bool TryGet(LabExperiment experiment, string id, out float value)
        {
            switch (experiment)
            {
                case LabExperiment.Jump: return PlanetGravity.TryGet(id, out value);
                case LabExperiment.Day: return Pick(id, out value, 1407.5f, 5832.5f, 23.93f, 24.62f, 9.93f, 10.7f, 17.24f, 16.1f);
                case LabExperiment.Year: return Pick(id, out value, 88f, 224.7f, 365.25f, 687f, 4333f, 10759f, 30687f, 60190f);
                default: return Pick(id, out value, 4879f, 12104f, 12742f, 6779f, 139820f, 116460f, 50724f, 49244f);
            }
        }

        private static bool Pick(string id, out float value, params float[] byPlanet)
        {
            int index = System.Array.IndexOf(Order, id);
            value = index < 0 ? 0f : byPlanet[index];
            return index >= 0;
        }

        private static readonly string[] Order =
            { "mercury", "venus", "earth", "mars", "jupiter", "saturn", "uranus", "neptune" };

        /// 金星逆向自转：自转实验里它朝反方向转。
        public static bool SpinsBackwards(string id) => id == "venus";

        /// 只有「谁更大」是数值大的一方获胜；跳得高、日短、年短都是数值小的一方。
        public static GravityGuess Answer(LabExperiment experiment, float first, float second)
        {
            if (Mathf.Approximately(first, second)) return GravityGuess.Same;
            bool firstWins = experiment == LabExperiment.Size ? first > second : first < second;
            return firstWins ? GravityGuess.First : GravityGuess.Second;
        }

        /// 揭晓时显示在行星名下面的数值。
        public static string Format(LabExperiment experiment, float value)
        {
            var culture = CultureInfo.InvariantCulture;
            switch (experiment)
            {
                case LabExperiment.Jump:
                    return PlanetGravity.Peak(value).ToString("0.00", culture) + " m";
                case LabExperiment.Day:
                    if (value >= 48f) return (value / 24f).ToString("0.#", culture) + " days";
                    int totalMinutes = Mathf.RoundToInt(value * 60f);
                    return (totalMinutes / 60) + " h " + (totalMinutes % 60) + " m";
                case LabExperiment.Year:
                    return value < 1000f ? Mathf.RoundToInt(value) + " days"
                        : (value / 365.25f).ToString("0.#", culture) + " years";
                default:
                    return value.ToString("N0", culture) + " km";
            }
        }

        /// 揭晓后的一句话结论，例如 "Mars jumps 2.6x higher"。
        public static string Fact(LabExperiment experiment, string firstName, float first, string secondName, float second)
        {
            var answer = Answer(experiment, first, second);
            if (answer == GravityGuess.Same) return "IT'S A TIE";
            string winner = answer == GravityGuess.First ? firstName : secondName;
            float ratio = Mathf.Max(first, second) / Mathf.Min(first, second);
            string times = ratio.ToString(ratio >= 100f ? "0" : "0.0", CultureInfo.InvariantCulture) + "x";
            switch (experiment)
            {
                case LabExperiment.Day: return winner + "'s day is " + times + " shorter";
                case LabExperiment.Year: return winner + "'s year is " + times + " shorter";
                case LabExperiment.Size: return winner + " is " + times + " wider";
                default: return winner + " jumps " + times + " higher";
            }
        }

        /// 面板底部的小字说明：交代这个演示做了哪些简化。
        public static string Note(LabExperiment experiment, string firstId, string secondId)
        {
            switch (experiment)
            {
                case LabExperiment.Day:
                    return SpinsBackwards(firstId) || SpinsBackwards(secondId)
                        ? "One turn = one day | Venus spins backwards"
                        : "One turn = one day | Speeds are to scale";
                case LabExperiment.Year: return "Orbit speeds are to scale | Distances are not";
                case LabExperiment.Size: return "Diameters are to scale";
                default:
                    return PlanetGravity.IsGiant(firstId) || PlanetGravity.IsGiant(secondId)
                        ? "No air resistance | 1 bar, no ground | 0.45x"
                        : "Same launch | No air resistance | 0.45x";
            }
        }
    }
}
