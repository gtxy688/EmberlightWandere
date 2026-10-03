using System;
using System.Linq;
using NUnit.Framework;

namespace Emberlight.Tests
{
    /// <summary>
    /// 词条 Id 空间与武器名册（`requirements.md` §4.2 / §4.7）。
    ///
    /// 这些是纯查表，但一旦错位影响面极大：武器图标按名册下标取图、卡名按名册下标取文案、
    /// 专属/质变按映射找归属武器。所以逐个钉住。
    /// </summary>
    [TestFixture]
    public sealed class EmberWeaponCatalogTests
    {
        // ── Id 分区 ───────────────────────────────────────────────────────────────────────

        [Test]
        public void StatIds_MatchIdSpace()
        {
            Assert.That(EmberWeaponCatalog.StatAtk, Is.EqualTo(0));
            Assert.That(EmberWeaponCatalog.StatAs, Is.EqualTo(1));
            Assert.That(EmberWeaponCatalog.StatLuck, Is.EqualTo(2));
            Assert.That(EmberWeaponCatalog.StatAmp, Is.EqualTo(3));
            Assert.That(EmberWeaponCatalog.StatShield, Is.EqualTo(4));
        }

        [Test]
        public void WeaponIds_AreInTenToNineteenRange()
        {
            Assert.That(EmberWeaponCatalog.Pierce, Is.EqualTo(10));
            Assert.That(EmberWeaponCatalog.Boom, Is.EqualTo(11));
            Assert.That(EmberWeaponCatalog.Meteor, Is.EqualTo(12));
            Assert.That(EmberWeaponCatalog.Basic, Is.EqualTo(13));
            Assert.That(EmberWeaponCatalog.Orbit, Is.EqualTo(14));
            Assert.That(EmberWeaponCatalog.Trail, Is.EqualTo(15));
        }

        [Test]
        public void MetamorphIds_AreInTwentyToTwentyNineRange()
        {
            foreach (var id in new[]
            {
                EmberWeaponCatalog.MetaBasic, EmberWeaponCatalog.MetaOrbit, EmberWeaponCatalog.MetaTrail,
                EmberWeaponCatalog.MetaPierce, EmberWeaponCatalog.MetaBoom, EmberWeaponCatalog.MetaMeteor
            })
            {
                Assert.That(id, Is.InRange(20, 29), $"质变 id {id} 不在 20–29");
            }
        }

        [Test]
        public void ExclusiveIds_AreInThirtyToThirtyNineRange()
        {
            foreach (var id in new[]
            {
                EmberWeaponCatalog.ExclBasic, EmberWeaponCatalog.ExclOrbit, EmberWeaponCatalog.ExclTrail,
                EmberWeaponCatalog.ExclPierce, EmberWeaponCatalog.ExclBoom, EmberWeaponCatalog.ExclMeteor
            })
            {
                Assert.That(id, Is.InRange(30, 39), $"专属 id {id} 不在 30–39");
            }
        }

        // ── 名册 ──────────────────────────────────────────────────────────────────────────

        [Test]
        public void Roster_HoldsExactlyTheSixSlotWeapons()
        {
            Assert.That(EmberWeaponCatalog.Roster, Has.Length.EqualTo(6));
            Assert.That(EmberWeaponCatalog.Roster, Is.EquivalentTo(new[]
            {
                EmberWeaponCatalog.Basic, EmberWeaponCatalog.Orbit, EmberWeaponCatalog.Trail,
                EmberWeaponCatalog.Pierce, EmberWeaponCatalog.Boom, EmberWeaponCatalog.Meteor
            }));
        }

        [Test]
        public void Roster_StartsWithFireballAtSlotZero()
        {
            // 开局界面的第一个卡位、EmberCardIcons 的格 0、RosterNames[0] 都依赖这个顺序
            Assert.That(EmberWeaponCatalog.RosterIdAt(0), Is.EqualTo(EmberWeaponCatalog.Basic));
        }

        [Test]
        public void Roster_ContainsNoDuplicates()
        {
            Assert.That(EmberWeaponCatalog.Roster.Distinct().Count(),
                Is.EqualTo(EmberWeaponCatalog.Roster.Length));
        }

