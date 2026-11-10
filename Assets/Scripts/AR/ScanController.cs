using UnityEngine;

namespace SolarSystemAR
{
    /// 扫描场景总控（方案 B）：
    /// 1) 识别成功 → 对应行星 3D 模型出现在实体卡片上自转，右下角浮现太阳系图标按钮；
    /// 2) 点图标按钮 → 弹出该行星知识卡；点 X 关闭后回到 AR 画面；
    /// 3) 识别丢失 → 模型隐藏，图标按钮保持可用（可继续浏览知识卡）。
    public class ScanController : MonoBehaviour
    {
        [SerializeField] private PlanetCardController card;
        [SerializeField] private ARScanUIController scanUI;
        [SerializeField] private GameObject browseButton; // 图标按钮：识别成功后出现，点开当前行星知识卡

        private ScanPlanetContent[] contents;
        private ScanPlanetContent current;
        private string currentId;

        private void Awake()
        {
            contents = FindObjectsByType<ScanPlanetContent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (browseButton != null) browseButton.SetActive(false);
        }

        public void OnTrackingFound(string targetName)
        {
            var info = PlanetData.Find(targetName);
            if (info == null)
            {
                Debug.LogWarning("ScanController: 未识别的目标 " + targetName);
                return;
            }
            currentId = info.id;
            if (current != null) current.Hide();
            current = FindContent(info.display);
            if (current != null) current.Show();
            if (browseButton != null) browseButton.SetActive(true);
            if (scanUI != null) scanUI.OnTrackingFound();
        }

        public void OnTrackingLost()
        {
            if (current != null)
            {
                current.Hide();
                current = null;
            }
            if (scanUI != null) scanUI.OnTrackingLost();
            // 知识卡若已打开则保持，方便阅读；X 手动关闭
        }

        /// 右下角图标按钮：打开当前识别行星的知识卡；未识别到时默认地球。
        public void OpenBrowser()
        {
            if (card == null) return;
            card.ShowPlanet(string.IsNullOrEmpty(currentId) ? "earth" : currentId);
        }

        public void GoBack()
        {
            SceneNavigator.GoTo(SceneNavigator.MainMenu);
        }

        private ScanPlanetContent FindContent(string display)
        {
            if (contents == null) return null;
            foreach (var c in contents)
            {
                if (c != null && c.name.StartsWith(display)) return c;
            }
            return null;
        }
    }
}
