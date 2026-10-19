using System;
using System.Linq;
using NUnit.Framework;

namespace Emberlight.Tests
{
    /// <summary>
    /// 候选数量、唯一性、归属门控与随机序列，见 requirements.md §4.3。
    /// </summary>
    [TestFixture]
    public sealed class EmberChoicePoolTests
    {
        static RunProgress NewRun(int slots = 2, int weapon = EmberWeaponCatalog.Basic)
        {
            var p = new RunProgress();
            p.ConfigureRun(new[] { weapon }, slots);
            return p;
        }

        static EmberOffer[] Build(RunProgress p, int seed)
        {
            return EmberChoicePool.Build(p, new Random(seed));
        }

        // ── 数量──────────────────────────────────────────────────────────────

        [Test]
        public void Build_ReturnsThreeOffersWhenSlotsAreFull()
        {
            // N=1 → 已拥有 1 把 → 无空位 → 只有基础 3 张
            var p = NewRun(slots: 1);
            var offers = Build(p, 1);
            Assert.That(offers, Has.Length.EqualTo(EmberChoicePool.BaseOfferCount));
        }

        [Test]
        public void Build_AddsOneExtraOfferPerEmptySlot()
        {
            // N=5 → 已拥有 1 把 → 4 个空位 → 3 + 4 = 7 张
            var p = NewRun(slots: 5);
            var offers = Build(p, 1);
            Assert.That(offers, Has.Length.EqualTo(EmberChoicePool.BaseOfferCount + 4));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void Build_OfferCountAlwaysMatchesThreePlusEmptySlots(int slots)
        {
            var p = NewRun(slots: slots);
            var offers = Build(p, 42);
            Assert.That(offers, Has.Length.EqualTo(EmberChoicePool.ExpectedCount(p)),
                $"N={slots} 时候选张数应等于 3 + 空位");
        }

        [Test]
        public void ExpectedCount_IsThreePlusEmptySlots()
        {
            Assert.That(EmberChoicePool.ExpectedCount(NewRun(slots: 1)), Is.EqualTo(3));
            Assert.That(EmberChoicePool.ExpectedCount(NewRun(slots: 3)), Is.EqualTo(5));
        }

        [Test]
        public void ExpectedCount_NullProgress_IsJustTheBaseCount()
        {
            Assert.That(EmberChoicePool.ExpectedCount(null), Is.EqualTo(EmberChoicePool.BaseOfferCount));
        }

        // ── 同屏唯一──────────────────────────────────────────────────────────

        [Test]
        public void Build_NeverRepeatsTheSameIdWeaponIdPair()
        {
            // 同屏唯一键是 (Id, WeaponId)。出现两张"火球·攻击力"就是回归。
            for (int seed = 0; seed < 60; seed++)
            {
                var p = NewRun(slots: 5);
                var offers = Build(p, seed);
                var keys = offers.Select(o => EmberChoicePool.OfferKey(o.Id, o.WeaponId)).ToList();
                Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Count),
                    $"种子 {seed} 的候选里出现了重复的 (Id, WeaponId)");
            }
        }

        [Test]
        public void OfferKey_EncodesBothIdAndWeaponId()
        {
            long a = EmberChoicePool.OfferKey(EmberWeaponCatalog.StatAtk, EmberWeaponCatalog.Basic);
            long b = EmberChoicePool.OfferKey(EmberWeaponCatalog.StatAtk, EmberWeaponCatalog.Orbit);
            long c = EmberChoicePool.OfferKey(EmberWeaponCatalog.StatAs, EmberWeaponCatalog.Basic);

            Assert.That(a, Is.Not.EqualTo(b), "同词条不同武器必须是不同的键");
            Assert.That(a, Is.Not.EqualTo(c), "同武器不同词条必须是不同的键");
        }

        [Test]
        public void OfferKey_GlobalEntriesUseMinusOneWeaponId()
        {
            long global = EmberChoicePool.OfferKey(EmberWeaponCatalog.StatAmp, -1);
            long weapon = EmberChoicePool.OfferKey(EmberWeaponCatalog.StatAmp, EmberWeaponCatalog.Basic);
            Assert.That(global, Is.Not.EqualTo(weapon));
        }

