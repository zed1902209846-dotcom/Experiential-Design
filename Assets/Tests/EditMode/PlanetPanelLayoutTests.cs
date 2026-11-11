using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SolarSystemAR.Tests
{
    public class PlanetPanelLayoutTests
    {
        private static readonly Rect Safe = new Rect(0, 0, 720, 1280);

        private static void AssertInside(Rect outer, Rect inner)
        {
            Assert.GreaterOrEqual(inner.xMin, outer.xMin);
            Assert.GreaterOrEqual(inner.yMin, outer.yMin);
            Assert.LessOrEqual(inner.xMax, outer.xMax);
            Assert.LessOrEqual(inner.yMax, outer.yMax);
        }

        [Test]
        public void SinglePanelGetsFullSizeBesideThePlanet()
        {
            var planet = new Rect(260, 540, 200, 200);
            var result = new List<Rect>();
            bool placed = PlanetPanelLayout.Arrange(Safe, new[] { planet }, new[] { planet }, result);
            Assert.IsTrue(placed);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Vector2(320, 400), result[0].size);
            Assert.IsFalse(result[0].Overlaps(planet));
            AssertInside(Safe, result[0]);
        }

        [Test]
        public void TwoPanelsAvoidPlanetsAndEachOther()
        {
            var planets = new[] { new Rect(80, 900, 160, 160), new Rect(460, 260, 160, 160) };
            var result = new List<Rect>();
            Assert.IsTrue(PlanetPanelLayout.Arrange(Safe, planets, planets, result));
            Assert.AreEqual(2, result.Count);
            Assert.IsFalse(result[0].Overlaps(result[1]));
            foreach (var panel in result)
            {
                AssertInside(Safe, panel);
                Assert.LessOrEqual(panel.height, 340f);
                foreach (var planet in planets) Assert.IsFalse(panel.Overlaps(planet));
            }
        }

        [Test]
        public void PanelCollapsesWhenThePlanetFillsTheScreen()
        {
            var result = new List<Rect>();
            bool placed = PlanetPanelLayout.Arrange(Safe, new[] { Safe }, new[] { Safe }, result);
            Assert.IsFalse(placed);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(Rect.zero, result[0]);
        }

        [Test]
        public void LayoutIsDeterministic()
        {
            var planets = new[] { new Rect(80, 900, 160, 160), new Rect(460, 260, 160, 160) };
            var first = new List<Rect>();
            var second = new List<Rect>();
            PlanetPanelLayout.Arrange(Safe, planets, planets, first);
            PlanetPanelLayout.Arrange(Safe, planets, planets, second);
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void LabelAvoidsPlanetsAndOpenPanels()
        {
            var planet = new Rect(260, 540, 200, 200);
            var panels = new List<Rect>();
            PlanetPanelLayout.Arrange(Safe, new[] { planet }, new[] { planet }, panels);
            Assert.IsTrue(PlanetPanelLayout.PlaceLabel(Safe, planet, new[] { planet }, panels, out Rect label));
            Assert.AreEqual(new Vector2(180, 48), label.size);
            Assert.IsFalse(label.Overlaps(planet));
            Assert.IsFalse(label.Overlaps(panels[0]));
            AssertInside(Safe, label);
        }

        [Test]
        public void OverlayFallsBackToNothingWhenBlocked()
        {
            Assert.IsFalse(PlanetPanelLayout.PlaceOverlay(Safe, new[] { Safe },
                new Vector2(660, 280), new Vector2(360, 272), out Rect rect));
            Assert.AreEqual(Rect.zero, rect);
        }
    }
}
