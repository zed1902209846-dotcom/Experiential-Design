using UnityEngine;

namespace SolarSystemAR
{
    /// 太阳系仪 AR 总控：
    /// 1) 整体 Model Target 追踪成功 → 激活全部 10 个锚点的 AR 行星；
    /// 2) 点击某颗行星 → 放大选中 + 弹出信息面板；
    /// 3) "查看内部结构" 在表面模型与 Nucleus 剖切模型间切换。
    public class AROrreryController : MonoBehaviour
    {
        [SerializeField] private CelestialInfoPanel infoPanel;
        [SerializeField] private ARScanUIController scanUI;
        [SerializeField] private bool hideContentOnLost = false;

        private CelestialARContent[] contents;
        private CelestialARContent selected;

        private void Awake()
        {
            contents = GetComponentsInChildren<CelestialARContent>(true);
        }

        public void OnTrackingFound()
        {
            gameObject.SetActive(true);
            foreach (var c in contents) c.OnTrackingFound();
            if (scanUI != null) scanUI.OnTrackingFound();
        }

        public void OnTrackingLost()
        {
            if (hideContentOnLost)
            {
                foreach (var c in contents) c.OnTrackingLost();
                gameObject.SetActive(false);
            }
            Deselect();
            if (scanUI != null) scanUI.OnTrackingLost();
        }

        public void Select(CelestialARContent content)
        {
            if (content == null || content.Data == null) return;
            if (selected != null && selected != content) selected.SetSelected(false);
            selected = content;
            selected.SetSelected(true);
            if (infoPanel != null) infoPanel.Show(content.Data, content.HasNucleus);
        }

        public void ToggleSelectedStructure()
        {
            if (selected == null) return;
            selected.ToggleStructure();
            if (infoPanel != null)
                infoPanel.SetStructureLabel(selected.IsShowingNucleus ? "SURFACE" : "VIEW INTERIOR");
        }

        public void Deselect()
        {
            if (selected != null)
            {
                selected.SetSelected(false);
                selected = null;
            }
            if (infoPanel != null) infoPanel.Close();
        }
    }
}
