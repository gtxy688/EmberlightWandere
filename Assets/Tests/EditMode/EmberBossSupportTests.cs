using NUnit.Framework;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberBossSupportTests
    {
        sealed class Host : EmberWaveDirector.Host
        {
            public int EnemyCount { get; set; }
            public Vector2 PlayerPosition => Vector2.zero;
            public bool AllowSupport = true;
            public int BossCalls, SupportCalls, WaveClears, FinalWins;
            public bool TrySpawnRolledTrash() { EnemyCount++; return true; }
            public bool TrySpawnTrashAt(int kind, Vector2 position, bool forceAffix) { EnemyCount++; return true; }
            public bool TrySpawnBossSupport()
            {
                if (!AllowSupport) return false;
                EnemyCount++; SupportCalls++; return true;
            }
            public void SpawnBoss() { EnemyCount++; BossCalls++; }
            public void ClearProjectiles() { }
            public void OnWaveCleared(int wave) { WaveClears++; }
            public void OnFinalBossDefeated() { FinalWins++; }
        }

        static EmberWaveDirector BossWave(Host host, LevelConfig config, int wave)
        {
            var director = new EmberWaveDirector(host, config, new System.Random(1));
            director.Begin();
            while (director.Wave < wave) director.Advance();
            return director;
        }

        [TestCase(EmberDifficulty.Casual, 15, 24)]
        [TestCase(EmberDifficulty.Standard, 18, 28)]
        [TestCase(EmberDifficulty.Hard, 22, 34)]
        public void SupportTargets_RespectDifficultyAndDoNotLeakIntoOrdinaryWaves(EmberDifficulty difficulty, int stage, int final)
        {
            var config = LevelConfig.Create(difficulty, 25);
            Assert.That(config.BossSupportTargetForWave(10), Is.EqualTo(stage));
            Assert.That(config.BossSupportTargetForWave(25), Is.EqualTo(final));
            Assert.That(config.BossSupportTargetForWave(9), Is.Zero);
            config.EnemyLimit = 1;
            Assert.That(config.BossSupportTargetForWave(25), Is.Zero, "The Boss itself reserves the only available slot.");
        }

        [TestCase(25, 10, 18)]
        [TestCase(25, 20, 18)]
        [TestCase(25, 25, 28)]
        [TestCase(50, 50, 28)]
        public void BossArrival_BringsSupportsWithoutConsumingOrdinaryQuota(int length, int wave, int count)
        {
            var host = new Host();
            var director = BossWave(host, LevelConfig.Create(EmberDifficulty.Standard, length), wave);
            director.Tick(2.9f);
            Assert.That(host.EnemyCount, Is.Zero, "No enemies may bypass the boss warning.");
            director.Tick(.2f);
            Assert.That(host.BossCalls, Is.EqualTo(1));
            Assert.That(host.SupportCalls, Is.EqualTo(count), "Boss arrival must include the selected number of minions.");
            Assert.That(host.EnemyCount, Is.EqualTo(count + 1));
            Assert.That(director.Quota, Is.Zero);
            Assert.That(director.Spawned, Is.Zero);
            director.CheckWaveCleared();
            Assert.That(host.WaveClears, Is.Zero);
        }

        [Test]
        public void Supports_RefillInBatchesStopAtTargetAndStopAfterBossDeath()
        {
            var host = new Host();
            var director = BossWave(host, LevelConfig.Create(EmberDifficulty.Standard, 25), 10);
            director.Tick(3.1f);
            Assert.That(host.SupportCalls, Is.EqualTo(18));
            host.EnemyCount -= 5;
            director.Tick(.5f);
            Assert.That(host.SupportCalls, Is.EqualTo(18));
            director.Tick(1.1f);
            Assert.That(host.SupportCalls, Is.EqualTo(21));
            director.Tick(1.5f);
            Assert.That(host.SupportCalls, Is.EqualTo(23));
            director.Tick(10);
            Assert.That(host.SupportCalls, Is.EqualTo(23));
            director.NoteBossDefeated();
            host.EnemyCount = 0;
            director.Tick(10);
            Assert.That(host.SupportCalls, Is.EqualTo(23));
            Assert.That(host.WaveClears, Is.EqualTo(1));
        }

        [Test]
        public void FailedSupportPlacement_RetriesAndGlobalLimitIncludesBoss()
        {
            var host = new Host { AllowSupport = false };
            var config = LevelConfig.Create(EmberDifficulty.Standard, 25);
            config.EnemyLimit = 5;
            var director = BossWave(host, config, 10);
            director.Tick(3.1f);
            Assert.That(host.EnemyCount, Is.EqualTo(1));
            host.AllowSupport = true;
            director.Tick(1.5f);
            director.Tick(1.5f);
            Assert.That(host.SupportCalls, Is.EqualTo(4));
            Assert.That(host.EnemyCount, Is.EqualTo(config.EnemyLimit));
            director.Tick(20);
            Assert.That(host.EnemyCount, Is.EqualTo(5));
        }

        [Test]
        public void FinalBossDeath_EndsWithoutWaitingForRemainingSupports()
        {
            var host = new Host();
            var director = BossWave(host, LevelConfig.Create(EmberDifficulty.Standard, 25), 25);
            director.Tick(3.1f);
            Assert.That(host.SupportCalls, Is.EqualTo(28));
            director.NoteBossDefeated();
            Assert.That(host.FinalWins, Is.EqualTo(1));
            Assert.That(host.WaveClears, Is.Zero);
            int count = host.SupportCalls;
            director.Tick(100);
            Assert.That(host.SupportCalls, Is.EqualTo(count));
        }
    }
}
