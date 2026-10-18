using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>原版角色共用的灰度部件；保留各兵种的颜色、尺寸与动画层级。</summary>
    public static class EmberCharacterArt
    {
        static Dictionary<string, Sprite> parts;

        public static Sprite Cloak => Part("classic-cloak");
        public static Sprite Hood => Part("classic-hood");
        public static Sprite Hem => Part("classic-hem");
        public static Sprite Lantern => Part("classic-lantern");

        static Sprite Part(string name)
        {
            if (parts == null)
            {
                parts = new Dictionary<string, Sprite>();
                foreach (var sprite in Resources.LoadAll<Sprite>("Art/Characters/classic-character-parts"))
                    parts[sprite.name] = sprite;
            }
            return parts.TryGetValue(name, out var part) ? part : EmberVisuals.Disc;
        }
    }
}
