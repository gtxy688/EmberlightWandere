using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Emberlight.Tests
{
    public sealed class EmberRuntimeSmokeTests
    {
        GameObject panelHost;
        int previousSpeed, previousHint;
        bool hadSpeed, hadHint;

        [SetUp]
        public void SetUp()
        {
            hadSpeed = PlayerPrefs.HasKey("ember_speed");
            hadHint = PlayerPrefs.HasKey("ember_speed_hint_seen");
            previousSpeed = GameSpeed.Multiplier;
            previousHint = PlayerPrefs.GetInt("ember_speed_hint_seen");
            GameSpeed.Multiplier = 1;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            if (panelHost != null) Object.Destroy(panelHost);
            foreach (var audio in Object.FindObjectsOfType<EmberAudio>()) Object.Destroy(audio.gameObject);
            var game = Object.FindObjectOfType<EmberGame>();
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            GameSpeed.Multiplier = previousSpeed;
            if (!hadSpeed) PlayerPrefs.DeleteKey("ember_speed");
            if (hadHint) PlayerPrefs.SetInt("ember_speed_hint_seen", previousHint);
            else PlayerPrefs.DeleteKey("ember_speed_hint_seen");
            PlayerPrefs.Save();
            Time.timeScale = 1;
        }

        static IEnumerator WaitFor(System.Func<bool> ready, float seconds, string message)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(ready(), Is.True, message);
        }

        static Button ButtonNamed(Transform host, string name)
        {
            return host.GetComponentsInChildren<Button>().Single(b => b.name == name);
        }

        static Button CardAt(Transform host, int index)
        {
            return host.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Blessing card ")).ElementAt(index);
        }

        static void ClickLabel(EmberGame game, string label)
        {
            var button = UiHost().GetComponentsInChildren<Button>().Single(b => b.interactable &&
                b.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text == label));
            button.onClick.Invoke();
        }

        static Transform UiHost()
        {
            var canvas = Object.FindObjectsOfType<Canvas>(true).FirstOrDefault(c => c.name == "UIRoot");
            Assert.That(canvas, Is.Not.Null, "The scene must contain the editable UIRoot prefab.");
            return canvas.transform;
        }

        static IEnumerator EnterRun(EmberGame game, Transform uiHost)
        {
            ClickLabel(game, EmberCopy.MenuChoices[0]);
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.SelectLength));
            ClickLabel(game, EmberCopy.MenuChoicesWithBack[0]);
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.SelectDifficulty));
            ClickLabel(game, EmberCopy.DifficultyChoices[1]);
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.SelectSlots));
            ClickLabel(game, "确认");
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.SelectRoster));
            ButtonNamed(uiHost, "Roster 13").onClick.Invoke();
            yield return null;
            ButtonNamed(uiHost, "Confirm starter").onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Playing));
            Assert.That(game.Progress.OwnedWeaponCount, Is.EqualTo(1));
            Assert.That(game.Progress.WeaponSlots, Is.EqualTo(2));
            Assert.That(game.Run.Health, Is.EqualTo(100));
        }

        static void CheckTextMeshes(Transform host)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in host.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.ForceMeshUpdate();
                if (string.IsNullOrWhiteSpace(text.text)) continue;
                Assert.That(text.textInfo.characterCount, Is.GreaterThan(0), text.text);
                foreach (char glyph in text.text.Where(c => !char.IsWhiteSpace(c)).Distinct())
                    Assert.That(text.font.HasCharacter(glyph, true), Is.True, "Missing glyph: " + glyph);
            }
        }

        static void Capture(Canvas canvas, string name, bool includeWorld = false)
        {
            var node = new GameObject("Smoke capture", typeof(Camera));
            var camera = node.GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .04f, .055f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 10;
            if (includeWorld) camera.CopyFrom(Camera.main);
            var texture = new RenderTexture(540, 960, 24);
            var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
                image.Apply();
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/TestRun"));
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                texture.Release();
                Object.Destroy(texture);
                Object.Destroy(image);
                Object.Destroy(node);
            }
        }

        [UnityTest]
        public IEnumerator MainScene_SetupCombatUpgradePauseLossAndReturn()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            Assert.That(game, Is.Not.Null);
            var uiHost = UiHost();
            int rootId = uiHost.GetInstanceID();
            var hudRect = (RectTransform)uiHost.Find("Safe area/Battle HUD");
            hudRect.anchorMin += new Vector2(.005f, 0);
            var editedAnchor = hudRect.anchorMin;
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            Assert.That(UiHost().GetInstanceID(), Is.EqualTo(rootId), "Startup replaced the editable scene instance.");
            Assert.That(hudRect.anchorMin, Is.EqualTo(editedAnchor), "Binding overwrote a layout edit.");
            Assert.That(Object.FindObjectsOfType<Canvas>(true).Count(c => c.name == "Emberlight UI"), Is.Zero,
                "Startup must bind the scene UIRoot instead of creating a second game UI.");
            yield return EnterRun(game, uiHost);
            CheckTextMeshes(uiHost);
            Capture(uiHost.GetComponent<Canvas>(), "maintainability-hud");

            Vector3 before = game.Run.Keeper.position;
            EmberKeeperMotion.Step(game.Run, Vector2.right, .1f);
            Assert.That(game.Run.Keeper.position.x, Is.GreaterThan(before.x));
            Assert.That(game.Run.Cam.transform.position.x, Is.EqualTo(game.Run.Keeper.position.x));
            yield return WaitFor(() => game.Run.Kills > 0 || game.Phase != EmberRunPhase.Playing, 20, "Automatic attack did not produce a kill");
            Assert.That(game.Run.Kills, Is.GreaterThan(0), "Phase=" + game.Phase + ", health=" + game.Run.Health + ", elapsed=" + game.Elapsed + ", speed=" + GameSpeed.Multiplier);

            game.OnWaveCleared(1); // exercise the real flow/panel callback without waiting a whole run
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Upgrade));
            yield return new WaitForSecondsRealtime(.5f); // finish card entrance animation before capture
            CheckTextMeshes(uiHost);
            Capture(uiHost.GetComponent<Canvas>(), "maintainability-upgrade");
            int pick = System.Array.FindIndex(game.CurrentOffers, o => o.Kind == EmberOfferKind.Generic);
            Assert.That(pick, Is.GreaterThanOrEqualTo(0));
            var chosen = game.CurrentOffers[pick];
            string title = EmberOfferPresentation.Build(chosen, game.Progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails).Title;
            var chosenCard = uiHost.GetComponentsInChildren<Button>().Single(b => b.name.StartsWith("Blessing card ") &&
                b.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text == title));
            chosenCard.onClick.Invoke();
            yield return WaitFor(() => game.Phase == EmberRunPhase.Playing, 3, "Card selection did not return to combat");

            ButtonNamed(uiHost, "Pause").onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Paused));
            float elapsed = game.Elapsed;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(game.Elapsed, Is.EqualTo(elapsed));
            ClickLabel(game, "继续战斗");
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Playing));
            var firstProgress = game.Progress;
            var firstWorld = game.Run.World;
            int firstWorldId = firstWorld.GetInstanceID();
            game.Run.Vitals.Tick(EmberKeeperVitals.HurtCooldown);
            game.HurtPlayer(100000);
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Lost));
            CheckTextMeshes(uiHost);
            ClickLabel(game, EmberCopy.ReturnChoice);
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Menu));
            Assert.That(game.Run.IsActive, Is.False);

            yield return EnterRun(game, uiHost);
            Assert.That(game.Progress, Is.Not.SameAs(firstProgress));
            Assert.That(game.Run.World.GetInstanceID(), Is.Not.EqualTo(firstWorldId));
            Assert.That(firstWorld == null, Is.True, "The previous run world was not destroyed.");
            Assert.That(game.Run.Kills, Is.Zero);
            Assert.That(game.Run.Elapsed, Is.LessThan(1f));
            Assert.That(game.Run.Vitals.HurtReady, Is.True);
            CheckTextMeshes(uiHost);
            Capture(uiHost.GetComponent<Canvas>(), "redundancy-restarted-hud");
        }

        [UnityTest]
        public IEnumerator BuffDrops_HudPauseUpgradeExpirationAndRestart()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var uiHost = UiHost();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            yield return EnterRun(game, uiHost);
            game.Run.Level.BuffDropChance = 1;
            System.Func<SpriteRenderer> findDrop = () => game.Run.World.GetComponentsInChildren<SpriteRenderer>()
                .FirstOrDefault(view => view.name == "buff-drop");
            yield return WaitFor(() => game.Buffs.Active.Count > 0 || findDrop() != null, 20, "Enemy death produced no Buff drop");
            var drop = findDrop();
            if (drop != null) game.Run.Keeper.position = drop.transform.position;
            yield return WaitFor(() => game.Buffs.Active.Count > 0, 3, "Buff drop was not picked up");
            game.Run.Level.BuffDropChance = 0;

            game.TryAddBuff(EmberBuffCatalog.Power);
            game.TryAddBuff(EmberBuffCatalog.Haste);
            game.TryAddBuff(EmberBuffCatalog.Slow);
            yield return null;
            var hudView = uiHost.GetComponent<EmberUiRoot>().Hud;
            var strip = hudView.Buffs.transform;
            Assert.That(strip.Find("power/Time").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("8s"));
            Assert.That(strip.Find("haste/Time").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("8s"));
            Assert.That(strip.Find("slow/Time").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("3s"));
            Assert.That(ActiveBuffNotices(uiHost).Any(t => t.text == "迟缓 -25%"), Is.True);
            var floatingRoot = (RectTransform)uiHost.Find("Safe area/Buff floating");
            var playerScreen = (Vector2)game.Run.Cam.WorldToScreenPoint(game.Run.Keeper.position);
            var feedbackScreen = RectTransformUtility.WorldToScreenPoint(null, floatingRoot.position);
            Assert.That(feedbackScreen.x, Is.EqualTo(playerScreen.x).Within(1));
            Assert.That(feedbackScreen.y - playerScreen.y,
                Is.EqualTo(EmberBuffHud.NoticeOffset * uiHost.GetComponent<Canvas>().scaleFactor).Within(1));
            Canvas.ForceUpdateCanvases();
            var iconsRect = (RectTransform)strip;
            var encounterRect = (RectTransform)hudView.EncounterRoot.transform;
            var bossRect = (RectTransform)hudView.BossRoot.transform;
            var corners = new Vector3[4];
            iconsRect.GetWorldCorners(corners);
            float iconsBottom = corners[0].y;
            encounterRect.GetWorldCorners(corners);
            Assert.That(corners[1].y, Is.LessThan(iconsBottom), "Encounter banner overlaps Buff icons.");
            float encounterBottom = corners[0].y;
            bossRect.GetWorldCorners(corners);
            Assert.That(corners[1].y, Is.LessThan(encounterBottom), "Boss bar overlaps the encounter banner.");
            CheckTextMeshes(uiHost);
            Capture(uiHost.GetComponent<Canvas>(), "buff-icons-hud", true);
            game.Pause();
            float remaining = game.Buffs.Active[0].Remaining;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(game.Buffs.Active[0].Remaining, Is.EqualTo(remaining));
            Assert.That(strip.gameObject.activeInHierarchy, Is.False);
            Assert.That(ActiveBuffNotices(uiHost), Is.Empty);
            Assert.That(game.TryAddBuff(EmberBuffCatalog.Slow), Is.False);
            ClickLabel(game, "继续战斗");
            game.OnWaveCleared(1);
            remaining = game.Buffs.Active[0].Remaining;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(game.Buffs.Active[0].Remaining, Is.EqualTo(remaining));
            game.SelectUpgrade(game.CurrentOffers.First(offer => offer.Kind == EmberOfferKind.Generic));
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Playing));
            Assert.That(game.Buffs.Active.Count, Is.EqualTo(3), "Changing waves cleared active Buffs.");
            yield return WaitFor(() => !game.Buffs.Active.Any(buff => buff.Config == EmberBuffCatalog.Slow), 5,
                "Slow did not expire while playing");
            Assert.That(game.Buffs.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(.001f));
            Assert.That(strip.Find("slow").gameObject.activeSelf, Is.False);
            game.TryAddBuff(EmberBuffCatalog.Haste);
            Assert.That(ActiveBuffNotices(uiHost).Any(t => t.text == "刷新 · 疾行 +30%"), Is.True);
            Assert.That(strip.Find("haste/Ring").GetComponent<Image>().fillAmount, Is.EqualTo(1));
            Capture(uiHost.GetComponent<Canvas>(), "buff-icons-refreshed", true);
            GameSpeed.Multiplier = 5;
            GameSpeed.Apply();
            yield return new WaitForSecondsRealtime(.65f);
            Assert.That(ActiveBuffNotices(uiHost).Length, Is.GreaterThan(0), "Game speed shortened the real-time pickup notice.");
            GameSpeed.Multiplier = 1;
            GameSpeed.Apply();
            yield return WaitFor(() => ActiveBuffNotices(uiHost).Length == 0, 2, "The floating notice did not dismiss itself.");
            game.Run.Vitals.Tick(1);
            game.HurtPlayer(100000);
            ClickLabel(game, EmberCopy.ReturnChoice);
            Assert.That(game.Buffs.Active, Is.Empty);
            yield return EnterRun(game, uiHost);
            Assert.That(game.Buffs.Active, Is.Empty);
            Assert.That(game.Buffs.DamageMultiplier, Is.EqualTo(1));
            Assert.That(game.Buffs.MoveSpeedMultiplier, Is.EqualTo(1));
            yield return null;
            Assert.That(strip.gameObject.activeSelf, Is.False);
            Assert.That(ActiveBuffNotices(uiHost), Is.Empty);
        }

        [UnityTest]
        public IEnumerator PickupArtwork_RendersAtGameScaleAndStillPicksUp()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var uiHost = UiHost();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            yield return EnterRun(game, uiHost);
            var pool = new EmberPool();
            pool.Bind(game.Run.World);
            var drops = new EmberDrops(pool, game);
            var level = new LevelConfig { HealDropChance = 1, BuffDropChance = 0 };
            drops.Bind(game.Run.World, level);
            try
            {
                drops.OnDeath(new Vector2(-2, 2.5f), 0, EnemyAffix.None, new ArtworkRandom(0));
                level.HealDropChance = 0;
                level.BuffDropChance = 1;
                drops.OnDeath(new Vector2(-.35f, 2.5f), 0, EnemyAffix.None, new ArtworkRandom(0));
                drops.OnDeath(new Vector2(1.65f, 2.5f), 0, EnemyAffix.None, new ArtworkRandom(.75));
                var views = game.Run.World.GetComponentsInChildren<SpriteRenderer>()
                    .Where(view => view.name == "healing-drop" || view.name == "buff-drop").ToArray();
                Assert.That(views.Length, Is.EqualTo(3));
                foreach (var view in views)
                {
                    Assert.That(view.sprite.name, Does.StartWith("pickup-"));
                    Assert.That(view.bounds.size.x, Is.EqualTo(.62f).Within(.001f));
                    Assert.That(view.color, Is.EqualTo(Color.white));
                }
                yield return null;
                Capture(uiHost.GetComponent<Canvas>(), "pickup-art-in-game", true);
                game.HurtPlayer(40);
                float injured = game.Run.Health;
                foreach (var view in views)
                {
                    game.Run.Keeper.position = view.transform.position;
                    drops.Tick(.01f, game.Run.Keeper.position);
                    if (view.sprite.name == "pickup-power")
                        Assert.That(ActiveBuffNotices(uiHost).Any(t => t.text == "增伤 +30%"), Is.True);
                    else if (view.sprite.name == "pickup-haste")
                        Assert.That(ActiveBuffNotices(uiHost).Any(t => t.text == "疾行 +30%"), Is.True);
                }
                yield return null;
                CheckTextMeshes(uiHost);
                Capture(uiHost.GetComponent<Canvas>(), "buff-icons-pickup", true);
                Assert.That(game.Run.Health, Is.GreaterThan(injured));
                Assert.That(game.Buffs.Active.Count, Is.EqualTo(2));
                Assert.That(game.Buffs.DamageMultiplier, Is.EqualTo(1.3f).Within(.001f));
                Assert.That(game.Buffs.MoveSpeedMultiplier, Is.EqualTo(1.3f).Within(.001f));
                Assert.That(views.All(view => !view.gameObject.activeSelf), Is.True);
            }
            finally { drops.Clear(); pool.Clear(true); }
        }

        static TextMeshProUGUI[] ActiveBuffNotices(Transform uiHost)
        {
            return uiHost.Find("Safe area/Buff floating").GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(text => text.gameObject.activeSelf).ToArray();
        }

        sealed class ArtworkRandom : System.Random
        {
            readonly double value;
            public ArtworkRandom(double value) { this.value = value; }
            public override double NextDouble() => value;
        }

        [UnityTest]
        public IEnumerator IntroPrefab_PreservesViewAndLayoutAndWaitsForReady()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var canvas = Object.FindObjectsOfType<Canvas>(true).SingleOrDefault(c => c.name == "IntroRoot");
            Assert.That(canvas, Is.Not.Null, "The loading view must already exist as a scene prefab instance.");
            int viewId = canvas.GetInstanceID();
            var background = (RectTransform)canvas.transform.Find("Night sky");
            background.anchorMin += new Vector2(.005f, 0);
            var editedAnchor = background.anchorMin;
            // TMP may add a submesh for its Latin fallback font; it is renderer internals, not an authored control.
            var nodes = canvas.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<TMP_SubMeshUI>() == null)
                .Select(t => t.GetInstanceID()).OrderBy(id => id).ToArray();
            float began = Time.unscaledTime; // Match the loader's frame-based, unscaled clock.
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null, 2, "Intro controller did not start");
            var intro = game.GetComponent<EmberIntro>();
            var phase = game.Phase;
            intro.Continue();
            Assert.That(game.Phase, Is.EqualTo(phase), "Continue must wait until loading is ready.");
            Assert.That(intro.Ready, Is.False);
            yield return WaitFor(() => intro.Ready, 20, "Loading did not finish");
            Assert.That(Time.unscaledTime - began, Is.GreaterThanOrEqualTo(4.9f));
            Assert.That(canvas.GetInstanceID(), Is.EqualTo(viewId));
            Assert.That(background.anchorMin, Is.EqualTo(editedAnchor));
            Assert.That(canvas.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<TMP_SubMeshUI>() == null)
                .Select(t => t.GetInstanceID()).OrderBy(id => id), Is.EqualTo(nodes), "Loading must preserve authored controls.");
            Assert.That(Object.FindObjectsOfType<Canvas>(true).Count(c => c.name == "Loading and title"), Is.Zero);
            CheckTextMeshes(canvas.transform);
            Capture(canvas, "prefab-intro-ready");
            intro.Continue();
            intro.Continue();
            yield return null;
            Assert.That(canvas != null, Is.True, "Continue must hide the loading prefab instead of destroying it.");
            Assert.That(canvas.gameObject.activeSelf, Is.False);
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Menu));
        }

        [UnityTest]
        public IEnumerator PrefabPanels_ReuseViewsAndReplaceCallbacksWithoutRebuilding()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            var view = UiHost().GetComponent<EmberUiRoot>();
            int nodes = view.GetComponentsInChildren<RectTransform>(true).Length;
            int confirmed = 0, selectedSlots = 0;
            view.GodsSelect.ShowSlots(_ => confirmed += 10);
            view.GodsSelect.ShowSlots(n => { confirmed++; selectedSlots = n; });
            ButtonNamed(view.transform, "Slot 3").onClick.Invoke();
            var confirm = ButtonNamed(view.transform, "Confirm");
            confirm.onClick.Invoke();
            confirm.onClick.Invoke();
            Assert.That(confirmed, Is.EqualTo(1));
            Assert.That(selectedSlots, Is.EqualTo(3));

            int starter = -1;
            view.GodsSelect.ShowRoster(3, _ => confirmed += 10);
            view.GodsSelect.ShowRoster(3, ids => { confirmed++; starter = ids[0]; });
            Assert.That(ButtonNamed(view.transform, "Confirm starter").interactable, Is.False);
            ButtonNamed(view.transform, "Roster 14").onClick.Invoke();
            confirm = ButtonNamed(view.transform, "Confirm starter");
            confirm.onClick.Invoke();
            confirm.onClick.Invoke();
            Assert.That(confirmed, Is.EqualTo(2));
            Assert.That(starter, Is.EqualTo(14));

            view.Settings.Show(() => confirmed += 10, false);
            view.Settings.Show(() => confirmed++, false);
            ButtonNamed(view.transform, "Button 返回").onClick.Invoke();
            Assert.That(confirmed, Is.EqualTo(3));

            var progress = new RunProgress();
            var offers = Enumerable.Repeat(new EmberOffer(RunProgress.StatShield, EmberRarity.Gold), 7).ToArray();
            view.Upgrade.Show(offers, progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, _ => confirmed += 10);
            CardAt(view.transform, 0).onClick.Invoke();
            view.Upgrade.Hide();
            view.Upgrade.Show(offers.Take(3).ToArray(), progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, _ => confirmed++);
            var card = CardAt(view.transform, 0);
            card.onClick.Invoke();
            card.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(confirmed, Is.EqualTo(4), "An old callback or duplicate selection fired.");
            Assert.That(view.GetComponentsInChildren<RectTransform>(true).Length, Is.EqualTo(nodes), "A prefab panel was rebuilt.");
            view.Upgrade.Hide();
        }

        [UnityTest]
        public IEnumerator UpgradePanel_ReuseCancelsOldSelectionAndUsesLatestCallbackOnce()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            panelHost = UiHost().gameObject;
            var canvas = panelHost.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var panel = panelHost.GetComponent<EmberUiRoot>().Upgrade;
            var progress = new RunProgress();
            var offers = Enumerable.Range(0, 7).Select(i => new EmberOffer(RunProgress.StatShield, EmberRarity.Gold)).ToArray();
            var ids = panelHost.GetComponentsInChildren<Transform>(true).Select(t => t.GetInstanceID()).OrderBy(id => id).ToArray();
            int picked = 0;
            for (int i = 0; i < 20; i++)
            {
                int count = i % 3 == 0 ? 7 : (i % 3 == 1 ? 3 : 4);
                panel.Show(offers.Take(count).ToArray(), progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, o => picked++);
                Canvas.ForceUpdateCanvases();
                for (int card = 0; card < count; card++)
                {
                    var rect = (RectTransform)CardAt(panelHost.transform, card).transform;
                    Assert.That(rect.rect.height, Is.GreaterThan(0));
                    if (count > 4)
                    {
                        Assert.That(rect.rect.height, Is.EqualTo(132f).Within(.1f));
                        if (card > 0)
                        {
                            var previous = (RectTransform)CardAt(panelHost.transform, card - 1).transform;
                            Assert.That(previous.anchoredPosition.y - rect.anchoredPosition.y, Is.EqualTo(144f).Within(.1f));
                        }
                    }
                    else if (card > 0)
                    {
                        var previous = (RectTransform)CardAt(panelHost.transform, card - 1).transform;
                        Assert.That(previous.anchorMin.y, Is.GreaterThan(rect.anchorMax.y), "Direct-layout cards overlap.");
                    }
                }
                panel.Hide();
            }
            Assert.That(panelHost.GetComponentsInChildren<Transform>(true).Select(t => t.GetInstanceID()).OrderBy(id => id), Is.EqualTo(ids));
            panel.Show(offers, progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, o => picked++);
            var scroll = panelHost.GetComponentInChildren<ScrollRect>();
            scroll.content.anchoredPosition = new Vector2(0, 200);
            scroll.velocity = new Vector2(0, 100);
            CardAt(panelHost.transform, 0).onClick.Invoke();
            panel.Hide();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(picked, Is.Zero);
            panel.Show(offers, progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, o => picked += 10);
            Assert.That(scroll.content.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(scroll.velocity, Is.EqualTo(Vector2.zero));
            var button = CardAt(panelHost.transform, 0);
            button.onClick.Invoke();
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(picked, Is.EqualTo(10));
        }
    }
}
