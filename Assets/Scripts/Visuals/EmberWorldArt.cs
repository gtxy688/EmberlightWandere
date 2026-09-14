using UnityEngine;
namespace Emberlight
{
    public static class EmberWorldArt
    {
        static Texture2D atlas;
        static readonly Sprite[] cells = new Sprite[12];
        public static Sprite Get(int cell)
        {
            if (cells[cell] != null) return cells[cell];
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/World/wanderer-sheet");
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
