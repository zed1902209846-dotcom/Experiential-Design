using UnityEngine;

namespace SolarSystemAR
{
    /// 初始页：START EXPLORE → 进入扫描场景。
    public class HomeController : MonoBehaviour
    {
        public void StartExplore()
        {
            SceneNavigator.GoTo(SceneNavigator.Scan);
        }
    }
}
