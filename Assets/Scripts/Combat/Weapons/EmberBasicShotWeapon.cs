using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Fireball. Exclusive ExclBasic +1 shot. Metamorph MetaBasic \u71ce\u539f: size*1.5 dmg*1.25 + burn on hit.</summary>
    public sealed class EmberBasicShotWeapon : EmberWeapon
    {
        sealed class Shot
        {
            public SpriteRenderer view;
            public Vector2 velocity;
            public float life, damage, trail;
        }

        readonly List<Shot> shots = new List<Shot>();
        float fireTimer;

        public override void Reset()
        {
            shots.Clear();
            fireTimer = 0;
        }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            var progress = ctx.Progress;
            bool owned = progress != null && progress.OwnsWeapon(RunProgress.WeaponBasic);
            bool wildfire = progress != null && progress.HasMetamorph(RunProgress.MetaBasic);

            fireTimer -= dt;
            if (owned && fireTimer <= 0 && ctx.EnemyCount() > 0)
            {
                Fire(ctx, wildfire);
                fireTimer = .8f / (1f + progress.WeaponAttackSpeed(RunProgress.WeaponBasic));
            }

            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var s = shots[i];
                s.life -= dt;
                Vector2 previous = s.view.transform.position;
                s.view.transform.position += (Vector3)(s.velocity * dt);
                if (!ctx.Visible(s.view.transform.position))
                {
                    if (ctx.Pool != null) ctx.Pool.Release("fire", s.view); else Object.Destroy(s.view.gameObject);
                    shots.RemoveAt(i);
                    continue;
                }
                if (ctx.Intercept != null && ctx.Intercept(previous, s.view.transform.position, .18f))
                {
                    if (ctx.Pool != null) ctx.Pool.Release("fire", s.view); else Object.Destroy(s.view.gameObject);
                    shots.RemoveAt(i);
                    continue;
                }
                s.trail -= dt;
                if (s.trail <= 0) { ctx.Effects.Trail(s.view.transform.position); s.trail = .045f; }
                for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (e == null || !e.Targetable) continue;
                    if (Vector2.Distance(s.view.transform.position, e.view.position) < e.radius + .18f)
                    {
                        Vector2 hit = e.view.position;
                        ctx.DealDamage(j, s.damage, previous);
                        if (wildfire && ctx.SpawnBurnPatch != null)
                            ctx.SpawnBurnPatch(hit, 0.5f);
                        s.life = 0;
                        break;
                    }
                }
                if (s.life <= 0) { if (ctx.Pool != null) ctx.Pool.Release("fire", s.view); else Object.Destroy(s.view.gameObject); shots.RemoveAt(i); }
            }
        }

        void Fire(EmberWeaponContext ctx, bool wildfire)
        {
            EmberAudio.Ensure().PlayFire();
            int nearest = ctx.FindNearestVisibleEnemy();
            if (nearest < 0) return;
            var target = ctx.GetEnemy(nearest);
            Vector2 dir = ((Vector2)target.view.position - ctx.PlayerPos).normalized;
            int count = 1 + ctx.Progress.ExtraShots;
            float size = wildfire ? .32f * 1.5f : .32f;
            float dmgMul = wildfire ? 1.25f : 1f;
            for (int i = 0; i < count; i++)
            {
                Vector2 v = Quaternion.Euler(0, 0, (i - (count - 1) * .5f) * 9) * dir;
                var r = ctx.Pool != null ? ctx.Pool.RentFire(ctx.World, ctx.PlayerPos, size) : EmberArt.Fire(ctx.World, ctx.PlayerPos, size);
                r.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90);
                shots.Add(new Shot
                {
                    view = r,
                    velocity = v * 9,
                    life = 1.8f,
                    damage = 22f * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponBasic)) * dmgMul
                });
            }
        }
    }
}
