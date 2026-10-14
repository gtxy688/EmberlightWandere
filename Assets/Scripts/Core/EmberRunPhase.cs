namespace Emberlight
{
    /// <summary>局长阶段。M3（战斗/敌人）与 M7（表现）靠它做门控，独立成顶层 enum 可免于依赖整个游戏类；
    /// 取值与顺序属于兼容契约，不要重排。</summary>
    public enum EmberRunPhase
    {
        Loading,
        Menu,
        SelectLength,
        SelectDifficulty,
        SelectSlots,
        SelectRoster,
        Playing,
        Upgrade,
        Paused,
        Lost,
        Won
    }
}
