#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Emberlight.Tests
{
    public sealed class EmberKeeperRigSmokeTests
    {
        [UnityTest]
        public IEnumerator Walk_FollowsMovementAndFreezesOutsidePlaying()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/HandPaintedSample/Prefabs/KeeperWalk.prefab");
            var keeper = Object.Instantiate(prefab);
            var phase = EmberRunPhase.Playing;
            try
            {
                keeper.GetComponent<EmberKeeperAnimation>().Initialize(() => phase);
                var leg = keeper.transform.Find("Keeper artwork/Rig/Near leg");
                var standing = leg.localRotation;
                float largestAngle = 0;
                for (int i = 0; i < 8; i++)
                {
                    keeper.transform.position += Vector3.right * .05f;
                    yield return null;
                    largestAngle = Mathf.Max(largestAngle, Quaternion.Angle(leg.localRotation, standing));
                }
                // 整段移动中应出现迈步，不能恰好在双腿交叉的那一帧断言角度。
                Assert.That(largestAngle, Is.GreaterThan(1));
                // 用非 Playing 阶段模拟暂停；即使时间仍推进、根位置变化，视图也必须冻结。
                phase = EmberRunPhase.Paused;
                var frozenRotation = leg.localRotation;
                var frozenPosition = leg.localPosition;
                for (int i = 0; i < 5; i++)
                {
                    keeper.transform.position += Vector3.left * .1f;
                    yield return null;
                }
                Assert.That(leg.localPosition, Is.EqualTo(frozenPosition));
                Assert.That(leg.localRotation, Is.EqualTo(frozenRotation));
                phase = EmberRunPhase.Playing;
                for (int i = 0; i < 8; i++)
                {
                    keeper.transform.position += Vector3.left * .05f;
                    yield return null;
                }
                Assert.That(keeper.transform.Find("Keeper artwork").localScale.x, Is.LessThan(0));
                Assert.That(keeper.transform.localScale.x, Is.GreaterThan(0));
            }
            finally { Object.Destroy(keeper); }
        }
    }
}
#endif
