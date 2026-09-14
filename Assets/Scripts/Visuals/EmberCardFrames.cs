using UnityEngine;

namespace Emberlight
{
    public static class EmberCardFrames
    {
        static Texture2D atlas;
        static readonly Sprite[] frames = new Sprite[4];
        public static Sprite Get(EmberRarity rarity)
        {
            int index = Mathf.Clamp((int)rarity, 0, 3);
            if (frames[index] != null) return frames[index];
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/UI/doodle-card-frames");
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
