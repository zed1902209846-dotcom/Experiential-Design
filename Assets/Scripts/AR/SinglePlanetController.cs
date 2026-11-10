using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolarSystemAR
{
    /// 单行星扫描模式总控：
    /// 按 Vuforia Model Target 名称绑定对应行星；识别成功显示该行星模型+信息面板。
    /// 注意：光滑球体几何相同，Vuforia 可能无法区分外形一致的球（Saturn 带环最稳），
    /// 真机验证后如不可靠，由 Codex 加"先选行星再扫描"的兜底页。
    public class SinglePlanetController : MonoBehaviour
    {
        [Serializable]
        public class Binding
        {
            public string targetName;
            public CelestialBodyData data;
            public CelestialARContent content;
        }

        [SerializeField] private List<Binding> bindings = new List<Binding>();
        [SerializeField] private CelestialInfoPanel infoPanel;
        [SerializeField] private ARScanUIController scanUI;

        private CelestialARContent active;

        public void OnTrackingFound(string targetName)
        {
            var b = bindings.Find(x => x.targetName == targetName);
            if (b == null)
            {
                Debug.LogWarning("SinglePlanetController: 未绑定的目标 " + targetName);
                return;
            }
            if (active != null && active != b.content) active.OnTrackingLost();
            active = b.content;
            active.OnTrackingFound();
            if (infoPanel != null && b.data != null) infoPanel.Show(b.data, active.HasNucleus);
            if (scanUI != null) scanUI.OnTrackingFound();
        }

        public void OnTrackingLost()
        {
            if (active != null)
            {
                active.OnTrackingLost();
                active = null;
            }
            if (infoPanel != null) infoPanel.Close();
            if (scanUI != null) scanUI.OnTrackingLost();
        }

        public void ToggleSelectedStructure()
        {
            if (active == null) return;
            active.ToggleStructure();
            if (infoPanel != null)
                infoPanel.SetStructureLabel(active.IsShowingNucleus ? "SURFACE" : "VIEW INTERIOR");
        }

        public void CloseInfo()
        {
            if (infoPanel != null) infoPanel.Close();
        }
    }
}
