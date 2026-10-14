using UnityEngine;
namespace Emberlight
{
    public static class EmberWorldArt
    {
        static Texture2D atlas;
        static readonly Sprite[] cells = new Sprite[12];
        // 子精灵命名同 Sprite Editor 的写法 "<atlas>_<index>"，自左上起行主序，index == cell。
        // 注意：切片资产用左下 pivot（0,0），下面的回落却是居中 Sprite.Create；SpriteRenderer 按 pivot 定位，
        // 走切片路径会让世界精灵整体偏左下半个身位。修法是把切片 Pivot 改成 Center 或重切；
        // UI 图集不受影响，因为 uGUI 按 RectTransform 定位而不看 pivot。
        const string Stem = "wanderer-sheet";
        public static Sprite Get(int cell)
        {
            if (cells[cell] != null) return cells[cell];
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/World/" + Stem);
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/World/" + Stem + "_" + cell);
                if (cut != null) { cells[cell] = cut; return cells[cell]; }
            }
            // 回落：meta 切过之后没重新导入，或根本没有图集。
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
