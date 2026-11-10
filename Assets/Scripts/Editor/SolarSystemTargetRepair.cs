using SolarSystemAR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace SolarSystemAR.EditorBuildScripts
{
    public static class SolarSystemTargetRepair
    {
        const string ScenePath = "Assets/Scenes/App/SolarSystemScene.unity";
        static readonly string[] TargetScenes =
        {
            "ScanScene", "Eearth", "MercuryScene", "VenusScene", "MarsScene",
            "JupiterScene", "SaturnScene", "UranusScene", "NeptuneScene", "SunScene", "SolarSystemScene"
        };

        class TargetSize
        {
            public float width;
            public float height;
        }

        [MenuItem("Solar System/Repair/Bind Existing AR Targets to Databases")]
        public static void BindExistingTargets()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Save the active scene before binding AR targets.");
                return;
            }

            var sizes = LoadDatabaseSizes();
            foreach (var sceneName in TargetScenes)
                if (!File.Exists("Assets/Scenes/App/" + sceneName + ".unity"))
                {
                    Debug.LogError("Missing scene: " + sceneName + "; no scenes were changed.");
                    return;
                }

            foreach (var sceneName in TargetScenes)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/App/" + sceneName + ".unity", OpenSceneMode.Single);
                var targets = FindImageTargets();
                if (targets.Count == 0)
                {
                    Debug.LogError(sceneName + " has no ImageTargetBehaviour; stopped before saving this scene.");
                    return;
                }

                foreach (var target in targets)
                {
                    var data = new SerializedObject(target);
                    var name = data.FindProperty("mTrackableName");
                    var path = data.FindProperty("mDataSetPath");
                    var type = data.FindProperty("mImageTargetType");
                    if (name == null || path == null || type == null)
                    {
                        Debug.LogError(sceneName + ": could not read Vuforia target fields.");
                        return;
                    }

                    string targetName = name.stringValue.Trim().ToLowerInvariant();
                    string databasePath = targetName.StartsWith("venus")
                        ? "Vuforia/VenusCard.xml"
                        : "Vuforia/SolarCards.xml";
                    if (!sizes.TryGetValue(databasePath, out var database) || !database.TryGetValue(targetName, out var size))
                    {
                        Debug.LogError(sceneName + ": " + targetName + " is missing from " + databasePath + "; scene was not saved.");
                        return;
                    }

                    Undo.RecordObject(target.transform, "Bind Vuforia Image Target");
                    target.transform.localScale = Vector3.one;
                    name.stringValue = targetName;
                    path.stringValue = databasePath;
                    type.intValue = 0;
                    data.FindProperty("mWidth").floatValue = size.width;
                    data.FindProperty("mHeight").floatValue = size.height;
                    data.FindProperty("mAspectRatio").floatValue = size.height / size.width;
                    data.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    Debug.LogError("Could not save " + sceneName + ".");
                    return;
                }
                Debug.Log(sceneName + ": bound " + targets.Count + " target(s) to the matching Vuforia database image.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("All AR target scenes now use their matching database images. No objects or UI were removed.");
        }

        [MenuItem("Solar System/Repair/SolarSystem Image Target (Preserve Objects)")]
        public static void Repair()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogError("Save the active scene before repairing SolarSystemScene.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var target = GameObject.Find("ImageTarget");
            var legacyTarget = GameObject.Find("SolarSystemTarget_V");
            var model = target != null ? target.transform.Find("SolarSystem") : null;
            var camera = GameObject.Find("ARCamera");

            if (target == null || legacyTarget == null || model == null || camera == null ||
                Object.FindFirstObjectByType<ScanController>(FindObjectsInactive.Include) == null ||
                Object.FindFirstObjectByType<PlanetCardController>(FindObjectsInactive.Include) == null)
            {
                Debug.LogError("SolarSystemScene is missing its target, model, ARCamera, or scan UI; no changes were saved.");
                return;
            }

            MonoBehaviour imageTarget = null;
            foreach (var component in target.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "ImageTargetBehaviour")
                {
                    imageTarget = component;
                    break;
                }

            if (imageTarget == null ||
                !HasComponent(target, "DefaultObserverEventHandler"))
            {
                Debug.LogError("ImageTarget is missing its Vuforia observer or default event handler; no changes were saved.");
                return;
            }

            var imageData = new SerializedObject(imageTarget);
            var trackableName = imageData.FindProperty("mTrackableName");
            var dataSetPath = imageData.FindProperty("mDataSetPath");
            var imageType = imageData.FindProperty("mImageTargetType");
            if (trackableName == null || dataSetPath == null || imageType == null)
            {
                Debug.LogError("ImageTarget Vuforia fields could not be verified; no changes were saved.");
                return;
            }

            Undo.RecordObjects(new Object[] { target.transform, model, legacyTarget }, "Repair SolarSystem Image Target");
            target.transform.localPosition = Vector3.zero;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;

            trackableName.stringValue = "solarsystem";
            dataSetPath.stringValue = "Vuforia/SolarCards.xml";
            imageType.intValue = 0;
            imageData.ApplyModifiedProperties();

            var controller = target.GetComponent<VuforiaTargetController>();
            if (controller == null) controller = Undo.AddComponent<VuforiaTargetController>(target);
            var controllerData = new SerializedObject(controller);
            controllerData.FindProperty("simulateTargetName").stringValue = "solarsystem";
            controllerData.ApplyModifiedProperties();

            var rotation = model.GetComponent<SelfRotation>();
            if (rotation == null) rotation = Undo.AddComponent<SelfRotation>(model.gameObject);
            rotation.axis = Vector3.up;
            rotation.degreesPerSecond = 12f;

            foreach (var component in legacyTarget.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "ImageTargetBehaviour")
                    component.enabled = false;
            legacyTarget.SetActive(false);

            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(model);
            EditorUtility.SetDirty(legacyTarget);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("Could not save SolarSystemScene.");
                return;
            }

            Debug.Log("SolarSystemScene repaired: SolarCards/solarsystem ImageTarget, unit transform, centered rotating model, existing scan UI preserved. Legacy target retained but inactive; ARCamera unchanged.");
        }

        static bool HasComponent(GameObject target, string typeName)
        {
            foreach (var component in target.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == typeName) return true;
            return false;
        }

        static List<MonoBehaviour> FindImageTargets()
        {
            var result = new List<MonoBehaviour>();
            foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (component != null && component.GetType().Name == "ImageTargetBehaviour") result.Add(component);
            return result;
        }

        static Dictionary<string, Dictionary<string, TargetSize>> LoadDatabaseSizes()
        {
            var result = new Dictionary<string, Dictionary<string, TargetSize>>();
            foreach (var databasePath in new[] { "Vuforia/SolarCards.xml", "Vuforia/VenusCard.xml" })
            {
                string path = "Assets/StreamingAssets/" + databasePath;
                var database = new Dictionary<string, TargetSize>();
                foreach (var target in XDocument.Load(path).Descendants("ImageTarget"))
                {
                    var size = target.Attribute("size").Value.Split(' ');
                    database[target.Attribute("name").Value.ToLowerInvariant()] = new TargetSize
                    {
                        width = float.Parse(size[0], System.Globalization.CultureInfo.InvariantCulture),
                        height = float.Parse(size[1], System.Globalization.CultureInfo.InvariantCulture)
                    };
                }
                result[databasePath] = database;
            }
            return result;
        }
    }
}
