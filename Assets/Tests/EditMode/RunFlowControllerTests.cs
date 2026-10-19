using System;
using NUnit.Framework;

namespace Emberlight.Tests
{
    public sealed class RunFlowControllerTests
    {
        sealed class TestHost : RunFlowController.Host
        {
            public RunProgress Progress { get; set; } = new RunProgress();
            public EmberRunPhase Phase { get; set; } = EmberRunPhase.Playing;
            public EmberOffer[] Offers { get; private set; }
            public EmberOffer Next = new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze);
            public int Finished;
            public int PicksAfterWave(int wave) => LevelConfig.Default.PicksAfterWave(wave);
            public void RequestChoices() { Progress.OfferChoices(); Offers = new[] { Next }; }
            public void OnUpgradeOffered(EmberOffer[] offers) { Offers = offers; Phase = EmberRunPhase.Upgrade; }
            public void OnUpgradeSequenceFinished() { Finished++; Phase = EmberRunPhase.Playing; }
        }

        TestHost host;
        RunFlowController flow;

        [SetUp]
        public void SetUp()
        {
            host = new TestHost();
            host.Progress.ConfigureRun(new[] { RunProgress.WeaponBasic }, 5);
            flow = new RunFlowController(host, new Random(42));
        }

        [Test]
        public void BossWave_RequiresTwoPicksBeforeReturningToCombat()
        {
            flow.OnWaveCleared(10);
            Assert.That(flow.PendingPicks, Is.EqualTo(2));
            Assert.That(flow.Choose(host.Offers[0]), Is.True);
            Assert.That(host.Finished, Is.Zero);
            Assert.That(flow.Choose(host.Offers[0]), Is.True);
            Assert.That(host.Finished, Is.EqualTo(1));
            Assert.That(flow.PendingPicks, Is.Zero);
        }

        [Test]
        public void Compensation_DoesNotConsumeScheduledPickOrChain()
        {
            host.Next = new EmberOffer(RunProgress.WeaponOrbit, EmberRarity.Gold);
            flow.OnWaveCleared(10);
            host.Next = new EmberOffer(RunProgress.WeaponTrail, EmberRarity.Gold);
            Assert.That(flow.Choose(host.Offers[0]), Is.True);
            Assert.That(flow.IsCompensationPick, Is.True);
            Assert.That(flow.PendingPicks, Is.EqualTo(1));
            host.Next = new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze);
            Assert.That(flow.Choose(host.Offers[0]), Is.True);
            Assert.That(flow.IsCompensationPick, Is.False);
            Assert.That(flow.PendingPicks, Is.EqualTo(1));
            Assert.That(flow.Choose(host.Offers[0]), Is.True);
            Assert.That(host.Finished, Is.EqualTo(1));
            Assert.That(host.Progress.OwnedWeaponCount, Is.EqualTo(3));
        }

        [Test]
        public void ForgedOrRepeatedChoice_DoesNotChangeBuild()
        {
            flow.OnWaveCleared(1);
            Assert.That(flow.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Diamond)), Is.False);
            Assert.That(host.Progress.Shield, Is.Zero);
            var valid = host.Offers[0];
            Assert.That(flow.Choose(valid), Is.True);
            Assert.That(flow.Choose(valid), Is.False);
            Assert.That(host.Progress.Shield, Is.EqualTo(250));
        }

        [Test]
        public void Refresh_IsLimitedAndDoesNotSpendScheduledPicks()
        {
            flow.OnWaveCleared(1);
            int count = host.Progress.RefreshesRemaining;
            for (int i = 0; i < count; i++) Assert.That(flow.TryRefresh(), Is.True);
            Assert.That(flow.TryRefresh(), Is.False);
            Assert.That(flow.PendingPicks, Is.EqualTo(1));
        }

        [Test]
        public void FinalWave_NoUpgradeSequence()
        {
            flow.OnWaveCleared(25);
            Assert.That(host.Finished, Is.EqualTo(1));
            Assert.That(host.Offers, Is.Null);
        }

        [Test]
        public void Reset_ClearsPendingAndCompensation()
        {
            host.Next = new EmberOffer(RunProgress.WeaponOrbit, EmberRarity.Gold);
            flow.OnWaveCleared(1);
            flow.Choose(host.Offers[0]);
            flow.Reset();
            Assert.That(flow.PendingPicks, Is.Zero);
            Assert.That(flow.IsCompensationPick, Is.False);
        }
    }
}
