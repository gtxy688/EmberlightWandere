using System;
using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>
    /// 只读构筑并生成候选；升级结算归 RunProgress。
    /// 维护抽取顺序时须保留随机数消耗顺序，确保同种子牌局可复现。
    /// </summary>
    public static class EmberChoicePool
    {
        /// <summary>基础候选张数（不含空位补的"新武器"）。见 `requirements.md` §4.3。</summary>
        public const int BaseOfferCount = 3;

        /// <summary>整次选卡的质变入池门控概率。</summary>
        public const double MetamorphEntryChance = 0.005;

        /// <summary>幸运对专属进池权重的上限。</summary>
        const int SpecialWeightCap = 5;

        /// <summary>专属权重的幸运换算除数。</summary>
        const float SpecialWeightLuckDivisor = 40f;

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

        /// <summary>幸运影响专属权重；质变仍使用固定权重。</summary>
        public static int WeightForSpecial(float luck)
        {
            int w = 1 + (int)(luck / SpecialWeightLuckDivisor);
            if (w < 1) w = 1;
            if (w > SpecialWeightCap) w = SpecialWeightCap;
            return w;
        }

        static EmberOffer MakeGenericOffer(int id, Random random, float luck, int weaponId)
        {
            return new EmberOffer(id, EmberRarityUtil.RollByLuck(random, luck), weaponId);
        }

        /// <summary>同屏唯一键：`(词条Id, WeaponId)`——两张火球·攻击力必须被判为同一键。</summary>
        public static long OfferKey(int id, int weaponId)
        {
            return ((long)id << 32) | (uint)(weaponId + 1);
        }

        /// <summary>
        /// 生成一组候选：基础 3 张来自「通用 / 专属 / 质变」，再按空位数补上未拥有的武器卡。
        /// 候选总数 = `3 + 空位数`。
        /// </summary>
        public static EmberOffer[] Build(RunProgress progress, Random random)
        {
            var base3 = new List<EmberOffer>();
            var usedKeys = new HashSet<long>();

            var candIds = new List<int>();
            var candWid = new List<int>();
            var candW = new List<int>();

            // 每把已拥有武器各有攻击力/攻速；幸运、增幅、护盾是全局项（WeaponId = -1）
            for (int i = 0; i < EmberWeaponCatalog.RosterCount; i++)
            {
                int wid = EmberWeaponCatalog.RosterIdAt(i);
                if (!progress.OwnsWeapon(wid)) continue;
                candIds.Add(EmberWeaponCatalog.StatAtk); candWid.Add(wid); candW.Add(10);
                candIds.Add(EmberWeaponCatalog.StatAs); candWid.Add(wid); candW.Add(10);
            }
            candIds.Add(EmberWeaponCatalog.StatLuck); candWid.Add(-1); candW.Add(10);
            candIds.Add(EmberWeaponCatalog.StatAmp); candWid.Add(-1); candW.Add(10);
            candIds.Add(EmberWeaponCatalog.StatShield); candWid.Add(-1); candW.Add(10);

            int specialW = WeightForSpecial(progress.Luck);
            for (int id = EmberWeaponCatalog.ExclBasic; id <= EmberWeaponCatalog.ExclMeteor; id++)
            {
                if (!progress.CanExclusive(id)) continue;
                candIds.Add(id);
                candWid.Add(-1);
                candW.Add(specialW);
            }

            // 每次发牌只进行一次质变入池门控。
            if (random.NextDouble() < MetamorphEntryChance)
            {
                for (int id = EmberWeaponCatalog.MetaBasic; id <= EmberWeaponCatalog.MetaMeteor; id++)
                {
                    if (!progress.CanMetamorph(id)) continue;
                    candIds.Add(id);
                    candWid.Add(-1);
                    candW.Add(10);
                }
            }

            // 三个不同的全局项始终在池内，基础候选不会不足；抽中项立即移出。
            while (base3.Count < BaseOfferCount && candIds.Count > 0)
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
                int wid = candWid[pick];
                candIds.RemoveAt(pick);
                candWid.RemoveAt(pick);
                candW.RemoveAt(pick);
                if (!usedKeys.Add(OfferKey(id, wid))) continue;
                if (EmberRarityUtil.IsGenericId(id))
                    base3.Add(MakeGenericOffer(id, random, progress.Luck, wid));
                else if (id >= EmberWeaponCatalog.ExclBasic)
                    base3.Add(new EmberOffer(id, EmberRarity.Gold));       // 专属固定黄金
                else
                    base3.Add(new EmberOffer(id, EmberRarity.Diamond));    // 质变固定钻石
            }

            // 额外张：未拥有的武器 = 空位数（不占基础 3 张的额度）
            var result = new List<EmberOffer>(base3);
            int empty = progress.EmptySlots;
            if (empty > 0)
            {
                var news = new List<int>();
                for (int i = 0; i < EmberWeaponCatalog.RosterCount; i++)
                {
                    int id = EmberWeaponCatalog.RosterIdAt(i);
                    if (progress.OwnsWeapon(id)) continue;
                    news.Add(id);
                }
                Shuffle(news, random);
                for (int i = 0; i < news.Count && i < empty; i++)
                    result.Add(new EmberOffer(news[i], EmberRarity.Gold));
            }

            return result.ToArray();
        }

        /// <summary>候选张数公式：`3 + 空位数`。</summary>
        public static int ExpectedCount(RunProgress progress)
        {
            return BaseOfferCount + (progress != null ? progress.EmptySlots : 0);
        }
    }
}
