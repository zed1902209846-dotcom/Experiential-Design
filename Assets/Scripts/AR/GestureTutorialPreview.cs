using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// A looping, noninteractive demonstration of the gesture requested by the tutorial.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GestureTutorialPreview : MaskableGraphic
    {
        private static readonly Color Planet = new Color(.2f, .70f, .85f);
        private static readonly Color Highlight = new Color(.46f, .88f, .96f);
        private static readonly Color Contact = new Color(.96f, .99f, 1f);
        private static readonly Color FingerEdge = new Color(.03f, .10f, .16f);
        private const int Segments = 32;
        private PlanetGesture gesture;
        private float startedAt;

        public void Play(PlanetGesture next)
        {
            gesture = next;
            startedAt = Time.unscaledTime;
            SetVerticesDirty();
        }

        private void Update() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float elapsed = Time.unscaledTime - startedAt;
            if (gesture == PlanetGesture.Resize)
            {
                float spread = .5f - .5f * Mathf.Cos(Mathf.Repeat(elapsed, 2.6f) / 2.6f * Mathf.PI * 2f);
                var offset = Vector2.Lerp(new Vector2(22f, 12f), new Vector2(77f, 31f), spread);
                DrawPlanet(mesh, Vector2.zero, Mathf.Lerp(25f, 43f, spread), 1f);
                DrawFinger(mesh, offset, new Vector2(.85f, -.53f), 1f);
                DrawFinger(mesh, -offset, new Vector2(-.85f, -.53f), 1f);
                return;
            }

            const float holdSeconds = .65f;
            const float dragSeconds = .95f;
            float time = Mathf.Repeat(elapsed, 2.45f);
            float progress = Mathf.Clamp01((time - holdSeconds) / dragSeconds);
            float x = Mathf.Lerp(-63f, 58f, Mathf.SmoothStep(0f, 1f, progress));
            float alpha = Mathf.Min(Mathf.Clamp01(time / .14f), Mathf.Clamp01((2.45f - time) / .23f));
            var center = new Vector2(x, 5f);
            var contact = center + new Vector2(4f, -10f);
            DrawPlanet(mesh, center, 30f, alpha);
            DrawFinger(mesh, contact, new Vector2(.37f, -.93f), alpha);
            if (time <= holdSeconds)
                Ring(mesh, contact, 24f, 19f, time / holdSeconds, Fade(Highlight, alpha));
        }

        private static void DrawPlanet(VertexHelper mesh, Vector2 center, float radius, float alpha)
        {
            Disc(mesh, center, radius + 2f, Fade(Highlight, alpha));
            Disc(mesh, center, radius, Fade(Planet, alpha));
            Disc(mesh, center + new Vector2(-radius * .31f, radius * .31f), radius * .19f, Fade(Highlight, alpha));
        }

        private static void DrawFinger(VertexHelper mesh, Vector2 contact, Vector2 direction, float alpha)
        {
            Vector2 end = contact + direction * 22f;
            Capsule(mesh, contact, end, 13f, Fade(FingerEdge, alpha));
            Capsule(mesh, contact, end, 9f, Fade(Contact, alpha));
        }

        private static void Capsule(VertexHelper mesh, Vector2 start, Vector2 end, float radius, Color color)
        {
            var delta = end - start;
            var normal = new Vector2(delta.y, -delta.x).normalized * radius;
            Quad(mesh, start + normal, end + normal, end - normal, start - normal, color);
            Disc(mesh, start, radius, color);
            Disc(mesh, end, radius, color);
        }

        private static void Disc(VertexHelper mesh, Vector2 center, float radius, Color color)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= Segments; i++)
            {
                mesh.AddVert(center + Direction(i / (float)Segments) * radius, color, Vector2.zero);
                if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
            }
        }

        private static void Ring(VertexHelper mesh, Vector2 center, float outside, float inside, float progress, Color color)
        {
            int steps = Mathf.CeilToInt(Segments * progress);
            for (int i = 0; i < steps; i++)
            {
                Vector2 start = Direction(i / (float)Segments);
                Vector2 end = Direction(Mathf.Min(progress, (i + 1f) / Segments));
                Quad(mesh, center + start * inside, center + start * outside,
                    center + end * outside, center + end * inside, color);
            }
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero);
            mesh.AddVert(b, color, Vector2.zero);
            mesh.AddVert(c, color, Vector2.zero);
            mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private static Vector2 Direction(float turn)
        {
            float angle = turn * Mathf.PI * 2f;
            return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        }

        private static Color Fade(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }
    }
}
