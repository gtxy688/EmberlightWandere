using UnityEngine;

namespace Emberlight
{
    public static class EmberUiArt
    {
        public enum Piece { Primary, Secondary, Panel, Title, Divider, Lantern }
        static readonly Sprite[] sprites = new Sprite[6];
        static Texture2D atlas;
        // 名字由 Tools/slice_atlases.py 按枚举顺序写进 doodle-ui.png.meta；两边必须同步：
        // 脚本是 rect 的权威，本数组是代码叫法的权威。
        static readonly string[] names = { "ui-primary", "ui-secondary", "ui-panel", "ui-title", "ui-divider", "ui-lantern" };
        // 归一化、以顶部为原点的矩形，只给下面的回落切片用（图集还没切成子精灵时）。
        // 切好的资产自带同样的 rect 与九宫边框，两条路径画出的像素一致。
        static readonly Rect[] regions = {
            new Rect(.028f,.073f,.446f,.180f), new Rect(.529f,.078f,.442f,.172f),
            new Rect(.028f,.359f,.446f,.260f), new Rect(.521f,.405f,.454f,.167f),
            new Rect(.025f,.768f,.454f,.081f), new Rect(.573f,.676f,.354f,.266f)
        };
        public static Sprite Get(Piece piece)
        {
            int i = (int)piece;
            if (sprites[i] != null) return sprites[i];
            // 首选 Tools/slice_atlases.py 切出的子精灵资产；图集仍按 Texture2D 加载——回落需要它，
            // 而且取 Sprite 也会把同一张纹理拉进来。
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/UI/doodle-ui");
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/UI/doodle-ui_" + names[i]);
                if (cut != null) { sprites[i] = cut; return sprites[i]; }
            }
            // 回落：图集完全缺失（没东西可切），或者本检出在 meta 切过之后还没重新导入，
            // 仍是一整张未切的纹理。
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
