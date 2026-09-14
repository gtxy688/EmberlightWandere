using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Meteor. Exclusive +1 impact. Metamorph \u5929\u706b bigger higher dmg.</summary>
    public sealed class EmberMeteorWeapon : EmberWeapon
    {
        sealed class Strike
        {
            public Vector2 point;
            public float timer;
            public float damage;
            public float radius;
            public SpriteRenderer warn;
        }

        readonly List<Strike> pending = new List<Strike>();
        float cd;

        public override void Reset()
        {
            pending.Clear();
            cd = 0;
        }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Progress == null || !ctx.Progress.OwnsWeapon(RunProgress.WeaponMeteor)) return;

            cd -= dt;
            if (cd <= 0 && ctx.EnemyCount() > 0)
            {
                int strikes = 1 + ctx.Progress.MeteorExtra;
                for (int s = 0; s < strikes; s++)
                    TryLock(ctx);
                cd = 3f / (1f + ctx.Progress.WeaponAttackSpeed(RunProgress.WeaponMeteor));
            }

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var s = pending[i];
                s.timer -= dt;
                if (s.warn != null)
                {
                    var c = s.warn.color;
                    c.a = .25f + .25f * Mathf.PingPong(ctx.Elapsed * 6f, 1f);
                    s.warn.color = c;
                }
                if (s.timer > 0) continue;
                if (s.warn != null) { if (ctx.Pool != null) ctx.Pool.Release("warn", s.warn); else Object.Destroy(s.warn.gameObject); }
                EmberAudio.Ensure().PlayMeteorImpact();
                ctx.Effects.Nova(s.point, s.radius);
                for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (Vector2.Distance(s.point, e.view.position) < s.radius)
                        ctx.Damage(j, s.damage);
                }
                pending.RemoveAt(i);
            }
        }

        void TryLock(EmberWeaponContext ctx)
        {
            int idx = ctx.FindRandomVisibleEnemy();
            if (idx < 0) return;
            var e = ctx.GetEnemy(idx);
            Vector2 point = e.view.position;
            bool skyfire = ctx.Progress.HasMetamorph(RunProgress.MetaMeteor);
            float radius = skyfire ? 2.53f : 1.6f; // skyfire +10% Weapon-Balance-v1
            if (ctx.Progress.WavePower > 0)
                radius += ctx.Progress.WavePower * 0.5f;
            float dmg = 45f * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponMeteor));
            if (skyfire) dmg *= 1.54f; // was 1.4; +10% Weapon-Balance-v1
            var warn = ctx.Pool != null ? ctx.Pool.RentShape("warn", ctx.World, point, Vector2.one * (radius * 2f), new Color(1f, .3f, .05f, .35f), 1) : EmberVisuals.Shape("Meteor warn", ctx.World, point, Vector2.one * (radius * 2f), new Color(1f, .3f, .05f, .35f), 1);
            warn.sprite = EmberArt.Ring;
            // Warning is silent; impact SFX plays when timer hits 0.
            pending.Add(new Strike { point = point, timer = 0.6f, damage = dmg, radius = radius, warn = warn });
        }
    }
}

