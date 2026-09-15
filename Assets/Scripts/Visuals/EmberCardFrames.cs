using UnityEngine;

namespace Emberlight
{
    public static class EmberCardFrames
    {
        static Texture2D atlas;
        static readonly Sprite[] frames = new Sprite[4];
        // Sub-sprite names as the Sprite Editor writes them: "<atlas>_<index>", row-major from
        // the top-left. Index order matches the EmberRarity enum (Bronze, Silver, Gold, Diamond),
        // which is also the order the atlas was generated in.
        const string Stem = "doodle-card-frames";
        public static Sprite Get(EmberRarity rarity)
        {
            int index = Mathf.Clamp((int)rarity, 0, 3);
            if (frames[index] != null) return frames[index];
            // Preferred: the sub-sprite cut in the Sprite Editor.
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/UI/" + Stem);
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/UI/" + Stem + "_" + index);
                if (cut != null) { frames[index] = cut; return frames[index]; }
            }
            // Fallback: not reimported since the meta was cut, or no atlas at all.
            if (atlas == null) return EmberArt.Panel;
            float w = atlas.width / 2f, h = atlas.height / 2f;
            // Include a small navy margin beyond the outer drawn border.
            Rect rect = new Rect((index%2)*w + w*.05f, (1-index/2)*h + h*.10f, w*.90f, h*.80f);
            frames[index] = Sprite.Create(atlas, rect, Vector2.one*.5f, 100, 0,
                SpriteMeshType.FullRect, new Vector4(w*.11f,h*.16f,w*.11f,h*.16f));
            frames[index].name = "Doodle frame " + rarity;
            return frames[index];
        }
    }
}
