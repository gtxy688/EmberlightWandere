using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 单个敌人的运行时数据（M3 拥有，M5 的行为策略只读/写其中字段）。
    /// 字段是公开的：这是**热路径上的逐帧数据**，行为策略与结算器直接读写，不做属性包装。
    /// </summary>
    public sealed class EmberEnemy
    {
        /// <summary>场景中的表现根节点（由对象池借出）。</summary>
        public Transform view;

        public float hp, maxHp, speed, radius, flash;

        /// <summary>头顶血条；Boss 的初始隐藏。</summary>
        public EmberEnemyBar bar;

        /// <summary>Boss 招式状态机；非 Boss 为 null。</summary>
        public EmberBoss ai;

        /// <summary>敌人种类 id（0/1/2/4/5/6/7/8 为杂兵，3 为 Boss）。</summary>
        public int kind;

        /// <summary>行为策略；Boss 走 <see cref="ai"/>，其余走这里。</summary>
        public IEnemyBehavior behavior;

        /// <summary>剪影与动画表现。</summary>
        public EmberEnemyVisual visual;

        public EnemyAffix affix;
        public bool dead, revived, detonating;
        public float reviveTimer, fuse, facing, charge, age;
        public Vector2 aim, moveDirection;

        /// <summary>躯干各部位的原始颜色，受击闪白后要还原。</summary>
        public SpriteRenderer[] parts;
        public Color[] colors;

        /// <summary>是否可被武器选中：节点在、血还活着、未死、未引爆、不在重生读条。</summary>
        public bool Targetable
        {
            get { return view != null && hp > 0 && !dead && !detonating && reviveTimer <= 0; }
        }

        /// <summary>是否为 Boss（kind 3）。</summary>
        public bool IsBoss { get { return kind == 3; } }
    }
}
