using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberKeeperVitalsTests
    {
        EmberRunState run;
        EmberRunPhase phase;
        EmberAudio[] initialAudio;

        [SetUp]
        public void SetUp()
        {
            initialAudio = Object.FindObjectsOfType<EmberAudio>();
            phase = EmberRunPhase.Playing;
            run = new EmberRunState(() => phase) { Progress = new RunProgress(), Level = LevelConfig.Create(EmberDifficulty.Hard, 25) };
            run.Vitals.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            // Only the old implementation creates this object; remove it after the regression test.
            foreach (var audio in Object.FindObjectsOfType<EmberAudio>())
                if (!initialAudio.Contains(audio)) Object.DestroyImmediate(audio.gameObject);
        }

        [Test]
        public void Hurt_DoesNotCreatePresentationObjects()
        {
            int before = Object.FindObjectsOfType<EmberAudio>().Length;
            run.Vitals.Hurt(10);
            Assert.That(Object.FindObjectsOfType<EmberAudio>().Length, Is.EqualTo(before));
        }

        [Test]
        public void Hurt_AppliesDifficultyOnceThenConsumesShield()
        {
            run.Progress.OfferChoices();
            run.Progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze));
            run.Vitals.Hurt(200);
            Assert.That(run.Progress.Shield, Is.EqualTo(0));
            Assert.That(run.Health, Is.EqualTo(90).Within(.001f));
        }

        [Test]
        public void Hurt_ShieldOnlyStillStartsInvulnerability()
        {
            run.Progress.OfferChoices();
            run.Progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze));
            Assert.That(run.Vitals.Hurt(10), Is.EqualTo(EmberVitalOutcome.NoChange));
            Assert.That(run.Vitals.Hurt(10), Is.EqualTo(EmberVitalOutcome.Rejected));
            run.Vitals.Tick(EmberKeeperVitals.HurtCooldown);
            Assert.That(run.Vitals.HurtReady, Is.True);
        }

        [TestCase(EmberRunPhase.Paused)]
        [TestCase(EmberRunPhase.Upgrade)]
        [TestCase(EmberRunPhase.Menu)]
        public void OutsideCombat_RejectsDamageAndDrops(EmberRunPhase other)
        {
            phase = other;
            Assert.That(run.Vitals.Hurt(10), Is.EqualTo(EmberVitalOutcome.Rejected));
            Assert.That(run.Vitals.PickHealingDrop(20), Is.EqualTo(EmberVitalOutcome.Rejected));
            Assert.That(run.Health, Is.EqualTo(100));
        }

        [Test]
        public void FullHealth_DropIsConsumedButOrdinaryHealIsRejected()
        {
            Assert.That(run.Vitals.Heal(20), Is.EqualTo(EmberVitalOutcome.Rejected));
            Assert.That(run.Vitals.PickHealingDrop(20), Is.EqualTo(EmberVitalOutcome.NoChange));
        }

        [Test]
        public void Heal_ClampsAndResetClearsDamage()
        {
            run.Vitals.Hurt(50);
            Assert.That(run.Vitals.PickHealingDrop(200), Is.EqualTo(EmberVitalOutcome.Changed));
            Assert.That(run.Health, Is.EqualTo(run.MaxHealth));
            run.Vitals.Reset();
            Assert.That(run.Vitals.HurtReady, Is.True);
        }
    }
}
