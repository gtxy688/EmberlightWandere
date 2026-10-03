using System;
using System.Linq;
using NUnit.Framework;

namespace Emberlight.Tests
{
    /// <summary>
    /// 开局构筑、属性落地、护盾吸收与计分（`requirements.md` §4.1 / §4.4 / §5）。
    ///
    /// 本模块零 `UnityEngine` 引用，可以直接 `new`——这也是它成为自动化首选的原因。
    /// </summary>
    [TestFixture]
    public sealed class RunProgressTests
    {
        static RunProgress NewRun(int slots = 2, int weapon = EmberWeaponCatalog.Basic)
        {
            var p = new RunProgress();
            p.ConfigureRun(new[] { weapon }, slots);
            return p;
        }

        // ── 开局（A1–A4）─────────────────────────────────────────────────────────────────

        [Test]
        public void ConfigureRun_WithASingleStarterWeapon_LeavesTheRestOfTheSlotsEmpty()
        {
            // v3 硬口径：开局恒为 1 把，禁止一次填满 N 槽。
            // 界面（EmberMenuFlow.OnRosterChosen）只会传 1 把，这条覆盖真实调用契约。
            var p = new RunProgress();
            p.ConfigureRun(new[] { EmberWeaponCatalog.Basic }, 5);

            Assert.That(p.OwnedWeaponCount, Is.EqualTo(1), "开局只能有 1 把武器");
            Assert.That(p.EmptySlots, Is.EqualTo(4), "其余槽位必须空着等选卡填");
        }

        [Test]
        public void ConfigureRun_WithSeveralInputWeapons_GrantsMoreThanOne()
        {
            // 记录真实行为：ConfigureRun 用 `granted < slots` 当上限，因此传入 k 把就会给 k 把
            // （k ≤ slots）。当前 UI 流程走不到（只传 1 把），但**它并不是"只给第一把"的守卫**。
            // 一旦有人从别处传多把进来，就会绕过"开局恒为 1 把"的口径。
            // 属待裁定事项，见 02-run-progression.md。
            var p = new RunProgress();
            p.ConfigureRun(new[] { EmberWeaponCatalog.Basic, EmberWeaponCatalog.Orbit }, 3);

            Assert.That(p.OwnedWeaponCount, Is.EqualTo(2),
                "若此断言失败，说明 ConfigureRun 已加上'只取第一把'的守卫——那是更好的行为，请同步更新本测试与文档");
        }

        [Test]
        public void ConfigureRun_EmptySlotsEqualsNMinusOne()
        {
            Assert.That(NewRun(slots: 1).EmptySlots, Is.EqualTo(0));
            Assert.That(NewRun(slots: 2).EmptySlots, Is.EqualTo(1));
            Assert.That(NewRun(slots: 5).EmptySlots, Is.EqualTo(4));
        }

        [TestCase(0, 1)]
        [TestCase(-3, 1)]
        [TestCase(6, 5)]
        [TestCase(99, 5)]
        public void ConfigureRun_ClampsSlotsToRange(int requested, int expected)
        {
            var p = new RunProgress();
            p.ConfigureRun(new[] { EmberWeaponCatalog.Basic }, requested);
            Assert.That(p.WeaponSlots, Is.EqualTo(expected));
        }

        [Test]
        public void ConfigureRun_NullOrEmptyRoster_FallsBackToFireball()
        {
            var a = new RunProgress();
            a.ConfigureRun(null, 2);
            Assert.That(a.OwnsWeapon(EmberWeaponCatalog.Basic), Is.True);
            Assert.That(a.CoreWeaponId, Is.EqualTo(EmberWeaponCatalog.Basic));

            var b = new RunProgress();
            b.ConfigureRun(new int[0], 2);
            Assert.That(b.OwnsWeapon(EmberWeaponCatalog.Basic), Is.True);
        }

        [Test]
        public void ConfigureRun_NonRosterId_FallsBackToFireball()
        {
            var p = new RunProgress();
            p.ConfigureRun(new[] { 999 }, 2);
            Assert.That(p.OwnedWeaponCount, Is.EqualTo(1));
            Assert.That(p.OwnsWeapon(EmberWeaponCatalog.Basic), Is.True);
        }

