using Emberlight.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Tests
{
    public sealed class EmberUiPrefabRepairTests
    {
        const string TestDirectory = "Assets/__EmberUiRepairTests";
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(TestDirectory), "Test directory must not contain user assets.");
            AssetDatabase.CreateFolder("Assets", "__EmberUiRepairTests");
            root = new GameObject("Test UIRoot", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            AssetDatabase.DeleteAsset(TestDirectory);
        }

        [Test]
        public void Repair_PersistsSpriteAndTextureAfterPrefabReload()
        {
            var image = AddImage("Joystick");
            image.color = new Color(.2f, .4f, .6f, .3f);
            image.rectTransform.sizeDelta = new Vector2(170, 110);
            image.gameObject.SetActive(false);

            Assert.AreEqual(1, EmberUiPrefabRepair.RepairHierarchy(root, TestDirectory + "/Sprites"));
            PrefabUtility.SaveAsPrefabAsset(root, TestDirectory + "/UI.prefab");
            var loaded = PrefabUtility.LoadPrefabContents(TestDirectory + "/UI.prefab");
            try
            {
                var saved = loaded.GetComponentInChildren<Image>(true);
                Assert.IsTrue(EditorUtility.IsPersistent(saved.sprite));
                Assert.IsTrue(EditorUtility.IsPersistent(saved.sprite.texture));
                Assert.AreEqual(EmberVisuals.Disc.rect, saved.sprite.rect);
                Assert.AreEqual(EmberVisuals.Disc.pixelsPerUnit, saved.sprite.pixelsPerUnit);
                Assert.AreEqual(image.color, saved.color);
                Assert.AreEqual(image.rectTransform.sizeDelta, saved.rectTransform.sizeDelta);
                Assert.IsFalse(saved.gameObject.activeSelf);
            }
            finally { PrefabUtility.UnloadPrefabContents(loaded); }
        }

        [Test]
        public void Repair_IsIdempotentAndPreservesManualBindings()
        {
            var joystick = AddImage("Joystick");
            var custom = AddImage("Battle HUD");
            var source = Object.Instantiate(EmberCardFrames.Get(EmberRarity.Diamond));
            AssetDatabase.CreateAsset(source, TestDirectory + "/Custom.asset");
            custom.sprite = source;

            Assert.AreEqual(1, EmberUiPrefabRepair.RepairHierarchy(root, TestDirectory + "/Sprites"));
            var path = AssetDatabase.GetAssetPath(joystick.sprite);
            var guid = AssetDatabase.AssetPathToGUID(path);
            Assert.AreEqual(0, EmberUiPrefabRepair.RepairHierarchy(root, TestDirectory + "/Sprites"));
            Assert.AreEqual(source, custom.sprite);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(joystick.sprite)));
        }

        [Test]
        public void Repair_LeavesIntentionalEmptyAndUnknownImagesAlone()
        {
            var overlay = AddImage("Overlay");
            var viewport = AddImage("Card viewport");
            var custom = AddImage("My custom decoration");
            custom.type = Image.Type.Sliced;
            Assert.AreEqual(0, EmberUiPrefabRepair.RepairHierarchy(root, TestDirectory + "/Sprites"));
            Assert.IsNull(overlay.sprite);
            Assert.IsNull(viewport.sprite);
            Assert.IsNull(custom.sprite);
        }

        [Test]
        public void Repair_UsesOriginalNineSliceBorderAndFilledBarSettings()
        {
            var panel = AddImage("Battle HUD");
            panel.type = Image.Type.Sliced;
            var fill = AddImage("Life fill");
            fill.type = Image.Type.Filled;
            fill.fillAmount = .37f;
            Assert.AreEqual(2, EmberUiPrefabRepair.RepairHierarchy(root, TestDirectory + "/Sprites"));
            Assert.AreEqual(EmberUiArt.Get(EmberUiArt.Piece.Panel).border, panel.sprite.border);
            Assert.AreEqual(EmberArt.Panel.border, fill.sprite.border);
            Assert.AreEqual(Image.Type.Filled, fill.type);
            Assert.AreEqual(.37f, fill.fillAmount);
        }

        Image AddImage(string name)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(root.transform, false);
            return image;
        }
    }
}
