using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

namespace SolarSystemAR.EditorTools
{
    /// 探测：ARCamera 加 PhysicsRaycaster + 创建 Image Target 并导出可配置 API 信息。
    public static class ScanSceneARProbe
    {
        private const string OutPath = @"D:\workbody\生成文件\2026-09-24-11-31-39\vuf_probe.txt";

        [MenuItem("Solar System/Setup/Step5 Probe")]
        public static void Run()
        {
            var lines = new List<string>();

            // 1. ARCamera 添加 PhysicsRaycaster
            var arcam = GameObject.Find("ARCamera");
            lines.Add("ARCamera found: " + (arcam != null));
            if (arcam != null && arcam.GetComponent<PhysicsRaycaster>() == null)
                Undo.AddComponent<PhysicsRaycaster>(arcam);
            lines.Add("PhysicsRaycaster on ARCamera: " + (arcam != null && arcam.GetComponent<PhysicsRaycaster>() != null));

            // 2. 通过 Vuforia 官方菜单创建 Image Target
            bool created = EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/Image Target");
            lines.Add("ExecuteMenuItem(Image Target): " + created);
            var go = Selection.activeGameObject;
            lines.Add("Selected GO: " + (go != null ? go.name : "NULL"));

            if (go != null)
            {
                var itb = go.GetComponent<ImageTargetBehaviour>();
                lines.Add("Has ImageTargetBehaviour: " + (itb != null));

                if (itb != null)
                {
                    lines.Add("--- SerializedProperties ---");
                    var so = new SerializedObject(itb);
                    var it = so.GetIterator();
                    while (it.Next(true))
                    {
                        string val = it.propertyType == SerializedPropertyType.String ? it.stringValue
                                   : it.propertyType == SerializedPropertyType.ObjectReference ? (it.objectReferenceValue != null ? it.objectReferenceValue.name : "null")
                                   : it.propertyType.ToString();
                        lines.Add("PROP " + it.propertyPath + " = " + val);
                    }

                    lines.Add("--- Public Methods of ImageTargetBehaviour ---");
                    foreach (var m in typeof(ImageTargetBehaviour).GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        lines.Add("API " + m.Name + "(" + string.Join(",", System.Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name)) + ")");

                    lines.Add("--- Public Properties ---");
                    foreach (var p in typeof(ImageTargetBehaviour).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        lines.Add("PROP2 " + p.PropertyType.Name + " " + p.Name);
                }
            }

            File.WriteAllLines(OutPath, lines);
            Debug.Log("[Probe] DONE -> " + OutPath);
        }
    }
}
