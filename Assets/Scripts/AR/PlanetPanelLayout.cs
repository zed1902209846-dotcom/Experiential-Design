using System.Collections.Generic;
using UnityEngine;

namespace SolarSystemAR
{
    /// Canvas 本地坐标。介绍框和名称均避开所有可见星球；没有可读空间时收起介绍框。
    public static class PlanetPanelLayout
    {
        private const float PlanetGap = 20f;
        private const float PanelGap = 12f;

        public static bool PlaceOverlay(Rect safe, IList<Rect> obstacles, Vector2 preferred, Vector2 minimum, out Rect rect)
        {
            return FindSpace(FreeSpace(safe, obstacles, PlanetGap), new Rect(safe.center.x, safe.yMin, 0, 0),
                preferred, minimum, out rect);
        }

        public static bool Arrange(Rect safe, IList<Rect> targets, IList<Rect> planets, List<Rect> result)
        {
            result.Clear();
            var free = FreeSpace(safe, planets, PlanetGap);
            bool allPlaced = true;
            for (int i = 0; i < targets.Count; i++)
            {
                Rect rect;
                // 新近打开的框优先；正文滚动，不再用覆盖星球的居中网格兜底。
                if (FindSpace(free, targets[i], new Vector2(320, targets.Count == 1 ? 400 : 340),
                    new Vector2(260, 180), out rect))
                {
                    result.Add(rect);
                    Subtract(free, Expand(rect, PanelGap));
                }
                else
                {
                    result.Add(Rect.zero);
                    allPlaced = false;
                }
            }
            return allPlaced;
        }

        public static bool PlaceLabel(Rect safe, Rect target, IList<Rect> planets, IList<Rect> occupied, out Rect rect)
        {
            var free = FreeSpace(safe, planets, PlanetGap);
            foreach (var panel in occupied)
                if (panel.width > 0 && panel.height > 0) Subtract(free, Expand(panel, PanelGap));
            return FindSpace(free, target, new Vector2(180, 48), new Vector2(180, 48), out rect);
        }

        private static List<Rect> FreeSpace(Rect safe, IList<Rect> planets, float gap)
        {
            var free = new List<Rect>();
            if (safe.width <= 0 || safe.height <= 0) return free;
            free.Add(safe);
            foreach (var planet in planets) Subtract(free, Expand(planet, gap));
            return free;
        }

        private static bool FindSpace(List<Rect> free, Rect target, Vector2 preferred, Vector2 minimum, out Rect result)
        {
            result = Rect.zero;
            float best = float.PositiveInfinity;
            foreach (var space in free)
            {
                float width = Mathf.Min(preferred.x, space.width);
                float height = Mathf.Min(preferred.y, space.height);
                if (width < minimum.x || height < minimum.y) continue;
                var candidate = new Rect(
                    Mathf.Clamp(target.center.x - width * 0.5f, space.xMin, space.xMax - width),
                    Mathf.Clamp(target.center.y - height * 0.5f, space.yMin, space.yMax - height), width, height);
                // 兼顾与所属星球的距离和可阅读面积；同一输入始终得到相同结果。
                float score = Vector2.Distance(candidate.center, target.center)
                    + (1f - width * height / (preferred.x * preferred.y)) * 240f;
                if (score >= best) continue;
                best = score;
                result = candidate;
            }
            return result.width > 0;
        }

        private static Rect Expand(Rect rect, float gap) => Rect.MinMaxRect(
            rect.xMin - gap, rect.yMin - gap, rect.xMax + gap, rect.yMax + gap);

        private static void Subtract(List<Rect> free, Rect obstacle)
        {
            // 保留重叠的最大空矩形，避免把星球上方/下方本来连续的空间切碎。
            for (int i = free.Count - 1; i >= 0; i--)
            {
                var space = free[i];
                if (!space.Overlaps(obstacle)) continue;
                free.RemoveAt(i);
                if (obstacle.xMin > space.xMin)
                    free.Add(Rect.MinMaxRect(space.xMin, space.yMin, obstacle.xMin, space.yMax));
                if (obstacle.xMax < space.xMax)
                    free.Add(Rect.MinMaxRect(obstacle.xMax, space.yMin, space.xMax, space.yMax));
                if (obstacle.yMin > space.yMin)
                    free.Add(Rect.MinMaxRect(space.xMin, space.yMin, space.xMax, obstacle.yMin));
                if (obstacle.yMax < space.yMax)
                    free.Add(Rect.MinMaxRect(space.xMin, obstacle.yMax, space.xMax, space.yMax));
            }
            for (int i = free.Count - 1; i >= 0; i--)
                for (int j = 0; j < free.Count; j++)
                {
                    if (i == j) continue;
                    var a = free[i]; var b = free[j];
                    if (a.xMin < b.xMin || a.yMin < b.yMin || a.xMax > b.xMax || a.yMax > b.yMax) continue;
                    free.RemoveAt(i);
                    break;
                }
        }
    }
}