        [Test]
        public void ConfigureRun_ResetsAllStateBetweenRuns()
        {
            var p = NewRun(slots: 3);
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatLuck, EmberRarity.Diamond));
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatAmp, EmberRarity.Diamond));
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatShield, EmberRarity.Gold));
            p.AddScore(50);
            Assert.That(p.Luck, Is.GreaterThan(0f), "前置条件：本局已积累属性");

            p.ConfigureRun(new[] { EmberWeaponCatalog.Basic }, 3);

            Assert.That(p.Luck, Is.EqualTo(0f), "新局幸运必须归零");
            Assert.That(p.DamageAmp, Is.EqualTo(0f), "新局增幅必须归零");
            Assert.That(p.Shield, Is.EqualTo(0f), "新局护盾必须归零");
            Assert.That(p.ShieldCapacity, Is.EqualTo(0f));
            Assert.That(p.Score, Is.EqualTo(0), "新局分数必须归零");
            Assert.That(p.RefreshesRemaining, Is.EqualTo(RunProgress.FreeRefreshes));
            Assert.That(p.OwnedWeaponCount, Is.EqualTo(1), "新局不得继承上一局武器");
        }

        [Test]
        public void ConfigureRun_OrbitAndTrailGetTheirBaselineForm()
        {
            // 环火没基线转速就是 0（看不见），燃地没基线强度就踩不出伤害
            var orbit = NewRun(weapon: EmberWeaponCatalog.Orbit);
            Assert.That(orbit.Orbits, Is.GreaterThanOrEqualTo(1), "环火开局必须至少 1 个环绕体");

            var trail = NewRun(weapon: EmberWeaponCatalog.Trail);
            Assert.That(trail.TrailPower, Is.GreaterThanOrEqualTo(0.20f), "燃地开局必须有可用的基础拖尾强度");
        }

        [Test]
        public void ConfigureRun_CoreWeaponIdIsTheStarterWeapon()
        {
            var p = NewRun(weapon: EmberWeaponCatalog.Meteor);
            Assert.That(p.CoreWeaponId, Is.EqualTo(EmberWeaponCatalog.Meteor));
        }

        // ── 通用词条落地（A16–A18）────────────────────────────────────────────────────────

        [Test]
        public void Choose_AttackAppliesOnlyToTheTargetWeapon()
        {
            // 攻击力是"单武器"作用域——改回全局会让早期伤害膨胀（硬约束 2）
            var p = NewRun(slots: 2, weapon: EmberWeaponCatalog.Basic);
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.Orbit, EmberRarity.Gold)); // 拿到环火

            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatAtk, EmberRarity.Diamond, EmberWeaponCatalog.Basic));

            Assert.That(p.WeaponMagnitude(EmberWeaponCatalog.Basic), Is.GreaterThan(0f));
            Assert.That(p.WeaponMagnitude(EmberWeaponCatalog.Orbit), Is.EqualTo(0f),
                "火球的攻击力不得外溢到环火");
        }

        [Test]
        public void Choose_AttackSpeedAppliesOnlyToTheTargetWeapon()
        {
            var p = NewRun(slots: 2, weapon: EmberWeaponCatalog.Basic);
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.Orbit, EmberRarity.Gold));

            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatAs, EmberRarity.Diamond, EmberWeaponCatalog.Basic));

            Assert.That(p.WeaponAttackSpeed(EmberWeaponCatalog.Basic), Is.GreaterThan(0f));
            Assert.That(p.WeaponAttackSpeed(EmberWeaponCatalog.Orbit), Is.EqualTo(0f));
        }

        [Test]
        public void Choose_AmpIsGlobalAndFeedsDamageMul()
        {
            var p = NewRun();
            Assert.That(p.DamageMul, Is.EqualTo(1f).Within(1e-6f), "未拿增幅时倍率应为 1");

            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatAmp, EmberRarity.Gold));

            Assert.That(p.DamageAmp, Is.EqualTo(0.15f).Within(1e-6f));
            Assert.That(p.DamageMul, Is.EqualTo(1.15f).Within(1e-6f));
        }

        [Test]
        public void DamageMul_IgnoresTheRetiredDamageBonusField()
        {
            // DamageBonus 是已停用的字段，DamageMul 只能由扩幅决定
            var p = NewRun();
            Assert.That(p.DamageBonus, Is.EqualTo(0f));
            Assert.That(p.DamageMul, Is.EqualTo(1f + p.DamageAmp).Within(1e-6f));
        }

        [Test]
        public void Choose_LuckIsCappedAtMaxLuck()
        {
            var p = NewRun();
            for (int i = 0; i < 10; i++)
            {
                p.OfferChoices();
                p.Choose(new EmberOffer(EmberWeaponCatalog.StatLuck, EmberRarity.Diamond));
            }
            Assert.That(p.Luck, Is.EqualTo(RunProgress.MaxLuck));
        }

        [Test]
        public void Choose_ShieldStacksWithoutACap()
        {
            // 护盾可叠加且当前显式数值是 250/500/750/1250（硬约束表里"除以 2"的旧口径已作废）
            var p = NewRun();
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatShield, EmberRarity.Bronze));
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatShield, EmberRarity.Bronze));

            Assert.That(p.Shield, Is.EqualTo(500f).Within(1e-6f), "两次青铜护盾应叠加到 500");
            Assert.That(p.ShieldCapacity, Is.EqualTo(500f).Within(1e-6f));
        }

        [Test]
        public void Choose_ShieldAtHigherRarityIsNotClampedToTheBronzeAmount()
        {
            // 曾有一条把护盾硬钳到 250 的死代码路径；走 `Choose` 的玩家不该被它影响
            var p = NewRun();
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatShield, EmberRarity.Diamond));
            Assert.That(p.Shield, Is.EqualTo(1250f).Within(1e-6f), "钻石护盾不得被砍成青铜数值");
        }

        // ── 选择合法性（A19 / A21 / A26）──────────────────────────────────────────────────

        [Test]
        public void Choose_WithoutPendingOffer_IsRejected()
        {
            var p = NewRun();
            Assert.That(p.Pending, Is.EqualTo(0));
            Assert.That(p.Choose(new EmberOffer(EmberWeaponCatalog.StatLuck, EmberRarity.Gold)), Is.False,
                "没有待选状态时不得接受选卡");
        }

        [Test]
        public void Choose_NewWeaponReportsGrantedFlag()
        {
            var p = NewRun(slots: 2);
            p.OfferChoices();
            bool grantedNew;
            var ok = p.Choose(new EmberOffer(EmberWeaponCatalog.Orbit, EmberRarity.Gold), out grantedNew);

            Assert.That(ok, Is.True);
            Assert.That(grantedNew, Is.True, "第一次拿到的武器必须报告为 new，补偿选卡依赖这个标志");
            Assert.That(p.OwnsWeapon(EmberWeaponCatalog.Orbit), Is.True);
        }

        [Test]
        public void Choose_WhenSlotsAreFull_RejectsNewWeapon()
        {
            var p = NewRun(slots: 1); // 满槽
            p.OfferChoices();
            bool grantedNew;
            var ok = p.Choose(new EmberOffer(EmberWeaponCatalog.Orbit, EmberRarity.Gold), out grantedNew);

            Assert.That(ok, Is.False, "满槽时不得接受新武器");
            Assert.That(grantedNew, Is.False);
            Assert.That(p.OwnsWeapon(EmberWeaponCatalog.Orbit), Is.False);
        }

        [Test]
        public void Choose_ConsumesExactlyOnePending()
        {
            var p = NewRun();
            p.OfferChoices();
            Assert.That(p.Pending, Is.EqualTo(1));
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatLuck, EmberRarity.Bronze));
            Assert.That(p.Pending, Is.EqualTo(0));
        }

        [Test]
        public void Choose_NonRosterWeaponId_IsRejected()
        {
            var p = NewRun(slots: 3);
            p.OfferChoices();
            Assert.That(p.Choose(new EmberOffer(19, EmberRarity.Gold)), Is.False, "19 不是名册武器");
        }

        // ── 专属与质变（A20）────────────────────────────────────────────────────────────

        [Test]
        public void CanMetamorph_RequiresOwningTheWeaponButNoExclusive()
        {
            var p = NewRun(weapon: EmberWeaponCatalog.Basic);

            Assert.That(p.CanMetamorph(EmberWeaponCatalog.MetaBasic), Is.True,
                "质变不得要求先拿专属（v3 已取消前置）");
            Assert.That(p.CanMetamorph(EmberWeaponCatalog.MetaOrbit), Is.False,
                "未拥有环火时环火质变不得进池");
        }

        [Test]
        public void CanExclusive_RequiresOwningTheWeapon()
        {
            var p = NewRun(weapon: EmberWeaponCatalog.Basic);
            Assert.That(p.CanExclusive(EmberWeaponCatalog.ExclBasic), Is.True);
            Assert.That(p.CanExclusive(EmberWeaponCatalog.ExclMeteor), Is.False);
        }

        [Test]
        public void Choose_MetamorphMarksItTakenAndItCannotBeTakenTwice()
        {
            var p = NewRun(weapon: EmberWeaponCatalog.Basic);

            p.OfferChoices();
            Assert.That(p.Choose(new EmberOffer(EmberWeaponCatalog.MetaBasic, EmberRarity.Diamond)), Is.True);
            Assert.That(p.HasMetamorph(EmberWeaponCatalog.MetaBasic), Is.True);

            p.OfferChoices();
            Assert.That(p.Choose(new EmberOffer(EmberWeaponCatalog.MetaBasic, EmberRarity.Diamond)), Is.False,
                "同一质变不得重复领取");
        }

        [Test]
        public void Choose_ExclusiveMarksItTakenAndItCannotBeTakenTwice()
        {
            var p = NewRun(weapon: EmberWeaponCatalog.Basic);

            p.OfferChoices();
            Assert.That(p.Choose(new EmberOffer(EmberWeaponCatalog.ExclBasic, EmberRarity.Gold)), Is.True);
            Assert.That(p.HasExclusive(EmberWeaponCatalog.ExclBasic), Is.True);

            p.OfferChoices();
            Assert.That(p.Choose(new EmberOffer(EmberWeaponCatalog.ExclBasic, EmberRarity.Gold)), Is.False);
        }

        // ── 刷新（A21）──────────────────────────────────────────────────────────────────

        [Test]
        public void TryRefresh_ConsumesExactlyTwoPerRun()
        {
            var p = NewRun();
            var rng = new Random(11);

            for (int i = 0; i < RunProgress.FreeRefreshes; i++)
            {
                p.OfferChoices();
                EmberOffer[] offers;
                Assert.That(p.TryRefresh(rng, 1, out offers), Is.True, $"第 {i + 1} 次刷新应成功");
                Assert.That(offers, Is.Not.Null);
            }

            p.OfferChoices();
            EmberOffer[] extra;
            Assert.That(p.TryRefresh(rng, 1, out extra), Is.False, "第 3 次刷新必须失败");
        }

        [Test]
        public void TryRefresh_WithoutPendingOffer_DoesNotConsumeABudget()
        {
            var p = NewRun();
            var rng = new Random(11);
            EmberOffer[] offers;
            Assert.That(p.TryRefresh(rng, 1, out offers), Is.False, "没有待选状态时不该能刷新");
            Assert.That(p.RefreshesRemaining, Is.EqualTo(RunProgress.FreeRefreshes), "失败的刷新不得扣次数");
        }

        // ── 护盾吸收与计分（A22 / A23）──────────────────────────────────────────────────

        [Test]
        public void AbsorbDamage_ConsumesShieldBeforeHealthAndReturnsTheOverflow()
        {
            var p = NewRun();
            p.OfferChoices();
            p.Choose(new EmberOffer(EmberWeaponCatalog.StatShield, EmberRarity.Silver)); // 500
            Assert.That(p.Shield, Is.EqualTo(500f).Within(1e-6f));

            Assert.That(p.AbsorbDamage(200f), Is.EqualTo(0f).Within(1e-6f), "护盾够吃时不应有溢出");
            Assert.That(p.Shield, Is.EqualTo(300f).Within(1e-6f));

            Assert.That(p.AbsorbDamage(500f), Is.EqualTo(200f).Within(1e-6f), "溢出部分就是要扣的血量");
            Assert.That(p.Shield, Is.EqualTo(0f).Within(1e-6f), "护盾不得变成负数");
        }

        [Test]
        public void AbsorbDamage_WithNoShield_ReturnsFullDamage()
        {
            var p = NewRun();
            Assert.That(p.AbsorbDamage(37f), Is.EqualTo(37f).Within(1e-6f));
        }

        [Test]
        public void AbsorbDamage_TreatsNegativeDamageAsZero()
        {
            var p = NewRun();
            Assert.That(p.AbsorbDamage(-50f), Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void AddScore_RejectsNegativeAmounts()
        {
            var p = NewRun();
            Assert.Throws<ArgumentOutOfRangeException>(() => p.AddScore(-1));
        }

        [Test]
        public void AddScore_AccumulatesAndFinalScoreHasNoMultiplier()
        {
            var p = NewRun();
            p.AddScore(3);
            p.AddScore(30);
            Assert.That(p.Score, Is.EqualTo(33));
            Assert.That(p.FinalScore(), Is.EqualTo(33), "ScoreMult 恒为 1.0，最终分就是原始分");
            Assert.That(p.ScoreMult, Is.EqualTo(1.0f).Within(1e-6f));
        }

        // ── 候选数量公式 ──────────────────────────────────────────────────────────────────

        [Test]
        public void OfferCountExpected_IsThreePlusEmptySlots()
        {
            var p = NewRun(slots: 4); // 1 把已拥有 → 3 个空位
            Assert.That(p.OfferCountExpected(), Is.EqualTo(6));
        }

        [Test]
        public void FullRoster_ReturnsSixDistinctWeapons()
        {
            var roster = RunProgress.FullRoster();
            Assert.That(roster, Has.Length.EqualTo(6));
            Assert.That(roster.Distinct().Count(), Is.EqualTo(6));
        }
    }
}
