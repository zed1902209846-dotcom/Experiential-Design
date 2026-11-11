using UnityEngine;
using UnityEngine.SceneManagement;

namespace SolarSystemAR
{
    public static class SceneNavigator
    {
        public const string MainMenu = "MainMenuScene";
        public const string AR = "ARScene";
        public const string Single = "SinglePlanetScene";
        public const string Home = "HomeScene";
        public const string Scan = "ScanScene";

        public static void GoTo(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
