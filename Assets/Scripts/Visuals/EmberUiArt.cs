using UnityEngine;

namespace Emberlight
{
    public static class EmberUiArt
    {
        public enum Piece { Primary, Secondary, Panel, Title, Divider, Lantern }
        static readonly Sprite[] sprites = new Sprite[6];
        static Texture2D atlas;
        // Names written into doodle-ui.png.meta by Tools/slice_atlases.py, in enum order.
        // Keep the two in sync: the script is the source of truth for the rects, this array is
        // the source of truth for what the code calls them.
        static readonly string[] names = { "ui-primary", "ui-secondary", "ui-panel", "ui-title", "ui-divider", "ui-lantern" };
        // Normalized top-origin rectangles. Only used by the fallback slice below, i.e. when the
        // atlas has not been cut into sub-sprites. The sliced assets carry these same rects and
        // the 9-slice border baked in, so the two paths draw identical pixels.
        static readonly Rect[] regions = {
            new Rect(.028f,.073f,.446f,.180f), new Rect(.529f,.078f,.442f,.172f),
            new Rect(.028f,.359f,.446f,.260f), new Rect(.521f,.405f,.454f,.167f),
            new Rect(.025f,.768f,.454f,.081f), new Rect(.573f,.676f,.354f,.266f)
        };
        public static Sprite Get(Piece piece)
        {
            int i = (int)piece;
            if (sprites[i] != null) return sprites[i];
            // Preferred: the real sub-sprite asset cut by Tools/slice_atlases.py. Atlas is still
            // loaded as a Texture2D even on this path: it is what the fallback below needs, and
            // asking for the Sprite would pull the same texture in anyway.
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/UI/doodle-ui");
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/UI/doodle-ui_" + names[i]);
                if (cut != null) { sprites[i] = cut; return sprites[i]; }
            }
            // Fallback: atlas missing entirely (nothing to slice), or still a single un-sliced
            // texture because this checkout has not been reimported since the meta was cut.
            if (atlas == null) return EmberArt.Panel;
            Rect r = regions[i];
            var pixels = new Rect(r.x * atlas.width, (1-r.yMax)*atlas.height, r.width*atlas.width, r.height*atlas.height);
            Vector4 border = i < 4 ? new Vector4(pixels.width*.20f,pixels.height*.30f,pixels.width*.20f,pixels.height*.30f) : Vector4.zero;
            sprites[i] = Sprite.Create(atlas,pixels,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);
            sprites[i].name = "Doodle UI " + piece;
            return sprites[i];
        }
    }
}
