#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed partial class EmberRuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator StageBoss_RealSkillAppliesSlowWithShieldAndShowsExistingFeedback()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var ui = UiHost();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready, 20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            yield return EnterRun(game, ui);
            for (int i = 0; i < 4; i++) { game.Progress.OfferChoices(); game.Progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Diamond)); }
            var combat = (EmberCombat)typeof(EmberGame).GetField("combat", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            var roster = (EmberEnemyRoster)typeof(EmberCombat).GetField("roster", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            var waves = (EmberWaveDirector)typeof(EmberCombat).GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            roster.Clear(false);
            while (waves.Wave < 10) waves.Advance();
            yield return WaitFor(() => combat.BossAlive, 7, "Boss did not arrive after its warning");
            // 固定遭遇，避免随机小兵接触触发无敌帧，让用例只验证 Boss 技能的真实命中。
            // 群战的移动与数量另由 BossSwarmScene 覆盖，不修改正式敌人配置。
            int supportIndex = 0;
            foreach (var enemy in roster.All.Where(e => !e.IsBoss))
            {
                float angle = supportIndex++ * 2.4f;
                enemy.view.position = (Vector2)game.Run.Keeper.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 7;
                enemy.speed = 0;
            }
            combat.Boss.view.position = (Vector2)game.Run.Keeper.position + Vector2.up * 5;
            Assert.That(game.HurtReady, Is.True);
            yield return WaitFor(() => game.Buffs.Active.Any(b => b.Config == EmberBuffCatalog.Slow), 15, "Stage Boss skills did not apply Slow");
            Assert.That(game.Run.Health, Is.EqualTo(100), "Shield absorption must still count as a debuff hit.");
            Assert.That(game.Buffs.MoveSpeedMultiplier, Is.EqualTo(.75f).Within(.001f));
            CheckTextMeshes(ui);
            Capture(ui.GetComponent<Canvas>(), "boss-slow-feedback", true);
            game.Pause();
            float remaining = game.Buffs.Active.Single(b => b.Config == EmberBuffCatalog.Slow).Remaining;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(game.Buffs.Active.Single(b => b.Config == EmberBuffCatalog.Slow).Remaining, Is.EqualTo(remaining));
        }

        [UnityTest]
        public IEnumerator UpgradeDescriptions_RenderAllCardFamiliesWithoutOverflow()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Emberlight.unity");
            var game = Object.FindObjectOfType<EmberGame>();
            var ui = UiHost();
            yield return WaitFor(() => game.GetComponent<EmberIntro>() != null && game.GetComponent<EmberIntro>().Ready, 20, "Loading did not finish");
            game.GetComponent<EmberIntro>().Continue();
            yield return EnterRun(game, ui);
            game.OnWaveCleared(1);
            var panel = ui.GetComponent<EmberUiRoot>().Upgrade;
            EmberOffer[][] pages =
            {
                new[] { new EmberOffer(2, EmberRarity.Gold), new EmberOffer(3, EmberRarity.Gold), new EmberOffer(4, EmberRarity.Gold) },
                new[] { new EmberOffer(0, EmberRarity.Gold, 13), new EmberOffer(1, EmberRarity.Gold, 14), new EmberOffer(1, EmberRarity.Gold, 15) },
                new[] { new EmberOffer(13, EmberRarity.Bronze), new EmberOffer(14, EmberRarity.Bronze), new EmberOffer(15, EmberRarity.Bronze) },
                new[] { new EmberOffer(10, EmberRarity.Bronze), new EmberOffer(11, EmberRarity.Bronze), new EmberOffer(12, EmberRarity.Bronze) },
                new[] { new EmberOffer(20, EmberRarity.Diamond), new EmberOffer(21, EmberRarity.Diamond), new EmberOffer(22, EmberRarity.Diamond) },
                new[] { new EmberOffer(23, EmberRarity.Diamond), new EmberOffer(24, EmberRarity.Diamond), new EmberOffer(25, EmberRarity.Diamond) },
                new[] { new EmberOffer(30, EmberRarity.Gold), new EmberOffer(31, EmberRarity.Gold), new EmberOffer(32, EmberRarity.Gold) },
                new[] { new EmberOffer(33, EmberRarity.Gold), new EmberOffer(34, EmberRarity.Gold), new EmberOffer(35, EmberRarity.Gold) }
            };
            for (int page = 0; page < pages.Length; page++)
            {
                panel.Show(pages[page], game.Progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, offer => { });
                yield return new WaitForSecondsRealtime(.5f);
                CheckTextMeshes(ui);
                foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>())
                    Assert.That(text.isTextOverflowing, Is.False, "Card text is clipped: " + text.text);
                Capture(ui.GetComponent<Canvas>(), "card-copy-" + page);
            }
            // 同样检查候选较多时使用的紧凑滚动卡面。
            panel.Show(pages[0].Concat(pages[4]).ToArray(), game.Progress, EmberCopy.UpgradeNames, EmberCopy.UpgradeDetails, offer => { });
            yield return new WaitForSecondsRealtime(.5f);
            CheckTextMeshes(ui);
            Capture(ui.GetComponent<Canvas>(), "card-copy-scroll");
            foreach (var text in panel.GetComponentsInChildren<TextMeshProUGUI>())
                Assert.That(text.isTextOverflowing, Is.False, "Compact card text is clipped: " + text.text + " (font=" + text.fontSize
                    + ", rect=" + text.rectTransform.rect.size + ", preferred=" + text.GetPreferredValues() + ")");
        }
    }
}
#endif
