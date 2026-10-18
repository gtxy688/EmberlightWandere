using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberPlayerSpriteTests
    {
        const string Root = "Assets/Art/CleanToonSample/Player";

        [Test]
        public void CurrentKeeper_HasTwoLoopingSpriteStatesAndPersistentFrames()
        {
            var keeper = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/CleanToonSample/Prefabs/Keeper.prefab");
            var renderer = keeper.transform.Find("Keeper artwork").GetComponent<SpriteRenderer>();
            var animator = renderer.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null, "The existing player prefab needs the requested Idle/Move Animator.");
            Assert.That(animator.applyRootMotion, Is.False);
            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.parameters.Single().name, Is.EqualTo("IsMoving"));
            Assert.That(controller.parameters.Single().type, Is.EqualTo(AnimatorControllerParameterType.Bool));
            var machine = controller.layers.Single().stateMachine;
            Assert.That(machine.defaultState.name, Is.EqualTo("Idle"));
            Assert.That(machine.states.Select(s => s.state.name), Is.EquivalentTo(new[] { "Idle", "Move" }));
            foreach (var state in machine.states.Select(s => s.state))
            {
                var clip = state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null);
                Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Assert.That(bindings.Length, Is.EqualTo(1), "Only the Sprite may be animated.");
                Assert.That(bindings[0].type, Is.EqualTo(typeof(SpriteRenderer)));
                Assert.That(bindings[0].propertyName, Is.EqualTo("m_Sprite"));
                Assert.That(AnimationUtility.GetCurveBindings(clip), Is.Empty, "No root motion, scale or whole-body bob curves.");
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                Assert.That(keys.Select(k => k.value).Distinct().Count(), Is.EqualTo(2));
                Assert.That(keys.First().value, Is.SameAs(keys.Last().value), "Close the two-frame loop.");
                foreach (var key in keys)
                    Assert.That(AssetDatabase.GetAssetPath(key.value), Is.EqualTo(Root + "/Player_" + state.name + "_Sheet.png"));
                Assert.That(state.transitions.Length, Is.EqualTo(1));
                Assert.That(state.transitions[0].hasExitTime, Is.False);
                Assert.That(state.transitions[0].duration, Is.EqualTo(0));
            }
        }

        [TestCase("Idle")]
        [TestCase("Move")]
        public void TwoFrameSheet_HasEqualCanvasFootPivotAndStablePersistentSprites(string mode)
        {
            string path = Root + "/Player_" + mode + "_Sheet.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.mipmapEnabled, Is.False);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            Assert.That(sprites.Length, Is.EqualTo(2));
            for (int i = 0; i < sprites.Length; i++)
            {
                Assert.That(sprites[i].name, Is.EqualTo("Player_" + mode + "_" + i));
                Assert.That(sprites[i].rect.size, Is.EqualTo(new Vector2(512, 512)));
                Assert.That(sprites[i].pivot, Is.EqualTo(new Vector2(256, 48)));
                Assert.That(sprites[i].pixelsPerUnit, Is.EqualTo(100));
                var exported = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Player_" + mode + "_" + i + ".png");
                Assert.That(exported, Is.Not.Null);
                Assert.That(exported.width, Is.EqualTo(512));
                Assert.That(exported.height, Is.EqualTo(512));
            }
        }

        [TestCase("Idle")]
        [TestCase("Move")]
        public void ExportedFrames_AreDifferentTransparentGroundedAndNotClipped(string mode)
        {
            var frames = new byte[2][];
            for (int i = 0; i < 2; i++)
            {
                frames[i] = System.IO.File.ReadAllBytes(Root + "/Player_" + mode + "_" + i + ".png");
                var image = new Texture2D(2, 2);
                try
                {
                    Assert.That(image.LoadImage(frames[i]), Is.True);
                    var pixels = image.GetPixels32();
                    int bottom = 512, top = 0, left = 512, right = 0;
                    for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
                    {
                        if (pixels[y * 512 + x].a <= 16) continue;
                        left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                        bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                    }
                    Assert.That(bottom, Is.InRange(47, 49), "Both planted-foot poses need the same ground line.");
                    Assert.That(top, Is.InRange(430, 434));
                    Assert.That(left, Is.GreaterThan(15));
                    Assert.That(right, Is.LessThan(497), "The lantern, cloak and feet must fit the canvas.");
                    Assert.That(pixels.Count(color => color.a == 0), Is.GreaterThan(pixels.Length / 2),
                        "The background must be true alpha, not an opaque black rectangle.");
                }
                finally { Object.DestroyImmediate(image); }
            }
            Assert.That(frames[0].SequenceEqual(frames[1]), Is.False, "Do not duplicate one image as two animation frames.");
        }

        [Test]
        public void Rebuild_PreservesPrefabSpriteAndAnimationIdentities()
        {
            string keeperPath = "Assets/Art/CleanToonSample/Prefabs/Keeper.prefab";
            var before = Identities();
            var keeper = AssetDatabase.LoadAssetAtPath<GameObject>(keeperPath);
            var body = keeper.transform.Find("Keeper artwork");
            var position = body.localPosition;
            var scale = body.localScale;
            Emberlight.Editor.EmberPlayerSpriteSetup.Build();
            Assert.That(Identities(), Is.EqualTo(before), "Re-running setup must not break existing persistent references.");
            body = AssetDatabase.LoadAssetAtPath<GameObject>(keeperPath).transform.Find("Keeper artwork");
            Assert.That(body.localPosition, Is.EqualTo(position));
            Assert.That(body.localScale, Is.EqualTo(scale));
        }

        static string[] Identities()
        {
            var paths = new[] { Root + "/Player_Idle_Sheet.png", Root + "/Player_Move_Sheet.png",
                Root + "/Player_Idle.anim", Root + "/Player_Move.anim", Root + "/Player.controller",
                "Assets/Art/CleanToonSample/Prefabs/Keeper.prefab" };
            return paths.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path)).Select(asset =>
            {
                Assert.That(asset, Is.Not.Null);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long id);
                return guid + ":" + id;
            }).OrderBy(id => id).ToArray();
        }
    }
}
