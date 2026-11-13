using UnityEngine;

namespace SolarSystemAR
{
    /// 太阳卡光源的几何：每颗星球配一盏贴身点光源，放在"太阳所在方向"上。
    /// 灯到星球的距离与星球半径成正比，所以亮度不随两张卡的远近变化，只有受光方向跟着卡片走。
    public static class SunLampMath
    {
        public struct Pose
        {
            public Vector3 position;
            public float range;
            public float intensity;
        }

        // 灯离球心 6 个半径：晨昏线落在约 80°，接近平行光的半明半暗。
        public const float DistanceInRadii = 6f;
        // 照射范围留出余量，球面边缘才不会被范围衰减压暗；范围越大越容易照到紧挨着的另一张卡。
        private const float RangeMarginInRadii = 3f;

        /// source：光从哪里来（太阳卡上的太阳中心）。两者几乎重合时没有可用方向，返回 false。
        public static bool TryCompute(Vector3 source, Vector3 planet, float radius, float brightness, out Pose pose)
        {
            pose = default;
            var toSource = source - planet;
            if (radius <= 0f || toSource.sqrMagnitude <= radius * radius * 0.01f) return false;
            var direction = toSource.normalized;
            // 太阳卡靠得很近时灯就放在太阳中心，绝不越过太阳——否则灯会落到太阳另一侧的行星里。
            float distance = Mathf.Min(radius * DistanceInRadii, toSource.magnitude);
            pose.position = planet + direction * distance;
            pose.range = distance + radius * RangeMarginInRadii;
            // 点光源按距离平方衰减：乘回去，球面受到的照度恒等于 brightness。
            pose.intensity = brightness * distance * distance;
            return true;
        }
    }
}