        [Test]
        public void Build_AllowsTheSameStatOnDifferentWeapons()
        {
            // 双武器玩家应能同时看到"火球·攻击力"和"环火·攻击力"——
            // 若按笼统"词条种类"去重就会误删其中一张
            var p = NewRun(slots: 2, weapon: EmberWeaponCatalog.Basic);
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.Orbit, EmberRarity.Gold));
            Assert.That(p.OwnedWeaponCount, Is.EqualTo(2), "前置条件：已拥有两把武器");

            var statAtk = Build(p, 3).Where(o => o.Id == EmberWeaponCatalog.StatAtk).ToList();
            if (statAtk.Count > 1)
            {
                Assert.That(statAtk.Select(o => o.WeaponId).Distinct().Count(), Is.EqualTo(statAtk.Count),
                    "同屏多张攻击力卡必须各自绑定不同武器");
            }
        }

        // ── 新武器仅在槽未满时出现────────────────────────────────────────────

        [Test]
        public void Build_ExcludesNewWeaponCardsWhenSlotsAreFull()
        {
            var p = NewRun(slots: 1); // 满槽
            for (int seed = 0; seed < 40; seed++)
            {
                foreach (var o in Build(p, seed))
                {
                    if (o.Id >= 10 && o.Id <= 19)
                        Assert.Fail($"满槽（N=1）时候选里出现了新武器卡 id={o.Id}（种子 {seed}）");
                }
            }
        }

        [Test]
        public void Build_WithEmptySlots_OffersUnownedWeapons()
        {
            var p = NewRun(slots: 2); // 1 个空位
            var found = false;
            for (int seed = 0; seed < 20 && !found; seed++)
            {
                foreach (var o in Build(p, seed))
                {
                    if (o.Id >= 10 && o.Id <= 19 && !p.OwnsWeapon(o.Id)) { found = true; break; }
                }
            }
            Assert.That(found, Is.True, "有空位时应能看到未拥有的武器卡");
        }

        [Test]
        public void Build_NewWeaponCardsAreGoldRarity()
        {
            var p = NewRun(slots: 5);
            foreach (var o in Build(p, 5))
            {
                if (o.Id >= 10 && o.Id <= 19)
                    Assert.That(o.Rarity, Is.EqualTo(EmberRarity.Gold), "武器卡固定黄金");
            }
        }

        // ── 专属与质变进池────────────────────────────────────────────────────────

        [Test]
        public void Build_ExcludesExclusivesAndMetamorphsForUnownedWeapons()
        {
            var p = NewRun(slots: 5, weapon: EmberWeaponCatalog.Basic); // 只拥有火球

            for (int seed = 0; seed < 60; seed++)
            {
                foreach (var o in Build(p, seed))
                {
                    if (o.Id >= 30 && o.Id <= 39)
                    {
                        Assert.That(EmberWeaponCatalog.WeaponForExcl(o.Id), Is.EqualTo(EmberWeaponCatalog.Basic),
                            $"未拥有的武器不该出现专属（id={o.Id}，种子 {seed}）");
                    }
                    if (o.Id >= 20 && o.Id <= 29)
                    {
                        Assert.That(EmberWeaponCatalog.WeaponForMeta(o.Id), Is.EqualTo(EmberWeaponCatalog.Basic),
                            $"未拥有的武器不该出现质变（id={o.Id}，种子 {seed}）");
                    }
                }
            }
        }

        [Test]
        public void Build_ExclusivesAreGoldAndMetamorphsAreDiamond()
        {
            var p = NewRun(slots: 5, weapon: EmberWeaponCatalog.Basic);
            for (int seed = 0; seed < 60; seed++)
            {
                foreach (var o in Build(p, seed))
                {
                    if (o.Id >= 30 && o.Id <= 39)
                        Assert.That(o.Rarity, Is.EqualTo(EmberRarity.Gold), "专属固定黄金");
                    if (o.Id >= 20 && o.Id <= 29)
                        Assert.That(o.Rarity, Is.EqualTo(EmberRarity.Diamond), "质变固定钻石");
                }
            }
        }

        // ── 旧通用不得回流────────────────────────────────────────────────────────

        [Test]
        public void Build_NeverEmitsRetiredGenericIds()
        {
            // 旧通用 Id 5–9（拾取 / 移速 / 生命 / 冲击波 / 治疗）已退出卡池
            for (int seed = 0; seed < 60; seed++)
            {
                var p = NewRun(slots: 5);
                foreach (var o in Build(p, seed))
                    Assert.That(o.Id, Is.Not.InRange(5, 9), $"旧通用 id={o.Id} 回流了（种子 {seed}）");
            }
        }

        // ── 候选有效性────────────────────────────────────────────────────────────

        [Test]
        public void Build_AlwaysReturnsAPickableHandEvenAtMaximumSlots()
        {
            for (int seed = 0; seed < 60; seed++)
            {
                var p = NewRun(slots: 5);
                var offers = Build(p, seed);
                Assert.That(offers, Is.Not.Empty, $"种子 {seed} 返回了空候选");
                Assert.That(offers.Length, Is.GreaterThanOrEqualTo(EmberChoicePool.BaseOfferCount));
            }
        }

        [Test]
        public void Build_EveryOfferIsAnAcceptedIdSpaceEntry()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var p = NewRun(slots: 5);
                foreach (var o in Build(p, seed))
                {
                    bool known =
                        EmberRarityUtil.IsGenericId(o.Id) ||
                        (o.Id >= 10 && o.Id <= 19) ||
                        (o.Id >= 20 && o.Id <= 29) ||
                        (o.Id >= 30 && o.Id <= 39);
                    Assert.That(known, Is.True, $"候选 id={o.Id} 不属于任何已定义的 Id 分区（种子 {seed}）");
                }
            }
        }

        [Test]
        public void Build_EveryGenericOfferTargetsAnOwnedWeaponOrIsGlobal()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var p = NewRun(slots: 5);
                foreach (var o in Build(p, seed))
                {
                    if (!EmberRarityUtil.IsGenericId(o.Id)) continue;
                    if (o.Id == EmberWeaponCatalog.StatLuck ||
                        o.Id == EmberWeaponCatalog.StatAmp ||
                        o.Id == EmberWeaponCatalog.StatShield)
                    {
                        Assert.That(o.WeaponId, Is.EqualTo(-1), $"{o.Id} 是全局词条，WeaponId 应为 -1");
                        continue;
                    }
                    // 攻击力 / 攻速必须绑定一把已拥有的武器
                    Assert.That(p.OwnsWeapon(o.WeaponId), Is.True,
                        $"{o.Id} 绑定了未拥有的武器 {o.WeaponId}（种子 {seed}）");
                }
            }
        }

        [Test]
        public void Build_IsReproducibleForTheSameSeed()
        {
            // 发牌必须可复现，否则出问题时无法重放牌局
            var a = Build(NewRun(slots: 3), 777);
            var b = Build(NewRun(slots: 3), 777);

            Assert.That(a.Length, Is.EqualTo(b.Length));
            for (int i = 0; i < a.Length; i++)
            {
                Assert.That(a[i].Id, Is.EqualTo(b[i].Id), $"第 {i} 张 id 不一致");
                Assert.That(a[i].Rarity, Is.EqualTo(b[i].Rarity), $"第 {i} 张品质不一致");
                Assert.That(a[i].WeaponId, Is.EqualTo(b[i].WeaponId), $"第 {i} 张武器绑定不一致");
            }
        }

        // ── 幸运对专属/质变进池权重 ──────────────────────────────────────────────────────

        [Test]
        public void WeightForSpecial_GrowsWithLuckAndIsCapped()
        {
            Assert.That(EmberChoicePool.WeightForSpecial(0f), Is.EqualTo(1));
            Assert.That(EmberChoicePool.WeightForSpecial(40f), Is.EqualTo(2));
            Assert.That(EmberChoicePool.WeightForSpecial(9999f), Is.EqualTo(5), "权重上限为 5");
        }

        [Test]
        public void WeightForSpecial_NeverBelowOne()
        {
            Assert.That(EmberChoicePool.WeightForSpecial(-100f), Is.EqualTo(1));
        }
    }
}
