using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberEnemySkinTests
    {
        const string Prefab = "Assets/Art/CleanToonSample/Prefabs/Shade.prefab";

        [Test]
        public void IllustratedSkin_HitFlashRestoresAndDoesNotLeakThroughPoolReuse()
        {
            var root = new GameObject("Skin hit feedback test");
            try
            {
                var pool = new EmberPool();
                pool.Bind(root.transform, shadePrefab: AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
                var view = pool.RentEnemy("shade", root.transform, Color.gray);
                var silhouette = view.GetComponent<EmberEnemySilhouette>();
                silhouette.Apply(0, Color.gray);
                var body = silhouette.WholeBody;
                Assert.That(body.sharedMaterial.HasProperty("_HitFlash"), Is.True,
                    "A white vertex tint cannot flash an already white-tinted illustrated sprite.");
                Assert.That(EditorUtility.IsPersistent(body.sharedMaterial), Is.True);
                var enemy = new EmberEnemy { view = view, kind = 0, flash = .1f };
                var visual = new EmberEnemyVisual(enemy, root.transform);
                var properties = new MaterialPropertyBlock();
                visual.Tick(1, LevelConfig.Default);
                body.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_HitFlash"), Is.GreaterThan(0));
                enemy.flash = 0;
                visual.Tick(1.1f, LevelConfig.Default);
                body.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_HitFlash"), Is.Zero);
                Assert.That(properties.GetTexture("_MainTex"), Is.SameAs(body.sprite.texture),
                    "Resetting hit feedback must preserve the current role's texture binding.");
                enemy.flash = .1f;
                visual.Tick(1.2f, LevelConfig.Default);
                // 刻意带着高亮归还，确认 Apply 自身也能兜住上次租用的 Renderer 覆写。
                // 正常 Release 的 Clear 清理在 PlayMode 的真实名册回收中检查。
                pool.Release("shade", view.gameObject);
                var reused = pool.RentEnemy("shade", root.transform, Color.gray);
                Assert.That(reused, Is.SameAs(view));
                silhouette.Apply(4, Color.gray);
                body.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_HitFlash"), Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Rebuild_PreservesSerializedSpriteIdentitiesAndPrefabGuid()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            string prefabGuid = AssetDatabase.AssetPathToGUID(Prefab);
            var original = new SerializedObject(prefab.GetComponent<EmberEnemySilhouette>()).FindProperty("skins");
            var ids = new string[original.arraySize];
            for (int i = 0; i < ids.Length; i++)
            {
                var sprite = original.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue;
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long fileId), Is.True);
                ids[i] = guid + ":" + fileId;
            }
            Emberlight.Editor.EmberEnemySkinSetup.Build();
            Assert.That(AssetDatabase.AssetPathToGUID(Prefab), Is.EqualTo(prefabGuid));
            var rebuilt = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)
                .GetComponent<EmberEnemySilhouette>()).FindProperty("skins");
            Assert.That(rebuilt.arraySize, Is.EqualTo(ids.Length));
            for (int i = 0; i < ids.Length; i++)
            {
                var sprite = rebuilt.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue;
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long fileId), Is.True);
                Assert.That(guid + ":" + fileId, Is.EqualTo(ids[i]), "Rebuilding must not break persistent role sprite references.");
            }
        }

        [Test]
        public void SkinMap_CoversEveryEnemyKindIncludingBossWithPersistentSprites()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var view = new SerializedObject(prefab.GetComponent<EmberEnemySilhouette>());
            var skins = view.FindProperty("skins");
            Assert.That(skins, Is.Not.Null, "Each enemy role needs its own serialized illustrated skin.");
            Assert.That(skins.arraySize, Is.EqualTo(9));
            var kinds = new int[9];
            var sprites = new Sprite[9];
            for (int i = 0; i < skins.arraySize; i++)
            {
                var entry = skins.GetArrayElementAtIndex(i);
                kinds[i] = entry.FindPropertyRelative("kind").intValue;
                sprites[i] = entry.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
                Assert.That(entry.FindPropertyRelative("height").floatValue, Is.GreaterThan(0));
                Assert.That(sprites[i], Is.Not.Null);
                Assert.That(EditorUtility.IsPersistent(sprites[i]), Is.True);
                Assert.That(AssetDatabase.GetAssetPath(sprites[i]), Does.StartWith("Assets/Art/CleanToonSample/Enemies/"));
            }
            Assert.That(kinds, Is.EquivalentTo(Enumerable.Range(0, 9)));
            Assert.That(sprites.Distinct().Count(), Is.EqualTo(9), "Roles must have distinct artwork rather than one tinted sprite.");
            Assert.That(view.FindProperty("directionalShieldSprite").objectReferenceValue, Is.Not.Null);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)]
        public void EverySkin_RestoresAfterPoolReuseAndHidesLegacyFace(int kind)
        {
            var root = new GameObject("Skin pool test");
            try
            {
                var pool = new EmberPool();
                pool.Bind(root.transform, shadePrefab: AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
                string key = kind == 3 ? "boss" : "shade";
                var first = pool.RentEnemy(key, root.transform, Color.gray);
                var view = first.GetComponent<EmberEnemySilhouette>();
                Assert.That(view, Is.Not.Null, "Illustrated Boss and trash must use a skin-capable prefab.");
                view.Apply(kind, Color.gray);
                var body = first.Find("Cloak").GetComponent<SpriteRenderer>();
                var sprite = body.sprite;
                var scale = body.transform.localScale;
                Assert.That(AssetDatabase.GetAssetPath(sprite), Does.StartWith("Assets/Art/CleanToonSample/Enemies/"));
                Assert.That(first.Find("Face").gameObject.activeSelf, Is.False);
                Assert.That(first.Find("Silhouette"), Is.Null,
                    "Whole-body skins must not allocate the old hidden role decorations for every spawn.");
                Assert.That(body.color, Is.EqualTo(Color.white));
                body.flipX = true;
                body.color = Color.red;
                body.transform.localScale *= 2;
                pool.Release(key, first.gameObject);
                var second = pool.RentEnemy(key, root.transform, Color.blue);
                Assert.That(second, Is.SameAs(first));
                view.Apply(kind, Color.blue);
                Assert.That(body.sprite, Is.SameAs(sprite));
                Assert.That(body.flipX, Is.False);
                Assert.That(body.color, Is.EqualTo(Color.white));
                Assert.That(body.transform.localScale, Is.EqualTo(scale));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
