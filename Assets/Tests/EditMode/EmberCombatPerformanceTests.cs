using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed class EmberCombatPerformanceTests
    {
        GameObject world;
        EmberPool pool;
        EmberBurnGround ground;
        EmberEnemy[] enemies;
        EmberWeaponContext context;

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("Performance test world");
            pool = new EmberPool();
            pool.Bind(world.transform);
            ground = new EmberBurnGround();
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(world); }

        static int AllocationSamples(Action action)
        {
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 4096,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                action();
                recorder.Stop();
                return recorder.Count;
            }
        }

        [Test]
        public void AllocationMeasurement_DetectsKnownAllocation()
        {
            int samples = AllocationSamples(() => GC.KeepAlive(new byte[1024]));
            TestContext.WriteLine("PERF allocation_control_samples=" + samples);
            Assert.That(samples, Is.GreaterThan(0), "Allocation measurement must detect a real allocation.");
        }

        void SpriteCycle()
        {
            var fire = pool.RentFire(world.transform, new Vector2(1, 2), .3f);
            pool.Release("fire", fire);
            var shape = pool.RentShape("warn", world.transform, Vector2.zero, Vector2.one, Color.white, 1);
            pool.Release("warn", shape);
        }

        [Test]
        public void ReusedSprites_DoNotAllocateManagedMemory()
        {
            for (int i = 0; i < 256; i++) SpriteCycle();
            int allocated = AllocationSamples(() => { for (int i = 0; i < 256; i++) SpriteCycle(); });
            TestContext.WriteLine("PERF sprite_cycles=256 allocation_samples=" + allocated);
            Assert.That(allocated, Is.Zero, "A pool hit should not allocate temporary factories.");
        }

        [Test]
        public void ReplacePatchesAtCapacity_ReusesManagedState()
        {
            for (int i = 0; i < 256; i++) ground.SpawnPatch(Vector2.zero, world.transform, 1f, pool);
            int allocated = AllocationSamples(() => { for (int i = 0; i < 256; i++) ground.SpawnPatch(Vector2.zero, world.transform, 1f, pool); });
            TestContext.WriteLine("PERF patch_replacements=256 allocation_samples=" + allocated);
            Assert.That(ground.Count, Is.EqualTo(EmberWeaponTuning.BurnGround.Capacity));
            Assert.That(allocated, Is.Zero, "A replacement should reuse both its view and logical state.");
        }

        void CreateTargets(int count)
        {
            enemies = new EmberEnemy[count];
            for (int i = 0; i < count; i++)
            {
                var node = new GameObject("Target");
                node.transform.SetParent(world.transform, false);
                node.transform.position = new Vector2((i % 15 - 7) * .8f, (i / 15 - 5) * .8f);
                enemies[i] = new EmberEnemy { view = node.transform, hp = 10000, maxHp = 10000 };
            }
            context = new EmberWeaponContext
            {
                World = world.transform, Pool = pool, Progress = new RunProgress(),
                EnemyCount = () => enemies.Length, GetEnemy = i => enemies[i],
                ContactDamageFrom = (i, damage, source) => enemies[i].hp -= damage
            };
        }

        [Test]
        public void OverlappingPatches_KeepReverseHitOrderAndDamage()
        {
            CreateTargets(2);
            enemies[0].view.position = Vector2.zero;
            enemies[1].view.position = Vector2.right;
            var hits = new List<int>();
            float received = 0;
            context.ContactDamageFrom = (i, amount, source) =>
            {
                hits.Add(i);
                received += amount;
                enemies[i].hp -= amount;
            };
            ground.SpawnPatch(new Vector2(.2f, 0), world.transform, .5f, pool);
            ground.SpawnPatch(new Vector2(.6f, 0), world.transform, 1f, pool);
            ground.Tick(.1f, context);
            Assert.That(hits, Is.EqualTo(new[] { 1, 0, 0 }));
            Assert.That(received, Is.EqualTo(4.6f).Within(.001f));
        }

        [Test]
        public void ExpiredPatch_ReusedStateResetsPowerAndLifetime()
        {
            CreateTargets(1);
            enemies[0].view.position = Vector2.zero;
            ground.SpawnPatch(Vector2.zero, world.transform, 1f, pool);
            ground.Tick(3f, context);
            Assert.That(ground.Count, Is.Zero);
            enemies[0].hp = 100;
            ground.SpawnPatch(Vector2.zero, world.transform, .5f, pool);
            ground.Tick(.1f, context);
            Assert.That(ground.Count, Is.EqualTo(1));
            Assert.That(enemies[0].hp, Is.EqualTo(99.08f).Within(.001f));
            ground.Tick(1.6f, context);
            Assert.That(ground.Count, Is.Zero);
        }

        [TestCase(.549f, true)]
        [TestCase(.55f, false)]
        [TestCase(.551f, false)]
        public void PatchRadius_StaysStrict(float distance, bool hit)
        {
            CreateTargets(1);
            enemies[0].view.position = new Vector2(distance, 0);
            int hits = 0;
            context.ContactDamageFrom = (i, amount, source) => { hits++; enemies[i].hp -= amount; };
            ground.SpawnPatch(Vector2.zero, world.transform, .5f, pool);
            ground.Tick(.1f, context);
            Assert.That(hits, Is.EqualTo(hit ? 1 : 0));
        }

        [Test]
        public void DenseBurnGround_ReportsCpuCost()
        {
            CreateTargets(150);
            for (int i = 0; i < 40; i++)
                ground.SpawnPatch(new Vector2((i % 8 - 4) * .7f, (i / 8 - 2) * .7f), world.transform, 1f, pool);
            for (int frame = 0; frame < 16; frame++) ground.Tick(.001f, context);
            var samples = new double[7];
            for (int sample = 0; sample < samples.Length; sample++)
            {
                long start = Stopwatch.GetTimestamp();
                for (int frame = 0; frame < 90; frame++) ground.Tick(.001f, context);
                samples[sample] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency / 90;
            }
            Array.Sort(samples);
            TestContext.WriteLine("PERF burn_patches=40 enemies=150 median_ms_per_tick=" + samples[3].ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
            Assert.That(ground.Count, Is.EqualTo(40));
            Assert.That(enemies[80].hp, Is.LessThan(enemies[80].maxHp));
        }
    }
}
