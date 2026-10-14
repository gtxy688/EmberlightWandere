using UnityEngine;

namespace Emberlight
{
    public static class EmberCardFrames
    {
        static Texture2D atlas;
        static readonly Sprite[] frames = new Sprite[4];
        // 子精灵命名同 Sprite Editor 的写法 "<atlas>_<index>"，自左上起行主序；
        // 索引顺序与 EmberRarity 枚举（Bronze/Silver/Gold/Diamond）一致，也是图集的生成顺序。
        const string Stem = "doodle-card-frames";
        public static Sprite Get(EmberRarity rarity)
        {
            int index = Mathf.Clamp((int)rarity, 0, 3);
            if (frames[index] != null) return frames[index];
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/UI/" + Stem);
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/UI/" + Stem + "_" + index);
                if (cut != null) { frames[index] = cut; return frames[index]; }
            }
            // 回落：meta 切过之后没重新导入，或根本没有图集。
            if (atlas == null) return EmberArt.Panel;
            float w = atlas.width / 2f, h = atlas.height / 2f;
            // 外框之外多留一点深蓝边距。
            Rect rect = new Rect((index%2)*w + w*.05f, (1-index/2)*h + h*.10f, w*.90f, h*.80f);
            frames[index] = Sprite.Create(atlas, rect, Vector2.one*.5f, 100, 0,
                SpriteMeshType.FullRect, new Vector4(w*.11f,h*.16f,w*.11f,h*.16f));
            frames[index].name = "Doodle frame " + rarity;
            return frames[index];
        }
    }
}
