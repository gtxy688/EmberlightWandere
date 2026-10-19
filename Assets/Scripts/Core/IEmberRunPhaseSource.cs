namespace Emberlight
{
    /// <summary>
    /// 只读阶段来源；战斗宿主继承此契约，表现模块使用 Initialize 注入的阶段委托。
    /// </summary>
    public interface IEmberRunPhaseSource
    {
        EmberRunPhase Phase { get; }
    }
}
