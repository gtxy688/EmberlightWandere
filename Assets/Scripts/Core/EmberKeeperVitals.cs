using System;

namespace Emberlight
{
    /// <summary>
    /// 灯芯（玩家）的生命结算：当前生命、无敌帧、受伤、回血、掉落拾取。
    /// 伤害链路只依赖本类与 <see cref="RunProgress"/>，不持有场景句柄，也不创建音频对象。
    ///
    /// 行为口径（不得静默变动）：
    /// - 难度倍率在 <see cref="Hurt"/> 内**统一乘一次**，覆盖接触/射手/爆炸/灼热/Boss；
    /// - 受伤后无敌帧 <see cref="HurtCooldown"/> 秒；
    /// - 护盾先乘难度倍率、再扣护盾，溢出部分扣生命（<see cref="RunProgress.AbsorbDamage"/>）；
    /// - 怪物治疗掉落**任意血量**都可拾取，溢出部分丢弃。
    /// </summary>
    public sealed class EmberKeeperVitals
    {
        /// <summary>每次难度下每局初始生命，见 `requirements.md` §3.3。</summary>
        public const float BaseMaxHealth = 100f;
        /// <summary>受伤后的无敌帧时长（秒）。</summary>
        public const float HurtCooldown = .65f;
        readonly Func<RunProgress> progress;
        readonly Func<float> damageMultiplier;
        /// <summary>当前生命。</summary>
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

        /// <summary>生命上限 = 基础值 + 构筑加成。</summary>
        public float MaxHealth
        {
            get
            {
                var current = progress();
                return BaseMaxHealth + (current != null ? current.BonusMaxHealth : 0f);
            }
        }

        /// <summary>残血且正在战斗时允许普通治疗。</summary>
        public bool NeedsHealing
        {
            get
            {
                return Current == EmberRunPhase.Playing && Health < MaxHealth;
            }
        }

        /// <summary>无敌帧是否已结束。</summary>
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

        /// <summary>按帧推进无敌帧倒计时。</summary>
        public void Tick(float dt)
        {
            HurtTimer -= dt;
        }

        /// <summary>把生命值钳到 [0, 上限]，返回实际生效的增量（可为负）。</summary>
        public float ApplyHealthDelta(float delta)
        {
            float before = Health;
            Health = Math.Max(0f, Math.Min(MaxHealth, before + delta));
            return Health - before;
        }

        /// <summary>结算一次生命变化；返回 true 表示灯芯因此倒下。</summary>
        public bool CommitHealthChange()
        {
            return Health <= 0f;
        }

        /// <summary>纯函数：治疗量钳制，供 UI 预览与测试复用。</summary>
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
