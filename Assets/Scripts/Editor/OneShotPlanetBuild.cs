#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SolarSystemAR.EditorBuildScripts
{
    /// 临时一次性执行器：当 D:\Trae生成文件\666\RUN_PLANET_BUILD.flag 存在时，
    /// 在下次脚本重载后自动依次执行 BuildAll + PlanetSceneBuilder.Run。
    /// 执行完写结果到 oneshot_result.txt。用完即删本文件。（rev2：等待 marker 后触发）
    public static class OneShotPlanetBuild
    {
        const string Marker = @"D:\Trae生成文件\666\RUN_PLANET_BUILD.flag";
        const string OutFile = @"D:\Trae生成文件\666\oneshot_result.txt";

        [InitializeOnLoadMethod]
        static void Check()
        {
            if (!File.Exists(Marker)) return;
            try { File.Delete(Marker); } catch { }
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                sb.AppendLine("[OneShot] BuildAll 开始");
                SceneBuilder.BuildAll();
                sb.AppendLine("[OneShot] BuildAll 完成");

                sb.AppendLine("[OneShot] PlanetSceneBuilder 开始");
                PlanetSceneBuilder.Run();
                sb.AppendLine("[OneShot] PlanetSceneBuilder 完成");

                sb.AppendLine("[OneShot] ALL DONE");
            }
            catch (System.Exception e)
            {
                sb.AppendLine("[OneShot] FAILED: " + e);
                Debug.LogError("[OneShot] FAILED: " + e);
            }
            File.WriteAllText(OutFile, sb.ToString());
        }
    }
}
#endif
