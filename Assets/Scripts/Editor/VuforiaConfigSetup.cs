using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SolarSystemAR.EditorBuildScripts
{
    /// Vuforia 11 移除了 Configuration 菜单入口，此脚本用于强制生成
    /// Assets/Resources/VuforiaConfiguration.asset 并在 Inspector 中选中，
    /// 用户随后在 "App License Key" 字段粘贴 License Key。
    /// 菜单：Solar System > Setup > Create Vuforia Configuration Asset
    public static class VuforiaConfigSetup
    {
        const string AssetPath = "Assets/Resources/VuforiaConfiguration.asset";

        [MenuItem("Solar System/Setup/Create Vuforia Configuration Asset")]
        public static void CreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetPath);
            if (config == null)
            {
                var type = FindVuforiaConfigurationType();
                if (type == null)
                {
                    Debug.LogError("VuforiaConfigSetup: 未找到 Vuforia.VuforiaConfiguration 类型，请确认 Vuforia 已安装。");
                    return;
                }

                // 优先走 Vuforia 官方单例（内部会创建并落盘资产）
                object instance = null;
                var instProp = type.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (instProp != null)
                {
                    try { instance = instProp.GetValue(null); } catch { /* 忽略，走手动创建 */ }
                }

                // 兜底：手动创建 ScriptableObject 资产
                if (instance == null)
                {
                    instance = ScriptableObject.CreateInstance(type);
                    if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
                    AssetDatabase.CreateAsset((UnityEngine.Object)instance, AssetPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                config = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetPath);

                if (config == null && instance is UnityEngine.Object so)
                {
                    // 单例存在但未落盘：把已有实例另存为目标路径
                    if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
                    AssetDatabase.CreateAsset(so, AssetPath);
                    AssetDatabase.SaveAssets();
                    config = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetPath);
                }
            }

            if (config == null)
            {
                Debug.LogError("VuforiaConfigSetup: 创建 " + AssetPath + " 失败。");
                return;
            }

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            Debug.Log("VuforiaConfigSetup: 已生成并选中 VuforiaConfiguration（" + AssetPath +
                      "）。请在 Inspector 中找到 App License Key 字段，粘贴你的 License Key，然后 Ctrl+S 保存。");
        }

        static Type FindVuforiaConfigurationType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType("Vuforia.VuforiaConfiguration");
                if (t != null) return t;
            }
            return null;
        }
    }
}
