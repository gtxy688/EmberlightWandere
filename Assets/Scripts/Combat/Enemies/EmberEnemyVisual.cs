using UnityEngine;

namespace Emberlight
{
    /// <summary>Distinct silhouettes and combat tells built from the existing procedural sprite library.</summary>
    public sealed class EmberEnemyVisual
    {
        readonly Transform root, shield;
        readonly SpriteRenderer tell, aura, core, wake;
        readonly Transform embers;
        readonly EmberCombat.Enemy enemy;
        readonly Transform[] bodyParts;
        readonly Vector3[] restPositions, restScales;
        readonly Quaternion[] restRotations;
        Vector3 previousPosition;
        float previousTime, locomotion, previousCharge, recoil;

        public EmberEnemyVisual(EmberCombat.Enemy enemy, Transform world)
        {
            this.enemy = enemy;
            var parts = new System.Collections.Generic.List<Transform>();
            foreach (Transform part in enemy.view)
                if (part.name == "Cloak" || part.name == "Hood" || part.name == "Face" || part.name == "Eye" || part.name == "Silhouette") parts.Add(part);
            bodyParts = parts.ToArray();
            restPositions = new Vector3[bodyParts.Length];
            restScales = new Vector3[bodyParts.Length];
            restRotations = new Quaternion[bodyParts.Length];
            for (int i=0;i<bodyParts.Length;i++)
            {
                restPositions[i]=bodyParts[i].localPosition;
                restScales[i]=bodyParts[i].localScale;
                restRotations[i]=bodyParts[i].localRotation;
            }
            previousPosition=enemy.view.position;
            previousTime=-1f;
            // World-sized tells must not inherit enemy body scaling.
            root = new GameObject("Enemy tells").transform;
            root.SetParent(world, false);
            root.position = enemy.view.position;
            core = Shape("Role mark", Vector2.zero, new Vector2(.22f, .22f), Color.white, 12);
            tell = Shape("Attack warning", Vector2.zero, Vector2.one, Color.clear, 3);
            aura = Shape("Affix ring", Vector2.zero, Vector2.one, Color.clear, 2);
            aura.sprite = EmberArt.Ring;
            wake = Shape("Wind wake", Vector2.zero, Vector2.one, Color.clear, 1);
            wake.sprite = EmberArt.Ring;
            core.transform.localPosition = Vector2.up * .28f;

            switch (enemy.kind)
            {
                case 4:
                    Shape("Candle staff", new Vector2(.42f, .1f), new Vector2(.09f, .9f), new Color(.46f, .26f, .59f), 11).sprite = EmberArt.Panel;
                    var flame = Shape("Violet flame", new Vector2(.42f, .58f), new Vector2(.24f, .35f), new Color(.85f, .5f, 1), 12);
                    flame.sprite = EmberArt.Flame;
                    core.color = new Color(.85f, .5f, 1);
                    break;
                case 5:
                    for (int i = -1; i <= 1; i++) Shape("Brood egg", new Vector2(i * .26f, -.12f), new Vector2(.25f, .33f), new Color(.7f, .85f, .3f), 12);
                    core.color = new Color(.8f, 1f, .35f);
                    break;
                case 6:
                    core.transform.localScale = new Vector2(.18f, .12f);
                    core.transform.localPosition = Vector2.zero;
                    core.color = new Color(.9f, 1f, .4f);
                    break;
                case 7:
                    shield = new GameObject("Directional shield").transform;
                    shield.SetParent(root, false);
                    var shieldBody = EmberVisuals.Shape("Solid shield", shield, new Vector2(.58f,0),new Vector2(.24f,.91f),new Color(.37f,.58f,.66f),13);
                    shieldBody.sprite=EmberArt.Panel;
                    var rim=EmberVisuals.Shape("Shield rim",shield,new Vector2(.65f,0),new Vector2(.065f,.78f),new Color(.69f,.84f,.87f),14);
                    rim.sprite=EmberArt.Panel;
                    EmberVisuals.Shape("Shield boss",shield,new Vector2(.58f,0),new Vector2(.15f,.18f),new Color(.78f,.87f,.86f),15);
                    core.color = new Color(.7f, .92f, 1f);
                    break;
                case 8:
                    core.sprite = EmberArt.Flame;
                    core.color = new Color(1f, .35f, .1f);
                    core.transform.localPosition = Vector2.up * .55f;
                    break;
                default: core.gameObject.SetActive(enemy.affix != EnemyAffix.None); break;
            }
            if (enemy.affix == EnemyAffix.Rebirth) core.color = new Color(.86f, .94f, 1f);
            if (enemy.affix == EnemyAffix.Burning)
            {
                embers = new GameObject("Burning embers").transform;
                embers.SetParent(root, false);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * .5f;
                    EmberArt.Fire(embers, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .6f, .16f, 12);
                }
            }
        }

        SpriteRenderer Shape(string name, Vector2 position, Vector2 size, Color color, int order)
        { return EmberVisuals.Shape(name, root, position, size, color, order); }

