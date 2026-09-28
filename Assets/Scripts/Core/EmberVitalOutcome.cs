namespace Emberlight
{
    /// <summary>
    /// 生命与治疗的结算结果。原先这些方法返回 bool，把"调用被拒绝"与"结算完成"压成同一个值，
    /// 调用方无法区分，只能一律补刷 HUD。改成三态后调用点可以精确复刻重构前的行为。
    /// </summary>
    public enum EmberVitalOutcome
    {
        /// <summary>阶段或参数不满足，本次调用未生效。</summary>
        Rejected,

        /// <summary>已结算，但生命值没有实际变化（满血治疗、护盾全吃等）。</summary>
        NoChange,

        /// <summary>已结算且生命值发生变化。</summary>
        Changed
    }
}
