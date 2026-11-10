using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 扫描界面状态：整体太阳系仪识别前显示扫描框；识别后提示点击行星。
    public class ARScanUIController : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject scanFrame;
        [SerializeField] private Text hintLabel;
        [SerializeField] private string foundHint = "Target locked - tap any planet";
        [SerializeField] private string lostHint = "Fit the whole orrery inside the frame";
        [SerializeField] private string lostStatus = "Scanning for the orrery...";

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
