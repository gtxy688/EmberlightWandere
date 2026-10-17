using System;

namespace Emberlight
{
    /// <summary>一次正在生效的 Buff；只有所属管理器可以推进和刷新时间。</summary>
    public sealed class Buff
    {
        public BuffConfig Config { get; }
        public float Remaining { get; private set; }

        internal Buff(BuffConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Refresh();
        }

        internal void Refresh() { Remaining = Config.Duration; }
        internal void Tick(float dt) { Remaining = Math.Max(0, Remaining - dt); }
    }
}
