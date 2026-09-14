using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Trail. Exclusive \u5ef6\u71c3 longer/hotter. Metamorph \u706b\u6d77 persistent ring.</summary>
    public sealed class EmberTrailWeapon : EmberWeapon
    {
        sealed class Flame
        {
            public SpriteRenderer view;
            public float life;
            public float powerScale;
        }

        readonly List<Flame> flames = new List<Flame>();
        float trailTimer;
        SpriteRenderer ringView;
        float seaPatchTimer;

        public override void Reset()
        {
            flames.Clear();
            trailTimer = 0;
            if (ringView != null) { Object.Destroy(ringView.gameObject); ringView = null; }
            seaPatchTimer = 0;
        }

        EmberPool pool;

        public void SpawnPatch(Vector2 pos, Transform world, float powerScale)
        {
            SpawnPatch(pos, world, powerScale, pool);
        }

        public void SpawnPatch(Vector2 pos, Transform world, float powerScale, EmberPool p)
        {
            pool = p;
            float size = powerScale >= 0.99f ? 1.2f : 0.85f;
            float life = powerScale >= 0.99f ? 2.5f : 1.6f;
            if (flames.Count >= 40)
            {
                var oldest = flames[0];
                flames.RemoveAt(0);
                if (oldest.view != null)
                {
                    if (pool != null) pool.Release("burn", oldest.view);
                    else Object.Destroy(oldest.view.gameObject);
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
                    for (int k = 0; k < 3; k++) EmberArt.Fire(r.transform, new Vector2((k - 1) * .28f, .05f * Mathf.Sin(k)), .20f, 2);
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
                for (int k = 0; k < 3; k++) EmberArt.Fire(r.transform, new Vector2((k - 1) * .28f, .05f * Mathf.Sin(k)), .20f, 2);
            }
            flames.Add(new Flame { view = r, life = life, powerScale = powerScale });

        }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Pool != null) pool = ctx.Pool;
            trailTimer -= dt;
            bool ownsTrail = ctx.Progress != null && ctx.Progress.OwnsWeapon(RunProgress.WeaponTrail);
            bool extend = ctx.Progress != null && ctx.Progress.TrailExtend;
            float interval = (extend ? .28f : .45f) / (1f + ctx.Progress.WeaponAttackSpeed(RunProgress.WeaponTrail));
            if (ownsTrail && ctx.Progress.TrailPower > 0 && trailTimer <= 0)
            {
                trailTimer = interval;
                SpawnPatch(ctx.PlayerPos, ctx.World, extend ? 1.25f : 1f);
            }

            // Metamorph 火海: visible persistent ring + DoT
            if (ctx.Progress != null && ctx.Progress.TrailRing && ownsTrail)
            {
                float ringR = 2.1f;
                float ringDps = 70f * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponTrail)) * (ctx.Progress.TrailPower > 0 ? ctx.Progress.TrailPower : 0.4f);
                if (ringView == null)
                {
                    ringView = EmberVisuals.Shape("Fire sea ring", ctx.World, ctx.PlayerPos, Vector2.one * (ringR * 2.05f), new Color(1f, .35f, .05f, .38f), 3);
                    ringView.sprite = EmberArt.Ring;
                }
                else
                {
                    ringView.transform.position = ctx.PlayerPos;
                    float pulse = 1f + .06f * Mathf.Sin(ctx.Elapsed * 6f);
                    ringView.transform.localScale = Vector3.one * (ringR * 2.05f * pulse);
                    ringView.color = new Color(1f, .4f + .15f * Mathf.Sin(ctx.Elapsed * 8f), .05f, .42f);
                }
                for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (e == null || !e.Targetable) continue;
                    if (Vector2.Distance(ctx.PlayerPos, e.view.position) < ringR + e.radius)
                        if (ctx.DealContactDamage(j, ringDps * dt, ctx.PlayerPos))
                            EmberAudio.Ensure().PlayWeaponBurnGround();
                }
                seaPatchTimer -= dt;
                if (seaPatchTimer <= 0f)
                {
                    seaPatchTimer = 0.35f;
                    SpawnPatch(ctx.PlayerPos, ctx.World, 1.1f);
                }
            }
            else if (ringView != null)
            {
                Object.Destroy(ringView.gameObject);
                ringView = null;
            }

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
                float trailPow = ctx.Progress.TrailPower > 0 ? ctx.Progress.TrailPower : 0.4f;
                if (extend) trailPow *= 1.35f;
                float hitR = f.powerScale >= 0.99f ? .7f : .55f;
                for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (e == null || !e.Targetable) continue;
                    if (Vector2.Distance(f.view.transform.position, e.view.position) < hitR)
                        if (ctx.DealContactDamage(j, 46 * trailPow * f.powerScale * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponTrail)) * dt, f.view.transform.position))
                            EmberAudio.Ensure().PlayWeaponBurnGround();
                }
                if (f.life <= 0) { if (pool != null) pool.Release("burn", f.view); else Object.Destroy(f.view.gameObject); flames.RemoveAt(i); }
            }
        }
    }
}
