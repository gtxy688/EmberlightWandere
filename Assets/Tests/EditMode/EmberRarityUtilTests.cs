using System;
using System.Linq;
using NUnit.Framework;

namespace Emberlight.Tests
{
    /// <summary>
    /// 幸运权重与通用升级增量，见 requirements.md §4.4 / §4.5。
    /// </summary>
    [TestFixture]
    public sealed class EmberRarityUtilTests
    {
        // ── 品质权重表──────────────────────────────────────────────────────────────

        [Test]
        public void WeightsForLuck_AtZero_MatchesAuthoritativeTable()
        {
            Assert.That(EmberRarityUtil.WeightsForLuck(0f), Is.EqualTo(new[] { 72, 22, 5, 1 }));
        }

        [Test]
        public void WeightsForLuck_AtSixty_MatchesAuthoritativeTable()
        {
            Assert.That(EmberRarityUtil.WeightsForLuck(60f), Is.EqualTo(new[] { 58, 28, 11, 3 }));
        }

        [Test]
        public void WeightsForLuck_AtOneTwenty_MatchesAuthoritativeTable()
        {
            Assert.That(EmberRarityUtil.WeightsForLuck(120f), Is.EqualTo(new[] { 45, 32, 18, 5 }));
        }

        [Test]
        public void WeightsForLuck_SumToExactlyHundredAtTheDocumentedAnchorPoints()
        {
            // requirements.md §4.5 只给了 0 / 60 / 120 三个锚点，这三处权重和必须正好是 100。
            foreach (var luck in new[] { 0f, 60f, 120f })
                Assert.That(EmberRarityUtil.WeightsForLuck(luck).Sum(), Is.EqualTo(100),
                    $"锚点幸运 {luck} 的权重和必须是 100");
        }

        [Test]
        public void WeightsForLuck_IntermediateValuesDriftFromHundredByAtMostTwo()
        {
            // 各档独立取整，权重和可偏离 100；RollWeighted 使用实际权重和作分母。
            // 此处限制取整偏差，不改变既有抽取分布。
            int worst = 0;
            float worstAt = -1f;
            for (float luck = 0f; luck <= 120f; luck += 1f)
            {
                int sum = EmberRarityUtil.WeightsForLuck(luck).Sum();
                int drift = Math.Abs(sum - 100);
                if (drift > worst) { worst = drift; worstAt = luck; }
            }

            Assert.That(worst, Is.LessThanOrEqualTo(2),
                $"幸运 {worstAt} 处权重和漂移到 {100 + worst}，超出已知的 ±2");
        }

        [Test]
        public void WeightsForLuck_InterpolatesLinearlyBetweenAnchorPoints()
        {
            // 0→60 区间中点：青铜 72→58 的中点是 65
            var mid = EmberRarityUtil.WeightsForLuck(30f);
            Assert.That(mid[0], Is.EqualTo(65), "0–60 区间应在 72 与 58 之间线性插值");
        }

        [Test]
        public void WeightsForLuck_ClampsBelowZeroAndAboveMax()
        {
            // MaxLuck = 120，超出按边界处理
            Assert.That(EmberRarityUtil.WeightsForLuck(-500f), Is.EqualTo(EmberRarityUtil.WeightsForLuck(0f)));
            Assert.That(EmberRarityUtil.WeightsForLuck(9999f), Is.EqualTo(EmberRarityUtil.WeightsForLuck(120f)));
        }

        [Test]
        public void WeightsForLuck_NeverNegative()
        {
            for (float luck = 0f; luck <= 120f; luck += 5f)
                foreach (var w in EmberRarityUtil.WeightsForLuck(luck))
                    Assert.That(w, Is.GreaterThanOrEqualTo(0), $"幸运 {luck} 出现负权重");
        }

        [Test]
        public void HigherLuck_ShiftsWeightAwayFromBronze()
        {
            Assert.That(EmberRarityUtil.WeightsForLuck(120f)[0],
                Is.LessThan(EmberRarityUtil.WeightsForLuck(0f)[0]),
                "幸运更高时青铜权重应下降");
        }

        // ── RollByLuck ───────────────────────────────────────────────────────────────────

