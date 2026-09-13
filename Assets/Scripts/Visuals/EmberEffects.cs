using System.Collections.Generic;
using UnityEngine;
namespace Emberlight
{
    public sealed class EmberEffects : MonoBehaviour
    {
        sealed class Particle { public SpriteRenderer sprite; public Vector2 velocity;public float age,life,size,growth;public Color color; }
        readonly List<Particle> active=new List<Particle>();
        readonly Stack<Particle> pool=new Stack<Particle>();
        EmberGame game;
        public int ActiveCount {get{return active.Count;}}
        public void Initialize(EmberGame owner){game=owner;}
        public void Emit(Vector2 p,Vector2 velocity,float size,float life,Color color,bool ring=false,float growth=0)
        {
            if(active.Count>=120)return; // mobile particle cap
            Particle s;
            if(pool.Count>0)s=pool.Pop();else s=new Particle{sprite=EmberVisuals.Shape("Ember particle",transform,p,Vector2.one,color,10)};
            s.sprite.gameObject.SetActive(true);s.sprite.sprite=ring?EmberArt.Ring:EmberVisuals.Disc;s.sprite.transform.position=p;
            s.velocity=velocity;s.age=0;s.life=life;s.size=size;s.growth=growth;s.color=color;s.sprite.color=color;s.sprite.transform.localScale=Vector3.one*size;active.Add(s);
        }
        public void Trail(Vector2 p){Emit(p,Random.insideUnitCircle*.15f,.16f,.25f,new Color(1,.46f,.08f,.6f));}
        public void Impact(Vector2 p,bool death)
        {
            Emit(p,Vector2.zero,death?.3f:.18f,.22f,new Color(1,.76f,.3f,.7f),true,death?1.5f:.7f);
            for(int i=0;i<(death?5:3);i++)Emit(p,Random.insideUnitCircle*(death?2.3f:1.4f),Random.Range(.07f,.15f),Random.Range(.2f,.45f),i%3==0&&death?new Color(.18f,.22f,.32f,.65f):new Color(1,.57f,.15f,.9f));
        }
        public void Nova(Vector2 p,float radius)
        {
            Emit(p,Vector2.zero,.2f,.48f,new Color(1,.65f,.16f,.75f),true,radius*2/.86f);
            Emit(p,Vector2.zero,.3f,.35f,new Color(1,.91f,.6f,.55f),true,radius*1.7f);
            for(int i=0;i<12;i++){float a=i*Mathf.PI*.1f;Emit(p,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*2,.12f,.42f,new Color(1,.55f,.1f,.8f));}
        }
        void Update()
        {
            if(game==null||game.State!=EmberGame.Mode.Playing)return;
            for(int i=active.Count-1;i>=0;i--)
            {
                var p=active[i];p.age+=Time.deltaTime;
                if(p.age>=p.life){p.sprite.gameObject.SetActive(false);pool.Push(p);active.RemoveAt(i);continue;}
                float t=p.age/p.life;p.sprite.transform.position+=(Vector3)(p.velocity*Time.deltaTime);
                p.sprite.transform.localScale=Vector3.one*(p.growth>0?p.size+p.growth*(1-(1-t)*(1-t)):p.size*(1-t*.8f));
                var c=p.color;c.a*=1-t;p.sprite.color=c;
            }
        }
    }
}
