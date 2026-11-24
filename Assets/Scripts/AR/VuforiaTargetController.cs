using UnityEngine;

namespace SolarSystemAR
{
    /// Vuforia 目标追踪桥接（Image Target）。
    /// 真机（VUFORIA_ENABLED）：监听 ObserverBehaviour 状态并转发 targetName；
    /// 编辑器测试：组件右上角菜单 Simulate Found/Lost，用 simulateTargetName 指定模拟目标名。
    public class VuforiaTargetController : MonoBehaviour
    {
        [SerializeField] private string simulateTargetName = "earth";

#if VUFORIA_ENABLED
        private Vuforia.ObserverBehaviour observer;
        private string trackedTargetName;

        private void OnEnable()
        {
            observer = GetComponent<Vuforia.ObserverBehaviour>();
            if (observer == null) return;
            observer.OnTargetStatusChanged += OnStatusChanged;
            OnStatusChanged(observer, observer.TargetStatus);
        }

        private void OnDisable()
        {
            if (observer != null) observer.OnTargetStatusChanged -= OnStatusChanged;
            SetTrackedTarget(null);
        }

        private void OnStatusChanged(Vuforia.ObserverBehaviour behaviour, Vuforia.TargetStatus status)
        {
            // Extended tracking can retain a pose after the physical card leaves the camera view.
            SetTrackedTarget(status.Status == Vuforia.Status.TRACKED ? behaviour.TargetName : null);
        }

        private void SetTrackedTarget(string targetName)
        {
            if (trackedTargetName == targetName) return;
            string previousTargetName = trackedTargetName;
            trackedTargetName = targetName;
            if (!string.IsNullOrEmpty(previousTargetName)) Notify(false, previousTargetName);
            if (!string.IsNullOrEmpty(targetName)) Notify(true, targetName);
        }
#endif

        private void Notify(bool found, string targetName)
        {
            var scan = FindFirstObjectByType<ScanController>(FindObjectsInactive.Include);
            if (scan != null)
            {
                var content = GetComponentInChildren<ScanPlanetContent>(true);
                if (found) scan.OnTrackingFound(targetName, content);
                else scan.OnTrackingLost(targetName, content);
            }
            var legacy = FindFirstObjectByType<PlanetScanController>(FindObjectsInactive.Include);
            if (legacy != null)
            {
                if (found) legacy.OnTrackingFound(targetName);
                else legacy.OnTrackingLost();
            }
        }

        [ContextMenu("Simulate Found")]
        public void SimulateFound()
        {
            Notify(true, simulateTargetName);
        }

        [ContextMenu("Simulate Lost")]
        public void SimulateLost()
        {
            Notify(false, simulateTargetName);
        }
    }
}