        [Test]
        public void RollByLuck_WithSeed_IsDeterministic()
        {
            var a = EmberRarityUtil.RollByLuck(new Random(1234), 60f);
            var b = EmberRarityUtil.RollByLuck(new Random(1234), 60f);
            Assert.That(a, Is.EqualTo(b), "同一随机种子必须得到同一品质，否则无法复现牌局");
        }

        [Test]
        public void RollByLuck_AlwaysReturnsADefinedRarity()
        {
            var rng = new Random(7);
            for (int i = 0; i < 500; i++)
            {
                var r = EmberRarityUtil.RollByLuck(rng, i % 121);
                Assert.That(Enum.IsDefined(typeof(EmberRarity), r), Is.True, $"抽到未定义品质 {r}");
            }
        }

        [Test]
        public void RollByLuck_AtZeroLuck_FavoursBronzeOverHighLuck()
        {
            // 用固定种子比较两档分布，避免把某个随机结果当作必然事件。
            int bronzeAtZero = CountBronze(0f, 4000, 2026);
            int bronzeAtMax = CountBronze(120f, 4000, 2026);

            Assert.That(bronzeAtZero, Is.GreaterThan(bronzeAtMax),
                $"幸运 0 的青铜占比（{bronzeAtZero}/4000）应高于幸运 120（{bronzeAtMax}/4000）");
        }

        static int CountBronze(float luck, int rolls, int seed)
        {
            var rng = new Random(seed);
            int bronze = 0;
            for (int i = 0; i < rolls; i++)
                if (EmberRarityUtil.RollByLuck(rng, luck) == EmberRarity.Bronze) bronze++;
            return bronze;
        }

        // ── 通用词条数值表──────────────────────────────────────────────────────────

