#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed partial class EmberRuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator StageBoss_WithSupportsRetiresSurvivorsAndOpensUpgrade()
        {
            yield return BossSwarmScene(false);
        }

        [UnityTest]
        public IEnumerator FinalBoss_WithSupportsRetiresSurvivorsAndWins()
        {
            yield return BossSwarmScene(true);
        }

        static IEnumerator BossSwarmScene(bool final)
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var ui = UiHost();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            yield return EnterRun(game, ui, 13);

            // 测试夹具只跳过前面的波次时间，不改变正式配置、场景或招式实现。
            var combat = (EmberCombat)typeof(EmberGame).GetField("combat", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            var roster = (EmberEnemyRoster)typeof(EmberCombat).GetField("roster", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            var waves = (EmberWaveDirector)typeof(EmberCombat).GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            roster.Clear(false);
            for (int i = 0; i < 4; i++)
            {
                game.Progress.OfferChoices();
                Assert.That(game.Progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Diamond)), Is.True);
            }
            if (!final) CaptureEnemyGallery(game, roster, ui.GetComponent<Canvas>());
            int bossWave = final ? 25 : 10;
            while (waves.Wave < bossWave) waves.Advance();
            Assert.That(combat.EnemyCount, Is.Zero, "Boss warning must begin on an empty battlefield.");
            yield return WaitFor(() => combat.BossAlive, 7, "Boss did not arrive after its real warning countdown");
            int target = final ? 28 : 18;
            Assert.That(roster.Count, Is.EqualTo(target + 1));
            Assert.That(roster.All.Where(e => !e.IsBoss).All(e => e.kind >= 0 && e.kind <= 2), Is.True);
            Assert.That(combat.Boss.view.GetComponent<EmberEnemySilhouette>().UsesWholeBodySkin, Is.True);
            Assert.That(combat.Boss.view.Find("Boss adornment"), Is.Null, "The illustrated Boss must not acquire a duplicate crown/lantern.");

            int oldRate = Application.targetFrameRate;
            float oldDelta = Time.captureDeltaTime;
            try
            {
                Application.targetFrameRate = 15;
                Time.captureDeltaTime = 1f / 15;
                string name = final ? "boss-swarm-final" : "boss-swarm-stage";
                using (var capture = new ArtCapture(game.Run.Cam, ui.GetComponent<Canvas>()))
                using (var encoder = capture.Encoder(name))
                {
                    int mostVisible = -1;
                    for (int frame = 0; frame < 135; frame++)
                    {
                        Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Playing));
                        Assert.That(roster.Count, Is.LessThanOrEqualTo(target + 1), "Refill exceeded the selected density.");
                        float angle = frame / 15f * .55f;
                        EmberKeeperMotion.Step(game.Run, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .35f, Time.captureDeltaTime);
                        yield return null;
                        capture.Frame(encoder);
                        // 选择 Boss 和余兵都在镜头内的代表画面，避免固定时刻恰好处于屏外冲锋。
                        var bossPoint = game.Run.Cam.WorldToViewportPoint(combat.Boss.view.position);
                        if (bossPoint.x > .05f && bossPoint.x < .95f && bossPoint.y > .1f && bossPoint.y < .7f)
                        {
                            int visible = roster.All.Count(e =>
                            {
                                var point = game.Run.Cam.WorldToViewportPoint(e.view.position);
                                return point.x > 0 && point.x < 1 && point.y > 0 && point.y < .8f;
                            });
                            if (visible > mostVisible) { mostVisible = visible; capture.Screenshot(name); }
                        }
                    }
                    Assert.That(mostVisible, Is.GreaterThan(1), "Preview must show the illustrated Boss alongside its minions.");
                }
            }
            finally { Time.captureDeltaTime = oldDelta; Application.targetFrameRate = oldRate; }

            Assert.That(roster.Count, Is.GreaterThan(1), "The fight should contain a Boss and living supports.");
            // 保住余兵以区分正常击杀与退场；同帧待展开的母体请求也必须作废。
            foreach (var enemy in roster.All.Where(e => !e.IsBoss)) enemy.hp = 100000;
            var broods = (System.Collections.Generic.List<Vector2>)typeof(EmberCombat)
                .GetField("pendingBroods", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            broods.Add(Vector2.zero);
            int kills = game.Run.Kills;
            int score = game.Progress.Score;
            combat.Boss.hp = 0;
            combat.Boss.dead = true;
            yield return WaitFor(() => game.Phase != EmberRunPhase.Playing, 2, "Boss death did not leave combat");
            Assert.That(game.Phase, Is.EqualTo(final ? EmberRunPhase.Won : EmberRunPhase.Upgrade));
            Assert.That(roster.Count, Is.Zero, "Surviving supports must retire rather than block the result.");
            Assert.That(broods, Is.Empty);
            Assert.That(game.Run.Kills, Is.EqualTo(kills + 1), "Retired supports are not kills.");
            Assert.That(game.Progress.Score, Is.EqualTo(score + 30), "Retired supports must not create extra score.");
            CheckTextMeshes(ui);
        }

        static void CaptureEnemyGallery(EmberGame game, EmberEnemyRoster roster, Canvas canvas)
        {
            var keeper = game.Run.Keeper.position;
            float size = game.Run.Cam.orthographicSize;
            try
            {
                game.Run.Keeper.position = new Vector3(0, -5, 0);
                game.Run.Cam.orthographicSize = 7.4f;
                int[] kinds = { 0, 1, 2, 4, 5, 6, 7, 8, 3 };
                for (int i = 0; i < kinds.Length; i++)
                {
                    int kind = kinds[i];
                    var position = new Vector2((i % 3 - 1) * 2.6f, 2.7f - i / 3 * 2.5f);
                    var enemy = roster.Spawn(kind == 3, 1, game, forcedKind: kind, position: position, allowInView: true);
                    Assert.That(enemy, Is.Not.Null);
                    enemy.view.position = position; // Boss 正式落点不使用传入位置；仅为这张对照图摆放。
                    enemy.facing = 180;
                    enemy.visual.Tick(.1f, game.Run.Level);
                    Assert.That(enemy.view.GetComponent<EmberEnemySilhouette>().WholeBody.sprite, Is.Not.Null);
                }
                using (var capture = new ArtCapture(game.Run.Cam, canvas))
                {
                    capture.Screenshot("enemy-skins-gallery");
                    var ordinary = roster.All.First(e => e.kind == 0);
                    ordinary.flash = .1f;
                    ordinary.visual.Tick(.2f, game.Run.Level);
                    capture.Screenshot("enemy-skin-hit-flash");
                }
                var flashedBody = roster.All.First(e => e.kind == 0).view.GetComponent<EmberEnemySilhouette>().WholeBody;
                roster.Clear(false);
                var properties = new MaterialPropertyBlock();
                flashedBody.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_HitFlash"), Is.Zero, "Actual roster release must clear hit flash before pooling.");
            }
            finally
            {
                roster.Clear(false);
                game.Run.Keeper.position = keeper;
                game.Run.Cam.orthographicSize = size;
            }
        }
    }
}
#endif
