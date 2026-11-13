using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 扫描界面状态：识别前显示扫描框；识别后提示点击行星或捏合缩放。
    public class ARScanUIController : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject scanFrame;
        [SerializeField] private Text hintLabel;
        [SerializeField] private string foundHint = "Tap: details | Drag: rotate\nHold 0.5s + drag: move | Pinch on planet: resize";
        [SerializeField] private string sunHint = "The Sun card is the light!\nMove a planet around it to make day and night";
        [SerializeField] private string lostHint = "Point the camera at your planet cards";
        [SerializeField] private string lostStatus = "Scanning for planet cards...";
        public RectTransform HintRect => hintLabel != null && hintLabel.gameObject.activeInHierarchy
            ? hintLabel.rectTransform : null;

        private bool sunlight;

        public void OnTrackingFound()
        {
            if (scanFrame != null) scanFrame.SetActive(false);
            if (hintLabel != null) hintLabel.text = sunlight ? sunHint : foundHint;
            if (statusText != null) statusText.text = "";
        }

        /// 太阳卡光源生效时，把底部提示换成玩法说明；失效后恢复常规手势提示。
        public void SetSunlight(bool active)
        {
            if (sunlight == active) return;
            sunlight = active;
            bool scanning = scanFrame != null && scanFrame.activeSelf;
            if (hintLabel != null && !scanning) hintLabel.text = active ? sunHint : foundHint;
        }

        public void OnTrackingLost()
        {
            sunlight = false;
            if (scanFrame != null) scanFrame.SetActive(true);
            if (hintLabel != null) hintLabel.text = lostHint;
            if (statusText != null) statusText.text = lostStatus;
        }

        public void GoBack()
        {
            SceneNavigator.GoTo(SceneNavigator.MainMenu);
        }
    }
}
