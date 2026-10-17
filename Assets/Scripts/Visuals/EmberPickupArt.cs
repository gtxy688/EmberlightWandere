using UnityEngine;

namespace Emberlight
{
    /// <summary>拾取物专用图集；按持久 Sprite 名称缓存，不依赖子资源返回顺序。</summary>
    public static class EmberPickupArt
    {
        public enum Kind { Healing, Power, Haste }
        static readonly string[] names = { "pickup-heal", "pickup-power", "pickup-haste" };
        static readonly Sprite[] sprites = new Sprite[3];
        static bool loaded;

        public static Sprite Get(Kind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= names.Length) throw new System.ArgumentOutOfRangeException(nameof(kind));
            if (!loaded)
            {
                foreach (var sprite in Resources.LoadAll<Sprite>("Art/Pickups/ember-pickups"))
                    for (int i = 0; i < names.Length; i++)
                        if (sprite.name == names[i]) sprites[i] = sprite;
                loaded = true;
            }
            if (sprites[index] != null) return sprites[index];
            // 资源不完整时保留可拾取的占位表现，工程检查负责发现缺失的切片。
            return kind == Kind.Healing ? EmberWorldArt.Get(9) : kind == Kind.Power ? EmberArt.Flame : EmberArt.Ring;
        }
    }
}
