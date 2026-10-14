using UnityEngine;

namespace Emberlight
{
    /// <summary>轨迹。专属 \u5ef6\u71c3 间隔更短、燃地更强；蜕变 \u706b\u6d77 持续火环带持续伤害。</summary>
    public sealed class EmberTrailWeapon : EmberWeapon
    {
        float trailTimer;
        SpriteRenderer ringView;
        float seaPatchTimer;
        public override void Reset()
        {
            trailTimer = 0;
            if (ringView != null)
            {
                Object.Destroy(ringView.gameObject);
                ringView = null;
            }

            seaPatchTimer = 0;
        }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Progress == null)
                return;
            trailTimer -= dt;
            bool ownsTrail = ctx.Progress != null && ctx.Progress.OwnsWeapon(RunProgress.WeaponTrail);
            bool extend = ctx.Progress != null && ctx.Progress.TrailExtend;
            float interval = (extend ? EmberWeaponTuning.Trail.ExtendedInterval : EmberWeaponTuning.Trail.Interval) / (1f + ctx.Progress.WeaponAttackSpeed(RunProgress.WeaponTrail));
            if (ownsTrail && ctx.Progress.TrailPower > 0 && trailTimer <= 0)
            {
                trailTimer = interval;
                ctx.SpawnBurnPatch?.Invoke(ctx.PlayerPos, extend ? 1.25f : 1f);
            }

            if (ctx.Progress != null && ctx.Progress.TrailRing && ownsTrail)
            {
                float ringR = EmberWeaponTuning.Trail.RingRadius;
                float ringDps = EmberWeaponTuning.Trail.RingDps * ctx.Progress.DamageMul * (1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponTrail)) * (ctx.Progress.TrailPower > 0 ? ctx.Progress.TrailPower : EmberWeaponTuning.BurnGround.DefaultPower);
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
                    if (e == null || !e.Targetable)
                        continue;
                    if (Vector2.Distance(ctx.PlayerPos, e.view.position) < ringR + e.radius)
                        if (ctx.DealContactDamage(j, ringDps * dt, ctx.PlayerPos))
                            ctx.PlaySound?.Invoke(EmberWeaponSound.BurnGround);
                }

                seaPatchTimer -= dt;
                if (seaPatchTimer <= 0f)
                {
                    seaPatchTimer = EmberWeaponTuning.Trail.SeaPatchInterval;
                    ctx.SpawnBurnPatch?.Invoke(ctx.PlayerPos, 1.1f);
                }
            }
            else if (ringView != null)
            {
                Object.Destroy(ringView.gameObject);
                ringView = null;
            }
        }
    }
}
