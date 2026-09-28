namespace Emberlight
{
    /// <summary>
    /// 局长阶段。原先嵌在 `EmberGame` 里，但 M3（战斗/敌人）与 M7（表现）都需要读它做门控，
    /// 嵌在 MonoBehaviour 上会让它们被迫依赖整个游戏类。提成独立顶层类型后，门控只需一个 enum。
    /// 取值与顺序保持与重构前的 `EmberGame.Mode` 完全一致。
    /// </summary>
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
