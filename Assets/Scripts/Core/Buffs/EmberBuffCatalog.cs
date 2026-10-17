namespace Emberlight
{
    /// <summary>第一批可用 Buff 的共享定义；当前调参数值见需求文档的 Buff 章节。</summary>
    public static class EmberBuffCatalog
    {
        public static readonly BuffConfig Power = new BuffConfig("power", "增伤", BuffAttribute.Damage, .3f, 8);
        public static readonly BuffConfig Haste = new BuffConfig("haste", "疾行", BuffAttribute.MoveSpeed, .3f, 8);
        public static readonly BuffConfig Slow = new BuffConfig("slow", "迟缓", BuffAttribute.MoveSpeed, -.25f, 3, true);
    }
}
