using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 把 M2 的纯数值配色转成 `UnityEngine.Color` 的适配器。
    ///
    /// 存在的理由：`EmberRarityUtil.Tint` 返回 `float[3]`（RGB，0–1），这样
    /// `EmberRarity.cs` / `RunProgress.cs` / `EmberChoicePool.cs` 都能保持零 `UnityEngine` 引用、
    /// 可以脱离 Unity 做 EditMode 测试；而"怎么变成 Unity 的颜色"属于表现层的事，放在这里。
    /// </summary>
    public static class EmberRarityTint
    {
        /// <summary>取稀有度配色。数组不足 3 项时缺的通道按 0 处理，不抛异常。</summary>
        public static Color ToColor(this EmberRarity rarity)
        {
            return EmberRarityUtil.Tint(rarity).ToColor();
        }

        /// <summary>把 `float[3]` 的 RGB 转成 <see cref="Color"/>（alpha = 1）。</summary>
        public static Color ToColor(this float[] rgb)
        {
            if (rgb == null || rgb.Length < 3) return Color.white;
            return new Color(rgb[0], rgb[1], rgb[2]);
        }
    }
}
