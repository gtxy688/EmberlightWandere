using UnityEngine;
namespace Emberlight
{
    public static class EmberWorld
    {
        public static float Limit = 22f;
        public static bool Visible(Camera camera,Vector3 point)
        {
            Vector3 v=camera.WorldToViewportPoint(point);
            return v.z>0&&v.x>.045f&&v.x<.955f&&v.y>.04f&&v.y<.82f;
        }
        public static Vector2 Clamp(Vector2 point,float inset=0)
        {float r=Limit-inset;return new Vector2(Mathf.Clamp(point.x,-r,r),Mathf.Clamp(point.y,-r,r));}
        public static void BuildBoundary(Transform parent)
        {
            var root=new GameObject("Ruined perimeter").transform;root.SetParent(parent,false);
            for(int side=0;side<4;side++)for(int i=0;i<=44;i++)
            {
                float n=i-22;Vector2 p=side==0?new Vector2(n,22.65f):side==1?new Vector2(n,-22.65f):side==2?new Vector2(22.65f,n):new Vector2(-22.65f,n);
                var stone=EmberVisuals.Shape("Boundary stone",root,p,new Vector2(.97f,.92f),new Color(.22f,.29f,.34f),2);stone.sprite=EmberArt.Panel;
                EmberVisuals.Shape("Stone cap",stone.transform,new Vector2(-.06f,.18f),new Vector2(.75f,.12f),new Color(.35f,.43f,.46f),3);
                if(i%6==0){var pillar=EmberVisuals.Shape("Lantern pillar",root,p,new Vector2(.6f,1.1f),new Color(.14f,.20f,.25f),4);pillar.sprite=EmberArt.Panel;EmberArt.Fire(root,p+Vector2.up*.5f,.28f,5);}
            }
        }
    }
    public sealed class EmberEnemyBar
    {
        readonly Transform root,fill;readonly float width;
        public EmberEnemyBar(Transform parent,bool boss)
        {
            width=boss?.8f:.7f;root=new GameObject("Health bar").transform;root.SetParent(parent,false);root.localPosition=new Vector3(0,.82f,0);
            var bg=EmberVisuals.Shape("Health track",root,Vector2.zero,new Vector2(width,.095f),new Color(.04f,.06f,.09f),15);bg.sprite=EmberArt.Panel;
            var f=EmberVisuals.Shape("Health fill",root,Vector2.zero,new Vector2(width,.055f),boss?new Color(1,.35f,.3f):new Color(.85f,.34f,.34f),16);f.sprite=EmberArt.Panel;fill=f.transform;root.gameObject.SetActive(!boss);
        }
        public void Set(float ratio){ratio=Mathf.Clamp01(ratio);fill.localScale=new Vector3(width*ratio,.055f,1);fill.localPosition=new Vector3(-width*(1-ratio)*.5f,0,0);}
    }
}

