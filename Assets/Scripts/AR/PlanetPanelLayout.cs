using System.Collections.Generic;
using UnityEngine;

namespace SolarSystemAR
{
    /// Canvas 本地坐标。介绍框和名称均避开所有可见星球；没有可读空间时收起介绍框。
    public static class PlanetPanelLayout
    {
        private const float PlanetGap = 20f;
        private const float PanelGap = 12f;
        private const float SwitchPenalty = 80f;
        private const float FollowSlow = 5f, FollowFast = 28f, FollowFastDistance = 120f;

        public static bool PlaceOverlay(Rect safe, IList<Rect> obstacles, Vector2 preferred, Vector2 minimum, out Rect rect)
        {
            return FindSpace(FreeSpace(safe, obstacles, PlanetGap), new Rect(safe.center.x, safe.yMin, 0, 0),
                preferred, minimum, out rect);
        }

        public static bool Arrange(Rect safe, IList<Rect> targets, IList<Rect> planets, List<Rect> result)
        {
            return Arrange(safe, targets, planets, null, result);
        }

        /// previousOffsets：与 targets 一一对应，上一帧介绍框中心相对星球中心的偏移（没有则为 null）。
        public static bool Arrange(Rect safe, IList<Rect> targets, IList<Rect> planets,
            IList<Vector2?> previousOffsets, List<Rect> result)
        {
            result.Clear();
            var free = FreeSpace(safe, planets, PlanetGap);
            bool allPlaced = true;
            for (int i = 0; i < targets.Count; i++)
            {
                Rect rect;
                // 新近打开的框优先；正文滚动，不再用覆盖星球的居中网格兜底。
                if (FindSpace(free, targets[i], new Vector2(320, targets.Count == 1 ? 400 : 340),
                    new Vector2(260, 180), out rect,
                    previousOffsets != null && i < previousOffsets.Count ? previousOffsets[i] : null))
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
            return PlaceLabel(safe, target, planets, occupied, null, out rect);
        }

        public static bool PlaceProfileToLeft(Rect safe, Rect target, IList<Rect> obstacles, IList<Rect> occupied,
            Vector2 preferred, out Rect rect)
        {
            var free = FreeSpace(safe, obstacles, PlanetGap);
            foreach (var profile in occupied) Subtract(free, Expand(profile, PanelGap));
            var minimum = new Vector2(212f, 190f);
            // Prefer directly beside the planet. A place above or below its left half can fit near an edge.
            return FindSpace(free, target, preferred, minimum, out rect, rightLimit: target.xMin - PanelGap)
                || FindSpace(free, target, preferred, minimum, out rect, rightLimit: target.center.x - PanelGap);
        }

        /// previousOffset：上一帧名称中心相对星球中心的偏移。上下两侧得分接近时保持原来的一侧，
        /// 避免追踪抖动让名称在星球上方和下方之间来回跳。
        public static bool PlaceLabel(Rect safe, Rect target, IList<Rect> planets, IList<Rect> occupied,
            Vector2? previousOffset, out Rect rect)
        {
            var free = FreeSpace(safe, planets, PlanetGap);
            foreach (var panel in occupied)
                if (panel.width > 0 && panel.height > 0) Subtract(free, Expand(panel, PanelGap));
            return FindSpace(free, target, new Vector2(180, 48), new Vector2(180, 48), out rect, previousOffset);
        }

        /// 让显示位置平滑地追向布局位置：几像素的追踪抖动被大幅削弱，大幅移动（换边、快速移动设备）仍然跟得紧。
        public static Vector2 Follow(Vector2 shown, Vector2 target, float deltaTime)
        {
            float distance = Vector2.Distance(shown, target);
            if (distance < 0.01f) return target;
            float rate = Mathf.Lerp(FollowSlow, FollowFast, distance / FollowFastDistance);
            return Vector2.Lerp(shown, target, 1f - Mathf.Exp(-rate * Mathf.Max(0f, deltaTime)));
        }

        private static List<Rect> FreeSpace(Rect safe, IList<Rect> planets, float gap)
        {
            var free = new List<Rect>();
            if (safe.width <= 0 || safe.height <= 0) return free;
            free.Add(safe);
            foreach (var planet in planets) Subtract(free, Expand(planet, gap));
            return free;
        }

        private static bool FindSpace(List<Rect> free, Rect target, Vector2 preferred, Vector2 minimum, out Rect result,
            Vector2? previousOffset = null, float rightLimit = float.PositiveInfinity)
        {
            result = Rect.zero;
            float best = float.PositiveInfinity;
            foreach (var available in free)
            {
                var space = available;
                space.xMax = Mathf.Min(space.xMax, rightLimit);
                float width = Mathf.Min(preferred.x, space.width);
                float height = Mathf.Min(preferred.y, space.height);
                if (width < minimum.x || height < minimum.y) continue;
                var candidate = new Rect(
                    Mathf.Clamp(target.center.x - width * 0.5f, space.xMin, space.xMax - width),
                    Mathf.Clamp(target.center.y - height * 0.5f, space.yMin, space.yMax - height), width, height);
                // 兼顾与所属星球的距离和可阅读面积；同一输入始终得到相同结果。
                float score = Vector2.Distance(candidate.center, target.center)
                    + (1f - width * height / (preferred.x * preferred.y)) * 240f;
                // 换边要比留在原处明显更好才值得，否则保持上一帧的位置关系。
                if (previousOffset.HasValue)
                    score += Mathf.Min(SwitchPenalty,
                        Vector2.Distance(candidate.center - target.center, previousOffset.Value));
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
