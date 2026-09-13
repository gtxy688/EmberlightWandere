using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Pierce sniper: fire only while standing. Exclusive +PierceLimit. Metamorph \u7a7f\u6768 split on hit.</summary>
    public sealed class EmberPierceWeapon : EmberWeapon
    {
        sealed class Arrow
        {
            public SpriteRenderer view;
            public Vector2 velocity;
            public float life, damage;
            public int hitsLeft;
            public bool isSplit;
            public readonly HashSet<EmberCombat.Enemy> hit = new HashSet<EmberCombat.Enemy>();
        }

        readonly List<Arrow> arrows = new List<Arrow>();
        float cd;

        public override void Reset()
        {
            arrows.Clear();
            cd = 0;
        }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Progress == null || !ctx.Progress.OwnsWeapon(RunProgress.WeaponPierce)) return;

            cd -= dt;
            // Hotfix: only shoot while standing; in-flight arrows keep ticking below.
            if (cd <= 0 && ctx.PlayerStanding && ctx.EnemyCount() > 0)
            {
                Fire(ctx, ctx.PlayerPos, null, false);
                cd = 1.1f / (1f + ctx.Progress.AttackSpeedBonus);
            }

            bool splitMeta = ctx.Progress.HasMetamorph(RunProgress.MetaPierce);

            for (int i = arrows.Count - 1; i >= 0; i--)
            {
                var a = arrows[i];
                a.life -= dt;
                Vector2 previous = a.view.transform.position;
                a.view.transform.position += (Vector3)(a.velocity * dt);
                if (!ctx.Visible(a.view.transform.position) || a.life <= 0 || a.hitsLeft <= 0)
                {
                    if (ctx.Pool != null) ctx.Pool.Release("fire", a.view); else Object.Destroy(a.view.gameObject);
                    arrows.RemoveAt(i);
                    continue;
                }
                if (ctx.Intercept != null) ctx.Intercept(previous, a.view.transform.position, .2f);
                for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (e == null || !e.Targetable) continue;
                    if (a.hit.Contains(e)) continue;
                    if (Vector2.Distance(a.view.transform.position, e.view.position) < e.radius + .2f)
                    {
                        a.hit.Add(e);
                        ctx.DealDamage(j, a.damage, previous);
                        a.hitsLeft--;
                        if (splitMeta && !a.isSplit)
                            SpawnSplit(ctx, a.view.transform.position, a.velocity.normalized);
                        if (a.hitsLeft <= 0) { a.life = 0; break; }
                    }
                }
            }
        }

        void SpawnSplit(EmberWeaponContext ctx, Vector2 pos, Vector2 dir)
        {
            Vector2 side = Quaternion.Euler(0, 0, 28f) * dir;
            Fire(ctx, pos, side, true);
        }

        void Fire(EmberWeaponContext ctx, Vector2 origin, Vector2? forcedDir, bool isSplit)
        {
            Vector2 dir;
            if (forcedDir.HasValue) dir = forcedDir.Value.normalized;
            else
            {
                int nearest = ctx.FindNearestVisibleEnemy();
                if (nearest < 0) return;
                var target = ctx.GetEnemy(nearest);
                dir = ((Vector2)target.view.position - origin).normalized;
            }
            int count = isSplit ? 1 : (1 + ctx.Progress.WeaponCount(RunProgress.WeaponPierce));
            float dmg = 30f * ctx.Progress.DamageMul * (isSplit ? 0.45f : 1f);
            int limit = ctx.Progress.PierceLimit;
            if (limit < 1) limit = 1;
            float size = isSplit ? .18f : .28f;
            float speed = isSplit ? 10f : 12f;
            for (int i = 0; i < count; i++)
            {
                Vector2 v = Quaternion.Euler(0, 0, (i - (count - 1) * .5f) * 12f) * dir;
                var r = ctx.Pool != null ? ctx.Pool.RentFire(ctx.World, origin, size) : EmberArt.Fire(ctx.World, origin, size);
                r.color = isSplit ? new Color(1f, .85f, .35f) : new Color(1f, .55f, .2f);
                r.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90);
                arrows.Add(new Arrow
                {
                    view = r,
                    velocity = v * speed,
                    life = isSplit ? 1.0f : 2.2f,
                    damage = dmg,
                    hitsLeft = isSplit ? 1 : limit,
                    isSplit = isSplit
                });
            }
        }
    }
}
