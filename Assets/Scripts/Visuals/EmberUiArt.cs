using UnityEngine;

namespace Emberlight
{
    public static class EmberUiArt
    {
        public enum Piece { Primary, Secondary, Panel, Title, Divider, Lantern }
        static readonly Sprite[] sprites = new Sprite[6];
        static Texture2D atlas;
        // 保留旧版具名资源的查询约定；未命中时使用下面的区域回落。
        static readonly string[] names = { "ui-primary", "ui-secondary", "ui-panel", "ui-title", "ui-divider", "ui-lantern" };
        // 回落矩形为归一化坐标，原点在顶部。
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
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/UI/doodle-ui_" + names[i]);
                if (cut != null) { sprites[i] = cut; return sprites[i]; }
            }
            // 图集缺失时用占位图；具名 Sprite 缺失时按区域切图。
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
