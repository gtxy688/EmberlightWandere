using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Boomerang. Exclusive \u56de\u9a6c extra leg hit. Metamorph \u6298\u8fd4\u4e0d\u5c3d extra round trip.</summary>
    public sealed class EmberBoomerangWeapon : EmberWeapon
    {
        sealed class Butterfly
        {
            public SpriteRenderer view;
            public Vector2 dir;
            public float traveled;
            public float damage;
            public bool returning;
            public int tripsLeft;
            public bool midLegCleared;
            public readonly HashSet<EmberCombat.Enemy> hitOut = new HashSet<EmberCombat.Enemy>();
            public readonly HashSet<EmberCombat.Enemy> hitBack = new HashSet<EmberCombat.Enemy>();
        }

        const float OutDist = 6f;
        const float Speed = 10f;
        readonly List<Butterfly> active = new List<Butterfly>();
        float cd;

        public override void Reset()
        {
            active.Clear();
            cd = 0;
        }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Progress == null || !ctx.Progress.OwnsWeapon(RunProgress.WeaponBoom)) return;

            cd -= dt;
            if (cd <= 0 && ctx.EnemyCount() > 0)
            {
                Fire(ctx);
                cd = 2.0f / (1f + ctx.Progress.AttackSpeedBonus); // Weapon-Balance-v1
            }

            bool extraLeg = ctx.Progress.BoomExtraLegHit;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var b = active[i];
                if (!b.returning)
                {
                    Vector2 step = b.dir * Speed * dt;
                    b.view.transform.position += (Vector3)step;
                    b.traveled += step.magnitude;
                    if (extraLeg && !b.midLegCleared && b.traveled >= OutDist * 0.5f)
                    {
                        b.hitOut.Clear();
                        b.midLegCleared = true;
                    }
                    HitLeg(ctx, b, b.hitOut);
                    if (b.traveled >= OutDist)
                    {
                        b.returning = true;
                        b.midLegCleared = false;
                    }
                }
                else
                {
                    Vector2 toPlayer = ctx.PlayerPos - (Vector2)b.view.transform.position;
                    float dist = toPlayer.magnitude;
                    if (extraLeg && !b.midLegCleared && dist <= OutDist * 0.5f)
                    {
                        b.hitBack.Clear();
                        b.midLegCleared = true;
                    }
                    if (dist < .35f)
                    {
                        if (b.tripsLeft > 0)
                        {
                            b.tripsLeft--;
                            b.returning = false;
                            b.traveled = 0;
                            b.midLegCleared = false;
                            b.hitOut.Clear();
                            b.hitBack.Clear();
                            // Aim again toward nearest
                            int nearest = ctx.FindNearestVisibleEnemy();
                            if (nearest >= 0)
                            {
                                var t = ctx.GetEnemy(nearest);
                                b.dir = ((Vector2)t.view.position - ctx.PlayerPos).normalized;
                            }
                            continue;
                        }
                        if (ctx.Pool != null) ctx.Pool.Release("fire", b.view); else Object.Destroy(b.view.gameObject);
                        active.RemoveAt(i);
                        continue;
                    }
                    b.view.transform.position += (Vector3)(toPlayer.normalized * Speed * dt);
                    HitLeg(ctx, b, b.hitBack);
                }
            }
        }

        void HitLeg(EmberWeaponContext ctx, Butterfly b, HashSet<EmberCombat.Enemy> hitSet)
        {
            for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
            {
                var e = ctx.GetEnemy(j);
                if (e == null || !e.Targetable) continue;
                if (hitSet.Contains(e)) continue;
                if (Vector2.Distance(b.view.transform.position, e.view.position) < e.radius + .28f)
                {
                    hitSet.Add(e);
                    ctx.DealDamage(j, b.damage, b.view.transform.position);
                }
            }
        }

        void Fire(EmberWeaponContext ctx)
        {
            int nearest = ctx.FindNearestVisibleEnemy();
            if (nearest < 0) return;
            var target = ctx.GetEnemy(nearest);
            Vector2 dir = ((Vector2)target.view.position - ctx.PlayerPos).normalized;
            float dmg = 18f * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponBoom));
            var r = ctx.Pool != null ? ctx.Pool.RentFire(ctx.World, ctx.PlayerPos, .30f) : EmberArt.Fire(ctx.World, ctx.PlayerPos, .30f);
            r.color = new Color(1f, .4f, .55f);
            active.Add(new Butterfly
            {
                view = r,
                dir = dir,
                damage = dmg,
                tripsLeft = ctx.Progress.BoomExtraTrips
            });
        }
    }
}
