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
        [SerializeField] private string foundHint = "Tap a planet for details | Pinch to resize";
        [SerializeField] private string lostHint = "Point the camera at your planet cards";
        [SerializeField] private string lostStatus = "Scanning for planet cards...";
        public RectTransform HintRect => hintLabel != null && hintLabel.gameObject.activeInHierarchy
            ? hintLabel.rectTransform : null;

        public void OnTrackingFound()
        {
            if (scanFrame != null) scanFrame.SetActive(false);
            if (hintLabel != null) hintLabel.text = foundHint;
            if (statusText != null) statusText.text = "";
        }

        public void OnTrackingLost()
        {
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
