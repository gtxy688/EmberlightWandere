using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>共享地面持续伤害；不要求持有燃地武器。</summary>
    public sealed class EmberBurnGround
    {
        sealed class Flame
        {
            public SpriteRenderer view;
            public float life;
            public float powerScale;
        }

        readonly List<Flame> flames = new List<Flame>();
        EmberPool pool;
        public int Count => flames.Count;

        // 场景节点与池由战斗宿主统一回收；此处只清本局状态。
        public void Reset()
        {
            flames.Clear();
        }

        public void SpawnPatch(Vector2 pos, Transform world, float powerScale, EmberPool p)
        {
            pool = p;
            float size = powerScale >= 0.99f ? 1.2f : 0.85f;
            float life = powerScale >= 0.99f ? EmberWeaponTuning.BurnGround.Lifetime : EmberWeaponTuning.BurnGround.SmallLifetime;
            if (flames.Count >= EmberWeaponTuning.BurnGround.Capacity)
            {
                var oldest = flames[0];
                flames.RemoveAt(0);
                if (oldest.view != null)
                {
                    if (pool != null)
                        pool.Release("burn", oldest.view);
                    else
                        Object.Destroy(oldest.view.gameObject);
                }
            }

            SpriteRenderer r;
            if (pool != null)
            {
                r = pool.RentShape("burn", world, pos, Vector2.one * size, new Color(.65f, .18f, .045f, .18f), 1);
                if (r.transform.childCount == 0)
                {
                    var edge = EmberVisuals.Shape("Smoldering edge", r.transform, Vector2.zero, Vector2.one, new Color(1, .35f, .06f, .3f), 2);
                    edge.sprite = EmberArt.Ring;
                    for (int k = 0; k < 3; k++)
                        EmberArt.Fire(r.transform, new Vector2((k - 1) * .28f, .05f * Mathf.Sin(k)), .20f, 2);
                }
                else
                {
                    r.transform.localScale = Vector3.one * size;
                    r.color = new Color(.65f, .18f, .045f, .18f);
                }
            }
            else
            {
                r = EmberVisuals.Shape("Burning ground", world, pos, Vector2.one * size, new Color(.65f, .18f, .045f, .18f), 1);
                var edge = EmberVisuals.Shape("Smoldering edge", r.transform, Vector2.zero, Vector2.one, new Color(1, .35f, .06f, .3f), 2);
                edge.sprite = EmberArt.Ring;
                for (int k = 0; k < 3; k++)
                    EmberArt.Fire(r.transform, new Vector2((k - 1) * .28f, .05f * Mathf.Sin(k)), .20f, 2);
            }

            flames.Add(new Flame { view = r, life = life, powerScale = powerScale });
        }

        public void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Progress == null)
                return;
            bool extend = ctx.Progress.TrailExtend;
            for (int i = flames.Count - 1; i >= 0; i--)
            {
                var f = flames[i];
                f.life -= dt;
                var shade = f.view.color;
                shade.a = .18f * Mathf.Clamp01(f.life);
                f.view.color = shade;
                for (int k = 1; k < f.view.transform.childCount; k++)
                {
                    var child = f.view.transform.GetChild(k);
                    float pulse = 1 + .15f * Mathf.Sin(ctx.Elapsed * 12 + k);
                    child.localScale = new Vector3(.20f, .30f * pulse, 1) * Mathf.Clamp01(f.life);
                }

                float trailPow = ctx.Progress.TrailPower > 0 ? ctx.Progress.TrailPower : EmberWeaponTuning.BurnGround.DefaultPower;
                if (extend)
                    trailPow *= EmberWeaponTuning.BurnGround.ExtendedPower;
                float hitR = f.powerScale >= 0.99f ? EmberWeaponTuning.BurnGround.Radius : EmberWeaponTuning.BurnGround.SmallRadius;
                for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (e == null || !e.Targetable)
                        continue;
                    if (Vector2.Distance(f.view.transform.position, e.view.position) < hitR)
                        if (ctx.DealContactDamage(j, EmberWeaponTuning.BurnGround.Dps * trailPow * f.powerScale * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponTrail)) * dt, f.view.transform.position))
                            ctx.PlaySound?.Invoke(EmberWeaponSound.BurnGround);
                }

                if (f.life <= 0)
                {
                    if (pool != null)
                        pool.Release("burn", f.view);
                    else
                        Object.Destroy(f.view.gameObject);
                    flames.RemoveAt(i);
                }
            }
        }
    }
}
