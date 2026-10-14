using NUnit.Framework;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberBurnGroundTests
    {
        GameObject world;
        EmberPool pool;
        EmberBurnGround ground;
        EmberEnemy enemy;
        EmberWeaponContext context;

        [SetUp]
        public void SetUp()
        {
            world = new GameObject("Burn test world");
            pool = new EmberPool();
            pool.Bind(world.transform);
            ground = new EmberBurnGround();
            var view = new GameObject("Target");
            view.transform.SetParent(world.transform);
            enemy = new EmberEnemy { view = view.transform, hp = 100, maxHp = 100 };
            context = new EmberWeaponContext
            {
                World = world.transform, Pool = pool, Progress = new RunProgress(),
                EnemyCount = () => 1, GetEnemy = i => enemy,
                ContactDamageFrom = (i, damage, source) => enemy.hp -= damage
            };
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(world); }

        [Test]
        public void SharedPatch_DamagesWithoutOwningTrail()
        {
            Assert.That(context.Progress.OwnsWeapon(RunProgress.WeaponTrail), Is.False);
            ground.SpawnPatch(Vector2.zero, world.transform, .5f, pool);
            ground.Tick(.1f, context);
            Assert.That(enemy.hp, Is.EqualTo(99.08f).Within(.001f));
        }

        [Test]
        public void Patch_ExpiresAndReturnsItsViewToPool()
        {
            ground.SpawnPatch(Vector2.zero, world.transform, .5f, pool);
            ground.Tick(2f, context);
            Assert.That(ground.Count, Is.Zero);
            Assert.That(world.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
        }

        [Test]
        public void PatchLimit_RecyclesOldestAndResetClearsRunState()
        {
            for (int i = 0; i < 45; i++) ground.SpawnPatch(Vector2.zero, world.transform, 1f, pool);
            Assert.That(ground.Count, Is.EqualTo(40));
            ground.Reset();
            ground.Tick(.1f, context);
            Assert.That(ground.Count, Is.Zero);
            Assert.That(enemy.hp, Is.EqualTo(100));
        }
    }
}
