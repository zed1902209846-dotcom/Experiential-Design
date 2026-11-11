using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SolarSystemAR.EditorTools
{
    /// 第6步：一键打安卓 APK（幂等设置）。
    /// 前置：AndroidPlayer 模块（SDK/NDK/OpenJDK）已安装。
    public static class Step6ApkBuilder
    {
        static readonly string BuildDirectory = Path.GetFullPath("Builds/Android");
        static readonly string OutPath = Path.Combine(BuildDirectory, "apk_build.txt");
        static readonly string ApkPath = Path.Combine(BuildDirectory, "ARSolarSystem.apk");

        [MenuItem("Solar System/Setup/Step6 Build Android APK %#j")]
        public static void Run()
        {
            Directory.CreateDirectory(BuildDirectory);
            var log = new System.Text.StringBuilder();
            try { Build(log); }
            catch (System.Exception e) { log.AppendLine("EXCEPTION: " + e); }
            File.WriteAllText(OutPath, log.ToString());
            Debug.Log("[BuildAPK]\n" + log);
        }

        static void Build(System.Text.StringBuilder log)
        {
            // 0. 模块检查
            var moduleDir = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
            if (!Directory.Exists(moduleDir)) { log.AppendLine("FATAL: AndroidPlayer module missing"); return; }
            foreach (var sub in new[] { "SDK", "NDK", "OpenJDK" })
            {
                bool ok = Directory.Exists(Path.Combine(moduleDir, sub));
                log.AppendLine(sub + ": " + (ok ? "OK" : "MISSING"));
                if (!ok) { log.AppendLine("FATAL: missing " + sub); return; }
            }

            // 1. 场景列表
            const string mainMenu = "Assets/Scenes/App/MainMenuScene.unity";
            var scenePaths = new[] { mainMenu }.Concat(EditorBuildSettings.scenes
                .Where(s => s.enabled && s.path != mainMenu && s.path != "Assets/Scenes/App/LoginScene.unity")
                .Select(s => s.path)).Distinct().ToArray();
            foreach (var s in scenePaths)
                if (!File.Exists(s)) { log.AppendLine("FATAL: scene missing " + s); return; }
            EditorBuildSettings.scenes = scenePaths.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
            log.AppendLine("scenes: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path)));

            // 2. Player 设置（幂等）
            PlayerSettings.companyName = "TaylorUniversity";
            PlayerSettings.productName = "AR Solar System";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.taylor.arsolarsystem");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            // Vuforia 11 对 Vulkan 仅实验性支持，锁定 OpenGLES3
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new UnityEngine.Rendering.GraphicsDeviceType[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            log.AppendLine("pkg: " + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)
                         + " | il2cpp+arm64+portrait | minSdk=" + (int)PlayerSettings.Android.minSdkVersion);

            // 3. 构建
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            var report = BuildPipeline.BuildPlayer(scenePaths, ApkPath, BuildTarget.Android, BuildOptions.None);
            log.AppendLine("RESULT: " + report.summary.result);
            log.AppendLine("SIZE_MB: " + (report.summary.totalSize / 1024 / 1024));
            log.AppendLine("ERRORS: " + report.summary.totalErrors);
            log.AppendLine("WARNINGS: " + report.summary.totalWarnings);
            if (File.Exists(ApkPath)) log.AppendLine("APK: " + ApkPath);
        }
    }
}
