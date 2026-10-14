using System;
using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>
    /// 单局构筑状态（M2）：槽位、已拥有武器、通用属性、幸运/增幅、专属/质变、免费刷新。
    /// 只负责"构筑状态怎么变"；候选怎么拼见 <see cref="EmberChoicePool"/>，Id 空间见
    /// <see cref="EmberWeaponCatalog"/>。**零 `UnityEngine` 引用**，可脱离 Unity 单测。
    /// </summary>
    public sealed class RunProgress
    {
        public const int StatAtk = EmberWeaponCatalog.StatAtk;
        public const int StatAs = EmberWeaponCatalog.StatAs;
        public const int StatLuck = EmberWeaponCatalog.StatLuck;
        public const int StatAmp = EmberWeaponCatalog.StatAmp;
        public const int StatShield = EmberWeaponCatalog.StatShield;

        public const int WeaponPierce = EmberWeaponCatalog.Pierce;
        public const int WeaponBoom = EmberWeaponCatalog.Boom;
        public const int WeaponMeteor = EmberWeaponCatalog.Meteor;
        public const int WeaponBasic = EmberWeaponCatalog.Basic;
        public const int WeaponOrbit = EmberWeaponCatalog.Orbit;
        public const int WeaponTrail = EmberWeaponCatalog.Trail;

        public const int MetaBasic = EmberWeaponCatalog.MetaBasic;
        public const int MetaOrbit = EmberWeaponCatalog.MetaOrbit;
        public const int MetaTrail = EmberWeaponCatalog.MetaTrail;
        public const int MetaPierce = EmberWeaponCatalog.MetaPierce;
        public const int MetaBoom = EmberWeaponCatalog.MetaBoom;
        public const int MetaMeteor = EmberWeaponCatalog.MetaMeteor;

        public const int ExclBasic = EmberWeaponCatalog.ExclBasic;
        public const int ExclOrbit = EmberWeaponCatalog.ExclOrbit;
        public const int ExclTrail = EmberWeaponCatalog.ExclTrail;
        public const int ExclPierce = EmberWeaponCatalog.ExclPierce;
        public const int ExclBoom = EmberWeaponCatalog.ExclBoom;
        public const int ExclMeteor = EmberWeaponCatalog.ExclMeteor;

        /// <summary>幸运上限（`requirements.md` §4.4）。</summary>
        public const int MaxLuck = 120;
        /// <summary>整局免费刷新次数（`requirements.md` §4.3）。</summary>
        public const int FreeRefreshes = 2;

        public int Score { get; private set; }
        public int Pending { get; private set; }
        public int ExtraShots { get; private set; }
        public float AttackSpeedBonus { get; private set; }
        public float Shield { get; private set; }
        public float ShieldCapacity { get; private set; }
        public float DamageBonus { get; private set; }
        public float DamageAmp { get; private set; }
        public float Luck { get; private set; }
        public int Orbits { get; private set; }
        public float TrailPower { get; private set; }
        public float MoveSpeedBonus { get; private set; }
        public float BonusMaxHealth { get; private set; }
        public float WavePower { get; private set; }
        public int PierceLimit { get; private set; }
        public int MeteorExtra { get; private set; }
        public int BoomExtraTrips { get; private set; }
        public bool BoomExtraLegHit { get; private set; }
        public bool TrailExtend { get; private set; }
        public bool TrailRing { get; private set; }
        public int WeaponSlots { get; private set; }
        public int CoreWeaponId { get; private set; }
        public int RefreshesRemaining { get; private set; }

        public int OwnedWeaponCount { get { return ownedWeapons.Count; } }

        public int EmptySlots
        {
            get
            {
                int e = WeaponSlots - ownedWeapons.Count;
                return e < 0 ? 0 : e;
            }
        }

        /// <summary>恒为 1.0；本项目不做随槽位数变化的分数倍率。</summary>
        public float ScoreMult { get { return 1.0f; } }

        /// <summary>全局伤害乘区（仅增幅词条）；单武器攻击力见 <see cref="WeaponMagnitude"/>。</summary>
        public float DamageMul { get { return 1f + DamageAmp; } }

        readonly HashSet<int> ownedWeapons = new HashSet<int>();
        readonly HashSet<int> metamorphs = new HashSet<int>();
        readonly HashSet<int> exclusives = new HashSet<int>();
        readonly Dictionary<int, int> weaponCount = new Dictionary<int, int>();
        readonly Dictionary<int, float> weaponMagnitude = new Dictionary<int, float>();
        readonly Dictionary<int, float> weaponAttackSpeed = new Dictionary<int, float>();

        public RunProgress()
        {
            ConfigureRun(new[] { WeaponBasic }, 2);
        }

        public bool OwnsWeapon(int id) { return ownedWeapons.Contains(id); }
        public bool HasMetamorph(int id) { return metamorphs.Contains(id); }
        public bool HasExclusive(int id) { return exclusives.Contains(id); }

        public int WeaponCount(int id)
        {
            int v;
            return weaponCount.TryGetValue(id, out v) ? v : 0;
        }

        public float WeaponMagnitude(int id)
        {
            float v;
            return weaponMagnitude.TryGetValue(id, out v) ? v : 0f;
        }

        public float WeaponAttackSpeed(int id)
        {
            float v;
            return weaponAttackSpeed.TryGetValue(id, out v) ? v : 0f;
        }

        void AddWeaponMagnitude(int id, float amount)
        {
            if (amount == 0f)
                return;
            float v;
            weaponMagnitude.TryGetValue(id, out v);
            weaponMagnitude[id] = v + amount;
        }

        void AddWeaponAttackSpeed(int id, float amount)
        {
            if (amount == 0f)
                return;
            float v;
            weaponAttackSpeed.TryGetValue(id, out v);
            weaponAttackSpeed[id] = v + amount;
        }

        /// <summary>武器名册副本（顺序即开局选武器界面的显示顺序）。</summary>
        public static int[] FullRoster()
        {
            return EmberWeaponCatalog.RosterCopy();
        }

        /// <summary>开一局：用名册内的武器填 <paramref name="slots"/> 个槽位，至少给 1 把。</summary>
        public void ConfigureRun(int[] slotWeapons, int slots)
        {
            if (slots < 1)
                slots = 1;
            if (slots > 5)
                slots = 5;
            WeaponSlots = slots;
            ownedWeapons.Clear();
            metamorphs.Clear();
            exclusives.Clear();
            weaponCount.Clear();
            weaponMagnitude.Clear();
            weaponAttackSpeed.Clear();
            ExtraShots = 0;
            AttackSpeedBonus = 0;
            DamageBonus = 0;
            Shield = ShieldCapacity = 0;
            DamageAmp = 0;
            Luck = 0;
            Orbits = 0;
            TrailPower = 0;
            MoveSpeedBonus = 0;
            BonusMaxHealth = 0;
            WavePower = 0;
            PierceLimit = 2;
            MeteorExtra = 0;
            BoomExtraTrips = 0;
            BoomExtraLegHit = false;
            TrailExtend = false;
            TrailRing = false;
            Score = 0;
            Pending = 0;
            RefreshesRemaining = FreeRefreshes;
            CoreWeaponId = WeaponBasic;
            if (slotWeapons == null || slotWeapons.Length == 0)
                slotWeapons = new[] { WeaponBasic };
            int granted = 0;
            for (int i = 0; i < slotWeapons.Length && granted < slots; i++)
            {
                int id = slotWeapons[i];
                if (!IsRosterWeapon(id))
                    continue;
                if (ownedWeapons.Contains(id))
                    continue;
                GrantWeaponBaseline(id);
                if (granted == 0)
                    CoreWeaponId = id;
                granted++;
            }

            if (ownedWeapons.Count == 0)
            {
                GrantWeaponBaseline(WeaponBasic);
                CoreWeaponId = WeaponBasic;
            }
        }

        /// <summary>兼容旧入口：单把起始武器 + N 个槽位。</summary>
        public void ConfigureRun(int coreWeaponId, int slots)
        {
            if (!IsRosterWeapon(coreWeaponId))
                coreWeaponId = WeaponBasic;
            ConfigureRun(new[] { coreWeaponId }, slots);
        }

        /// <summary>是否为占槽武器（10–19 名册内）。判定归 <see cref="EmberWeaponCatalog"/>。</summary>
        static bool IsRosterWeapon(int id)
        {
            return EmberWeaponCatalog.IsRosterWeapon(id);
        }

        void GrantWeaponBaseline(int id)
        {
            ownedWeapons.Add(id);
            if (id == WeaponOrbit && Orbits < 1)
                Orbits = 1;
            if (id == WeaponTrail && TrailPower < 0.20f)
                TrailPower = 0.20f;
            if (id == WeaponPierce)
            {
                int cur;
                weaponCount.TryGetValue(WeaponPierce, out cur);
                if (cur < 1)
                    weaponCount[WeaponPierce] = 0; // 起手 1 支箭（1+0）
            }
        }

        public void AddScore(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            Score += amount;
        }

        public int FinalScore() { return Score; }

        /// <summary>标记"有一张待选的卡"；候选生成后调用一次。</summary>
        public void OfferChoices() { Pending = 1; }

        public bool SlotsFree() { return ownedWeapons.Count < WeaponSlots; }

        /// <summary>质变可否进池：已拥有对应武器且本局未拿过；无专属前置。</summary>
        public bool CanMetamorph(int metaId)
        {
            if (metamorphs.Contains(metaId))
                return false;
            int w = WeaponForMeta(metaId);
            return w > 0 && ownedWeapons.Contains(w);
        }

        /// <summary>专属可否进池：已拥有对应武器且本局未拿过。</summary>
        public bool CanExclusive(int exclId)
        {
            if (exclusives.Contains(exclId))
                return false;
            int w = WeaponForExcl(exclId);
            return w > 0 && ownedWeapons.Contains(w);
        }

        static int WeaponForMeta(int metaId) { return EmberWeaponCatalog.WeaponForMeta(metaId); }
        static int WeaponForExcl(int exclId) { return EmberWeaponCatalog.WeaponForExcl(exclId); }

        /// <summary>
        /// 生成候选并进入待选状态。抽卡规则与随机数消耗顺序全在 <see cref="EmberChoicePool.Build"/>，
        /// **不要在这里重建第二套抽卡逻辑**，否则同一随机种子的牌局会变。
        /// </summary>
        public EmberOffer[] Choices(Random random, int waveCleared)
        {
            var offers = EmberChoicePool.Build(this, random);
            OfferChoices();
            return offers;
        }

        /// <summary>消耗一次免费刷新并重建候选；没有次数或当前无待选卡时返回 false。</summary>
        public bool TryRefresh(Random random, int waveCleared, out EmberOffer[] offers)
        {
            offers = null;
            if (RefreshesRemaining <= 0 || Pending <= 0)
                return false;
            RefreshesRemaining--;
            offers = Choices(random, waveCleared);
            return true;
        }

        /// <summary>
        /// 应用一次选择。<paramref name="grantedNewWeapon"/> 为 true 表示这次拿到了一把新武器
        /// （调用方据此补发一次选卡）。Id 分区见 `requirements.md` §4.7。
        /// </summary>
        public bool Choose(EmberOffer offer, out bool grantedNewWeapon)
        {
            grantedNewWeapon = false;
            if (Pending == 0 || offer.Id < 0)
                return false;

            if (EmberRarityUtil.IsGenericId(offer.Id))
            {
                switch (offer.Id)
                {
                    case StatShield:
                        float grant = EmberRarityUtil.ShieldAmount(offer.Rarity);
                        Shield += grant;
                        ShieldCapacity += grant;
                        break;
                    case StatAtk:
                    {
                        int wid = offer.WeaponId;
                        if (!IsRosterWeapon(wid) || !ownedWeapons.Contains(wid))
                            wid = CoreWeaponId;
                        if (!ownedWeapons.Contains(wid))
                            return false;
                        AddWeaponMagnitude(wid, EmberRarityUtil.GenericAtkAs(offer.Rarity));
                        break;
                    }

                    case StatAs:
                    {
                        int wid = offer.WeaponId;
                        if (!IsRosterWeapon(wid) || !ownedWeapons.Contains(wid))
                            wid = CoreWeaponId;
                        if (!ownedWeapons.Contains(wid))
                            return false;
                        AddWeaponAttackSpeed(wid, EmberRarityUtil.GenericAttackSpeed(offer.Rarity));
                        break;
                    }

                    case StatLuck:
                        Luck += EmberRarityUtil.GenericLuck(offer.Rarity);
                        if (Luck > MaxLuck)
                            Luck = MaxLuck;
                        break;
                    case StatAmp:
                        DamageAmp += EmberRarityUtil.GenericAmp(offer.Rarity);
                        break;
                    default:
                        return false;
                }

                Pending--;
                return true;
            }

            if (offer.Id >= 10 && offer.Id <= 19)
            {
                if (!IsRosterWeapon(offer.Id))
                    return false;
                bool first = !ownedWeapons.Contains(offer.Id);
                if (first)
                {
                    if (!SlotsFree())
                        return false;
                    GrantWeaponBaseline(offer.Id);
                    grantedNewWeapon = true;
                }

                Pending--;
                return true;
            }

            if (offer.Id >= 20 && offer.Id <= 29)
            {
                if (!CanMetamorph(offer.Id))
                    return false;
                metamorphs.Add(offer.Id);
                ApplyMetamorph(offer.Id);
                Pending--;
                return true;
            }

            if (offer.Id >= 30 && offer.Id <= 39)
            {
                if (!CanExclusive(offer.Id))
                    return false;
                exclusives.Add(offer.Id);
                ApplyExclusive(offer.Id);
                Pending--;
                return true;
            }

            return false;
        }

        public bool Choose(EmberOffer offer)
        {
            bool unused;
            return Choose(offer, out unused);
        }

        /// <summary>先扣护盾、再算溢出伤害；返回仍需扣掉的生命值。</summary>
        public float AbsorbDamage(float damage)
        {
            damage = System.Math.Max(0, damage);
            float absorbed = System.Math.Min(Shield, damage);
            Shield -= absorbed;
            return damage - absorbed;
        }

        void ApplyExclusive(int id)
        {
            switch (id)
            {
                case ExclBasic:
                    ExtraShots += EmberWeaponTuning.Basic.ExtraShots;
                    break;
                case ExclOrbit:
                    Orbits += EmberWeaponTuning.Orbit.ExtraOrbs;
                    break;
                case ExclTrail:
                    TrailExtend = true;
                    TrailPower += EmberWeaponTuning.Trail.ExtraPower;
                    break;
                case ExclPierce:
                    PierceLimit += EmberWeaponTuning.Pierce.ExtraTargets;
                    break;
                case ExclBoom:
                    BoomExtraLegHit = true;
                    break;
                case ExclMeteor:
                    MeteorExtra += EmberWeaponTuning.Meteor.ExtraImpacts;
                    break;
            }
        }

        void ApplyMetamorph(int id)
        {
            // MetaTrail / MetaBoom 需要额外状态；其余质变由武器 Tick 自查 HasMetamorph。
            switch (id)
            {
                case MetaTrail:
                    TrailRing = true;
                    break;
                case MetaBoom:
                    BoomExtraTrips += EmberWeaponTuning.Boom.ExtraTrips;
                    break;
            }
        }

        /// <summary>UI API：候选张数公式 `3 + 空位数`，转发到 <see cref="EmberChoicePool.ExpectedCount"/>。</summary>
        public int OfferCountExpected()
        {
            return EmberChoicePool.ExpectedCount(this);
        }
    }
}
