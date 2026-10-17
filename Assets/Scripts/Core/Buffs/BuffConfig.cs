using System;

namespace Emberlight
{
    public enum BuffAttribute { MoveSpeed, Damage }

    /// <summary>共享且不可变的 Buff 规则；倒计时属于各自的 Buff 对象。</summary>
    public sealed class BuffConfig
    {
        public string Id { get; }
        public string Name { get; }
        public BuffAttribute Attribute { get; }
        public float Bonus { get; }
        public float Duration { get; }
        public bool IsDebuff { get; }

        public BuffConfig(string id, string name, BuffAttribute attribute, float bonus, float duration, bool isDebuff = false)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Buff Id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Buff name is required.", nameof(name));
            if (!Enum.IsDefined(typeof(BuffAttribute), attribute)) throw new ArgumentOutOfRangeException(nameof(attribute));
            if (float.IsNaN(bonus) || float.IsInfinity(bonus)) throw new ArgumentOutOfRangeException(nameof(bonus));
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            Id = id;
            Name = name;
            Attribute = attribute;
            Bonus = bonus;
            Duration = duration;
            IsDebuff = isDebuff;
        }
    }
}
