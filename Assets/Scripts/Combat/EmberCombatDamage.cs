using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 通过 Context 结算伤害与死亡；击杀、Boss 结果和分裂请求交回战斗根处理。
    /// 子体须等全部武器 Tick 结束后生成，避免本帧遍历被新增敌人打断。
    /// </summary>
    public static class EmberCombatDamage
    {
        /// <summary>受击闪白时长（秒）。</summary>
        public const float HitFlashSeconds = .10f;

        /// <summary>一次死亡结算的结果，交回战斗根分派。</summary>
        public struct Outcome
        {
            /// <summary>本次结算中是否打掉了 Boss。</summary>
            public bool BossDefeated;
            /// <summary>本次增加的击杀数（含分裂子体）；调用方据此累加波次击杀计数。</summary>
            public int Kills;
        }

        /// <summary>一次结算所需的全部引用。</summary>
        public sealed class Context
        {
            public EmberEnemyRoster Roster;
            public LevelConfig Config;
            public EmberEffects Effects;
            public Transform Player;
            public Camera Camera;
            public BuffManager PlayerBuffs;
            /// <summary>行为策略回调用的共享 context（由战斗根持有并复用）。</summary>
            public EmberEnemyContext EnemyContext;
            public System.Action PlayHit;
            /// <summary>击杀记分回调（参数为本次应加的分数）。</summary>
            public System.Action<int> AddScore;
            /// <summary>记录一个分裂落点（烬胎母体死亡位置）。</summary>
            public System.Action<Vector2> NoteSplitPoint;
            public EmberDrops Drops;
            /// <summary>掉落 roll 用的随机源；必须与 <see cref="EmberEnemyRoster"/> 用同一个实例。</summary>
            public System.Random Random;
        }

        /// <summary>按目标下标扣血；来源视为玩家当前位置。</summary>
        public static void Apply(Context ctx, int index, float amount)
        {
            Apply(ctx, index, amount, ctx.Player != null ? (Vector2)ctx.Player.position : Vector2.zero, true);
        }

        /// <summary>按目标下标扣血，并显式给出伤害来源点（接触伤害要按来源算正面减伤）。</summary>
        public static void Apply(Context ctx, int index, float amount, Vector2 source)
        {
            Apply(ctx, index, amount, source, true);
        }

        /// <summary>
        /// 扣血主入口。<paramref name="playHit"/> 为 false 时静音（接触伤害每次接触都播会吵）。
        /// 不可选中或不在视口内的目标直接忽略，防止"隔屏打死"。
        /// </summary>
        public static void Apply(Context ctx, int index, float amount, Vector2 source, bool playHit)
        {
            var roster = ctx.Roster;
            if (index < 0 || index >= roster.Count) return;
            var e = roster.At(index);
            if (!e.Targetable || !EmberWorld.Visible(ctx.Camera, e.view.position)) return;

            // 临时增伤在实际命中时统一应用一次，覆盖弹体、接触与共享燃地。
            amount *= ctx.PlayerBuffs != null ? ctx.PlayerBuffs.DamageMultiplier : 1f;

            // 铁灯卫：正面 60° 内减伤。
            if (e.kind == 7)
                amount *= ShieldBehavior.DamageMultiplier(
                    e.facing, source - (Vector2)e.view.position, ctx.Config.ShieldFrontMultiplier);

            e.hp = Mathf.Max(0f, e.hp - amount);
            if (playHit && ctx.PlayHit != null) ctx.PlayHit();
            e.bar.Set(e.hp / e.maxHp);
            if (e.flash <= 0f)
            {
                if (ctx.Effects != null) ctx.Effects.Impact(e.view.position, false);
                e.flash = HitFlashSeconds;
            }
            if (amount >= 1f && ctx.Player != null)
                e.view.position += (e.view.position - ctx.Player.position).normalized * (e.IsBoss ? .015f : .06f);
            if (e.hp > 0f) return;

            if (TryBeginRebirth(e, ctx.Config)) return;

            // 引线虫等可以在"致死"这一步拒绝死亡（改为引爆）。
            if (e.behavior != null && e.behavior.OnLethal(BuildEnemyContext(ctx, e))) return;
            e.dead = true;
        }

        /// <summary>
        /// 重生词缀起读条：死亡后按配置延时以 40% 生命复活一次；Boss 不重生。
        /// 返回 true 表示本次死亡已被重生接管。
        /// </summary>
        public static bool TryBeginRebirth(EmberEnemy e, LevelConfig config)
        {
            if (e.affix != EnemyAffix.Rebirth || e.revived || e.IsBoss) return false;
            e.revived = true;
            e.hp = 0f;
            e.dead = e.detonating = e.slowShot = false;
            e.reviveTimer = config.RebirthDelay;
            e.charge = e.fuse = 0f;
            // 复活后给一套全新的行为状态，避免残留读条中的射击。
            e.behavior = EmberEnemyRoster.CreateBehavior(e.kind);
            return true;
        }

        /// <summary>
        /// 处理所有 <c>dead</c> 的敌人：重生接管 → 行为 OnDeath → 掉落 → 回收 → 记分。
        /// 必须在全部武器 Tick 之后调用，否则会在遍历中改动敌人列表。
        /// </summary>
        public static Outcome ResolveDeaths(Context ctx)
        {
            var outcome = new Outcome();
            var roster = ctx.Roster;
            var enemies = roster.All;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (!e.dead) continue;

                // 也覆盖"活着时被点燃、随后死于自己爆炸"的引线虫。
                if (TryBeginRebirth(e, ctx.Config)) continue;

                bool boss = e.IsBoss;
                if (e.behavior != null) e.behavior.OnDeath(BuildEnemyContext(ctx, e));
                if (ctx.Effects != null) ctx.Effects.Impact(e.view.position, true);
                if (ctx.Drops != null)
                    ctx.Drops.OnDeath(e.view.position, e.kind, e.affix, ctx.Random);
                // 与行为回调共同形成两批子体；当前确认保留此数量，不能单独删掉一路。
                if (e.kind == 5 && ctx.NoteSplitPoint != null) ctx.NoteSplitPoint(e.view.position);

                roster.Release(e);
                roster.RemoveAt(i);
                outcome.Kills++;
                if (ctx.AddScore != null)
                    ctx.AddScore(boss ? 30 : e.kind == 6 ? 0 : e.affix != EnemyAffix.None ? 3 : 1);

                if (boss)
                {
                    // Boss 阵亡后立即停止本轮结算：弹幕与波次都交给战斗根处理。
                    outcome.BossDefeated = true;
                    break;
                }
            }

            return outcome;
        }

        /// <summary>构造一次行为回调用的 context（行为策略不得长期持有它）。</summary>
        static EmberEnemyContext BuildEnemyContext(Context ctx, EmberEnemy e)
        {
            var c = ctx.EnemyContext;
            c.Enemy = e;
            c.Config = ctx.Config;
            c.SpeedMultiplier = e.affix == EnemyAffix.Wind && e.age % ctx.Config.WindPeriod < ctx.Config.WindDuration
                ? ctx.Config.WindSpeedMultiplier
                : 1f;
            return c;
        }
    }
}
