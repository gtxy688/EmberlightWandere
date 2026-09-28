namespace Emberlight
{
    /// <summary>
    /// 只读的局长阶段来源。
    ///
    /// 表现层（M7）只需要回答"现在该不该播"这一个问题，不应该为此拿到整个游戏对象——
    /// 重构前 `EmberEffects` 与 `EmberKeeperAnimation` 就是靠持有 `EmberGame` 才能读到阶段。
    /// 把这一项抽成最小接口后，表现层不再依赖 M2/M3/M6 中的任何一个。
    /// </summary>
    public interface IEmberRunPhaseSource
    {
        /// <summary>当前局长阶段。</summary>
        EmberRunPhase Phase { get; }
    }
}