        [Test]
        public void GenericAtkAs_MatchesRequirementTable()
        {
            Assert.That(EmberRarityUtil.GenericAtkAs(EmberRarity.Bronze), Is.EqualTo(0.10f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAtkAs(EmberRarity.Silver), Is.EqualTo(0.20f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAtkAs(EmberRarity.Gold), Is.EqualTo(0.30f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAtkAs(EmberRarity.Diamond), Is.EqualTo(0.40f).Within(1e-6f));
        }

        [Test]
        public void GenericAttackSpeed_UsesItsOwnDiamondStep()
        {
            // 钻石攻速与攻击力使用不同档位。
            Assert.That(EmberRarityUtil.GenericAttackSpeed(EmberRarity.Bronze), Is.EqualTo(0.10f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAttackSpeed(EmberRarity.Silver), Is.EqualTo(0.20f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAttackSpeed(EmberRarity.Gold), Is.EqualTo(0.30f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAttackSpeed(EmberRarity.Diamond), Is.EqualTo(0.50f).Within(1e-6f));

            Assert.That(EmberRarityUtil.GenericAttackSpeed(EmberRarity.Diamond),
                Is.Not.EqualTo(EmberRarityUtil.GenericAtkAs(EmberRarity.Diamond)),
                "攻速钻石档必须区别于攻击力钻石档（+50% vs +40%）");
        }

        [Test]
        public void GenericLuck_MatchesRequirementTable()
        {
            Assert.That(EmberRarityUtil.GenericLuck(EmberRarity.Bronze), Is.EqualTo(25));
            Assert.That(EmberRarityUtil.GenericLuck(EmberRarity.Silver), Is.EqualTo(50));
            Assert.That(EmberRarityUtil.GenericLuck(EmberRarity.Gold), Is.EqualTo(75));
            Assert.That(EmberRarityUtil.GenericLuck(EmberRarity.Diamond), Is.EqualTo(125));
        }

        [Test]
        public void GenericAmp_MatchesRequirementTable()
        {
            Assert.That(EmberRarityUtil.GenericAmp(EmberRarity.Bronze), Is.EqualTo(0.05f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAmp(EmberRarity.Silver), Is.EqualTo(0.10f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAmp(EmberRarity.Gold), Is.EqualTo(0.15f).Within(1e-6f));
            Assert.That(EmberRarityUtil.GenericAmp(EmberRarity.Diamond), Is.EqualTo(0.25f).Within(1e-6f));
        }

        [Test]
        public void ShieldAmount_MatchesRequirementTable()
        {
            Assert.That(EmberRarityUtil.ShieldAmount(EmberRarity.Bronze), Is.EqualTo(250f).Within(1e-6f));
            Assert.That(EmberRarityUtil.ShieldAmount(EmberRarity.Silver), Is.EqualTo(500f).Within(1e-6f));
            Assert.That(EmberRarityUtil.ShieldAmount(EmberRarity.Gold), Is.EqualTo(750f).Within(1e-6f));
            Assert.That(EmberRarityUtil.ShieldAmount(EmberRarity.Diamond), Is.EqualTo(1250f).Within(1e-6f));
        }

        // ── 分区与名称 ────────────────────────────────────────────────────────────────────

        [Test]
        public void KindOf_MapsEveryIdRange()
        {
            Assert.That(EmberRarityUtil.KindOf(0), Is.EqualTo(EmberOfferKind.Generic));
            Assert.That(EmberRarityUtil.KindOf(4), Is.EqualTo(EmberOfferKind.Generic));
            Assert.That(EmberRarityUtil.KindOf(10), Is.EqualTo(EmberOfferKind.Weapon));
            Assert.That(EmberRarityUtil.KindOf(19), Is.EqualTo(EmberOfferKind.Weapon));
            Assert.That(EmberRarityUtil.KindOf(20), Is.EqualTo(EmberOfferKind.Metamorph));
            Assert.That(EmberRarityUtil.KindOf(29), Is.EqualTo(EmberOfferKind.Metamorph));
            Assert.That(EmberRarityUtil.KindOf(30), Is.EqualTo(EmberOfferKind.Exclusive));
            Assert.That(EmberRarityUtil.KindOf(39), Is.EqualTo(EmberOfferKind.Exclusive));
        }

        [Test]
        public void IsGenericId_TrueOnlyForZeroThroughFour()
        {
            // 旧的 5–9 通用已退出卡池，不能回流
            for (int id = 0; id <= 4; id++)
                Assert.That(EmberRarityUtil.IsGenericId(id), Is.True, $"{id} 应是通用词条");
            for (int id = 5; id <= 9; id++)
                Assert.That(EmberRarityUtil.IsGenericId(id), Is.False, $"旧通用 {id} 不应再算通用词条");
            Assert.That(EmberRarityUtil.IsGenericId(13), Is.False);
        }

        [Test]
        public void AllNames_HasOneEntryPerRarityAndTheyAreDistinct()
        {
            Assert.That(EmberRarityUtil.AllNames, Has.Length.EqualTo(4));
            Assert.That(EmberRarityUtil.AllNames.Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void Name_IsIndexedByRarityEnum()
        {
            Assert.That(EmberRarityUtil.Name(EmberRarity.Bronze), Is.EqualTo(EmberRarityUtil.AllNames[0]));
            Assert.That(EmberRarityUtil.Name(EmberRarity.Silver), Is.EqualTo(EmberRarityUtil.AllNames[1]));
            Assert.That(EmberRarityUtil.Name(EmberRarity.Gold), Is.EqualTo(EmberRarityUtil.AllNames[2]));
            Assert.That(EmberRarityUtil.Name(EmberRarity.Diamond), Is.EqualTo(EmberRarityUtil.AllNames[3]));
        }

        [Test]
        public void Name_OutOfRangeRarity_FallsBackToBronze()
        {
            Assert.That(EmberRarityUtil.Name((EmberRarity)99), Is.EqualTo(EmberRarityUtil.AllNames[0]));
        }

        // ── M2 纯度 ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Tint_ReturnsThreeRgbChannelsRatherThanUnityColor()
        {
            // 配色必须保持为 float[3]，这样本文件才能零 UnityEngine 引用、可脱离 Unity 单测
            foreach (EmberRarity r in Enum.GetValues(typeof(EmberRarity)))
            {
                var tint = EmberRarityUtil.Tint(r);
                Assert.That(tint, Is.Not.Null, $"{r} 没有配色");
                Assert.That(tint, Has.Length.EqualTo(3), $"{r} 的配色不是 3 个 RGB 通道");
                foreach (var c in tint)
                    Assert.That(c, Is.InRange(0f, 1f), $"{r} 的通道值 {c} 超出 0–1");
            }
        }

        [Test]
        public void Tint_IsDistinctPerRarity()
        {
            var bronze = EmberRarityUtil.Tint(EmberRarity.Bronze);
            var diamond = EmberRarityUtil.Tint(EmberRarity.Diamond);
            Assert.That(bronze, Is.Not.EqualTo(diamond), "不同稀有度应有可区分的配色");
        }
    }
}
