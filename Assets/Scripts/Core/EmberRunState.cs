using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 本局的运行时状态与场景引用：构筑进度、关卡配置、计时、击杀、以及这一局创建出来的
    /// 世界/灯芯/相机/特效句柄。
    ///
    /// 这里**只放数据，不放规则**：伤害、护盾、选卡推进分别属于 `EmberKeeperVitals`、
    /// `RunProgress`、`RunFlowController`。这样战斗与表现模块可以只读本对象，而不必认识
    /// `EmberGame`。注意本类引用了 `Transform`/`Camera`，因此不能脱离 Unity 单测——
    /// 纯逻辑仍在 `RunProgress` 里。
    /// </summary>
    public sealed class EmberRunState
    {
        /// <summary>本局构筑：槽位、已拥有武器、通用属性、选卡池。</summary>
        public RunProgress Progress { get; set; }

        /// <summary>本局关卡配置（难度倍率、局长、Boss 波）。</summary>
        public LevelConfig Level { get; set; }

        /// <summary>本局已进行的游戏内秒数。</summary>
        public float Elapsed { get; set; }

        /// <summary>本局击杀数（含分裂子体）。</summary>
        public int Kills { get; set; }

        /// <summary>本局世界根节点，装载装饰、边界与灯芯。</summary>
        public Transform World { get; set; }

        /// <summary>灯芯（玩家）根节点。</summary>
        public Transform Keeper { get; set; }

        /// <summary>本局使用的正交相机。</summary>
        public Camera Cam { get; set; }

        /// <summary>世界节点上的粒子特效宿主。</summary>
        public EmberEffects Effects { get; set; }

        /// <summary>灯芯生命结算；每局共用同一个实例，<see cref="Clear"/> 时重置。</summary>
        public EmberKeeperVitals Vitals { get; private set; }

        /// <summary>当前生命，转发自 <see cref="Vitals"/>。</summary>
        public float Health { get { return Vitals.Health; } }

        /// <summary>生命上限 = 基础值 + 本局构筑加成，转发自 <see cref="Vitals"/>。</summary>
        public float MaxHealth { get { return Vitals.MaxHealth; } }

        public EmberRunState(System.Func<EmberRunPhase> phaseProvider)
        {
            Vitals = new EmberKeeperVitals(this, phaseProvider);
        }

        public bool IsActive { get { return Progress != null; } }

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
        }
    }
}
