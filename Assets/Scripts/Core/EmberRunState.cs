using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 持有本局数据与场景引用；生命、构筑和选卡规则由各自模块处理。
    /// </summary>
    public sealed class EmberRunState
    {
        public RunProgress Progress { get; set; }
        public LevelConfig Level { get; set; }
        public float Elapsed { get; set; }
        /// <summary>本局击杀数（含分裂子体）。</summary>
        public int Kills { get; set; }
        /// <summary>本局世界根节点，装载装饰、边界与灯芯。</summary>
        public Transform World { get; set; }
        public Transform Keeper { get; set; }
        public Camera Cam { get; set; }
        public EmberEffects Effects { get; set; }
        /// <summary>灯芯生命结算；每局共用同一个实例，<see cref = "Clear"/> 时重置。</summary>
        public EmberKeeperVitals Vitals { get; private set; }
        /// <summary>玩家的临时 Buff 与 Debuff；独立于选卡构筑。</summary>
        public BuffManager Buffs { get; } = new BuffManager();

        public float Health
        {
            get
            {
                return Vitals.Health;
            }
        }

        public float MaxHealth
        {
            get
            {
                return Vitals.MaxHealth;
            }
        }

        public EmberRunState(System.Func<EmberRunPhase> phaseProvider)
        {
            Vitals = new EmberKeeperVitals(() => Progress, () => Level != null ? Level.DamageMultiplier : 1f, phaseProvider);
        }

        public bool IsActive
        {
            get
            {
                return Progress != null;
            }
        }

        /// <summary>收局/回营地时清空引用，避免留下悬空的已销毁对象。</summary>
        public void Clear()
        {
            Progress = null;
            Elapsed = 0f;
            Kills = 0;
            World = null;
            Keeper = null;
            Effects = null;
            Vitals.Reset();
            Buffs.Clear();
        }
    }
}
