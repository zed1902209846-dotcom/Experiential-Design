using UnityEngine;

namespace SolarSystemAR
{
    /// 主菜单星球入口按钮：点击后加载 sceneName 指定的场景。
    public class PlanetSceneLauncher : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        public void Open()
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            SceneNavigator.GoTo(sceneName);
        }
    }
}
