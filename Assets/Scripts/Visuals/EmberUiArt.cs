using UnityEngine;

namespace Emberlight
{
    public static class EmberUiArt
    {
        public enum Piece { Primary, Secondary, Panel, Title, Divider, Lantern }
        static readonly Sprite[] sprites = new Sprite[6];
        static Texture2D atlas;
        // Normalized top-origin rectangles, measured on the delivered atlas.
        static readonly Rect[] regions = {
            new Rect(.028f,.073f,.446f,.180f), new Rect(.529f,.078f,.442f,.172f),
            new Rect(.028f,.359f,.446f,.260f), new Rect(.521f,.405f,.454f,.167f),
            new Rect(.025f,.768f,.454f,.081f), new Rect(.573f,.676f,.354f,.266f)
        };
        public static Sprite Get(Piece piece)
        {
            int i = (int)piece;
            if (sprites[i] != null) return sprites[i];
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/UI/doodle-ui");
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
