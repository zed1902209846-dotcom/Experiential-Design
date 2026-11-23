using UnityEngine;

namespace SolarSystemAR
{
    /// A camera-facing, collider-free figure that stays independent of the planet's rotation.
    public sealed class GravityStickFigure
    {
        private const float FootOffset = .2f;
        private readonly GameObject root;
        private readonly ScanPlanetContent content;
        private readonly SpriteRenderer renderer;
        private readonly Artwork artwork;

        public Bounds SurfaceBounds => content != null ? content.BodyWorldBounds : new Bounds();
        public bool Ready => content != null && content.Model != null && content.Model.activeInHierarchy;
        public Transform Root => root.transform;

        internal GravityStickFigure(ScanPlanetContent content, Transform parent, Artwork artwork)
        {
            this.content = content;
            this.artwork = artwork;
            root = new GameObject("Gravity Jumper " + content.name, typeof(SpriteRenderer));
            root.transform.SetParent(parent, false);
            root.SetActive(false);
            renderer = root.GetComponent<SpriteRenderer>();
        }

        public void Pose(Camera camera, float figureSize, float rise, bool airborne)
        {
            if (!Ready || camera == null)
            {
                Hide();
                return;
            }

            root.transform.SetPositionAndRotation(GroundPosition(camera, figureSize) + camera.transform.up * rise,
                camera.transform.rotation);
            var parentScale = root.transform.parent.lossyScale;
            root.transform.localScale = new Vector3(
                figureSize / Mathf.Max(.0001f, Mathf.Abs(parentScale.x)),
                figureSize / Mathf.Max(.0001f, Mathf.Abs(parentScale.y)),
                figureSize / Mathf.Max(.0001f, Mathf.Abs(parentScale.z)));
            var sprite = airborne ? artwork.Airborne : artwork.Standing;
            if (renderer.sprite != sprite) renderer.sprite = sprite;
            root.SetActive(true);
        }

        public Vector3 GroundPosition(Camera camera, float figureSize)
        {
            var bounds = SurfaceBounds;
            float radius = bounds.extents.x;
            float depth = radius * .04f;
            float foot = figureSize * FootOffset;
            float height = Mathf.Sqrt(Mathf.Max(0f, radius * radius - depth * depth - foot * foot));
            return bounds.center + camera.transform.up * height - camera.transform.forward * depth;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        public void Dispose()
        {
            Hide();
            if (root != null) Object.Destroy(root);
        }

        /// Two precomposited poses per lab color. Each pose renders once, including outline and
        /// overlapping joints, so the strokes cannot intersect or compete in the depth buffer.
        internal sealed class Artwork
        {
            private const int Width = 144;
            private const int Height = 224;
            private const int PixelsPerUnit = 192;
            private const int GroundPixel = 16;
            private const float CoreRadius = .0325f;
            private const float OutlineRadius = .065f;
            private static readonly Vector2 Hip = new Vector2(0f, .38f);
            private static readonly Vector2 Shoulders = new Vector2(0f, .68f);
            private static readonly Vector2 Neck = new Vector2(0f, .77f);
            private static readonly Vector2 Head = new Vector2(0f, .89f);

            public Sprite Standing { get; }
            public Sprite Airborne { get; }

            public Artwork(Color color)
            {
                Standing = Draw(color, .46f, "Stick figure standing");
                Airborne = Draw(color, .84f, "Stick figure jumping");
            }

            private static Sprite Draw(Color color, float hands, string name)
            {
                var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true);
                texture.name = name;
                texture.filterMode = FilterMode.Trilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                var outline = Color.Lerp(color, Color.white, .75f);
                var pixels = new Color32[Width * Height];
                var hand = new Vector2(.24f, hands);
                var foot = new Vector2(FootOffset, 0f);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        // The left and right limbs mirror each other.
                        float horizontal = Mathf.Abs(x + .5f - Width * .5f) / PixelsPerUnit;
                        var point = new Vector2(horizontal, (y + .5f - GroundPixel) / PixelsPerUnit);
                        float line = Mathf.Min(DistanceSquared(point, Hip, Neck), DistanceSquared(point, Shoulders, hand));
                        line = Mathf.Min(line, DistanceSquared(point, Hip, foot));
                        float distance = Mathf.Min(Mathf.Sqrt(line), Mathf.Abs(Vector2.Distance(point, Head) - .12f));
                        var ink = Color.Lerp(outline, color, Coverage(distance, CoreRadius));
                        ink.a = Coverage(distance, OutlineRadius);
                        pixels[y * Width + x] = ink;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(true, true);
                var sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height),
                    new Vector2(.5f, GroundPixel / (float)Height), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = name;
                return sprite;
            }

            private static float Coverage(float distance, float radius)
                => Mathf.Clamp01((radius - distance) * PixelsPerUnit + .5f);

            private static float DistanceSquared(Vector2 point, Vector2 from, Vector2 to)
            {
                var axis = to - from;
                float fraction = Mathf.Clamp01(Vector2.Dot(point - from, axis) / axis.sqrMagnitude);
                return (point - (from + axis * fraction)).sqrMagnitude;
            }

            public void Dispose()
            {
                Object.Destroy(Standing.texture);
                Object.Destroy(Airborne.texture);
                Object.Destroy(Standing);
                Object.Destroy(Airborne);
            }
        }
    }
}
