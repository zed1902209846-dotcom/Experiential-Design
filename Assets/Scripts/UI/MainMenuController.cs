using UnityEngine;

namespace SolarSystemAR
{
    public class MainMenuController : MonoBehaviour
    {
        public void StartExploration()
        {
            SceneNavigator.GoTo(SceneNavigator.Scan);
        }

    }
}
