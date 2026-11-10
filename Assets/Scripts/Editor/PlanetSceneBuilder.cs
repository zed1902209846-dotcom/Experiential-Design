using System.Collections.Generic;
using System.IO;
using System.Text;
using SolarSystemAR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SolarSystemAR.EditorBuildScripts
{
    /// 为 SolarCards 数据库中「除地球外的 8 个目标」各生成一个独立场景。
    /// 做法：以 ScanScene 为模板克隆 —— 完整保留 ARCamera / Vuforia 配置 / 扫描 HUD / 资料卡 UI，
    /// 只保留本场景对应的那一个识别目标与其内容，其余目标及内容删除。
    /// 菜单：Solar System > Setup > Build Per-Planet Scenes
    public static class PlanetSceneBuilder
    {
        const string Dir = "Assets/Scenes/App";
        const string ScanScenePath = Dir + "/ScanScene.unity";
        const string OutPath = @"D:\Trae生成文件\666\planet_scenes_build.txt";

        class Def
        {
            public string scene;    // 场景文件名
            public string target;   // Vuforia 目标名（与 SolarCards 数据库一致）
            public string display;  // PlanetData.display，用于匹配 *Content 对象

            public Def(string scene, string target, string display)
            {
                this.scene = scene; this.target = target; this.display = display;
            }
        }

        static readonly Def[] Defs =
        {
            new Def("MercuryScene",     "mercury",     "Mercury"),
            new Def("MarsScene",        "mars",        "Mars"),
            new Def("JupiterScene",     "jupiter",     "Jupiter"),
            new Def("SaturnScene",      "saturn",      "Saturn"),
            new Def("UranusScene",      "uranus",      "Uranus"),
            new Def("NeptuneScene",     "neptune",     "Neptune"),
            new Def("SunScene",         "sun",         "Sun"),
            new Def("SolarSystemScene", "solarsystem", "SolarSystem"),
        };

        [MenuItem("Solar System/Setup/Build Per-Planet Scenes")]
        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("=== Build Per-Planet Scenes ===");
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                log.AppendLine("ABORT: save the active scene before rebuilding: " + activeScene.path);
                WriteReport(log);
                Debug.LogWarning(log.ToString());
                return;
            }

            try
            {
                foreach (var d in Defs)
                {
                    var source = EditorSceneManager.OpenScene(ScanScenePath, OpenSceneMode.Single);
                    string destination = Dir + "/" + d.scene + ".unity";
                    if (!EditorSceneManager.SaveScene(source, destination))
                        throw new IOException("Could not save scene clone: " + destination);

                    var scene = EditorSceneManager.GetActiveScene();
                    int removed = StripToSingleTarget(d, log);
                    ValidateScene(d);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("Could not save repaired scene: " + destination);
                    log.AppendLine(d.scene + " -> " + d.target + "; removed " + removed + " unrelated objects; saved");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                log.AppendLine("Build Settings preserved; all 8 requested scenes are already listed.");
            }
            catch (System.Exception e)
            {
                log.AppendLine("ERROR: " + e);
            }

            WriteReport(log);
            Debug.Log("PlanetSceneBuilder completed:\n" + log);
        }

        static void WriteReport(StringBuilder log)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllText(OutPath, log.ToString(), new UTF8Encoding(false));
        }

        static int StripToSingleTarget(Def d, System.Text.StringBuilder log)
        {
            int removed = 0;

            var contents = Object.FindObjectsByType<ScanPlanetContent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var content in contents)
            {
                if (content == null || content.gameObject.name.StartsWith(d.display, System.StringComparison.OrdinalIgnoreCase)) continue;
                Object.DestroyImmediate(content.gameObject);
                removed++;
            }

            MonoBehaviour target = null;
            var targetObjects = new HashSet<GameObject>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "ImageTargetBehaviour") continue;
                string name = ReadTrackableName(mb);
                if (name == d.target && target == null)
                {
                    target = mb;
                    SetSimulateName(mb, d.target, log);
                    continue;
                }
                if (target != null && mb.gameObject == target.gameObject) continue;
                targetObjects.Add(mb.gameObject);
            }

            foreach (var targetObject in targetObjects)
            {
                if (targetObject == null) continue;
                Object.DestroyImmediate(targetObject);
                removed++;
            }

            var retainedContents = Object.FindObjectsByType<ScanPlanetContent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ScanPlanetContent retained = null;
            foreach (var content in retainedContents)
            {
                if (content == null || !content.gameObject.name.StartsWith(d.display, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (retained == null) retained = content;
                else Object.DestroyImmediate(content.gameObject);
            }

            if (target == null || retained == null)
                throw new System.InvalidOperationException("Missing target or content for " + d.scene);

            retained.transform.SetParent(target.transform, false);
            retained.transform.localPosition = Vector3.zero;
            retained.transform.localRotation = Quaternion.identity;
            return removed;
        }

        static void ValidateScene(Def d)
        {
            var targets = new List<MonoBehaviour>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mb != null && mb.GetType().Name == "ImageTargetBehaviour" && ReadTrackableName(mb).Length > 0)
                    targets.Add(mb);
            if (targets.Count != 1 || ReadTrackableName(targets[0]) != d.target)
                throw new System.InvalidOperationException(d.scene + " must contain exactly one " + d.target + " ImageTarget");

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var arCamera = GameObject.Find("ARCamera");
            int arCameraCount = 0;
            foreach (var camera in cameras)
                if (camera != null && camera.gameObject == arCamera) arCameraCount++;
            if (arCamera == null || arCameraCount != 1)
                throw new System.InvalidOperationException(d.scene + " must contain exactly one Vuforia ARCamera");
            if (arCamera.GetComponent<AudioListener>() == null || arCamera.GetComponent<PhysicsRaycaster>() == null ||
                !HasComponentNamed(arCamera, "VuforiaBehaviour") || !HasComponentNamed(arCamera, "DefaultInitializationErrorHandler"))
                throw new System.InvalidOperationException(d.scene + " ARCamera is missing a required component");

            var contents = Object.FindObjectsByType<ScanPlanetContent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (contents.Length != 1 || contents[0].transform.parent != targets[0].transform)
                throw new System.InvalidOperationException(d.scene + " content must be parented to its ImageTarget");
            var contentData = new SerializedObject(contents[0]);
            var prefab = contentData.FindProperty("planetPrefab");
            if (prefab == null || prefab.objectReferenceValue == null)
                throw new System.InvalidOperationException(d.scene + " content has no planet prefab");
            if (Object.FindFirstObjectByType<ScanController>(FindObjectsInactive.Include) == null ||
                Object.FindFirstObjectByType<PlanetCardController>(FindObjectsInactive.Include) == null ||
                Object.FindFirstObjectByType<PlanetPreviewRig>(FindObjectsInactive.Include) == null)
                throw new System.InvalidOperationException(d.scene + " is missing scan or planet-card UI");

            if (targets[0].GetComponent("VuforiaTargetController") == null)
                throw new System.InvalidOperationException(d.scene + " target has no VuforiaTargetController");
            SetSimulateName(targets[0], d.target, new StringBuilder());
        }

        static bool HasComponentNamed(GameObject go, string typeName)
        {
            foreach (var component in go.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == typeName) return true;
            return false;
        }

        // ---------------- 工具 ----------------
        /// 读 ImageTargetBehaviour 的 mTrackableName（字符串字面量取字段，避免跨类引用）
        static string ReadTrackableName(MonoBehaviour mb)
        {
            if (mb == null || mb.GetType().Name != "ImageTargetBehaviour") return "";
            var so = new SerializedObject(mb);
            var p = so.FindProperty("mTrackableName");
            return p == null ? "" : (p.stringValue ?? "").Trim().ToLower();
        }

        /// 把 VuforiaTargetController 的 simulateTargetName 设成本场景的目标名，方便编辑器里 Simulate Found
        static void SetSimulateName(MonoBehaviour targetBehaviour, string target, System.Text.StringBuilder log)
        {
            var vtc = targetBehaviour.GetComponent("VuforiaTargetController") as Component;
            if (vtc == null) { log.AppendLine("  警告：目标上未找到 VuforiaTargetController"); return; }
            var so = new SerializedObject(vtc);
            var p = so.FindProperty("simulateTargetName");
            if (p != null) { p.stringValue = target; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
