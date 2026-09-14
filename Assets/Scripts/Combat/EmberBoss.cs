using System.Collections.Generic;
using UnityEngine;
namespace Emberlight
{
    // Explicit tick keeps telegraphs frozen during upgrades, pause and results.
    public sealed class EmberBoss
    {
        public enum AttackState { Pursuit, DashWarning, Dash, EruptionWarning, VolleyWarning, Recovery }
        public AttackState State {get;private set;}
        public string Phase {get{return enraged?"\u72c2\u6012\u0020\u00b7\u0020"+Label():Label();}}
        readonly EmberGame game;readonly Transform body,world;readonly List<SpriteRenderer> warnings=new List<SpriteRenderer>();
        readonly int tier; readonly System.Action<Vector2,int> volley; readonly Transform adornment;
        Vector2 direction;float timer=2;bool enraged;int cycle;
        public EmberBoss(EmberGame owner,Transform root,Transform arena,int tier=1,System.Action<Vector2,int> volley=null)
        {
            game=owner;body=root;world=arena;this.tier=tier;this.volley=volley;
            adornment=new GameObject("Boss adornment").transform;adornment.SetParent(root,false);root=adornment;
            for(int i=-1;i<=1;i++)
            {
                var tooth=EmberVisuals.Shape("Broken crown",root,new Vector2(i*.19f,.53f),new Vector2(.10f,i==1?.20f:.30f),new Color(.60f,.48f,.29f),6);
                tooth.sprite=EmberArt.Panel;tooth.transform.localRotation=Quaternion.Euler(0,0,-i*12);
            }
            var band=EmberVisuals.Shape("Crown band",root,new Vector2(0,.43f),new Vector2(.58f,.09f),new Color(.60f,.48f,.29f),6);band.sprite=EmberArt.Panel;
            var cage=EmberVisuals.Shape("Night lantern",root,new Vector2(.44f,-.05f),new Vector2(.22f,.32f),new Color(.33f,.28f,.39f),7);cage.sprite=EmberArt.Panel;
            EmberArt.Fire(root,new Vector2(.44f,-.04f),.12f,8).color=new Color(.75f,.35f,.85f);
            var halo=EmberVisuals.Shape("Night crown",root,new Vector2(0,.30f),Vector2.one*1.1f,new Color(.7f,.3f,.65f,.6f),2);halo.sprite=EmberArt.Ring;
            EmberVisuals.Shape("Heart",root,new Vector2(0,-.08f),Vector2.one*.16f,new Color(1,.3f,.55f),7);
        }
        string Label(){switch(State){case AttackState.VolleyWarning:return "暗星齐射";case AttackState.DashWarning:return "\u51b2\u950b\u9884\u8b66";case AttackState.Dash:return "\u6697\u5f71\u51b2\u950b";case AttackState.EruptionWarning:return "\u6697\u7130\u7206\u53d1";case AttackState.Recovery:return "\u7834\u7efd";default:return "\u8ffd\u730e";}}
        public void Tick(float dt,Vector2 player,float hpRatio)
        {
            enraged=hpRatio<=.5f;timer-=dt;
            if(State==AttackState.Pursuit)
            {
                body.position=EmberWorld.Clamp(Vector2.MoveTowards(body.position,player,dt*(enraged?1.35f:.8f)),.8f);
                if(timer<=0)
                {
                    cycle++;
                    if(tier>=2 && cycle%3==0)
                    {
                        State=AttackState.VolleyWarning;timer=1f;
                        var r=EmberVisuals.Shape("Volley warning",world,body.position,Vector2.one*3,new Color(.75f,.3f,1f,.7f),2);
                        r.sprite=EmberArt.Ring;warnings.Add(r);
                    }
                    else if(cycle%2==1)
                    {
                        State=AttackState.DashWarning;timer=enraged?.8f:1.15f;direction=(player-(Vector2)body.position).normalized;
                        var r=EmberVisuals.Shape("Dash warning",world,(Vector2)body.position+direction*2.8f,new Vector2(1.3f,5.6f),new Color(.9f,.12f,.28f,.27f),1);r.sprite=EmberArt.Panel;r.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);warnings.Add(r);
                    }
                    else
                    {
                        State=AttackState.EruptionWarning;timer=enraged?.95f:1.35f;
                        int count=(enraged?5:3)+Mathf.Min(3,tier-1);
                        for(int i=0;i<count;i++)
                        {Vector2 p=i==0?player:player+new Vector2(Mathf.Cos(i*2.4f),Mathf.Sin(i*2.4f))*2.3f;var r=EmberVisuals.Shape("Eruption warning",world,EmberWorld.Clamp(p,1),Vector2.one*2.4f,new Color(1,.15f,.35f,.8f),2);r.sprite=EmberArt.Ring;warnings.Add(r);}
                    }
                }
            }
            else if(State==AttackState.DashWarning&&timer<=0){Clear();State=AttackState.Dash;timer=.65f;}
            else if(State==AttackState.Dash)
            {
                body.position=EmberWorld.Clamp((Vector2)body.position+direction*dt*8,.8f);
                if(Vector2.Distance(body.position,player)<1.2f)game.HurtPlayer(22);
                if(timer<=0){State=AttackState.Recovery;timer=1.5f;}
            }
            else if(State==AttackState.EruptionWarning&&timer<=0)
            {
                foreach(var r in warnings){if(Vector2.Distance(r.transform.position,player)<1.2f)game.HurtPlayer(25);game.PlayBossBurst(r.transform.position);}
                Clear();State=AttackState.Recovery;timer=1.5f;
            }
            else if(State==AttackState.VolleyWarning&&timer<=0)
            { Clear();if(volley!=null)volley(body.position,6+Mathf.Min(tier,5)*2);State=AttackState.Recovery;timer=1.5f; }
            else if(State==AttackState.Recovery&&timer<=0){State=AttackState.Pursuit;timer=enraged?1.2f:2;}
        }
        public void Dispose(){Clear();if(adornment!=null){adornment.gameObject.SetActive(false);Object.Destroy(adornment.gameObject);}}
        public void Clear(){foreach(var r in warnings)if(r!=null)Object.Destroy(r.gameObject);warnings.Clear();}
    }
}
