using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Orbit. Exclusive +1 orb. Metamorph \u65e5\u5195: radius/spin/DPS up + burn trail. Same-target: nearest orb only.</summary>
    public sealed class EmberOrbitWeapon : EmberWeapon
    {
        readonly List<SpriteRenderer> orbs = new List<SpriteRenderer>();
        float burnDropTimer;

        public override void Reset()
        {
            orbs.Clear();
            burnDropTimer = 0;
        }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Progress == null || !ctx.Progress.OwnsWeapon(RunProgress.WeaponOrbit)) return;
            int count = ctx.Progress.Orbits;
            bool corona = ctx.Progress.HasMetamorph(RunProgress.MetaOrbit);
            float radius = corona ? 2.3f : 1.6f;
            float spin = (corona ? 3.6f : 2.5f) * (1f + ctx.Progress.WeaponAttackSpeed(RunProgress.WeaponOrbit));
            float dps = (corona ? 75f : 65f) * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponOrbit));

            while (orbs.Count < count)
            {
                orbs.Add(ctx.Pool != null ? ctx.Pool.RentFire(ctx.World, ctx.PlayerPos, .33f) : EmberArt.Fire(ctx.World, ctx.PlayerPos, .33f));

            }
            while (orbs.Count > count)
            {
                var extra = orbs[orbs.Count - 1];
                orbs.RemoveAt(orbs.Count - 1);
                if (ctx.Pool != null) ctx.Pool.Release("fire", extra); else if (extra != null) Object.Destroy(extra.gameObject);
            }

            burnDropTimer -= dt;
            bool dropBurn = corona && burnDropTimer <= 0;
            if (dropBurn) burnDropTimer = .35f;

            for (int i = 0; i < orbs.Count && i < count; i++)
            {
                float a = ctx.Elapsed * spin + i * Mathf.PI * 2 / count;
                Vector2 p = ctx.PlayerPos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                orbs[i].transform.position = p;
                float flicker=1f+.10f*Mathf.Sin(ctx.Elapsed*10+i*2);
                orbs[i].transform.localScale=new Vector3(.28f,.46f*flicker,1);
                orbs[i].transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                if ((int)(ctx.Elapsed * 24) != (int)((ctx.Elapsed - dt) * 24)) ctx.Effects.Trail(p);
                if (dropBurn && ctx.SpawnBurnPatch != null) ctx.SpawnBurnPatch(p, 0.35f);
            }
            // Weapon-Balance-v1: one orb damages each enemy (nearest in range) — no multi-orb stack.
            for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
            {
                var e = ctx.GetEnemy(j);
                if (e == null || !e.Targetable) continue;
                float best = 1e10f;
                bool hit = false;
                Vector2 source = ctx.PlayerPos;
                for (int i = 0; i < orbs.Count && i < count; i++)
                {
                    float d = Vector2.Distance(orbs[i].transform.position, e.view.position);
                    if (d < e.radius + .35f && d < best) { best = d; hit = true; source = orbs[i].transform.position; }
                }
                if (hit && ctx.DealContactDamage(j, dps * dt, source))
                    EmberAudio.Ensure().PlayWeaponOrbitContact();
            }
        }
    }
}
