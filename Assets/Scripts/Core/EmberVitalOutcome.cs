namespace Emberlight
{
    /// <summary>生命与治疗的结算结果：三态区分「调用被拒绝」与「已结算但数值未变」，调用点无需一律补刷 HUD。</summary>
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
