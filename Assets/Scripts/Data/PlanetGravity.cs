using UnityEngine;

namespace SolarSystemAR
{
    /// NASA equatorial gravity, including rotation; giant planets use the 1-bar reference level.
    /// https://nssdc.gsfc.nasa.gov/planetary/factsheet/index.html
    public static class PlanetGravity
    {
        public const float LaunchSpeed = 3.130495f; // 0.50 m peak on Earth (g = 9.8).
        public static bool TryGet(string id, out float gravity)
        {
            switch (id)
            {
                case "mercury": gravity = 3.7f; return true;
                case "venus": gravity = 8.9f; return true;
                case "earth": gravity = 9.8f; return true;
                case "mars": gravity = 3.7f; return true;
                case "jupiter": gravity = 23.1f; return true;
                case "saturn": gravity = 9.0f; return true;
                case "uranus": gravity = 8.7f; return true;
                case "neptune": gravity = 11.0f; return true;
                default: gravity = 0; return false;
            }
        }
        public static bool IsGiant(string id) => id == "jupiter" || id == "saturn" || id == "uranus" || id == "neptune";
        public static float Peak(float gravity) => LaunchSpeed * LaunchSpeed / (2f * gravity);
        public static float FlightTime(float gravity) => 2f * LaunchSpeed / gravity;
        public static float Height(float gravity, float time) => time < 0 ? 0 : Mathf.Max(0, LaunchSpeed * time - 0.5f * gravity * time * time);
    }
}
