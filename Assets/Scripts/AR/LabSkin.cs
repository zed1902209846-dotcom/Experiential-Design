using UnityEngine;

namespace SolarSystemAR
{
    /// 实验面板用的圆角底板和描边：运行时生成九宫格图，不依赖额外的美术资源。
    public static class LabSkin
    {
        private const int Size = 64;
        private const float Radius = 20f;
        private const float RingThickness = 3f;
        private static Sprite solid, ring;

        public static Sprite Solid { get { if (solid == null) solid = Build(false); return solid; } }
        public static Sprite Ring { get { if (ring == null) ring = Build(true); return ring; } }

        private static Sprite Build(bool hollow)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[Size * Size];
            float half = Size * 0.5f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // 圆角矩形的有向距离：内部为负，边缘 1 像素做抗锯齿。
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - Radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - Radius);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - Radius;
                    float alpha = Mathf.Clamp01(0.5f - distance);
                    if (hollow) alpha *= Mathf.Clamp01(0.5f + distance + RingThickness);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            float border = Radius + 4f;
            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
