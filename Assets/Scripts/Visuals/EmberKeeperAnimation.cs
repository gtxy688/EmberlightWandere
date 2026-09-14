using UnityEngine;
namespace Emberlight
{
    // Continuous local transforms: no frame swaps or changes to gameplay position.
    public sealed class EmberKeeperAnimation : MonoBehaviour
    {
        Transform body, cloak, face, lantern;
        SpriteRenderer glow;
        EmberGame game;
        Vector3 previous;
        Vector2 drift;
        float phase, moving;
        public void Initialize()
        {
            body = new GameObject("Floating keeper").transform;
            body.SetParent(transform,false);
            // Ground shadow. EmberVisuals.Character used to draw this before its keeper branch
            // returned early; removing that dead branch took the keeper's shadow with it. Hangs
            // off body so it travels with the hover bob, matching the enemy silhouette.
            EmberVisuals.Shape("Shadow",body,new Vector2(0,-.25f),new Vector2(.8f,.25f),new Color(0,0,0,.25f),1);
            var blue = new Color(.20f,.35f,.45f);
            cloak=EmberVisuals.Shape("Cloak",body,new Vector2(0,-.08f),new Vector2(.66f,.78f),blue,3).transform;
            EmberVisuals.Shape("Hood rim",body,new Vector2(0,.23f),new Vector2(.69f,.62f),new Color(.26f,.43f,.52f),4);
            EmberVisuals.Shape("Hood",body,new Vector2(0,.25f),new Vector2(.61f,.55f),blue,5);
            face=EmberVisuals.Shape("Hidden face",body,new Vector2(0,.21f),new Vector2(.44f,.32f),new Color(.035f,.06f,.085f),6).transform;
            for(int i=-1;i<=1;i+=2)
                EmberVisuals.Shape("Warm eye",face,new Vector2(i*.22f,.02f),new Vector2(.13f,.25f),new Color(1f,.79f,.35f),7);
            lantern=new GameObject("Swinging lantern").transform;
            lantern.SetParent(body,false);lantern.localPosition=new Vector2(.37f,.10f);
            var loop=EmberVisuals.Shape("Handle",lantern,new Vector2(0,-.04f),new Vector2(.15f,.15f),new Color(.72f,.51f,.22f),7);loop.sprite=EmberArt.Ring;
            var housing=EmberVisuals.Shape("Lantern case",lantern,new Vector2(0,-.18f),new Vector2(.23f,.28f),new Color(.62f,.39f,.17f),7);housing.sprite=EmberArt.Panel;
            var glass=EmberVisuals.Shape("Warm glass",lantern,new Vector2(0,-.18f),new Vector2(.15f,.19f),new Color(1f,.68f,.22f),8);glass.sprite=EmberArt.Panel;
            EmberArt.Fire(lantern,new Vector2(0,-.18f),.11f,9);
            glow=EmberVisuals.Shape("Lantern light",lantern,new Vector2(0,-.18f),Vector2.one*.65f,new Color(1f,.5f,.1f,.12f),2);glow.sprite=EmberArt.Glow;
            previous=transform.position;
            game=Object.FindObjectOfType<EmberGame>();
        }
        void LateUpdate()
        {
            Vector3 delta=transform.position-previous;previous=transform.position;
            float dt=Time.deltaTime;if(dt<=0||body==null)return;

            if(game!=null && game.State!=EmberGame.Mode.Playing)return;
            Vector2 velocity=delta.sqrMagnitude<1f?(Vector2)delta/dt:Vector2.zero;
            float blend=1f-Mathf.Exp(-dt*9f);
            drift=Vector2.Lerp(drift,Vector2.ClampMagnitude(velocity/3.3f,1),blend);
            moving=Mathf.Lerp(moving,Mathf.Clamp01(velocity.magnitude/3.3f),blend);
            phase+=dt*(2.4f+moving*2f);
            body.localPosition=new Vector3(0,.025f*Mathf.Sin(phase),0);
            body.localRotation=Quaternion.Euler(0,0,-drift.x*5f);
            cloak.localScale=new Vector3(.66f*(1f+.025f*Mathf.Sin(phase)),.78f*(1f-.018f*Mathf.Sin(phase)),1);
            face.localPosition=new Vector3(drift.x*.035f,.21f+drift.y*.016f,0);
            lantern.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase*.85f)*(3f+moving*9f)+drift.x*9f);
            glow.color=new Color(1f,.5f,.1f,.12f+.015f*Mathf.Sin(phase*2.3f));
        }
    }
}
