using System;
using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>Gods-Select-v3 run state: 4 generics + luck/amp, exclusives/metamorphs, slot roster, refreshes.</summary>
    public sealed class RunProgress
    {
        public const int StatAtk = 0;
        public const int StatAs = 1;
        public const int StatLuck = 2;
        public const int StatAmp = 3;
        public const int StatShield = 4;

        public const int WeaponPierce = 10;
        public const int WeaponBoom = 11;
        public const int WeaponMeteor = 12;
        public const int WeaponBasic = 13;
        public const int WeaponOrbit = 14;
        public const int WeaponTrail = 15;

        public const int MetaBasic = 20;
        public const int MetaOrbit = 21;
        public const int MetaTrail = 22;
        public const int MetaPierce = 23;
        public const int MetaBoom = 24;
        public const int MetaMeteor = 25;

        public const int ExclBasic = 30;
        public const int ExclOrbit = 31;
        public const int ExclTrail = 32;
        public const int ExclPierce = 33;
        public const int ExclBoom = 34;
        public const int ExclMeteor = 35;

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

        /// <summary>Combined damage multiplier: (1+ATK) * (1+AMP).</summary>
        public float DamageMul { get { return (1f + DamageBonus) * (1f + DamageAmp); } }

        readonly HashSet<int> ownedWeapons = new HashSet<int>();
        readonly HashSet<int> metamorphs = new HashSet<int>();
        readonly HashSet<int> exclusives = new HashSet<int>();
        readonly Dictionary<int, int> weaponCount = new Dictionary<int, int>();
        readonly Dictionary<int, float> weaponMagnitude = new Dictionary<int, float>();

        static readonly int[] RosterIds = { WeaponBasic, WeaponOrbit, WeaponTrail, WeaponPierce, WeaponBoom, WeaponMeteor };
        static readonly int[] GenericIds = { StatAtk, StatAs, StatLuck, StatAmp, StatShield };

        static readonly int[] MetaIds = { MetaBasic, MetaOrbit, MetaTrail, MetaPierce, MetaBoom, MetaMeteor };
        static readonly int[] ExclIds = { ExclBasic, ExclOrbit, ExclTrail, ExclPierce, ExclBoom, ExclMeteor };
        static readonly int[] MetaWeaponOf = { WeaponBasic, WeaponOrbit, WeaponTrail, WeaponPierce, WeaponBoom, WeaponMeteor };
        static readonly int[] ExclWeaponOf = { WeaponBasic, WeaponOrbit, WeaponTrail, WeaponPierce, WeaponBoom, WeaponMeteor };

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

        public IReadOnlyCollection<int> OwnedWeaponsView { get { return ownedWeapons; } }

        public static int[] FullRoster() { return (int[])RosterIds.Clone(); }

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

        static bool IsRosterWeapon(int id)
        {
            for (int i = 0; i < RosterIds.Length; i++)
                if (RosterIds[i] == id) return true;
            return false;
        }

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

        static int WeaponForMeta(int metaId)
        {
            for (int i = 0; i < MetaIds.Length; i++)
                if (MetaIds[i] == metaId) return MetaWeaponOf[i];
            return 0;
        }

        static int WeaponForExcl(int exclId)
        {
            for (int i = 0; i < ExclIds.Length; i++)
                if (ExclIds[i] == exclId) return ExclWeaponOf[i];
            return 0;
        }

        static void Shuffle(List<int> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        int WeightForSpecial(Random random)
        {
            // Higher luck → higher exclusive/metamorph entry weight (vs generic base 10).
            int w = 1 + (int)(Luck / 40f); // jack: diamond felt too common
            if (w < 1) w = 1;
            if (w > 5) w = 5;
            return w;
        }

        EmberOffer MakeGenericOffer(int id, Random random)
        {
            return new EmberOffer(id, EmberRarityUtil.RollByLuck(random, Luck));
        }

        /// <summary>Gods-Select-v3: base 3 (generic/excl/meta, unique kinds) + empty-slot new weapons.</summary>
        public EmberOffer[] Choices(Random random, int waveCleared)
        {
            var base3 = new List<EmberOffer>();
            var usedIds = new HashSet<int>();
            var usedGeneric = new HashSet<int>();

            // Candidate bag: generics + owned exclusives/metamorphs
            var candIds = new List<int>();
            var candW = new List<int>();
            for (int i = 0; i < GenericIds.Length; i++)
            {
                candIds.Add(GenericIds[i]);
                candW.Add(10);
            }
            int specialW = WeightForSpecial(random);
            for (int i = 0; i < ExclIds.Length; i++)
            {
                if (CanExclusive(ExclIds[i]))
                {
                    candIds.Add(ExclIds[i]);
                    candW.Add(specialW);
                }
            }
            // 质变进池：整次选卡仅 0.5% 概率放入候选（jack）
            if (random.NextDouble() < 0.005)
            {
                for (int i = 0; i < MetaIds.Length; i++)
                {
                    if (CanMetamorph(MetaIds[i]))
                    {
                        candIds.Add(MetaIds[i]);
                        candW.Add(10); // once gated in, compete like a generic
                    }
                }
            }

            while (base3.Count < 3 && candIds.Count > 0)
            {
                int total = 0;
                for (int i = 0; i < candW.Count; i++) total += candW[i];
                if (total <= 0) break;
                int roll = random.Next(total);
                int acc = 0;
                int pick = 0;
                for (int i = 0; i < candIds.Count; i++)
                {
                    acc += candW[i];
                    if (roll < acc) { pick = i; break; }
                }
                int id = candIds[pick];
                candIds.RemoveAt(pick);
                candW.RemoveAt(pick);
                if (!usedIds.Add(id)) continue;
                if (EmberRarityUtil.IsGenericId(id))
                {
                    if (!usedGeneric.Add(id)) continue;
                    base3.Add(MakeGenericOffer(id, random));
                }
                else if (id >= 30)
                    base3.Add(new EmberOffer(id, EmberRarity.Gold));
                else
                    base3.Add(new EmberOffer(id, EmberRarity.Diamond));
            }

            // Pad base 3 with unseen generics
            if (base3.Count < 3)
            {
                var pad = new List<int>();
                for (int i = 0; i < GenericIds.Length; i++)
                    if (!usedIds.Contains(GenericIds[i])) pad.Add(GenericIds[i]);
                Shuffle(pad, random);
                for (int i = 0; i < pad.Count && base3.Count < 3; i++)
                {
                    int id = pad[i];
                    if (!usedIds.Add(id)) continue;
                    usedGeneric.Add(id);
                    base3.Add(MakeGenericOffer(id, random));
                }
            }
            for (int g = 0; g < GenericIds.Length && base3.Count < 3; g++)
            {
                int id = GenericIds[g];
                if (usedIds.Contains(id)) continue;
                usedIds.Add(id);
                base3.Add(MakeGenericOffer(id, random));
            }
            while (base3.Count < 3)
                base3.Add(MakeGenericOffer(StatAtk, random));

            // Extra: unowned weapons = empty slots (do not consume base-3 kind budget)
            var result = new List<EmberOffer>(base3);
            int empty = EmptySlots;
            if (empty > 0)
            {
                var news = new List<int>();
                for (int i = 0; i < RosterIds.Length; i++)
                {
                    int id = RosterIds[i];
                    if (ownedWeapons.Contains(id)) continue;
                    news.Add(id);
                }
                Shuffle(news, random);
                for (int i = 0; i < news.Count && i < empty; i++)
                    result.Add(new EmberOffer(news[i], EmberRarity.Gold));
            }

            return result.ToArray();
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
                        DamageBonus += EmberRarityUtil.GenericAtkAs(offer.Rarity);
                        break;
                    case StatAs: AttackSpeedBonus += EmberRarityUtil.GenericAttackSpeed(offer.Rarity); break;
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

        /// <summary>Returns the part of an incoming hit that reaches health.</summary>
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

        /// <summary>UI API: offer count formula.</summary>
        public int OfferCountExpected()
        {
            return 3 + EmptySlots;
        }
    }
}
