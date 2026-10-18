#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Emberlight.Tests
{
    public sealed class EmberKeeperSpriteSmokeTests
    {
        [UnityTest]
        public IEnumerator TwoFramePlayer_PlaysBothStatesMirrorsAndFreezesWithoutMovingItsRoot()
        {
            var keeper = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/CleanToonSample/Prefabs/Keeper.prefab"));
            var view = keeper.GetComponent<EmberKeeperAnimation>();
            var body = keeper.transform.Find("Keeper artwork").GetComponent<SpriteRenderer>();
            var animator = body.GetComponent<Animator>();
            var phase = EmberRunPhase.Playing;
            float previousCapture = Time.captureDeltaTime;
            float previousScale = Time.timeScale;
            try
            {
                Assert.That(animator, Is.Not.Null);
                Time.timeScale = 1;
                Time.captureDeltaTime = 1f / 40;
                view.Initialize(() => phase);
                var position = body.transform.localPosition;
                var scale = body.transform.localScale;
                var idle = new HashSet<Sprite>();
                for (int frame = 0; frame < 40; frame++)
                {
                    yield return null;
                    idle.Add(body.sprite);
                }
                Assert.That(idle.Count, Is.EqualTo(2), "Idle must actually cycle its two authored sprites.");
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
                Assert.That(keeper.transform.position, Is.EqualTo(Vector3.zero), "Idle must not move the gameplay root.");

                var walking = new HashSet<Sprite>();
                for (int frame = 0; frame < 32; frame++)
                {
                    keeper.transform.position += Vector3.right * .02f;
                    yield return null;
                    if (frame > 1) walking.Add(body.sprite);
                }
                Assert.That(walking.Count, Is.EqualTo(2), "Move must cycle two distinct foot-contact drawings.");
                Assert.That(walking.Overlaps(idle), Is.False);
                Assert.That(body.flipX, Is.True, "The source faces left, so right movement mirrors it.");
                Assert.That(body.transform.localPosition, Is.EqualTo(position));
                Assert.That(body.transform.localScale, Is.EqualTo(scale));
                Assert.That(body.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(keeper.transform.position.x, Is.EqualTo(.64f).Within(.0001f));
                Assert.That(keeper.transform.localScale, Is.EqualTo(Vector3.one));

                foreach (var direction in new[] { Vector3.up, Vector3.down, new Vector3(1, 1, 0) })
                {
                    for (int frame = 0; frame < 4; frame++)
                    {
                        keeper.transform.position += direction * .02f;
                        yield return null;
                    }
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Move"), Is.True);
                    Assert.That(body.flipX, Is.True, "Vertical movement must retain the previous horizontal facing.");
                }
                for (int frame = 0; frame < 4; frame++)
                {
                    keeper.transform.position += Vector3.left * .02f;
                    yield return null;
                }
                Assert.That(body.flipX, Is.False);

                // 即使 timeScale 仍为 1，选卡和暂停阶段也必须冻结，而非依赖全局暂停。
                foreach (var stopped in new[] { EmberRunPhase.Paused, EmberRunPhase.Upgrade })
                {
                    phase = stopped;
                    var frozen = body.sprite;
                    var clock = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    for (int frame = 0; frame < 15; frame++)
                    {
                        keeper.transform.position += Vector3.right * .01f;
                        yield return null;
                    }
                    Assert.That(body.sprite, Is.SameAs(frozen));
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(clock));
                    Assert.That(body.flipX, Is.False);
                }
                phase = EmberRunPhase.Playing;
                for (int frame = 0; frame < 4; frame++) yield return null;
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
                Assert.That(body.flipX, Is.False, "Stopping retains facing.");
                Time.timeScale = 0;
                var stoppedSprite = body.sprite;
                for (int frame = 0; frame < 12; frame++) yield return null;
                Assert.That(body.sprite, Is.SameAs(stoppedSprite));
                Time.timeScale = 1;
                view.Initialize(() => phase);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
                Assert.That(body.flipX, Is.False);
                Assert.That(animator.applyRootMotion, Is.False);
            }
            finally
            {
                Time.captureDeltaTime = previousCapture;
                Time.timeScale = previousScale;
                Object.Destroy(keeper);
            }
        }
    }
}
#endif
