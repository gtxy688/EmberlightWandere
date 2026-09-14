using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// Pre-baked versions of the code-native sprites under Resources/Art/Baked.
    ///
    /// These shapes used to be rasterised in C# on first use (EmberArt.Create /
    /// EmberVisuals.Disc). Emberlight/Bake runtime art writes them out as PNGs so the
    /// work happens at build time; the runtime rasterisers remain as the fallback for
    /// when the assets have not been baked yet.
    ///
    /// Textures are loaded as Texture2D (not Sprite) and sliced with the original
    /// pivot / pixels-per-unit / 9-slice border, so Sprite.Create output is identical
    /// to what the rasterisers produced.
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
