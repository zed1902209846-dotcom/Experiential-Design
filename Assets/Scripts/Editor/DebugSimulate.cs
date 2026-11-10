using UnityEditor;
using UnityEngine;

namespace SolarSystemAR.EditorTools
{
    /// Play 模式调试：菜单直接触发指定目标的 Simulate Found/Lost（绕开 GUI 选中）。
    public static class DebugSimulate
    {
        [MenuItem("Solar System/Debug/Simulate Found (Mars) %#m")]
        static void FoundMars() { Run("MarsTarget_V", true); }

        [MenuItem("Solar System/Debug/Simulate Lost (Mars) %#k")]
        static void LostMars() { Run("MarsTarget_V", false); }

        static void Run(string goName, bool found)
        {
            var go = GameObject.Find(goName);
            if (go == null) { Debug.LogError("[DebugSimulate] not found: " + goName); return; }
            var c = go.GetComponent<SolarSystemAR.VuforiaTargetController>();
            if (c == null) { Debug.LogError("[DebugSimulate] no controller: " + goName); return; }
            if (found) c.SimulateFound(); else c.SimulateLost();
            Debug.Log("[DebugSimulate] " + (found ? "FOUND" : "LOST") + " -> " + goName);
        }
    }
}
