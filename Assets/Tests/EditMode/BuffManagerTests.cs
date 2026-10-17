using System;
using NUnit.Framework;

namespace Emberlight.Tests
{
    public sealed class BuffManagerTests
    {
        static BuffConfig Speed(string id, float amount, float duration)
        {
            return new BuffConfig(id, "疾行", BuffAttribute.MoveSpeed, amount, duration, amount < 0);
        }

        [Test]
        public void Buffs_ModifyOnlyTheirOwnAttribute()
        {
            var manager = new BuffManager();
            manager.Add(Speed("haste", .3f, 8));
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(.0001));
            Assert.That(manager.DamageMultiplier, Is.EqualTo(1));
            manager.Add(new BuffConfig("power", "增伤", BuffAttribute.Damage, .5f, 8));
            Assert.That(manager.DamageMultiplier, Is.EqualTo(1.5f).Within(.0001));
        }

        [Test]
        public void Expiration_RemovesOnlyTheExpiredContribution()
        {
            var manager = new BuffManager();
            manager.Add(Speed("haste", .3f, 8));
            manager.Add(Speed("slow", -.25f, 3));
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(1.05f).Within(.0001));
            manager.Tick(3);
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(.0001));
            manager.Tick(5);
            Assert.That(manager.Active, Is.Empty);
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(1));
        }

        [Test]
        public void ReapplyingSameBuff_RefreshesDurationWithoutStacking()
        {
            var manager = new BuffManager();
            var config = Speed("haste", .3f, 8);
            Assert.That(manager.Add(config), Is.True, "A new Buff should report that it was added.");
            manager.Tick(6);
            Assert.That(manager.Add(config), Is.False, "Refreshing must be distinguishable from adding.");
            Assert.That(manager.Active.Count, Is.EqualTo(1));
            Assert.That(manager.Active[0].Remaining, Is.EqualTo(8));
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(.0001));
            manager.Tick(7);
            Assert.That(manager.Active.Count, Is.EqualTo(1));
            manager.Tick(1);
            Assert.That(manager.Active, Is.Empty);
        }

        [Test]
        public void SharedConfig_HasIndependentTimersOnDifferentOwners()
        {
            var config = Speed("haste", .3f, 8);
            var first = new BuffManager();
            var second = new BuffManager();
            first.Add(config);
            second.Add(config);
            first.Tick(3);
            Assert.That(first.Active[0].Remaining, Is.EqualTo(5));
            Assert.That(second.Active[0].Remaining, Is.EqualTo(8));
            Assert.That(config.Duration, Is.EqualTo(8));
        }

        [Test]
        public void RemovingDebuff_DoesNotEraseAnExistingBuff()
        {
            var manager = new BuffManager();
            manager.Add(Speed("haste", .3f, 8));
            manager.Add(Speed("slow", -.25f, 3));
            Assert.That(manager.Remove("slow"), Is.True);
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(.0001));
            Assert.That(manager.Remove("slow"), Is.False);
        }

        [Test]
        public void StrongSlows_CannotReverseOrStopMovement()
        {
            var manager = new BuffManager();
            manager.Add(Speed("first", -.8f, 3));
            manager.Add(Speed("second", -.8f, 4));
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(.25f));
            manager.Clear();
            Assert.That(manager.MoveSpeedMultiplier, Is.EqualTo(1));
            Assert.That(manager.DamageMultiplier, Is.EqualTo(1));
            Assert.That(manager.Active, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDuration_IsRejected(float duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Speed("haste", .3f, duration));
        }

        [Test]
        public void InvalidTick_DoesNotExtendBuffs()
        {
            var manager = new BuffManager();
            manager.Add(Speed("haste", .3f, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Tick(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Tick(float.NaN));
            Assert.That(manager.Active[0].Remaining, Is.EqualTo(8));
        }
    }
}
