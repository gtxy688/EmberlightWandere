namespace Emberlight
{
    /// <summary>
    /// 只读的局长阶段来源。表现层（M7）只需回答「现在该不该播」，因此只暴露这一项最小接口，
    /// 拿不到整个游戏对象，也不再依赖 M2 / M3 / M6 中的任何一个。
    /// </summary>
    public interface IEmberRunPhaseSource
    {
        /// <summary>当前局长阶段。</summary>
        EmberRunPhase Phase { get; }
    }
}
