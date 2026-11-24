using NUnit.Framework;
using UnityEngine;

namespace SolarSystemAR.Tests
{
    public class SunLampMathTests
    {
        [Test]
        public void LampSitsOnTheSunwardSideOfThePlanet()
        {
            var sun = new Vector3(1f, 0f, 0f);
            Assert.IsTrue(SunLampMath.TryCompute(sun, Vector3.zero, 0.02f, 1f, out var pose));
            Assert.AreEqual(0.12f, pose.position.x, 1e-4f);
            Assert.AreEqual(0f, pose.position.y, 1e-4f);
            Assert.AreEqual(0f, pose.position.z, 1e-4f);
        }

        [Test]
        public void MovingTheSunCardToTheOtherSideFlipsTheLitSide()
        {
            SunLampMath.TryCompute(new Vector3(0.3f, 0f, 0f), Vector3.zero, 0.02f, 1f, out var right);
            SunLampMath.TryCompute(new Vector3(-0.3f, 0f, 0f), Vector3.zero, 0.02f, 1f, out var left);
            Assert.Less(Vector3.Distance(right.position, -left.position), 1e-5f);
            Assert.Greater(right.position.x, 0f);
        }

        [Test]
        public void BrightnessOnThePlanetDoesNotDependOnCardDistanceOrPlanetSize()
        {
            SunLampMath.TryCompute(new Vector3(0.1f, 0f, 0f), Vector3.zero, 0.015f, 1.6f, out var near);
            SunLampMath.TryCompute(new Vector3(0.9f, 0f, 0f), Vector3.zero, 0.015f, 1.6f, out var far);
            SunLampMath.TryCompute(new Vector3(0.9f, 0f, 0f), Vector3.zero, 0.05f, 1.6f, out var big);
            Assert.AreEqual(near.intensity, far.intensity, 1e-6f);
            float Irradiance(SunLampMath.Pose pose, float radius)
            {
                float distance = radius * SunLampMath.DistanceInRadii;
                return pose.intensity / (distance * distance);
            }
            Assert.AreEqual(1.6f, Irradiance(near, 0.015f), 1e-4f);
            Assert.AreEqual(1.6f, Irradiance(big, 0.05f), 1e-4f);
        }

        [Test]
        public void RangeCoversTheWholePlanet()
        {
            const float radius = 0.02f;
            SunLampMath.TryCompute(Vector3.up, Vector3.zero, radius, 1f, out var pose);
            float distance = pose.position.magnitude;
            Assert.Greater(pose.range, distance + radius);
        }

        [Test]
        public void LampNeverGoesPastTheSun()
        {
            // 太阳比 6 个半径更近：灯停在太阳中心，照度仍然恒定。
            var sun = new Vector3(0.05f, 0f, 0f);
            Assert.IsTrue(SunLampMath.TryCompute(sun, Vector3.zero, 0.02f, 2f, out var pose));
            Assert.Less(Vector3.Distance(pose.position, sun), 1e-6f);
            Assert.AreEqual(2f, pose.intensity / (0.05f * 0.05f), 1e-4f);
            Assert.Greater(pose.range, 0.05f + 0.02f);
        }

        [Test]
        public void SunOnTopOfThePlanetGivesNoLamp()
        {
            Assert.IsFalse(SunLampMath.TryCompute(Vector3.zero, Vector3.zero, 0.02f, 1f, out _));
            Assert.IsFalse(SunLampMath.TryCompute(Vector3.right, Vector3.zero, 0f, 1f, out _));
        }
    }
}
