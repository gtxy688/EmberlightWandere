using UnityEngine;
namespace Emberlight
{
    public static class EmberWorldArt
    {
        static Texture2D atlas;
        static readonly Sprite[] cells = new Sprite[12];
        // Sub-sprite names as the Sprite Editor writes them: "<atlas>_<index>", row-major from
        // the top-left, so index == cell.
        //
        // NOTE these are cut with a bottom-left pivot (pivot 0,0), while the fallback below
        // Sprite.Creates them centred. SpriteRenderer positions by pivot, so picking the sliced
        // path shifts every world sprite down-left by half its size. Fix by setting the slices'
        // Pivot to Center in the Sprite Editor, or by re-cutting; the UI atlases are unaffected
        // because uGUI positions by RectTransform, not by pivot.
        const string Stem = "wanderer-sheet";
        public static Sprite Get(int cell)
        {
            if (cells[cell] != null) return cells[cell];
            // Preferred: the sub-sprite cut in the Sprite Editor.
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/World/" + Stem);
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/World/" + Stem + "_" + cell);
                if (cut != null) { cells[cell] = cut; return cells[cell]; }
            }
            // Fallback: not reimported since the meta was cut, or no atlas at all.
            if (atlas == null) return EmberArt.Flame;
            float w=atlas.width/4f,h=atlas.height/3f;
            cells[cell]=Sprite.Create(atlas,new Rect((cell%4)*w,(2-cell/4)*h,w,h),Vector2.one*.5f,w,0,SpriteMeshType.FullRect);
            cells[cell].name="Wanderer art "+cell;
            return cells[cell];
        }
        public static void Projectile(SpriteRenderer renderer,int cell,float width,float height)
        {
            renderer.sprite=Get(cell);renderer.color=Color.white;
            renderer.transform.localScale=new Vector3(width,height,1);
            for(int i=0;i<renderer.transform.childCount;i++) renderer.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
}
