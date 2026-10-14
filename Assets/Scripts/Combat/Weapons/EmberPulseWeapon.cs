using UnityEngine;

namespace Emberlight
{
    /// <summary>波次脉冲：WavePower &gt; 0 时每 4 秒，半径 3+WP×1.5，伤害 50×WP。</summary>
    public sealed class EmberPulseWeapon : EmberWeapon
    {
        float pulseTimer;

        public override void Reset() { pulseTimer = 0; }

        public override void Tick(float dt, EmberWeaponContext ctx)
        {
            pulseTimer -= dt;
            if (ctx.Progress.WavePower > 0 && pulseTimer <= 0)
            {
                pulseTimer = 4;
                float radius = 3 + ctx.Progress.WavePower * 1.5f;
                ctx.Effects.Nova(ctx.PlayerPos, radius);
                for (int j = ctx.EnemyCount() - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (e == null || !e.Targetable) continue;
                    if (Vector2.Distance(ctx.PlayerPos, e.view.position) < radius)
                        ctx.DealDamage(j, 50 * ctx.Progress.WavePower, ctx.PlayerPos);
                }
            }
        }
    }
}
