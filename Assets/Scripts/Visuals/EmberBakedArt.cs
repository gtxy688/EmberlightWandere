using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 预烘好的代码原生精灵（Resources/Art/Baked）：把首次使用时的 C# 光栅化挪到构建期；
    /// 尚未烘制时，EmberArt / EmberVisuals 里的运行时光栅化仍是回落路径。
    /// 按 Texture2D（不是 Sprite）加载，并用原 pivot / PPU / 九宫边框切片，输出与原光栅化一致。
    /// </summary>
    public static class EmberBakedArt
    {
        const string Dir = "Art/Baked/";

        static Texture2D flame, ring, panel, glow, disc;

        public static Texture2D Flame { get { return flame ?? (flame = Load("art-flame")); } }
        public static Texture2D Ring { get { return ring ?? (ring = Load("art-ring")); } }
        public static Texture2D Panel { get { return panel ?? (panel = Load("art-panel")); } }
        public static Texture2D Glow { get { return glow ?? (glow = Load("art-glow")); } }
        public static Texture2D Disc { get { return disc ?? (disc = Load("art-disc")); } }

        static Texture2D Load(string name) { return Resources.Load<Texture2D>(Dir + name); }
    }
}