        public void Tick(float time, LevelConfig config)
        {
            AnimateBody(time);
            root.position = enemy.view.position;
            if (embers != null) embers.localRotation = Quaternion.Euler(0, 0, time * 80f);
            if (shield != null) shield.localRotation = Quaternion.Euler(0, 0, enemy.facing);
            tell.color = Color.clear;
            tell.transform.localPosition = Vector3.zero;
            tell.transform.localRotation = Quaternion.identity;
            if (enemy.reviveTimer > 0)
            {
                tell.sprite = EmberArt.Ring;
                tell.transform.localScale = Vector3.one * (1f + enemy.reviveTimer / config.RebirthDelay);
                tell.color = new Color(.85f, .95f, 1f, .9f);
                core.transform.localScale = Vector3.one * (.2f + .06f * Mathf.Sin(time * 12f));
            }
            else if (enemy.detonating)
            {
                tell.sprite = EmberArt.Ring;
                tell.transform.localScale = Vector3.one * config.BombRadius * 2;
                tell.color = new Color(1f, .2f, .06f, .55f + .35f * Mathf.Sin(time * 24f));
                core.transform.localScale = Vector3.one * (.2f + .18f * (1f - enemy.fuse / config.BombFuse));
            }
            else if (enemy.charge > 0)
            {
                tell.sprite = EmberArt.Panel;
                float length = config.ShooterFar;
                tell.transform.localPosition = enemy.aim * (length * .5f);
                tell.transform.localScale = new Vector3(length, .045f + enemy.charge * .05f, 1f);
                tell.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(enemy.aim.y, enemy.aim.x) * Mathf.Rad2Deg);
                tell.color = new Color(.85f, .4f, 1f, .25f + enemy.charge * .45f);
            }
            aura.color = Color.clear;
            wake.color = Color.clear;
            if (enemy.affix == EnemyAffix.None) return;
            Color color = enemy.affix == EnemyAffix.Burning ? new Color(1f, .35f, .08f, .40f)
                : enemy.affix == EnemyAffix.Wind ? new Color(.25f, 1f, .9f, .55f) : new Color(.85f, .92f, 1f, .65f);
            aura.color = color;
            aura.transform.localScale = Vector3.one * (enemy.affix == EnemyAffix.Burning ? config.BurningRadius * 2f : 1.2f);
            aura.transform.localRotation = Quaternion.Euler(0, 0, time * 65f);
            if (enemy.affix == EnemyAffix.Wind && enemy.age % config.WindPeriod < config.WindDuration)
            {
                wake.color = color * new Color(1, 1, 1, .45f);
                wake.transform.position = enemy.view.position - (Vector3)enemy.moveDirection * .45f;
                wake.transform.localScale = new Vector3(.8f, .5f, 1);
            }
        }

        void AnimateBody(float time)
        {
            float dt=previousTime<0?0:Mathf.Clamp(time-previousTime,0,.1f);
            Vector3 delta=enemy.view.position-previousPosition;
            previousTime=time;previousPosition=enemy.view.position;
            float speed=dt>.0001f?delta.magnitude/dt:0;
            locomotion=Mathf.Lerp(locomotion,Mathf.Clamp01(speed/2f),1f-Mathf.Exp(-dt*10f));
            if(previousCharge>.1f && enemy.charge==0)recoil=1f;
            previousCharge=enemy.charge;
            recoil=Mathf.MoveTowards(recoil,0,dt*6f);
            float rate=enemy.kind==6?12f:enemy.kind==1?8f:enemy.kind==2||enemy.kind==3?2.8f:4.5f;
            float phase=time*rate+enemy.view.GetInstanceID()%31;
            float wave=Mathf.Sin(phase);
            float bob=wave*(.009f+.026f*locomotion);
            float squash=wave*(enemy.kind==5?.055f:.025f)*locomotion;
            float lean=-enemy.moveDirection.x*locomotion*(enemy.kind==7?2f:4f);
            if(enemy.kind==4){squash-=enemy.charge*.055f;lean+=recoil*8f;}
            if(enemy.detonating){squash=Mathf.Sin(time*27f)*.065f;bob=0;}
            if(enemy.reviveTimer>0){squash=-.10f;bob=-.04f;lean=0;}
            for(int i=0;i<bodyParts.Length;i++)
            {
                var part=bodyParts[i];if(part==null)continue;
                part.localPosition=restPositions[i]+new Vector3(0,bob,0);
                part.localScale=Vector3.Scale(restScales[i],new Vector3(1+squash,1-squash,1));
                part.localRotation=restRotations[i]*Quaternion.Euler(0,0,lean);
            }
        }

        public void Clear()
        {
            // Pooled enemies must return in their neutral pose.
            for(int i=0;i<bodyParts.Length;i++)
                if(bodyParts[i]!=null){bodyParts[i].localPosition=restPositions[i];bodyParts[i].localScale=restScales[i];bodyParts[i].localRotation=restRotations[i];}
            if (root == null) return;
            root.gameObject.SetActive(false);
            Object.Destroy(root.gameObject);
        }
    }
}
