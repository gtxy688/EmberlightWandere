using UnityEngine;

namespace Emberlight
{
    /// <summary>把 M2 的纯数值配色（`EmberRarityUtil.Tint` 的 `float[3]` RGB，0–1）转成 `UnityEngine.Color`；数值层因此保持零 `UnityEngine` 引用，可脱离 Unity 跑 EditMode 测试。</summary>
    public static class EmberRarityTint
    {
        /// <summary>取稀有度配色；引用为空或不足 3 通道时回落白色，不抛异常。</summary>
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
