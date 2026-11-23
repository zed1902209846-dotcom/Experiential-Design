using System.Collections.Generic;
using UnityEngine;

namespace SolarSystemAR
{
    /// 多卡片扫描：每个目标独立显示模型、名称和点击介绍。
    public class ScanController : MonoBehaviour
    {
        [SerializeField] private ARScanUIController scanUI;
        private PlanetAnnotations annotations;
        private GravityJumpComparison gravityLab;
        private ARSceneLighting lighting;
        private readonly Dictionary<string, ScanPlanetContent> trackedContents = new Dictionary<string, ScanPlanetContent>();
        private int previousSleepTimeout;
        private bool museumScan;

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
            museumScan = gameObject.scene.name == SceneNavigator.Scan;
            if (scanUI == null) return;
            if (museumScan)
            {
                annotations.ProfileOpened += OnProfileOpened;
                annotations.GesturePracticed += scanUI.OnMuseumGesture;
                scanUI.BeginMuseumGuide();
            }
            else scanUI.ShowScanGuide();
        }

        public void OnTrackingFound(string targetName, ScanPlanetContent content)
        {
            var info = PlanetData.Find(targetName);
            if (info == null)
            {
                Debug.LogWarning("ScanController: 未识别的目标 " + targetName);
                return;
            }
            if (content == null)
            {
                Debug.LogWarning("ScanController: 目标缺少行星模型 " + targetName);
                return;
            }
            if (trackedContents.TryGetValue(info.id, out var previous))
            {
                if (previous == content) return;
                HideContent(previous);
            }
            trackedContents[info.id] = content;
            content.Show();
            if (content.Model != null) annotations.Track(content, info);
            gravityLab.Sync(trackedContents);
            SyncSunlight();
            if (scanUI == null) return;
            if (museumScan) scanUI.OnMuseumCardFound(CardName(info), gravityLab.FirstPlanetId, gravityLab.SecondPlanetId != null);
            else scanUI.OnCardFound(CardName(info), trackedContents.Count == 1);
        }

        public void OnTrackingLost(string targetName, ScanPlanetContent content)
        {
            var info = PlanetData.Find(targetName);
            if (info == null || !trackedContents.TryGetValue(info.id, out var activeContent)
                || activeContent != content) return;
            bool hadPair = gravityLab.SecondPlanetId != null;
            bool interrupted = gravityLab.IsJumping && (info.id == gravityLab.FirstPlanetId || info.id == gravityLab.SecondPlanetId);
            HideContent(activeContent);
            trackedContents.Remove(info.id);
            gravityLab.Sync(trackedContents);
            SyncSunlight();
            if (scanUI == null) return;
            if (museumScan) scanUI.OnMuseumCardLost(CardName(info), trackedContents.Count > 0, hadPair,
                gravityLab.SecondPlanetId != null, interrupted);
            else scanUI.OnCardLost(CardName(info), trackedContents.Count > 0);
        }

        /// 手动重置；单张卡丢失使用带目标名的重载。
        public void OnTrackingLost()
        {
            foreach (var content in trackedContents.Values) HideContent(content);
            trackedContents.Clear();
            gravityLab.Sync(trackedContents);
            SyncSunlight();
            if (scanUI == null) return;
            if (museumScan) scanUI.BeginMuseumGuide();
            else scanUI.ShowScanGuide();
        }

        private void OnProfileOpened(PlanetData.Info info)
        {
            if (scanUI != null && gravityLab.SecondPlanetId == null)
                scanUI.OnMuseumProfileOpened(gravityLab.FirstPlanetId);
        }

        private static string CardName(PlanetData.Info info)
        {
            return info.id == "solarsystem" ? "Solar System" : info.display;
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
            if (annotations != null && museumScan)
            {
                annotations.ProfileOpened -= OnProfileOpened;
                if (scanUI != null) annotations.GesturePracticed -= scanUI.OnMuseumGesture;
            }
            Screen.sleepTimeout = previousSleepTimeout;
        }

        private void HideContent(ScanPlanetContent content)
        {
            annotations.Untrack(content);
            if (content != null) content.Hide();
        }
    }
}
