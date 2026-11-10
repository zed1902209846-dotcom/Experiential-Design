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
        private bool tracked;

        private void OnEnable()
        {
            observer = GetComponent<Vuforia.ObserverBehaviour>();
            if (observer != null) observer.OnTargetStatusChanged += OnStatusChanged;
        }

        private void OnDisable()
        {
            if (observer != null) observer.OnTargetStatusChanged -= OnStatusChanged;
        }

        private void OnStatusChanged(Vuforia.ObserverBehaviour behaviour, Vuforia.TargetStatus status)
        {
            bool isTracking = status.Status == Vuforia.Status.TRACKED ||
                              status.Status == Vuforia.Status.EXTENDED_TRACKED;
            if (isTracking == tracked) return;
            tracked = isTracking;
            Notify(tracked, behaviour.TargetName);
        }
#endif

        private void Notify(bool found, string targetName)
        {
            var scan = FindFirstObjectByType<ScanController>(FindObjectsInactive.Include);
            if (scan != null)
            {
                if (found) scan.OnTrackingFound(targetName);
                else scan.OnTrackingLost();
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
