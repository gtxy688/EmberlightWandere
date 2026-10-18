using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberArtSampleTests
    {
        // 要抓的是 Prefab 引用临时图片、遗漏外观或丢失工程内依赖，不能只检查 PNG 在磁盘上。
        [TestCase("HandPaintedSample", "Keeper")]
        [TestCase("HandPaintedSample", "KeeperWalk")]
        [TestCase("HandPaintedSample", "Shade")]
        [TestCase("HandPaintedSample", "Courtyard")]
        [TestCase("CleanToonSample", "Keeper")]
        [TestCase("CleanToonSample", "Shade")]
        public void SamplePrefab_HasPersistentSpritesAndNoMissingReferences(string family, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/" + family + "/Prefabs/" + name + ".prefab");
            Assert.That(prefab, Is.Not.Null, "The playable art sample prefab is missing.");
            var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sprite, Is.Not.Null, renderer.name);
                Assert.That(EditorUtility.IsPersistent(renderer.sprite), Is.True, renderer.name);
                Assert.That(renderer.sprite.texture, Is.Not.Null, renderer.name);
            }
        }

        [TestCase(1, "HandPaintedSample")]
        [TestCase(2, "HandPaintedSample")]
        [TestCase(4, "HandPaintedSample")]
        [TestCase(5, "HandPaintedSample")]
        [TestCase(6, "HandPaintedSample")]
        [TestCase(7, "HandPaintedSample")]
        [TestCase(8, "HandPaintedSample")]
        [TestCase(4, "CleanToonSample")]
        [TestCase(7, "CleanToonSample")]
        public void SharedEnemyPool_NormalAndSpecialReuseRestoreTheirOwnAppearance(int specialKind, string family)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/" + family + "/Prefabs/Shade.prefab");
            var world = new GameObject("Art pool regression");
            try
            {
                var pool = new EmberPool();
                pool.Bind(world.transform, shadePrefab: prefab);
                var first = pool.RentEnemy("shade", world.transform, Color.gray);
                var silhouette = first.GetComponent<EmberEnemySilhouette>();
                silhouette.Apply(0, Color.gray);
                var body = first.Find("Cloak").GetComponent<SpriteRenderer>();
                var normalSprite = body.sprite;
                var normalColor = body.color;
                var normalPosition = body.transform.localPosition;
                var normalScale = body.transform.localScale;
                var visibleNames = first.GetComponentsInChildren<SpriteRenderer>().Select(r => r.name).ToArray();

                // 模拟受击和移动留下的变换，再让同一对象切换兵种。
                body.color = Color.red;
                body.transform.localPosition += Vector3.up;
                body.transform.localScale *= 2;
                body.transform.localRotation = Quaternion.Euler(0, 0, 30);
                pool.Release("shade", first.gameObject);
                var special = pool.RentEnemy("shade", world.transform, Color.cyan);
                Assert.That(special, Is.SameAs(first));
                silhouette.Apply(specialKind, Color.cyan);
                Assert.That(body.sprite, Is.Not.SameAs(normalSprite), "The special enemy kept the normal enemy's artwork.");
                Assert.That(special.Find("Face").gameObject.activeSelf, Is.True);

                pool.Release("shade", special.gameObject);
                var normal = pool.RentEnemy("shade", world.transform, Color.gray);
                Assert.That(normal, Is.SameAs(first));
                silhouette.Apply(0, Color.gray);
                Assert.That(body.sprite, Is.SameAs(normalSprite));
                Assert.That(body.color, Is.EqualTo(normalColor));
                Assert.That(body.transform.localPosition, Is.EqualTo(normalPosition));
                Assert.That(body.transform.localScale, Is.EqualTo(normalScale));
                Assert.That(body.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(normal.GetComponentsInChildren<SpriteRenderer>().Select(r => r.name), Is.EqualTo(visibleNames));
            }
            finally { Object.DestroyImmediate(world); }
        }
    }
}
