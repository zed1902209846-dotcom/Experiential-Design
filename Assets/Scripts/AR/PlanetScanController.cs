using UnityEngine;

namespace SolarSystemAR
{
    /// 地球/木星双目标扫描总控：
    /// 识别成功 → 显示对应行星 3D 模型 + INFO 按钮；点 INFO 弹出该行星科普卡片设计图。
    /// Vuforia 目标命名须与 earthTargetName / jupiterTargetName 一致（默认 earth / jupiter）。
    public class PlanetScanController : MonoBehaviour
    {
        [SerializeField] private string earthTargetName = "earth";
        [SerializeField] private ScanPlanetContent earthContent;
        [SerializeField] private Sprite earthCard;
        [SerializeField] private string jupiterTargetName = "jupiter";
        [SerializeField] private ScanPlanetContent jupiterContent;
        [SerializeField] private Sprite jupiterCard;

        [SerializeField] private GameObject infoButton;
        [SerializeField] private InfoCardPopup cardPopup;
        [SerializeField] private ARScanUIController scanUI;

        private ScanPlanetContent activeContent;
        private Sprite activeCard;

        public void OnTrackingFound(string targetName)
        {
            ScanPlanetContent content = null;
            Sprite card = null;
            if (Matches(targetName, earthTargetName)) { content = earthContent; card = earthCard; }
            else if (Matches(targetName, jupiterTargetName)) { content = jupiterContent; card = jupiterCard; }

            if (content == null)
            {
                Debug.LogWarning("PlanetScanController: 未绑定的目标 " + targetName);
                return;
            }

            if (activeContent != null && activeContent != content) activeContent.Hide();
            activeContent = content;
            activeCard = card;
            activeContent.Show();
            if (infoButton != null) infoButton.SetActive(true);
            if (scanUI != null) scanUI.OnTrackingFound();
        }

        public void OnTrackingLost()
        {
            if (activeContent != null)
            {
                activeContent.Hide();
                activeContent = null;
            }
            activeCard = null;
            if (infoButton != null) infoButton.SetActive(false);
            if (cardPopup != null) cardPopup.Close();
            if (scanUI != null) scanUI.OnTrackingLost();
        }

        public void OpenInfo()
        {
            if (activeCard != null && cardPopup != null) cardPopup.Show(activeCard);
        }

        public void GoBack()
        {
            SceneNavigator.GoTo(SceneNavigator.Home);
        }

        private static bool Matches(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
                   string.Equals(a.Trim(), b.Trim(), System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
