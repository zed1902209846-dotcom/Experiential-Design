using System.Collections.Generic;
using UnityEngine;

namespace SolarSystemAR
{
    /// 多卡片扫描：每个目标独立显示模型、名称和点击介绍。
    public class ScanController : MonoBehaviour
    {
        [SerializeField] private ARScanUIController scanUI;
        private ScanPlanetContent[] contents;
        private PlanetAnnotations annotations;
        private GravityJumpComparison gravityLab;
        private ARSceneLighting lighting;
        private readonly Dictionary<string, ScanPlanetContent> trackedContents = new Dictionary<string, ScanPlanetContent>();
        private int previousSleepTimeout;

        private void Awake()
        {
            // 展馆里访客常常只举着手机看而不触屏：扫描期间保持屏幕常亮，离开场景时恢复。
            previousSleepTimeout = Screen.sleepTimeout;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            lighting = GetComponent<ARSceneLighting>();
            if (lighting == null) lighting = gameObject.AddComponent<ARSceneLighting>();
            annotations = GetComponent<PlanetAnnotations>();
            if (annotations == null) annotations = gameObject.AddComponent<PlanetAnnotations>();
            gravityLab = GetComponent<GravityJumpComparison>();
            if (gravityLab == null) gravityLab = gameObject.AddComponent<GravityJumpComparison>();
            contents = FindObjectsByType<ScanPlanetContent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public void OnTrackingFound(string targetName)
        {
            var info = PlanetData.Find(targetName);
            if (info == null)
            {
                Debug.LogWarning("ScanController: 未识别的目标 " + targetName);
                return;
            }
            var content = FindContent(info.display);
            if (content == null) return;
            trackedContents[info.id] = content;
            content.Show();
            if (content.Model != null) annotations.Track(content, info);
            if (scanUI != null) scanUI.OnTrackingFound();
            gravityLab.Sync(trackedContents);
            SyncSunlight();
        }

        public void OnTrackingLost(string targetName)
        {
            var info = PlanetData.Find(targetName);
            if (info == null || !trackedContents.TryGetValue(info.id, out var content)) return;
            annotations.Untrack(content);
            if (content != null) content.Hide();
            trackedContents.Remove(info.id);
            gravityLab.Sync(trackedContents);
            SyncSunlight();
            if (trackedContents.Count == 0 && scanUI != null) scanUI.OnTrackingLost();
        }

        /// 手动重置；单张卡丢失使用带目标名的重载。
        public void OnTrackingLost()
        {
            foreach (var content in trackedContents.Values)
            {
                annotations.Untrack(content);
                if (content != null) content.Hide();
            }
            trackedContents.Clear();
            gravityLab.Sync(trackedContents);
            SyncSunlight();
            if (scanUI != null) scanUI.OnTrackingLost();
        }

        private void SyncSunlight()
        {
            lighting.Sync(trackedContents);
            if (scanUI != null) scanUI.SetSunlight(lighting.SunActive);
        }

        public void GoBack()
        {
            SceneNavigator.GoTo(SceneNavigator.MainMenu);
        }

        private void OnDestroy()
        {
            Screen.sleepTimeout = previousSleepTimeout;
        }

        private ScanPlanetContent FindContent(string display)
        {
            if (contents == null) return null;
            foreach (var content in contents)
                if (content != null && content.name.StartsWith(display)) return content;
            return null;
        }
    }
}
