using UnityEngine;

namespace Emberlight
{
    /// <summary>在地图内、完整相机视口外取点。没有合法区域时返回 false，不把屏内点当作兜底。</summary>
    public static class EmberSpawnPlacement
    {
        public static bool TryFind(Camera camera, LevelConfig config, System.Random random,
            Vector2? requested, out Vector2 position)
        {
            position = default;
            if (camera == null) return false;
            float distance = Mathf.Abs(camera.transform.position.z);
            Vector3 min = camera.ViewportToWorldPoint(new Vector3(0, 0, distance));
            Vector3 max = camera.ViewportToWorldPoint(new Vector3(1, 1, distance));
            Vector3 upperLeft = camera.ViewportToWorldPoint(new Vector3(0, 1, distance));
            Vector3 lowerRight = camera.ViewportToWorldPoint(new Vector3(1, 0, distance));
            float margin = config.SpawnScreenPadding;
            var view = Rect.MinMaxRect(Mathf.Min(Mathf.Min(min.x, max.x), Mathf.Min(upperLeft.x, lowerRight.x)) - margin,
                Mathf.Min(Mathf.Min(min.y, max.y), Mathf.Min(upperLeft.y, lowerRight.y)) - margin,
                Mathf.Max(Mathf.Max(min.x, max.x), Mathf.Max(upperLeft.x, lowerRight.x)) + margin,
                Mathf.Max(Mathf.Max(min.y, max.y), Mathf.Max(upperLeft.y, lowerRight.y)) + margin);
            float limit = config.MapLimit - config.SpawnMapInset;
            var arena = Rect.MinMaxRect(-limit, -limit, limit, limit);
            if (requested.HasValue && arena.Contains(requested.Value) && !view.Contains(requested.Value))
            {
                position = requested.Value;
                return true;
            }
            // 只在视口附近的外沿带生成，避免把怪刷到地图远端、拉长等待时间。
            var band = Rect.MinMaxRect(Mathf.Max(arena.xMin, view.xMin - config.SpawnBandWidth),
                Mathf.Max(arena.yMin, view.yMin - config.SpawnBandWidth),
                Mathf.Min(arena.xMax, view.xMax + config.SpawnBandWidth),
                Mathf.Min(arena.yMax, view.yMax + config.SpawnBandWidth));
            return TryFind(view, band, random, out position);
        }

        public static bool TryFind(Rect excludedView, Rect arena, System.Random random, out Vector2 position)
        {
            float left = Mathf.Clamp(excludedView.xMin, arena.xMin, arena.xMax);
            float right = Mathf.Clamp(excludedView.xMax, arena.xMin, arena.xMax);
            float bottom = Mathf.Clamp(excludedView.yMin, arena.yMin, arena.yMax);
            float top = Mathf.Clamp(excludedView.yMax, arena.yMin, arena.yMax);
            var a = Rect.MinMaxRect(arena.xMin, arena.yMin, left, arena.yMax);
            var b = Rect.MinMaxRect(right, arena.yMin, arena.xMax, arena.yMax);
            var c = Rect.MinMaxRect(left, arena.yMin, right, bottom);
            var d = Rect.MinMaxRect(left, top, right, arena.yMax);
            float total = Area(a) + Area(b) + Area(c) + Area(d);
            position = default;
            if (total <= 0) return false;
            double pick = random.NextDouble() * total;
            Rect region;
            if (pick < Area(a)) region = a;
            else if ((pick -= Area(a)) < Area(b)) region = b;
            else if ((pick -= Area(b)) < Area(c)) region = c;
            else region = d;
            position = new Vector2(Mathf.Lerp(region.xMin, region.xMax, (float)random.NextDouble()),
                Mathf.Lerp(region.yMin, region.yMax, (float)random.NextDouble()));
            return arena.Contains(position) && !excludedView.Contains(position);
        }

        static float Area(Rect rect) => Mathf.Max(0, rect.width) * Mathf.Max(0, rect.height);
    }
}
