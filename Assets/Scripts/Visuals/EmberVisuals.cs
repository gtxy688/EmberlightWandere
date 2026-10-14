using UnityEngine;
namespace Emberlight
{
    public static class EmberVisuals
    {
        static Sprite disc;
        public static Sprite Disc
        {
            get
            {
                if (disc != null) return disc;
                // 优先构建期烘好的 PNG，未烘制时就地光栅化。
                var baked = EmberBakedArt.Disc;
                if (baked != null)
                {
                    disc = Sprite.Create(baked, new Rect(0, 0, baked.width, baked.height), new Vector2(.5f, .5f), 64);
                    return disc;
                }
                var tex = new Texture2D(64,64,TextureFormat.RGBA32,false);
                for(int y=0;y<64;y++) for(int x=0;x<64;x++)
                { float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f)); tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31.5f-d))); }
                tex.Apply(); tex.filterMode=FilterMode.Bilinear;
                disc=Sprite.Create(tex,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
                return disc;
            }
        }
        public static SpriteRenderer Shape(string name, Transform parent, Vector2 position, Vector2 scale, Color color, int order)
        {
            var g=new GameObject(name); g.transform.SetParent(parent,false);
            g.transform.localPosition=position; g.transform.localScale=scale;
            var r=g.AddComponent<SpriteRenderer>(); r.sprite=Disc; r.color=color; r.sortingOrder=order; return r;
        }
        /// <summary>兜帽剪影。只走敌人/杂兵这条路径：玩家灯芯不在这里建，它的形体（兜帽边缘、细长发光眼、摆动提灯）归 EmberKeeperAnimation.Initialize。</summary>
        public static Transform Character(string name, Transform parent, Color body)
        {
            var root=new GameObject(name).transform; root.SetParent(parent,false);
            Shape("Shadow",root,new Vector2(0,-.25f),new Vector2(.8f,.25f),new Color(0,0,0,.25f),1);
            Shape("Cloak",root,Vector2.zero,new Vector2(.65f,.8f),body,3);
            Shape("Hood",root,new Vector2(0,.25f),new Vector2(.65f,.6f),body*1.2f,4);
            Shape("Face",root,new Vector2(0,.24f),new Vector2(.43f,.3f),new Color(.07f,.10f,.16f),5);
            for(int i=-1;i<=1;i+=2) Shape("Eye",root,new Vector2(i*.1f,.25f),new Vector2(.06f,.08f),new Color(1,.35f,.4f),6);
            return root;
        }
    }
}