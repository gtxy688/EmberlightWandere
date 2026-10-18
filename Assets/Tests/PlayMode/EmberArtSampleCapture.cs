#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed partial class EmberRuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator CleanToonCharacters_RetainPaintedEnvironmentHudAndBuffFeedback()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var ui = UiHost();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            yield return EnterRun(game, ui, 10);
            var keeperArtwork = game.Run.Keeper.Find("Keeper artwork");
            Assert.That(keeperArtwork, Is.Not.Null,
                "The selected clean cartoon keeper prefab was not connected to the main scene.");
            Assert.That(game.Run.Keeper.GetComponentInChildren<EmberKeeperRig>(), Is.Null);
            Assert.That(UnityEditor.AssetDatabase.GetAssetPath(keeperArtwork.GetComponent<SpriteRenderer>().sprite),
                Is.EqualTo("Assets/Art/CleanToonSample/Player/Player_Idle_Sheet.png"),
                "The existing keeper prefab must render the delivered idle sprite sheet.");
            var courtyard = game.Run.World.Find("Courtyard(Clone)");
            Assert.That(courtyard, Is.Not.Null);
            Assert.That(courtyard.Find("Stone floor").GetComponent<SpriteRenderer>().sprite.name,
                Is.EqualTo("courtyard-floor"));
            var hud = ui.GetComponent<EmberUiRoot>().Hud;
            Assert.That(hud.Root.GetComponent<UnityEngine.UI.Image>().sprite.name, Is.EqualTo("hud-panel"));
            Assert.That(hud.Buffs, Is.Not.Null);
            CheckTextMeshes(ui);

            // 仅测试夹具通过反射布置战况，不为录屏向 Runtime 增加后门接口。
            var combat = (EmberCombat)typeof(EmberGame).GetField("combat", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            var roster = (EmberEnemyRoster)typeof(EmberCombat).GetField("roster", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            PlaceArtSampleEnemies(roster, game, 6, false);
            foreach (var enemy in game.Run.World.GetComponentsInChildren<EmberEnemySilhouette>())
            {
                Assert.That(enemy.transform.Find("Face").gameObject.activeSelf, Is.False,
                    "The ordinary enemy must not draw a second face over the illustrated sprite.");
                Assert.That(UnityEditor.AssetDatabase.GetAssetPath(enemy.transform.Find("Cloak").GetComponent<SpriteRenderer>().sprite),
                    Is.EqualTo("Assets/Art/CleanToonSample/Textures/shade-v1.png"));
            }
            int oldRate = Application.targetFrameRate;
            float oldCaptureDelta = Time.captureDeltaTime;
            try
            {
                Application.targetFrameRate = 15;
                Time.captureDeltaTime = 1f / 15;
                using (var capture = new ArtCapture(game.Run.Cam, ui.GetComponent<Canvas>()))
                {
                    yield return RecordArtSample(capture, game, "player-two-normal", false);
                    // 密集战况使用真实的护盾词条，避免初始白板构筑提前阵亡打断录像。
                    game.Progress.OfferChoices();
                    Assert.That(game.Progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze)), Is.True);
                    PlaceArtSampleEnemies(roster, game, 28, true);
                    yield return RecordArtSample(capture, game, "player-two-dense", true);
                    EmberKeeperMotion.Step(game.Run, Vector2.one, 100);
                    yield return null;
                    capture.Screenshot("player-two-map-edge");
                }
            }
            finally
            {
                Time.captureDeltaTime = oldCaptureDelta;
                Application.targetFrameRate = oldRate;
            }
        }

        static void PlaceArtSampleEnemies(EmberEnemyRoster roster, EmberGame game, int count, bool mixed)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                var position = (Vector2)game.Run.Keeper.position + new Vector2(Mathf.Cos(angle) * 3.5f, Mathf.Sin(angle) * 5);
                int kind = mixed && i == 0 ? 4 : mixed && i == 1 ? 7 : 0;
                Assert.That(roster.Spawn(false, 1, game, forcedKind: kind, position: position, allowInView: true), Is.Not.Null);
            }
        }

        static IEnumerator RecordArtSample(ArtCapture capture, EmberGame game, string name, bool dense)
        {
            var walkingSprites = new System.Collections.Generic.HashSet<Sprite>();
            var keeperRenderer = game.Run.Keeper.Find("Keeper artwork").GetComponent<SpriteRenderer>();
            using (var encoder = capture.Encoder(name))
            {
                for (int frame = 0; frame < 90; frame++)
                {
                    Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Playing),
                        name + ", frame=" + frame + ", health=" + game.Run.Health + ", shield=" + game.Progress.Shield);
                    float angle = frame / 15f * .7f;
                    var stick = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .6f) * .4f;
                    EmberKeeperMotion.Step(game.Run, stick, Time.captureDeltaTime);
                    if (dense && frame == 5) game.TryAddBuff(EmberBuffCatalog.Power);
                    if (dense && frame == 12) game.TryAddBuff(EmberBuffCatalog.Haste);
                    if (dense && frame == 24)
                    {
                        game.TryAddBuff(EmberBuffCatalog.Power);
                        game.TryAddBuff(EmberBuffCatalog.Haste);
                        game.TryAddBuff(EmberBuffCatalog.Slow);
                    }
                    yield return null;
                    walkingSprites.Add(keeperRenderer.sprite);
                    capture.Frame(encoder);
                    if (frame == 35)
                    {
                        capture.Screenshot(name);
                        if (!dense)
                        {
                            float size = game.Run.Cam.orthographicSize;
                            try
                            {
                                game.Run.Cam.orthographicSize = 3;
                                capture.Screenshot("player-two-detail");
                            }
                            finally { game.Run.Cam.orthographicSize = size; }
                        }
                    }
                }
            }
            Assert.That(walkingSprites.Count, Is.EqualTo(2),
                "Moving must cycle exactly the two authored sprites instead of moving one static picture.");
            Assert.That(new FileInfo(capture.PathFor(name, ".mp4")).Length, Is.GreaterThan(10000));
        }

        // 直接录制主相机渲染的真实战斗与 Canvas，不拼图，也不改变正式场景资产。
        sealed class ArtCapture : IDisposable
        {
            readonly Camera camera;
            readonly Canvas canvas;
            readonly RenderTexture target, previousTarget;
            readonly Texture2D image;
            readonly RenderMode previousMode;
            readonly Camera previousCamera;
            readonly float previousDistance, previousAspect;
            readonly string directory;

            public ArtCapture(Camera camera, Canvas canvas)
            {
                this.camera = camera;
                this.canvas = canvas;
                previousTarget = camera.targetTexture;
                previousAspect = camera.aspect;
                previousMode = canvas.renderMode;
                previousCamera = canvas.worldCamera;
                previousDistance = canvas.planeDistance;
                directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/TestRun"));
                Directory.CreateDirectory(directory);
                target = new RenderTexture(540, 960, 24);
                image = new Texture2D(540, 960, TextureFormat.RGBA32, false);
                camera.targetTexture = target;
                camera.aspect = 540f / 960;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
            }

            public string PathFor(string name, string extension) => Path.Combine(directory, name + extension);

            public MediaEncoder Encoder(string name) => new MediaEncoder(PathFor(name, ".mp4"), new VideoTrackAttributes
            {
                frameRate = new MediaRational(15), width = 540, height = 960, includeAlpha = false
            });

            void ReadFrame()
            {
                var previous = RenderTexture.active;
                try
                {
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
                    image.Apply();
                }
                finally { RenderTexture.active = previous; }
            }

            public void Frame(MediaEncoder encoder)
            {
                ReadFrame();
                Assert.That(encoder.AddFrame(image), Is.True, "Unity could not encode a gameplay frame.");
            }

            public void Screenshot(string name)
            {
                ReadFrame();
                File.WriteAllBytes(PathFor(name, ".png"), image.EncodeToPNG());
            }

            public void Dispose()
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                target.Release();
                Object.Destroy(target);
                Object.Destroy(image);
            }
        }
    }
}
#endif
