using System;
using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>
    /// 候选卡生成（M2，`requirements.md` §4.3）。纯规则，**零 `UnityEngine` 引用**。
    ///
    /// 从 `RunProgress.Choices()` 搬出来的原因：那 130 行是"候选怎么拼"，而 `RunProgress` 同时
    /// 又是"构筑状态怎么变"。混在一起时，想单独验证"满槽不出新武器""同屏唯一键是
    /// (Id, WeaponId)"这些规则，必须先把整套构筑状态摆好。
    ///
    /// 现在本类只从 <see cref="RunProgress"/> 读只读事实（拥有哪些武器、已拿哪些质变/专属、
    /// 空位剩几个），不写任何状态——状态的写入仍归 `RunProgress.Choose`。
    ///
    /// **随机数消耗顺序必须与搬移前逐字一致**：同一个 `System.Random` 种子要得到同一组候选，
    /// 否则 jack 复现一手牌局时会看到不同的牌。改动本类的抽卡顺序等于改动可复现性。
    /// </summary>
    public static class EmberChoicePool
    {
        /// <summary>基础候选张数（不含空位补的"新武器"）。见 `requirements.md` §4.3。</summary>
        public const int BaseOfferCount = 3;

        /// <summary>整次选卡把质变放入候选的概率（jack 口径：约 0.5%）。</summary>
        public const double MetamorphEntryChance = 0.005;

        /// <summary>幸运对专属/质变进池权重的上限。</summary>
        const int SpecialWeightCap = 5;

        /// <summary>专属/质变进池权重的除数：`1 + Luck / 40`。</summary>
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

        /// <summary>幸运越高，专属/质变进池权重越高（通用词条基准权重为 10）。</summary>
        public static int WeightForSpecial(float luck)
        {
            int w = 1 + (int)(luck / SpecialWeightLuckDivisor); // jack: diamond felt too common
            if (w < 1) w = 1;
            if (w > SpecialWeightCap) w = SpecialWeightCap;
            return w;
        }

        static EmberOffer MakeGenericOffer(int id, Random random, float luck, int weaponId = -1)
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

            // 质变进池：整次选卡仅约 0.5% 概率放入候选（jack）
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

            // 按权重抽，直到凑满基础 3 张或池子抽干；抽走的候选项立即移出（不重复进池）
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

            // 池子凑不满 3 张时补位：先补已有武器的攻击力/攻速，再补全局项
            if (base3.Count < BaseOfferCount)
            {
                var pad = new List<int>();
                var padW = new List<int>();
                for (int i = 0; i < EmberWeaponCatalog.RosterCount; i++)
                {
                    int wid = EmberWeaponCatalog.RosterIdAt(i);
                    if (!progress.OwnsWeapon(wid)) continue;
                    if (!usedKeys.Contains(OfferKey(EmberWeaponCatalog.StatAtk, wid))) { pad.Add(EmberWeaponCatalog.StatAtk); padW.Add(wid); }
                    if (!usedKeys.Contains(OfferKey(EmberWeaponCatalog.StatAs, wid))) { pad.Add(EmberWeaponCatalog.StatAs); padW.Add(wid); }
                }
                int[] globals = { EmberWeaponCatalog.StatLuck, EmberWeaponCatalog.StatAmp, EmberWeaponCatalog.StatShield };
                for (int i = 0; i < globals.Length; i++)
                    if (!usedKeys.Contains(OfferKey(globals[i], -1))) { pad.Add(globals[i]); padW.Add(-1); }

                var order = new List<int>();
                for (int i = 0; i < pad.Count; i++) order.Add(i);
                Shuffle(order, random);
                for (int oi = 0; oi < order.Count && base3.Count < BaseOfferCount; oi++)
                {
                    int i = order[oi];
                    if (!usedKeys.Add(OfferKey(pad[i], padW[i]))) continue;
                    base3.Add(MakeGenericOffer(pad[i], random, progress.Luck, padW[i]));
                }
            }

            // 最后兜底：只补"已有武器·攻击力"，保证玩家永远有牌可选
            while (base3.Count < BaseOfferCount)
            {
                int wid = progress.CoreWeaponId;
                if (!progress.OwnsWeapon(wid))
                {
                    for (int i = 0; i < EmberWeaponCatalog.RosterCount; i++)
                    {
                        int r = EmberWeaponCatalog.RosterIdAt(i);
                        if (progress.OwnsWeapon(r)) { wid = r; break; }
                    }
                }
                usedKeys.Add(OfferKey(EmberWeaponCatalog.StatAtk, wid));
                base3.Add(MakeGenericOffer(EmberWeaponCatalog.StatAtk, random, progress.Luck, wid));
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
