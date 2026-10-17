using NUnit.Framework;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class BuffIntegrationTests
    {
        sealed class Host : IEmberCombatHost
        {
            public EmberRunPhase Phase { get; set; } = EmberRunPhase.Playing;
            public float Elapsed => 0;
            public RunProgress Progress { get; } = new RunProgress();
            public BuffManager Buffs { get; } = new BuffManager();
            public EmberKeeperVitals Vitals { get; }
            public Host() { Vitals = new EmberKeeperVitals(() => Progress, () => 1, () => Phase); }
            public bool HurtReady => Vitals.HurtReady;
            public bool NeedsHealing => Vitals.NeedsHealing;
            public void HurtPlayer(float amount) { Vitals.Hurt(amount); }
            public bool PickHealingDrop(float amount) => Vitals.PickHealingDrop(amount) != EmberVitalOutcome.Rejected;
            public bool TryAddBuff(BuffConfig config)
            {
                if (Phase != EmberRunPhase.Playing) return false;
                Buffs.Add(config);
                return true;
            }
            public void PlayBossBurst(Vector2 point) { }
            public void RegisterKill() { }
            public void OnWaveCleared(int wave) { }
            public void EndRun(bool won) { }
        }

        sealed class FixedRandom : System.Random
        {
            readonly double roll;
            public FixedRandom(double roll) { this.roll = roll; }
            public override double NextDouble() => roll;
        }

        GameObject world, cameraNode, player;
        EmberPool pool;
        Host host;
        Camera camera;

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("Buff test world");
            player = new GameObject("Player");
            player.transform.SetParent(world.transform);
            cameraNode = new GameObject("Buff test camera", typeof(Camera));
            camera = cameraNode.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.aspect = .5625f;
            camera.transform.position = new Vector3(0, 0, -10);
            pool = new EmberPool();
            pool.Bind(world.transform);
            host = new Host();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(world);
            Object.DestroyImmediate(cameraNode);
        }

        [TestCase(0.0, "power")]
        [TestCase(0.75, "haste")]
        public void DeathDrop_PicksBuffWithoutReplacingHealing(double roll, string expectedId)
        {
            var level = new LevelConfig { HealDropChance = 1, BuffDropChance = 1 };
            var drops = new EmberDrops(pool, host);
            drops.Bind(world.transform, level);
            host.HurtPlayer(40);
            drops.OnDeath(Vector2.zero, 0, EnemyAffix.None, new FixedRandom(roll));
            drops.Tick(.1f, Vector2.zero);
            Assert.That(host.Vitals.Health, Is.EqualTo(80));
            Assert.That(host.Buffs.Active.Count, Is.EqualTo(1));
            Assert.That(host.Buffs.Active[0].Config.Id, Is.EqualTo(expectedId));
            Assert.That(world.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
        }

        [Test]
        public void DropViews_UseApprovedArtworkAndResetSpriteWhenPooled()
        {
            var drops = new EmberDrops(pool, host);
            drops.Bind(world.transform, new LevelConfig { HealDropChance = 1, BuffDropChance = 1 });
            drops.OnDeath(Vector2.zero, 0, EnemyAffix.None, new FixedRandom(0));
            var first = world.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(System.Array.Find(first, view => view.name == "healing-drop").sprite.name, Is.EqualTo("pickup-heal"));
            Assert.That(System.Array.Find(first, view => view.name == "buff-drop").sprite.name, Is.EqualTo("pickup-power"));
            drops.Tick(.1f, Vector2.zero);
            drops.OnDeath(Vector2.zero, 0, EnemyAffix.None, new FixedRandom(.75));
            var second = world.GetComponentsInChildren<SpriteRenderer>();
            var haste = System.Array.Find(second, view => view.name == "buff-drop");
            Assert.That(haste.sprite.name, Is.EqualTo("pickup-haste"));
            Assert.That(haste.color, Is.EqualTo(Color.white), "Colored sprites must not be tinted a second time.");
        }

        [Test]
        public void SplitChildren_DoNotGenerateHealingOrBuffDrops()
        {
            var drops = new EmberDrops(pool, host);
            drops.Bind(world.transform, new LevelConfig { HealDropChance = 1, BuffDropChance = 1 });
            drops.OnDeath(Vector2.zero, 6, EnemyAffix.Burning, new FixedRandom(0));
            drops.Tick(.1f, Vector2.zero);
            Assert.That(host.Buffs.Active, Is.Empty);
            Assert.That(world.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
        }

        [Test]
        public void PausedDrop_DoesNotExpireAndCanBePickedAfterResume()
        {
            var drops = new EmberDrops(pool, host);
            drops.Bind(world.transform, new LevelConfig { HealDropChance = 0, BuffDropChance = 1 });
            drops.OnDeath(Vector2.zero, 0, EnemyAffix.None, new FixedRandom(.5));
            host.Phase = EmberRunPhase.Paused;
            drops.Tick(100, Vector2.zero);
            Assert.That(host.Buffs.Active, Is.Empty);
            host.Phase = EmberRunPhase.Playing;
            drops.Tick(.1f, Vector2.zero);
            Assert.That(host.Buffs.Active.Count, Is.EqualTo(1));
        }

        [Test]
        public void SlowBolt_RespectsInvulnerabilityAndPooledOrdinaryBoltHasNoBuff()
        {
            var shots = new EmberEnemyProjectiles(pool);
            shots.Bind(world.transform, camera);
            shots.Fire(Vector2.up, Vector2.down, 10, 8, EmberBuffCatalog.Slow);
            shots.Move(.1f);
            shots.HitPlayer(Vector2.zero, host.HurtPlayer, host.TryAddBuff, () => host.HurtReady);
            Assert.That(host.Vitals.Health, Is.EqualTo(92));
            Assert.That(host.Buffs.MoveSpeedMultiplier, Is.EqualTo(.75f));
            host.Buffs.Clear();
            shots.Fire(Vector2.up, Vector2.down, 10, 8, EmberBuffCatalog.Slow);
            shots.Move(.1f);
            shots.HitPlayer(Vector2.zero, host.HurtPlayer, host.TryAddBuff, () => host.HurtReady);
            Assert.That(host.Vitals.Health, Is.EqualTo(92));
            Assert.That(host.Buffs.Active, Is.Empty);
            host.Vitals.Tick(1);
            shots.Fire(Vector2.up, Vector2.down, 10, 8);
            shots.Move(.1f);
            shots.HitPlayer(Vector2.zero, host.HurtPlayer, host.TryAddBuff, () => host.HurtReady);
            Assert.That(host.Vitals.Health, Is.EqualTo(84));
            Assert.That(host.Buffs.Active, Is.Empty);
        }

        [Test]
        public void Shooter_OnlyEveryThirdShotAppliesSlow()
        {
            var shots = new EmberEnemyProjectiles(pool);
            shots.Bind(world.transform, camera);
            var node = new GameObject("Shooter");
            node.transform.SetParent(world.transform);
            node.transform.position = Vector2.up * 2;
            var enemy = new EmberEnemy { view = node.transform, hp = 100 };
            var context = new EmberEnemyContext
            {
                Enemy = enemy, Player = player.transform, Camera = camera, Projectiles = shots, Config = new LevelConfig()
            };
            var shooter = new ShooterBehavior();
            for (int shot = 1; shot <= 3; shot++)
            {
                shooter.Tick(3, context);
                Assert.That(enemy.slowShot, Is.EqualTo(shot == 3));
                shooter.Tick(.4f, context);
                shots.Move(.334f);
                shots.HitPlayer(Vector2.zero, host.HurtPlayer, host.TryAddBuff, () => host.HurtReady);
                Assert.That(host.Buffs.Active.Count, Is.EqualTo(shot == 3 ? 1 : 0));
                host.Vitals.Tick(1);
            }
        }

        [Test]
        public void InterceptedSlowBolt_CannotApplyDebuff()
        {
            var shots = new EmberEnemyProjectiles(pool);
            shots.Bind(world.transform, camera);
            shots.Fire(Vector2.up, Vector2.down, 10, 8, EmberBuffCatalog.Slow);
            shots.Move(.1f);
            Assert.That(shots.Intercept(new Vector2(-1, .5f), new Vector2(1, .5f), .2f), Is.True);
            shots.HitPlayer(Vector2.zero, host.HurtPlayer, host.TryAddBuff, () => host.HurtReady);
            Assert.That(host.Vitals.Health, Is.EqualTo(100));
            Assert.That(host.Buffs.Active, Is.Empty);
        }

        [Test]
        public void DamageBuff_AppliesOnceAtSharedDamageEntryAndExpires()
        {
            var level = LevelConfig.Create(EmberDifficulty.Standard, 25);
            var roster = new EmberEnemyRoster(pool, level, world.transform, player.transform, new System.Random(1), camera);
            var enemy = roster.Spawn(false, 1, forcedKind: 0);
            enemy.view.position = Vector2.zero;
            enemy.hp = enemy.maxHp = 100;
            var context = new EmberCombatDamage.Context
            {
                Roster = roster, Config = level, Camera = camera, Player = player.transform, PlayerBuffs = host.Buffs
            };
            host.Buffs.Add(EmberBuffCatalog.Power);
            EmberCombatDamage.Apply(context, 0, 10);
            Assert.That(enemy.hp, Is.EqualTo(87).Within(.001));
            host.Buffs.Tick(8);
            EmberCombatDamage.Apply(context, 0, 10);
            Assert.That(enemy.hp, Is.EqualTo(77).Within(.001));
        }

        [Test]
        public void Movement_ReadsBuffsAndRunClearRemovesAllContributions()
        {
            var run = new EmberRunState(() => EmberRunPhase.Playing) { Keeper = player.transform };
            run.Buffs.Add(EmberBuffCatalog.Haste);
            run.Buffs.Add(EmberBuffCatalog.Slow);
            EmberKeeperMotion.Step(run, Vector2.right, 1);
            Assert.That(player.transform.position.x, Is.EqualTo(3.465f).Within(.001));
            run.Clear();
            Assert.That(run.Buffs.Active, Is.Empty);
            Assert.That(run.Buffs.MoveSpeedMultiplier, Is.EqualTo(1));
        }
    }
}
