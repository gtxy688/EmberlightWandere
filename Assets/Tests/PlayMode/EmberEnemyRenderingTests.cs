#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed class EmberEnemyRenderingTests
    {
        [UnityTest]
        public IEnumerator SkinMaterial_RendersArtworkAndTransparentEdgesBeforeAndAfterHitFlash()
        {
            var root = new GameObject("Enemy render test");
            root.transform.position = new Vector3(1000, 1000, 0);
            var target = new RenderTexture(96, 96, 24);
            var pixels = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var pool = new EmberPool();
                pool.Bind(root.transform, shadePrefab: AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Art/CleanToonSample/Prefabs/Shade.prefab"));
                var view = pool.RentEnemy("shade", root.transform, Color.gray);
                view.GetComponent<EmberEnemySilhouette>().Apply(0, Color.gray);
                var enemy = new EmberEnemy { view = view, kind = 0 };
                var visual = new EmberEnemyVisual(enemy, root.transform);
                var camera = new GameObject("Skin camera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.SetParent(root.transform, false);
                camera.transform.localPosition = new Vector3(0, .12f, -10);
                camera.orthographic = true;
                camera.orthographicSize = .8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.02f, .04f, .06f, 1);
                camera.targetTexture = target;

                visual.Tick(1, LevelConfig.Default);
                var normal = RenderPixels(camera, target, pixels);
                int colored = normal.Count(c => c.r + c.g + c.b > .35f);
                int white = normal.Count(c => c.r > .95f && c.g > .95f && c.b > .95f);
                Assert.That(colored, Is.GreaterThan(300), "The authored body must actually render.");
                Assert.That(white, Is.LessThan(colored / 10), "The ordinary purple ghost must not become an opaque white quad.");
                Assert.That(normal[0].r + normal[0].g + normal[0].b, Is.LessThan(.2f), "Transparent corners must preserve the background.");

                enemy.flash = .1f;
                visual.Tick(1, LevelConfig.Default);
                var hit = RenderPixels(camera, target, pixels);
                int brighter = Enumerable.Range(0, hit.Length).Count(i =>
                    hit[i].r + hit[i].g + hit[i].b - normal[i].r - normal[i].g - normal[i].b > .6f);
                Assert.That(brighter, Is.GreaterThan(colored / 2), "Hit feedback must brighten the sampled artwork, not only a C# parameter.");
                Assert.That(hit[0].r + hit[0].g + hit[0].b, Is.LessThan(.2f));

                var body = view.GetComponent<EmberEnemySilhouette>().WholeBody;
                body.flipX = true;
                enemy.flash = 0;
                visual.Tick(1, LevelConfig.Default);
                enemy.flash = .1f;
                visual.Tick(1, LevelConfig.Default);
                var flipped = RenderPixels(camera, target, pixels);
                float mirrorError = Enumerable.Range(0, hit.Length).Average(i =>
                {
                    var mirror = hit[i / 96 * 96 + 95 - i % 96];
                    return Mathf.Abs(mirror.r - flipped[i].r) + Mathf.Abs(mirror.g - flipped[i].g) + Mathf.Abs(mirror.b - flipped[i].b);
                });
                Assert.That(mirrorError, Is.LessThan(.01f), "Updating hit properties must preserve actual SpriteRenderer.flipX rendering.");

                enemy.flash = 0;
                body.flipX = false;
                visual.Tick(1, LevelConfig.Default);
                var restored = RenderPixels(camera, target, pixels);
                Assert.That(Enumerable.Range(0, restored.Length).Max(i => Mathf.Abs(restored[i].r - normal[i].r)
                    + Mathf.Abs(restored[i].g - normal[i].g) + Mathf.Abs(restored[i].b - normal[i].b)), Is.LessThan(.02f));
                visual.Clear();
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.Destroy(target);
                Object.Destroy(pixels);
                Object.Destroy(root);
            }
            yield return null;
        }

        static Color[] RenderPixels(Camera camera, RenderTexture target, Texture2D image)
        {
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            return image.GetPixels();
        }
    }
}
#endif