        [TestCase(-1)]
        [TestCase(6)]
        [TestCase(99)]
        public void RosterIdAt_OutOfRange_ReturnsZero(int index)
        {
            Assert.That(EmberWeaponCatalog.RosterIdAt(index), Is.EqualTo(0));
        }

        [Test]
        public void RosterCopy_IsIndependentOfTheSource()
        {
            var copy = EmberWeaponCatalog.RosterCopy();
            copy[0] = 9999;
            Assert.That(EmberWeaponCatalog.RosterIdAt(0), Is.EqualTo(EmberWeaponCatalog.Basic),
                "RosterCopy 必须返回副本，否则调用方能改坏全局名册");
        }

        // ── 归属判定 ──────────────────────────────────────────────────────────────────────

        [Test]
        public void IsRosterWeapon_TrueOnlyForTheSixWeapons()
        {
            foreach (var id in EmberWeaponCatalog.Roster)
                Assert.That(EmberWeaponCatalog.IsRosterWeapon(id), Is.True, $"{id} 应在名册内");

            foreach (var id in new[] { 0, 1, 2, 3, 4, 5, 9, 19, 20, 29, 30, 39, 99 })
                Assert.That(EmberWeaponCatalog.IsRosterWeapon(id), Is.False, $"{id} 不应在名册内");
        }

        [Test]
        public void WeaponForMeta_MapsEveryMetamorphToItsOwnWeapon()
        {
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.MetaBasic), Is.EqualTo(EmberWeaponCatalog.Basic));
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.MetaOrbit), Is.EqualTo(EmberWeaponCatalog.Orbit));
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.MetaTrail), Is.EqualTo(EmberWeaponCatalog.Trail));
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.MetaPierce), Is.EqualTo(EmberWeaponCatalog.Pierce));
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.MetaBoom), Is.EqualTo(EmberWeaponCatalog.Boom));
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.MetaMeteor), Is.EqualTo(EmberWeaponCatalog.Meteor));
        }

        [Test]
        public void WeaponForExcl_MapsEveryExclusiveToItsOwnWeapon()
        {
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.ExclBasic), Is.EqualTo(EmberWeaponCatalog.Basic));
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.ExclOrbit), Is.EqualTo(EmberWeaponCatalog.Orbit));
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.ExclTrail), Is.EqualTo(EmberWeaponCatalog.Trail));
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.ExclPierce), Is.EqualTo(EmberWeaponCatalog.Pierce));
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.ExclBoom), Is.EqualTo(EmberWeaponCatalog.Boom));
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.ExclMeteor), Is.EqualTo(EmberWeaponCatalog.Meteor));
        }

        [Test]
        public void WeaponForMeta_NonMetamorph_ReturnsZero()
        {
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.StatAtk), Is.EqualTo(0));
            Assert.That(EmberWeaponCatalog.WeaponForMeta(EmberWeaponCatalog.ExclBasic), Is.EqualTo(0));
            Assert.That(EmberWeaponCatalog.WeaponForMeta(-1), Is.EqualTo(0));
        }

        [Test]
        public void WeaponForExcl_NonExclusive_ReturnsZero()
        {
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.StatAtk), Is.EqualTo(0));
            Assert.That(EmberWeaponCatalog.WeaponForExcl(EmberWeaponCatalog.MetaBasic), Is.EqualTo(0));
            Assert.That(EmberWeaponCatalog.WeaponForExcl(-1), Is.EqualTo(0));
        }

        [Test]
        public void EveryMetamorphAndExclusiveMapsToARosterWeapon()
        {
            // 映射表与名册表是两份平行的数组，错位会让"质变火球"挂到环火上
            for (int id = 20; id <= 25; id++)
                Assert.That(EmberWeaponCatalog.IsRosterWeapon(EmberWeaponCatalog.WeaponForMeta(id)), Is.True,
                    $"质变 {id} 映射到的武器不在名册里");

            for (int id = 30; id <= 35; id++)
                Assert.That(EmberWeaponCatalog.IsRosterWeapon(EmberWeaponCatalog.WeaponForExcl(id)), Is.True,
                    $"专属 {id} 映射到的武器不在名册里");
        }
    }
}
