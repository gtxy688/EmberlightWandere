using System;
using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>
    /// 同 Id 刷新、不叠层。属性从仍生效的 Buff 汇总，不直接改写基础属性，避免到期恢复错误。
    /// 调用方只在 Playing 阶段传入游戏时间；本模块不依赖 Unity、场景或 UI。
    /// </summary>
    public sealed class BuffManager
    {
        public const float MinimumMoveSpeedMultiplier = .25f;
        readonly List<Buff> active = new List<Buff>();
        public IReadOnlyList<Buff> Active { get; }
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float DamageMultiplier { get; private set; } = 1f;

        public BuffManager() { Active = active.AsReadOnly(); }

        /// <summary>返回 true 表示新增，false 表示刷新已有 Buff；调用方可据此展示反馈。</summary>
        public bool Add(BuffConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            foreach (var buff in active)
            {
                if (buff.Config.Id != config.Id) continue;
                if (!ReferenceEquals(buff.Config, config))
                    throw new ArgumentException("The same Buff Id must use the same configuration.", nameof(config));
                buff.Refresh();
                return false;
            }
            active.Add(new Buff(config));
            Recalculate();
            return true;
        }

        public void Tick(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0)
                throw new ArgumentOutOfRangeException(nameof(dt));
            bool changed = false;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].Tick(dt);
                if (active[i].Remaining > 0) continue;
                active.RemoveAt(i);
                changed = true;
            }
            if (changed) Recalculate();
        }

        public bool Remove(string id)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Config.Id != id) continue;
                active.RemoveAt(i);
                Recalculate();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            active.Clear();
            Recalculate();
        }

        void Recalculate()
        {
            float move = 1f, damage = 1f;
            foreach (var buff in active)
            {
                if (buff.Config.Attribute == BuffAttribute.MoveSpeed) move += buff.Config.Bonus;
                else if (buff.Config.Attribute == BuffAttribute.Damage) damage += buff.Config.Bonus;
            }
            MoveSpeedMultiplier = Math.Max(MinimumMoveSpeedMultiplier, move);
            DamageMultiplier = Math.Max(0, damage);
        }
    }
}
