using System;
using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>
    /// Gods-Select-v3 run state: slots, owned weapons, generic stats, luck/amp, exclusives/metamorphs,
    /// free refreshes. 只负责"构筑状态怎么变"，不管"候选怎么拼"——后者见 <see cref="EmberChoicePool"/>。
    /// Id 空间见 <see cref="EmberWeaponCatalog"/>。本类**零 `UnityEngine` 引用**，可脱离 Unity 单测。
    /// </summary>
    public sealed class RunProgress
    {
        // ── 通用词条 Id（转发自 EmberWeaponCatalog，保持既有调用点可用）─────────────────────
        public const int StatAtk = EmberWeaponCatalog.StatAtk;
        public const int StatAs = EmberWeaponCatalog.StatAs;
        public const int StatLuck = EmberWeaponCatalog.StatLuck;
        public const int StatAmp = EmberWeaponCatalog.StatAmp;
        public const int StatShield = EmberWeaponCatalog.StatShield;

        // ── 武器 Id ────────────────────────────────────────────────────────────────────────
        public const int WeaponPierce = EmberWeaponCatalog.Pierce;
        public const int WeaponBoom = EmberWeaponCatalog.Boom;
        public const int WeaponMeteor = EmberWeaponCatalog.Meteor;
        public const int WeaponBasic = EmberWeaponCatalog.Basic;
        public const int WeaponOrbit = EmberWeaponCatalog.Orbit;
        public const int WeaponTrail = EmberWeaponCatalog.Trail;

        // ── 质变 Id ────────────────────────────────────────────────────────────────────────
        public const int MetaBasic = EmberWeaponCatalog.MetaBasic;
        public const int MetaOrbit = EmberWeaponCatalog.MetaOrbit;
        public const int MetaTrail = EmberWeaponCatalog.MetaTrail;
        public const int MetaPierce = EmberWeaponCatalog.MetaPierce;
        public const int MetaBoom = EmberWeaponCatalog.MetaBoom;
        public const int MetaMeteor = EmberWeaponCatalog.MetaMeteor;

        // ── 专属 Id ────────────────────────────────────────────────────────────────────────
        public const int ExclBasic = EmberWeaponCatalog.ExclBasic;
        public const int ExclOrbit = EmberWeaponCatalog.ExclOrbit;
        public const int ExclTrail = EmberWeaponCatalog.ExclTrail;
        public const int ExclPierce = EmberWeaponCatalog.ExclPierce;
        public const int ExclBoom = EmberWeaponCatalog.ExclBoom;
        public const int ExclMeteor = EmberWeaponCatalog.ExclMeteor;


        public const int MaxLuck = 120;
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
        public int OwnedWeaponCount { get { return ownedWeapons.Count; } }
        public int EmptySlots { get { int e = WeaponSlots - ownedWeapons.Count; return e < 0 ? 0 : e; } }
        public int RefreshesRemaining { get; private set; }

        /// <summary>Always 1.0 (no ScoreMult).</summary>
        public float ScoreMult { get { return 1.0f; } }

        /// <summary>Global amp only (Hotfix-Weapon-AtkAs-v1). Per-weapon ATK uses WeaponMagnitude.</summary>
        public float DamageMul { get { return 1f + DamageAmp; } }

        readonly HashSet<int> ownedWeapons = new HashSet<int>();
        readonly HashSet<int> metamorphs = new HashSet<int>();
        readonly HashSet<int> exclusives = new HashSet<int>();
        readonly Dictionary<int, int> weaponCount = new Dictionary<int, int>();
        readonly Dictionary<int, float> weaponMagnitude = new Dictionary<int, float>();
        readonly Dictionary<int, float> weaponAttackSpeed = new Dictionary<int, float>();

        /// <summary>候选项与 id 空间的唯一出处在 EmberWeaponCatalog / EmberChoicePool。</summary>

        public RunProgress()
        {
            ConfigureRun(new[] { WeaponBasic }, 2);
        }

        public bool OwnsWeapon(int id) { return ownedWeapons.Contains(id); }
        public bool HasEvolved(int id) { return metamorphs.Contains(id); }
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
            if (amount == 0f) return;
            float v;
            weaponMagnitude.TryGetValue(id, out v);
            weaponMagnitude[id] = v + amount;
        }

        void AddWeaponAttackSpeed(int id, float amount)
        {
            if (amount == 0f) return;
            float v;
            weaponAttackSpeed.TryGetValue(id, out v);
            weaponAttackSpeed[id] = v + amount;
        }

        public IReadOnlyCollection<int> OwnedWeaponsView { get { return ownedWeapons; } }

        /// <summary>武器名册副本（顺序即开局选武器界面的显示顺序）。转发自 <see cref="EmberWeaponCatalog"/>。</summary>
        public static int[] FullRoster() { return EmberWeaponCatalog.RosterCopy(); }

        /// <summary>Open-run: fill N slots from roster picks (at least 1). No skip fallback.</summary>
        public void ConfigureRun(int[] slotWeapons, int slots)
        {
            if (slots < 1) slots = 1;
            if (slots > 5) slots = 5;
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
                if (!IsRosterWeapon(id)) continue;
                if (ownedWeapons.Contains(id)) continue;
                GrantWeaponBaseline(id);
                if (granted == 0) CoreWeaponId = id;
                granted++;
            }
            if (ownedWeapons.Count == 0)
            {
                GrantWeaponBaseline(WeaponBasic);
                CoreWeaponId = WeaponBasic;
            }
        }

        /// <summary>Back-compat: single core + N empty slots.</summary>
        public void ConfigureRun(int coreWeaponId, int slots)
        {
            if (!IsRosterWeapon(coreWeaponId)) coreWeaponId = WeaponBasic;
            ConfigureRun(new[] { coreWeaponId }, slots);
        }

        /// <summary>是否为占槽武器（10–19 名册内）。判定放在 <see cref="EmberWeaponCatalog"/>。</summary>
        static bool IsRosterWeapon(int id) { return EmberWeaponCatalog.IsRosterWeapon(id); }

        void GrantWeaponBaseline(int id)
        {
            ownedWeapons.Add(id);
            if (id == WeaponOrbit && Orbits < 1) Orbits = 1;
            if (id == WeaponTrail && TrailPower < 0.20f) TrailPower = 0.20f;
            if (id == WeaponPierce)
            {
                int cur;
                weaponCount.TryGetValue(WeaponPierce, out cur);
                if (cur < 1) weaponCount[WeaponPierce] = 0; // fan starts at 1 bolt (1+0)
            }
        }

        public void AddScore(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Score += amount;
        }

        public int FinalScore() { return Score; }

        public void OfferChoices() { Pending = 1; }

        public bool SlotsFree() { return ownedWeapons.Count < WeaponSlots; }

        /// <summary>Metamorph: own weapon + not yet taken. No exclusive prerequisite.</summary>
        public bool CanMetamorph(int metaId)
        {
            if (metamorphs.Contains(metaId)) return false;
            int w = WeaponForMeta(metaId);
            return w > 0 && ownedWeapons.Contains(w);
        }

        public bool CanEvolve(int evoId) { return CanMetamorph(evoId); }

        public bool CanExclusive(int exclId)
        {
            if (exclusives.Contains(exclId)) return false;
            int w = WeaponForExcl(exclId);
            return w > 0 && ownedWeapons.Contains(w);
        }

        static int WeaponForMeta(int metaId) { return EmberWeaponCatalog.WeaponForMeta(metaId); }

        static int WeaponForExcl(int exclId) { return EmberWeaponCatalog.WeaponForExcl(exclId); }

        /// <summary>
        /// 生成候选：基础 3 张（通用/专属/质变，同屏唯一键 `(Id, WeaponId)`）+ 每个空位补 1 张未拥有武器。
        /// 具体规则与随机数消耗顺序见 <see cref="EmberChoicePool.Build"/>——
        /// **不要在这里重建一套抽卡逻辑**，否则 jack 复现同一手牌局会看到不同的牌。
        /// </summary>
        public EmberOffer[] Choices(Random random, int waveCleared)
        {
            return EmberChoicePool.Build(this, random);
        }

        /// <summary>Spend one free refresh; rebuild offer list. Returns false if none left.</summary>
        public bool TryRefresh(Random random, int waveCleared, out EmberOffer[] offers)
        {
            offers = null;
            if (RefreshesRemaining <= 0 || Pending <= 0) return false;
            RefreshesRemaining--;
            offers = Choices(random, waveCleared);
            return true;
        }

        /// <summary>Apply pick. Returns true on success. grantedNewWeapon set when a roster weapon was newly owned.</summary>
        public bool Choose(EmberOffer offer, out bool grantedNewWeapon)
        {
            grantedNewWeapon = false;
            if (Pending == 0 || offer.Id < 0) return false;

            // Generics 0-3
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
                        if (!ownedWeapons.Contains(wid)) return false;
                        AddWeaponMagnitude(wid, EmberRarityUtil.GenericAtkAs(offer.Rarity));
                        break;
                    }
                    case StatAs:
                    {
                        int wid = offer.WeaponId;
                        if (!IsRosterWeapon(wid) || !ownedWeapons.Contains(wid))
                            wid = CoreWeaponId;
                        if (!ownedWeapons.Contains(wid)) return false;
                        AddWeaponAttackSpeed(wid, EmberRarityUtil.GenericAttackSpeed(offer.Rarity));
                        break;
                    }
                    case StatLuck:
                        Luck += EmberRarityUtil.GenericLuck(offer.Rarity);
                        if (Luck > MaxLuck) Luck = MaxLuck;
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

            // New / owned roster weapons 10-15 (v3: picking owned mid-run is not offered; still safe-guard)
            if (offer.Id >= 10 && offer.Id <= 19)
            {
                if (!IsRosterWeapon(offer.Id)) return false;
                bool first = !ownedWeapons.Contains(offer.Id);
                if (first)
                {
                    if (!SlotsFree()) return false;
                    GrantWeaponBaseline(offer.Id);
                    grantedNewWeapon = true;
                }
                Pending--;
                return true;
            }

            // Metamorphs 20-29
            if (offer.Id >= 20 && offer.Id <= 29)
            {
                if (!CanMetamorph(offer.Id)) return false;
                metamorphs.Add(offer.Id);
                ApplyMetamorph(offer.Id);
                Pending--;
                return true;
            }

            // Exclusives 30-39
            if (offer.Id >= 30 && offer.Id <= 39)
            {
                if (!CanExclusive(offer.Id)) return false;
                exclusives.Add(offer.Id);
                ApplyExclusive(offer.Id);
                Pending--;
                return true;
            }

            return false;
        }

        /// <summary>
        /// ⚠️ **零调用点，且与现行需求冲突，不要调用它。**
        ///
        /// 它把护盾硬钳到 250——那是"护盾数值 /2"之前的旧口径残留。
        /// 现行需求（`requirements.md` §4.4）是 250 / 500 / 750 / 1250 且可叠加，
        /// 所以走这条路的钻石护盾会被砍成青铜护盾。
        ///
        /// 真正的护盾发放点在 <see cref="Choose"/> 的 `StatShield` 分支（直接加算、不限上限）。
        /// 保留本方法只是不想在这轮动公开 API；清理前先确认全仓无引用。
        /// </summary>
        public void AddShield(float amount)
        {
            if (amount <= 0) return;
            Shield += amount;
            if (Shield > ShieldCapacity) ShieldCapacity = Shield;
            if (Shield > 250f) { Shield = 250f; if (ShieldCapacity > 250f) ShieldCapacity = 250f; }
        }

        public float AbsorbDamage(float damage)
        {
            damage = System.Math.Max(0, damage);
            float absorbed = System.Math.Min(Shield, damage);
            Shield -= absorbed;
            return damage - absorbed;
        }

        public bool Choose(EmberOffer offer)
        {
            bool unused;
            return Choose(offer, out unused);
        }

        void ApplyExclusive(int id)
        {
            switch (id)
            {
                case ExclBasic: ExtraShots += 1; break; // Weapon-Balance-v1
                case ExclOrbit: Orbits += 1; break; // Weapon-Balance-v1
                case ExclTrail:
                    TrailExtend = true;
                    TrailPower += 0.25f;
                    break;
                case ExclPierce:
                    PierceLimit += 1; // Weapon-Balance-v1: +1 layer only
                    break;
                case ExclBoom:
                    BoomExtraLegHit = true;
                    break;
                case ExclMeteor:
                    MeteorExtra += 1;
                    break;
            }
        }

        void ApplyMetamorph(int id)
        {
            switch (id)
            {
                case MetaTrail:
                    TrailRing = true;
                    break;
                case MetaBoom:
                    BoomExtraTrips += 1;
                    break;
                case MetaMeteor:
                    // size/dmg handled in weapon via HasMetamorph
                    break;
                // MetaBasic / MetaOrbit / MetaPierce: weapon Tick gates on HasEvolved/HasMetamorph
            }
        }

        public float HealAmount(EmberRarity rarity)
        {
            return 20f + 10f * (int)rarity;
        }

        /// <summary>
        /// UI API：候选张数公式 `3 + 空位数`。
        /// 转发到 <see cref="EmberChoicePool.ExpectedCount"/>，避免公式出现第二个版本。
        /// </summary>
        public int OfferCountExpected()
        {
            return EmberChoicePool.ExpectedCount(this);
        }
    }
}
