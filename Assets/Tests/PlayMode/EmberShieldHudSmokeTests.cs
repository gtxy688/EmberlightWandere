using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Emberlight.Tests
{
    public sealed partial class EmberRuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator ShieldHud_GrantDamageExhaustionRefillAndRestart()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var ui = UiHost();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready,
                20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            yield return EnterRun(game, ui);
            yield return null;

            var view = ui.GetComponent<EmberUiRoot>().Hud;
            var canvas = ui.GetComponent<Canvas>();
            int shieldId = view.Shield.GetInstanceID();
            float emptyAlpha = view.ShieldText.color.a;
            Assert.That(view.Shield.transform.parent.Find("Shield icon"), Is.Null);
            Assert.That(view.Shield.fillAmount, Is.Zero);
            Assert.That(view.ShieldText.text, Is.EqualTo("护盾 0"));
            Capture(canvas, "shield-empty", true);

            GrantShield(game.Progress);
            yield return null;
            Color activeText = view.ShieldText.color;
            Assert.That(view.ShieldText.text, Is.EqualTo("护盾 250"));
            Assert.That(view.Shield.fillAmount, Is.EqualTo(1));
            Assert.That(activeText.a, Is.GreaterThan(emptyAlpha));
            Capture(canvas, "shield-full", true);

            game.HurtPlayer(100);
            yield return null;
            Assert.That(game.Run.Health, Is.EqualTo(100), "Shield damage must not reduce health.");
            Assert.That(view.ShieldText.text, Is.EqualTo("护盾 150"));
            Assert.That(view.Shield.fillAmount, Is.EqualTo(.6f).Within(.001f));
            Capture(canvas, "shield-partial", true);

            game.Run.Vitals.Tick(EmberKeeperVitals.HurtCooldown);
            game.HurtPlayer(200);
            yield return null;
            Assert.That(game.Run.Health, Is.EqualTo(50), "Only damage beyond the remaining shield reaches health.");
            Assert.That(view.Shield.fillAmount, Is.Zero);
            Assert.That(view.ShieldText.text, Is.EqualTo("护盾 0"));
            Assert.That(view.ShieldText.color.a, Is.EqualTo(emptyAlpha));
            Capture(canvas, "shield-exhausted", true);

            GrantShield(game.Progress);
            yield return null;
            Assert.That(view.ShieldText.color, Is.EqualTo(activeText));
            Assert.That(view.Shield.fillAmount, Is.EqualTo(.5f), "Keep the existing cumulative-grant capacity rule.");
            CheckTextMeshes(ui);
            Assert.That(view.ShieldText.isTextOverflowing, Is.False);
            Assert.That(view.WaveText.isTextOverflowing, Is.False);
            Capture(canvas, "shield-refilled", true);

            ButtonNamed(ui, "Pause").onClick.Invoke();
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Paused));
            ClickLabel("继续战斗");
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Playing));
            game.Run.Vitals.Tick(EmberKeeperVitals.HurtCooldown);
            game.HurtPlayer(100000);
            Assert.That(game.Phase, Is.EqualTo(EmberRunPhase.Lost));
            ClickLabel(EmberCopy.ReturnChoice);
            yield return EnterRun(game, ui);
            yield return null;
            Assert.That(view.Shield.fillAmount, Is.Zero);
            Assert.That(view.ShieldText.text, Is.EqualTo("护盾 0"));
            Assert.That(view.ShieldText.color.a, Is.EqualTo(emptyAlpha));
            Assert.That(view.Shield.GetInstanceID(), Is.EqualTo(shieldId));
            Assert.That(view.Shield.transform.parent.Find("Shield icon"), Is.Null);
            CheckTextMeshes(ui);
            Capture(canvas, "shield-restarted", true);
        }

        static void GrantShield(RunProgress progress)
        {
            progress.OfferChoices();
            Assert.That(progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze)), Is.True);
        }
    }
}
