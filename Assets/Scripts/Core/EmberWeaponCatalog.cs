namespace Emberlight
{
    /// <summary>
    /// 词条 Id 空间与武器名册的**唯一出处**（`requirements.md` §4.2 / §4.7）。
    /// 归属判定、候选生成与武器 Tick 门控都从这里取 id。
    /// 本类**不含规则、不含状态、零 `UnityEngine` 引用**。
    /// </summary>
    public static class EmberWeaponCatalog
    {
        // 通用词条 0–4
        public const int StatAtk = 0;
        public const int StatAs = 1;
        public const int StatLuck = 2;
        public const int StatAmp = 3;
        public const int StatShield = 4;

        // 武器 10–19（占槽）
        public const int Pierce = 10;
        public const int Boom = 11;
        public const int Meteor = 12;
        public const int Basic = 13;
        public const int Orbit = 14;
        public const int Trail = 15;

        // 质变 20–29
        public const int MetaBasic = 20;
        public const int MetaOrbit = 21;
        public const int MetaTrail = 22;
        public const int MetaPierce = 23;
        public const int MetaBoom = 24;
        public const int MetaMeteor = 25;

        // 专属 30–39
        public const int ExclBasic = 30;
        public const int ExclOrbit = 31;
        public const int ExclTrail = 32;
        public const int ExclPierce = 33;
        public const int ExclBoom = 34;
        public const int ExclMeteor = 35;
        /// <summary>一把武器的身份与文案，避免多份平行数组错位。</summary>
        public sealed class Definition
        {
            public int Id { get; }
            public int MetamorphId { get; }
            public int ExclusiveId { get; }
            public string Name { get; }
            public string Description { get; }

            internal Definition(int id, int metamorphId, int exclusiveId, string name, string description)
            {
                Id = id;
                MetamorphId = metamorphId;
                ExclusiveId = exclusiveId;
                Name = name;
                Description = description;
            }
        }

        // 顺序也是开局界面的显示顺序；所有身份映射均由这张表生成。
        static readonly Definition[] definitions =
        {
            new Definition(Basic, MetaBasic, ExclBasic, "火球", "自动发射火球\n锁定最近的可见敌人"),
            new Definition(Orbit, MetaOrbit, ExclOrbit, "环火", "火球环绕身边\n持续灼烧接触到的敌人"),
            new Definition(Trail, MetaTrail, ExclTrail, "燃地", "沿途留下火焰\n持续伤害踏入火区的敌人"),
            new Definition(Pierce, MetaPierce, ExclPierce, "穿透火矢", "停下脚步发射火矢\n贯穿前方敌人，移动时停火"),
            new Definition(Boom, MetaBoom, ExclBoom, "回旋烬蝶", "掷出回旋火蝴蝶\n飞出与返回均可命中敌人"),
            new Definition(Meteor, MetaMeteor, ExclMeteor, "天降火雨", "标记目标区域\n短暂预警后轰击范围内敌人")
        };
        /// <summary>兼容数组调用方，但不暴露可修改的内部名册。</summary>
        public static int[] Roster
        {
            get
            {
                return RosterCopy();
            }
        }

        /// <summary>名册长度（= 武器总数）。</summary>
        public static int RosterCount
        {
            get
            {
                return definitions.Length;
            }
        }

        /// <summary>下标取武器 id；越界返回 0（不是合法武器 id）。</summary>
        public static int RosterIdAt(int index)
        {
            return index >= 0 && index < definitions.Length ? definitions[index].Id : 0;
        }

        /// <summary>是否为占槽武器（10–19 名册内）。</summary>
        public static bool IsRosterWeapon(int id)
        {
            return Find(id) != null;
        }

        /// <summary>质变 id → 对应武器 id；非质变返回 0。</summary>
        public static int WeaponForMeta(int metaId)
        {
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i].MetamorphId == metaId)
                    return definitions[i].Id;
            return 0;
        }

        /// <summary>专属 id → 对应武器 id；非专属返回 0。</summary>
        public static int WeaponForExcl(int exclId)
        {
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i].ExclusiveId == exclId)
                    return definitions[i].Id;
            return 0;
        }

        /// <summary>名册的一个副本（调用方可自由排序）。</summary>
        public static int[] RosterCopy()
        {
            var copy = new int[definitions.Length];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = definitions[i].Id;
            return copy;
        }

        public static Definition Find(int weaponId)
        {
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i].Id == weaponId)
                    return definitions[i];
            return null;
        }
    }
}
