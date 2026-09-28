using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 战斗模块（M3）对宿主的最小契约。
    ///
    /// 重构前 `EmberCombat` / `EmberBoss` / `EmberHealingDrops` 直接持有 `EmberGame` 并读它的
    /// `State`、`Progress`、`Elapsed`——这些参数是纯逻辑，却让整个战斗模拟绑在 MonoBehaviour
    /// 上，无法脱离场景验证。现在只保留战斗真正需要的能力。
    ///
    /// 表现层（M7）的阶段门控不需要这些能力，用 <see cref="IEmberRunPhaseSource"/> 即可。
    /// </summary>
    public interface IEmberCombatHost : IEmberRunPhaseSource
    {
        /// <summary>本局已进行的游戏内秒数（敌人动画与武器节奏用）。</summary>
        float Elapsed { get; }

        /// <summary>本局构筑进度：武器归属、属性加成、计分。</summary>
        RunProgress Progress { get; }

        /// <summary>无敌帧是否已结束——接触伤害要先问这一次能不能打中。</summary>
        bool HurtReady { get; }

        /// <summary>是否处在"残血且战斗中"，治疗掉落据此加快吸附。</summary>
        bool NeedsHealing { get; }

        /// <summary>结算一次敌方伤害（难度倍率与护盾顺序由宿主保证）。</summary>
        void HurtPlayer(float damage);

        /// <summary>治疗灯芯；满血或非战斗阶段返回 false。</summary>
        bool HealPlayer(float amount);

        /// <summary>拾取怪物治疗掉落；返回 false 表示本次调用无效。</summary>
        bool PickHealingDrop(float amount);

        /// <summary>Boss 招式落点的爆发特效。</summary>
        void PlayBossBurst(Vector2 point);

        /// <summary>记一次击杀（分裂子体也计入）。</summary>
        void RegisterKill();

        /// <summary>一波清完，交回宿主决定发牌。</summary>
        void OnWaveCleared(int clearedWave);

        /// <summary>收局（最终 Boss 死亡 = 胜利）。</summary>
        void EndRun(bool won);
    }
}
