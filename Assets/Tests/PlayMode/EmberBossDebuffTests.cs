using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Emberlight.Tests
{
    public sealed class EmberBossDebuffTests
    {
        sealed class Host : IEmberCombatHost
        {
            public EmberRunPhase Phase { get; set; } = EmberRunPhase.Playing;
            public float Elapsed => 0;
            public RunProgress Progress { get; } = new RunProgress();
            public BuffManager Buffs { get; } = new BuffManager();
            public bool HurtReady { get; set; } = true;
            public bool NeedsHealing => false;
            public bool Lethal;
            public int Hits, BuffRequests;
            public void HurtPlayer(float damage)
            {
                if (!HurtReady || Phase != EmberRunPhase.Playing) return;
                Hits++;
                HurtReady = false;
                if (Lethal) Phase = EmberRunPhase.Lost;
            }
            public bool TryAddBuff(BuffConfig config) { BuffRequests++; Buffs.Add(config); return true; }
            public bool PickHealingDrop(float amount) => false;
            public void PlayBossBurst(Vector2 point) { }
            public void RegisterKill() { }
            public void OnWaveCleared(int wave) { }
            public void EndRun(bool won) { }
        }

        GameObject world, body;
        Host host;

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("Boss debuff test");
            body = new GameObject("Boss");
            body.transform.SetParent(world.transform, false);
            host = new Host();
        }

        [UnityTearDown]
        public IEnumerator TearDown() { Object.Destroy(world); yield return null; }

        EmberBoss StartDash(int tier)
        {
            var boss = new EmberBoss(host, body.transform, world.transform, tier);
            boss.Tick(2.01f, Vector2.zero, 1);
            Assert.That(boss.State, Is.EqualTo(EmberBoss.AttackState.DashWarning));
            Assert.That(host.BuffRequests, Is.Zero, "A warning is not a hit.");
            boss.Tick(1.2f, Vector2.zero, 1);
            Assert.That(boss.State, Is.EqualTo(EmberBoss.AttackState.Dash));
            return boss;
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void EveryBossTier_DashHitAppliesSharedSlow(int tier)
        {
            StartDash(tier).Tick(.05f, Vector2.zero, 1);
            Assert.That(host.Hits, Is.EqualTo(1));
            Assert.That(host.Buffs.Active.Count, Is.EqualTo(1));
            Assert.That(host.Buffs.Active[0].Config, Is.SameAs(EmberBuffCatalog.Slow));
            Assert.That(host.Buffs.MoveSpeedMultiplier, Is.LessThan(1));
        }

        [Test]
        public void MissOrInvulnerability_DoesNotApplyDebuff()
        {
            var boss = StartDash(1);
            boss.Tick(.05f, Vector2.one * 20, 1);
            Assert.That(host.Hits, Is.Zero);
            Assert.That(host.BuffRequests, Is.Zero);
            host.HurtReady = false;
            boss.Tick(.05f, Vector2.zero, 1);
            Assert.That(host.BuffRequests, Is.Zero);
        }

        [Test]
        public void LethalHit_DoesNotRequestBuffAfterGameOver()
        {
            host.Lethal = true;
            StartDash(3).Tick(.05f, Vector2.zero, 1);
            Assert.That(host.Phase, Is.EqualTo(EmberRunPhase.Lost));
            Assert.That(host.BuffRequests, Is.Zero);
        }

        [Test]
        public void RepeatedHit_RefreshesInsteadOfStacking()
        {
            var boss = StartDash(2);
            boss.Tick(.05f, Vector2.zero, 1);
            Assert.That(host.Buffs.Active.Count, Is.EqualTo(1));
            host.Buffs.Tick(1);
            host.HurtReady = true;
            boss.Tick(.05f, Vector2.zero, 1);
            Assert.That(host.Buffs.Active.Count, Is.EqualTo(1));
            Assert.That(host.Buffs.Active[0].Remaining, Is.EqualTo(EmberBuffCatalog.Slow.Duration));
        }

        [Test]
        public void EruptionHit_AppliesSlowAfterItsWarning()
        {
            var boss = StartDash(1);
            boss.Tick(.7f, Vector2.one * 20, 1); // 避开冲锋，进入硬直。
            boss.Tick(1.6f, Vector2.zero, 1);
            boss.Tick(2.01f, Vector2.zero, 1);
            Assert.That(boss.State, Is.EqualTo(EmberBoss.AttackState.EruptionWarning));
            Assert.That(host.BuffRequests, Is.Zero);
            boss.Tick(1.4f, Vector2.zero, 1);
            Assert.That(host.Buffs.Active.Count, Is.EqualTo(1));
            Assert.That(host.Buffs.Active[0].Config, Is.SameAs(EmberBuffCatalog.Slow));
        }
    }
}
