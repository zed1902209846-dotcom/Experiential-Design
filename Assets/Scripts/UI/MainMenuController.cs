using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    public class MainMenuController : MonoBehaviour
    {
        public void StartExploration()
        {
            SceneNavigator.GoTo(SceneNavigator.Scan);
        }

        public void Logout()
        {
            SessionState.Reset();
            SceneNavigator.GoTo(SceneNavigator.Login);
        }
    }
}
