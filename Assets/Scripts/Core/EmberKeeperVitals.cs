using System;

namespace Emberlight
{
    /// <summary>
    /// 结算玩家生命、护盾吸收、无敌帧与治疗；不持有场景或音频对象。
    /// 返回结算结果，由宿主决定呈现与收局动作。
    /// </summary>
    public sealed class EmberKeeperVitals
    {
        /// <summary>各难度共用的初始生命，见 requirements.md §3.3。</summary>
        public const float BaseMaxHealth = 100f;
        /// <summary>受伤后的无敌帧时长（秒）。</summary>
        public const float HurtCooldown = .65f;
        readonly Func<RunProgress> progress;
        readonly Func<float> damageMultiplier;
        public float Health { get; private set; }
        /// <summary>无敌帧剩余秒数；&gt; 0 时免疫后续伤害。</summary>
        public float HurtTimer { get; private set; }

        /// <summary>本局阶段闸门，由组合根注入。</summary>
        readonly Func<EmberRunPhase> phase;
        public EmberKeeperVitals(Func<RunProgress> progressProvider, Func<float> damageMultiplierProvider, Func<EmberRunPhase> phaseProvider)
        {
            progress = progressProvider;
            damageMultiplier = damageMultiplierProvider;
            phase = phaseProvider;
            Health = BaseMaxHealth;
        }

        EmberRunPhase Current
        {
            get
            {
                return phase != null ? phase() : EmberRunPhase.Playing;
            }
        }

        /// <summary>当前单局的固定生命上限。</summary>
        public float MaxHealth { get { return BaseMaxHealth; } }

        /// <summary>残血且正在战斗时允许普通治疗。</summary>
        public bool NeedsHealing
        {
            get
            {
                return Current == EmberRunPhase.Playing && Health < MaxHealth;
            }
        }

        public bool HurtReady
        {
            get
            {
                return HurtTimer <= 0f;
            }
        }

        /// <summary>开一局：满血、无无敌帧。</summary>
        public void Reset()
        {
            Health = MaxHealth;
            HurtTimer = 0f;
        }

        public void Tick(float dt)
        {
            HurtTimer -= dt;
        }

        /// <summary>结算一次生命变化；返回 true 表示灯芯因此倒下。</summary>
        public bool CommitHealthChange()
        {
            return Health <= 0f;
        }

        /// <summary>治疗量非负，溢出生命上限的部分丢弃。</summary>
        public static float HealedHealth(float current, float maximum, float amount)
        {
            return Math.Min(maximum, current + Math.Max(0, amount));
        }

        /// <summary>
        /// 结算一次敌方伤害。Rejected 表示阶段不对或仍在无敌帧；否则说明这次伤害已走完
        /// 难度倍率 → 护盾 → 生命的完整链路（即使护盾全吃、生命没掉）。
        /// </summary>
        public EmberVitalOutcome Hurt(float damage)
        {
            if (Current != EmberRunPhase.Playing || HurtTimer > 0f || damage <= 0f)
                return EmberVitalOutcome.Rejected;
            float before = Health;
            // 难度倍率只在这里乘一次；护盾先吸收，溢出部分才进生命。
            float remaining = progress().AbsorbDamage(damage * damageMultiplier());
            Health = Math.Max(0f, Health - remaining);
            HurtTimer = HurtCooldown;
            return Health != before ? EmberVitalOutcome.Changed : EmberVitalOutcome.NoChange;
        }

        /// <summary>治疗入口：仅在残血时生效（剩余量丢弃）。</summary>
        public EmberVitalOutcome Heal(float amount)
        {
            if (!NeedsHealing || amount <= 0f)
                return EmberVitalOutcome.Rejected;
            float before = Health;
            Health = HealedHealth(Health, MaxHealth, amount);
            return Health != before ? EmberVitalOutcome.Changed : EmberVitalOutcome.NoChange;
        }

        /// <summary>
        /// 怪物治疗掉落：任意血量都可拾取，满血时治疗溢出量丢弃。
        /// Rejected 表示不在 Play 阶段或数量非法；否则调用方应播拾取音。
        /// </summary>
        public EmberVitalOutcome PickHealingDrop(float amount)
        {
            if (Current != EmberRunPhase.Playing || amount <= 0f)
                return EmberVitalOutcome.Rejected;
            float before = Health;
            if (Health < MaxHealth)
                Health = HealedHealth(Health, MaxHealth, amount);
            return Health != before ? EmberVitalOutcome.Changed : EmberVitalOutcome.NoChange;
        }
    }
}
