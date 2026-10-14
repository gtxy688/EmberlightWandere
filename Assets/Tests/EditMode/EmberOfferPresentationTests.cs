using NUnit.Framework;

namespace Emberlight.Tests
{
    public sealed class EmberOfferPresentationTests
    {
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
            Assert.That(EmberUpgradePanel.Preview(EmberWeaponCatalog.MetaOrbit, EmberRarity.Gold, new RunProgress()), Does.Contain("75"));
        }
    }
}
