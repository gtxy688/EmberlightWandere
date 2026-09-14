using UnityEngine;
namespace Emberlight
{
    // Shared code-native sprites; textures are generated once, not per effect.
    public static class EmberArt
    {
        static Sprite flame, ring, panel, glow;
        public static Sprite Glow { get { return glow ?? (glow = Create(3)); } }
        public static Sprite Flame { get { return flame ?? (flame = Create(0)); } }
        public static Sprite Ring { get { return ring ?? (ring = Create(1)); } }
        public static Sprite Panel { get { return panel ?? (panel = Create(2)); } }
        static Sprite Create(int kind)
        {
            const int n=128;
            // Prefer the build-time baked PNG (Emberlight/Bake runtime art); fall back to
            // rasterising here so the game still runs before the art has been baked.
            var baked = Baked(kind);
            if (baked != null)
                return Sprite.Create(baked,new Rect(0,0,baked.width,baked.height),Vector2.one*.5f,n,0,SpriteMeshType.FullRect,kind==2?new Vector4(24,24,24,24):Vector4.zero);
            var t=new Texture2D(n,n,TextureFormat.RGBA32,false);t.name="EmberArt"+kind;t.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1,a;
                if(kind==0)
                {
                    float h=(v+1)*.5f;
                    float width=Mathf.Sin(Mathf.Pow(h,.65f)*Mathf.PI)*.68f;
                    float bend=.22f*h*h;
                    a=Mathf.Clamp01((width-Mathf.Abs(u-bend))*n*.5f);
                }
                else if(kind==3)a=Mathf.Pow(Mathf.Clamp01(1-Mathf.Sqrt(u*u+v*v)),2);
                else if(kind==1)a=Mathf.Clamp01((.045f-Mathf.Abs(Mathf.Sqrt(u*u+v*v)-.86f))*n);
                else
                {
                    Vector2 q=new Vector2(Mathf.Abs(u)-.75f,Mathf.Abs(v)-.75f);
                    float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-.23f;
                    a=Mathf.Clamp01(-d*n*.5f);
                }
                t.SetPixel(x,y,new Color(1,1,1,a));
            }
            t.Apply();return Sprite.Create(t,new Rect(0,0,n,n),Vector2.one*.5f,n,0,SpriteMeshType.FullRect,kind==2?new Vector4(24,24,24,24):Vector4.zero);
        }
        static Texture2D Baked(int kind)
        {
            switch(kind)
            {
                case 0: return EmberBakedArt.Flame;
                case 1: return EmberBakedArt.Ring;
                case 2: return EmberBakedArt.Panel;
                default: return EmberBakedArt.Glow;
            }
        }
        public static SpriteRenderer Fire(Transform parent,Vector2 position,float size,int order=8)
        {
            var r=EmberVisuals.Shape("Living flame",parent,position,new Vector2(size,size*1.5f),new Color(1,.30f,.055f),order);r.sprite=Flame;
            var glow=EmberVisuals.Shape("Glow",r.transform,Vector2.zero,Vector2.one*1.8f,new Color(1,.4f,.04f,.35f),order-1);glow.sprite=Glow;
            var core=EmberVisuals.Shape("Golden core",r.transform,new Vector2(0,-.12f),new Vector2(.55f,.65f),new Color(1,.85f,.37f),order+1);core.sprite=Flame;
            return r;
        }
    }
}

