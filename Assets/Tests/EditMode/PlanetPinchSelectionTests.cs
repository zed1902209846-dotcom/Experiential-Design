using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SolarSystemAR.Tests
{
    public class PlanetPinchSelectionTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private Camera camera;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("Selection test camera");
            objects.Add(go);
            camera = go.AddComponent<Camera>();
            camera.pixelRect = new Rect(0, 0, 800, 600);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in objects)
            {
                var planet = go.GetComponent<PlanetPinchScale>();
                if (planet != null) typeof(PlanetPinchScale).GetMethod("OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(planet, null);
                Object.DestroyImmediate(go);
            }
            objects.Clear();
        }

        private PlanetPinchScale Planet(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objects.Add(go);
            go.transform.position = position;
            var planet = go.AddComponent<PlanetPinchScale>();
            // EditMode does not run these MonoBehaviour lifecycle callbacks.
            typeof(PlanetPinchScale).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(planet, null);
            typeof(PlanetPinchScale).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(planet, null);
            return planet;
        }

        private PlanetPinchScale Select(Vector2 point)
        {
            return (PlanetPinchScale)typeof(PlanetPinchScale).GetMethod("PlanetAtPosition",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { camera, point });
        }

        [Test]
        public void ModelCenterSelectsPlanetButEmptySpaceDoesNot()
        {
            var planet = Planet(new Vector3(0, 0, 5));
            Assert.AreSame(planet, Select(new Vector2(400, 300)));
            Assert.IsNull(Select(new Vector2(20, 20)));
        }

        [TestCase(20, 0, 5)]
        [TestCase(0, 0, -5)]
        [TestCase(0, 0, 150)]
        public void OffscreenAndClippedPlanetsAreNotSelected(float x, float y, float z)
        {
            Planet(new Vector3(x, y, z));
            Assert.IsNull(Select(new Vector2(400, 300)));
        }

        [Test]
        public void OverlappingPlanetsSelectFrontModel()
        {
            Planet(new Vector3(0, 0, 8));
            var front = Planet(new Vector3(0, 0, 4));
            Assert.AreSame(front, Select(new Vector2(400, 300)));
        }

        [Test]
        public void HiddenRendererAndDisabledPlanetAreNotSelected()
        {
            var planet = Planet(new Vector3(0, 0, 5));
            planet.GetComponent<Renderer>().enabled = false;
            Assert.IsNull(Select(new Vector2(400, 300)));
            planet.GetComponent<Renderer>().enabled = true;
            planet.gameObject.SetActive(false);
            Assert.IsNull(Select(new Vector2(400, 300)));
        }

        [Test]
        public void SelectionFollowsMovedAndScaledModel()
        {
            var planet = Planet(new Vector3(0, 0, 5));
            planet.transform.position = new Vector3(2, 0, 5);
            planet.transform.localScale = Vector3.one * 2;
            Assert.IsNull(Select(new Vector2(400, 300)));
            Assert.AreSame(planet, Select(camera.WorldToScreenPoint(planet.transform.position)));
        }
    }
}
