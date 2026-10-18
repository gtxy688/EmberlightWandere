using NUnit.Framework;

namespace Emberlight.Tests
{
    public sealed class EmberOfferPresentationTests
    {
        [TestCase(EmberWeaponCatalog.StatAtk, "攻击加成 +10% → +40%")]
        [TestCase(EmberWeaponCatalog.StatAs, "攻速加成 +10% → +40%")]
        [TestCase(EmberWeaponCatalog.StatAmp, "伤害增幅 +5% → +20%")]
        public void GenericPreview_ShowsCurrentAndSelectedBonus(int id, string expected)
        {
            var progress = new RunProgress(new[] { EmberWeaponCatalog.Basic }, 2);
            progress.OfferChoices();
            Assert.That(progress.Choose(new EmberOffer(id, EmberRarity.Bronze, EmberWeaponCatalog.Basic)), Is.True);
            var presentation = EmberOfferPresentation.Build(new EmberOffer(id, EmberRarity.Gold, EmberWeaponCatalog.Basic),
                progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails);
            Assert.That(presentation.NumericPreview, Is.EqualTo(expected));
        }

        [Test]
        public void LuckPreview_ClampsTheSelectedResultToItsActualCap()
        {
            var progress = new RunProgress();
            for (int i = 0; i < 4; i++) { progress.OfferChoices(); progress.Choose(new EmberOffer(RunProgress.StatLuck, EmberRarity.Bronze)); }
            Assert.That(EmberOfferPresentation.Preview(RunProgress.StatLuck, EmberRarity.Gold, progress),
                Is.EqualTo("幸运 100 → 120 · 上限 120"));
        }

        [Test]
        public void AttackSpeedDescription_ExplainsOrbitAndTrailSpecificEffects()
        {
            var progress = new RunProgress();
            var orbit = EmberOfferPresentation.Build(new EmberOffer(RunProgress.StatAs, EmberRarity.Gold, EmberWeaponCatalog.Orbit),
                progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails);
            var trail = EmberOfferPresentation.Build(new EmberOffer(RunProgress.StatAs, EmberRarity.Gold, EmberWeaponCatalog.Trail),
                progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails);
            Assert.That(orbit.Description, Does.Contain("旋转速度"));
            Assert.That(trail.Description, Does.Contain("铺设间隔"));
            Assert.That(trail.Description, Does.Contain("不提高单块火焰"));
        }

        [Test]
        public void ExtendedTrailPreview_UsesTheOwnedWeaponsCurrentAttackSpeed()
        {
            var progress = new RunProgress(new[] { EmberWeaponCatalog.Trail }, 2);
            progress.OfferChoices();
            Assert.That(progress.Choose(new EmberOffer(RunProgress.StatAs, EmberRarity.Diamond, EmberWeaponCatalog.Trail)), Is.True);
            Assert.That(EmberOfferPresentation.Preview(RunProgress.ExclTrail, EmberRarity.Gold, progress),
                Is.EqualTo("铺火间隔 0.3 → 0.19 秒"));
        }

        [TestCase(EmberWeaponCatalog.Basic, "自动发射火球")]
        [TestCase(EmberWeaponCatalog.Orbit, "火球环绕身边")]
        [TestCase(EmberWeaponCatalog.Trail, "沿途留下火焰")]
        [TestCase(EmberWeaponCatalog.Pierce, "停下脚步发射火矢")]
        [TestCase(EmberWeaponCatalog.Boom, "掷出回旋火蝴蝶")]
        [TestCase(EmberWeaponCatalog.Meteor, "标记目标区域")]
        public void WeaponDescription_MatchesItsId(int id, string description)
        {
            Assert.That(EmberCopy.UpgradeDetails[id], Does.StartWith(description));
        }

        [TestCase(EmberWeaponCatalog.ExclBasic)]
        [TestCase(EmberWeaponCatalog.ExclOrbit)]
        [TestCase(EmberWeaponCatalog.ExclPierce)]
        public void ExclusivePreview_ShowsOneAdditionalUnit(int id)
        {
            Assert.That(EmberUpgradePanel.Preview(id, EmberRarity.Gold, new RunProgress()), Does.Contain("+1"));
        }

        [Test]
        public void WildfirePreview_DistinguishesSizeFromDamage()
        {
            Assert.That(EmberUpgradePanel.Preview(EmberWeaponCatalog.MetaBasic, EmberRarity.Gold, new RunProgress()), Does.Contain("1.25"));
        }

        [Test]
        public void CoronaPreview_MatchesActualBaseDps()
        {
            Assert.That(EmberUpgradePanel.Preview(EmberWeaponCatalog.MetaOrbit, EmberRarity.Gold, new RunProgress()),
                Does.Contain("基础 75 伤害/秒"));
        }
    }
}
