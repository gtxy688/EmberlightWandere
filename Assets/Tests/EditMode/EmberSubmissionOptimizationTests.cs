using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed class EmberSubmissionOptimizationTests
    {
        GameObject world, playerNode, cameraNode, uiNode;
        EmberPool pool;
        EmberEnemyRoster roster;

        [TearDown]
        public void TearDown()
        {
            foreach (var node in new[] { world, playerNode, cameraNode, uiNode })
                if (node != null) Object.DestroyImmediate(node);
        }

        void CreateRoster(float x, float y, float aspect, float size = 8)
        {
            world = new GameObject("Spawn test world");
            playerNode = new GameObject("Spawn test player");
            playerNode.transform.position = new Vector3(x, y);
            cameraNode = new GameObject("Spawn test camera", typeof(Camera));
            var camera = cameraNode.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.aspect = aspect;
            camera.transform.position = new Vector3(x, y, -10);
            pool = new EmberPool();
            pool.Bind(world.transform);
            var config = LevelConfig.Create(EmberDifficulty.Standard, 25);
            var random = new System.Random(21);
            roster = new EmberEnemyRoster(pool, config, world.transform, playerNode.transform, random, camera);
        }

        [TestCase(0, 0, .5625f)]
        [TestCase(22, 0, .5625f)]
        [TestCase(-22, 22, .5625f)]
        [TestCase(22, -22, .5625f)]
        [TestCase(0, 0, 3f)]
        public void OrdinarySpawn_IsOutsideFullViewportAndInsideArena(float x, float y, float aspect)
        {
            CreateRoster(x, y, aspect);
            var camera = cameraNode.GetComponent<Camera>();
            for (int i = 0; i < 24; i++)
            {
                var enemy = roster.Spawn(false, 1, forcedKind: 0);
                Assert.That(enemy, Is.Not.Null);
                Vector3 position = enemy.view.position;
                Vector3 viewport = camera.WorldToViewportPoint(position);
                Assert.That(viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1, Is.True,
                    "Spawned inside camera viewport at " + position);
                Assert.That(Mathf.Abs(position.x), Is.LessThanOrEqualTo(22));
                Assert.That(Mathf.Abs(position.y), Is.LessThanOrEqualTo(22));
            }
        }

        [Test]
        public void ViewCoveringArena_DoesNotSpawnAnInvisibleQuotaEntry()
        {
            CreateRoster(0, 0, 1, 32);
            Assert.That(roster.Spawn(false, 1, forcedKind: 0), Is.Null);
            Assert.That(roster.Count, Is.Zero);
        }

        [Test]
        public void EncounterRequestedInsideViewport_IsMovedOutside()
        {
            CreateRoster(22, 0, .5625f);
            var enemy = roster.Spawn(false, 3, forcedKind: 2, position: new Vector2(22, 5), forceAffix: true);
            var viewport = cameraNode.GetComponent<Camera>().WorldToViewportPoint(enemy.view.position);
            Assert.That(viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1, Is.True);
            Assert.That(enemy.affix, Is.Not.EqualTo(EnemyAffix.None));
        }

        sealed class WaveHost : EmberWaveDirector.Host
        {
            public bool CanSpawn;
            public int EnemyCount => 0;
            public Vector2 PlayerPosition => Vector2.zero;
            public bool TrySpawnRolledTrash() => CanSpawn;
            public bool TrySpawnTrashAt(int kind, Vector2 position, bool forceAffix) => CanSpawn;
            public void SpawnBoss() { }
            public void ClearProjectiles() { }
            public void OnWaveCleared(int wave) { }
            public void OnFinalBossDefeated() { }
        }

        [Test]
        public void FailedSpawn_DoesNotConsumeWaveQuotaAndCanRetry()
        {
            var host = new WaveHost();
            var waves = new EmberWaveDirector(host, LevelConfig.Create(EmberDifficulty.Standard, 25), new System.Random(1));
            waves.Begin();
            waves.Tick(1);
            Assert.That(waves.Spawned, Is.Zero);
            host.CanSpawn = true;
            waves.Tick(1);
            Assert.That(waves.Spawned, Is.EqualTo(1));
        }

        [TestCase(typeof(EmberUpgradePanel))]
        [TestCase(typeof(EmberSettingsPanel))]
        [TestCase(typeof(EmberGodsSelectPanel))]
        public void UnboundPanel_ReportsConfigurationErrorInsteadOfBuildingUi(Type type)
        {
            uiNode = new GameObject("Unbound panel", typeof(RectTransform));
            var component = uiNode.AddComponent(type);
            Assert.Throws<InvalidOperationException>(() =>
            {
                if (component is EmberUpgradePanel upgrade) upgrade.Initialize();
                else if (component is EmberSettingsPanel settings) settings.Initialize();
                else ((EmberGodsSelectPanel)component).Initialize();
            });
            Assert.That(uiNode.transform.childCount, Is.Zero);
        }

        [Test]
        public void UnchangedHudData_DoesNotDirtyWaveText()
        {
            uiNode = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/UIRoot.prefab"));
            var view = uiNode.GetComponent<EmberUiRoot>();
            var hud = new EmberHud(view.Hud, null);
            hud.Show(true);
            var progress = new RunProgress();
            hud.Set(80, 100, progress, 1.01f, 3, 1, 10, 12);
            hud.Stage(1, 25, "标准", "渐入长夜");
            int dirtied = 0;
            view.Hud.WaveText.RegisterDirtyVerticesCallback(() => dirtied++);
            for (int i = 0; i < 20; i++)
            {
                hud.Set(80, 100, progress, 1.02f, 3, 1, 10, 12);
                hud.Stage(1, 25, "标准", "渐入长夜");
            }
            Assert.That(dirtied, Is.Zero);
            Assert.That(view.Hud.WaveText.text, Is.EqualTo("标准 · 1/25 波 · 渐入长夜"));
        }

        [Test]
        public void Hud_UpdatesSecondsAndChangedValuesAndToleratesMissingProgress()
        {
            uiNode = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/UIRoot.prefab"));
            var view = uiNode.GetComponent<EmberUiRoot>();
            var hud = new EmberHud(view.Hud, null);
            hud.Show(true);
            var progress = new RunProgress();
            hud.Set(80, 100, progress, 1.01f, 3, 1, 10, 12);
            string clock = view.Hud.Clock.text;
            hud.Set(80, 100, progress, 1.99f, 3, 1, 10, 12);
            Assert.That(view.Hud.Clock.text, Is.EqualTo(clock));
            hud.Set(70, 100, progress, 2.01f, 4, 1, 9, 12);
            Assert.That(view.Hud.Clock.text, Does.StartWith("00:02").And.Contain("击杀 4"));
            Assert.That(view.Hud.HealthText.text, Does.Contain("70 / 100"));
            Assert.DoesNotThrow(() => hud.Set(70, 100, null, 2.01f, 4, 1, 9, 12));
            Assert.That(view.Hud.Clock.text, Does.Not.Contain("武器"));
            hud.Stage(2, 25, "标准", "第二波");
            Assert.That(view.Hud.WaveText.text, Does.Contain("2/25"));
        }
    }
}
