using System.Reflection;
using Emberlight.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed class EmberShieldHudTests
    {
        GameObject root;
        EmberUiRoot view;

        [SetUp]
        public void SetUp()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/UIRoot.prefab"));
            view = root.GetComponent<EmberUiRoot>();
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(root); }

        [Test]
        public void Hud_ExhaustedShieldDimsAndRefillingRestoresItsTint()
        {
            var progress = new RunProgress();
            progress.OfferChoices();
            progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze));
            var hud = new EmberHud(view.Hud, null);
            hud.Set(100, 100, progress, 0, 0, 1, 1);
            Color active = view.Hud.ShieldText.color;
            Assert.That(view.Hud.Shield.fillAmount, Is.EqualTo(1));
            progress.AbsorbDamage(250);
            hud.Set(100, 100, progress, 0, 0, 1, 1);
            Assert.That(view.Hud.ShieldText.color.a, Is.LessThan(active.a), "An empty shield should be visibly inactive.");
            Assert.That(view.Hud.Shield.fillAmount, Is.Zero);
            progress.OfferChoices();
            progress.Choose(new EmberOffer(RunProgress.StatShield, EmberRarity.Bronze));
            hud.Set(100, 100, progress, 0, 0, 1, 1);
            Assert.That(view.Hud.ShieldText.color, Is.EqualTo(active));
            Assert.That(view.Hud.ShieldText.text, Does.Contain("250"));
        }

        [Test]
        public void Reconnect_AfterShieldLabelReparentingPreservesHudRolesAndManualLayout()
        {
            var previous = view.Hud;
            var text = previous.ShieldText;
            text.transform.SetParent(previous.Shield.transform.parent, false);
            text.rectTransform.anchoredPosition = new Vector2(11, 3);
            var reconnect = typeof(EmberUiPrefabSetup).GetMethod("ConfigureHud", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(reconnect, Is.Not.Null);
            Assert.DoesNotThrow(() => reconnect.Invoke(null, new object[] { view }));
            EmberShieldHudSetup.Configure(view.Hud);
            Assert.That(view.Hud.ShieldText, Is.SameAs(text));
            Assert.That(view.Hud.Clock, Is.SameAs(previous.Clock));
            Assert.That(view.Hud.WaveText, Is.SameAs(previous.WaveText));
            Assert.That(text.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(11, 3)));
        }
    }
}
