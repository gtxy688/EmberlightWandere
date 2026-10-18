using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberKeeperRigTests
    {
        const string PrefabPath = "Assets/Art/HandPaintedSample/Prefabs/KeeperWalk.prefab";

        [Test]
        public void Walk_KeepsTheShoulderAndHipsAttachedWhileSamplingPositionCurves()
        {
            var keeper = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                var rig = keeper.GetComponentInChildren<EmberKeeperRig>();
                var cape = rig.transform.Find("Rig/Upper body/Cape");
                var near = rig.transform.Find("Rig/Near leg");
                var far = rig.transform.Find("Rig/Far leg");
                float shoulderHeight = cape.localPosition.y, nearHip = near.localPosition.x, farHip = far.localPosition.x;
                rig.Initialize();
                rig.Step(Vector2.right, .2f);
                Assert.That(cape.localPosition.y, Is.EqualTo(shoulderHeight).Within(.0001f), "The cape detached from the shoulder.");
                Assert.That(near.localPosition.x, Is.EqualTo(nearHip).Within(.0001f));
                Assert.That(far.localPosition.x, Is.EqualTo(farHip).Within(.0001f));
            }
            finally { Object.DestroyImmediate(keeper); }
        }

        [Test]
        public void Walk_MovesLegsIndependentlyAndLeavesGameplayRootUntouched()
        {
            var keeper = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                keeper.transform.position = new Vector3(3, 4, 0);
                var position = keeper.transform.position;
                var scale = keeper.transform.localScale;
                var rig = keeper.GetComponentInChildren<EmberKeeperRig>();
                var near = rig.transform.Find("Rig/Near leg");
                var far = rig.transform.Find("Rig/Far leg");
                rig.Initialize();
                rig.Step(Vector2.right, .15f);
                Assert.That(Quaternion.Angle(near.localRotation, far.localRotation), Is.GreaterThan(10),
                    "Walking must articulate the two legs, not merely bob the assembled picture.");
                Assert.That(Mathf.Abs(near.localPosition.y - far.localPosition.y), Is.GreaterThan(.02f));
                Assert.That(keeper.transform.position, Is.EqualTo(position));
                Assert.That(keeper.transform.localScale, Is.EqualTo(scale));
                Assert.That(keeper.transform.rotation, Is.EqualTo(Quaternion.identity));
            }
            finally { Object.DestroyImmediate(keeper); }
        }

        [Test]
        public void Stop_RestoresStandingPoseAndTurningMirrorsTheCompleteVisual()
        {
            var keeper = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                var rig = keeper.GetComponentInChildren<EmberKeeperRig>();
                var leg = rig.transform.Find("Rig/Near leg");
                var restPosition = leg.localPosition;
                var restRotation = leg.localRotation;
                rig.Initialize();
                rig.Step(Vector2.left * 2, .15f);
                Assert.That(rig.transform.localScale.x, Is.LessThan(0));
                Assert.That(keeper.transform.localScale.x, Is.GreaterThan(0), "Collision/motion root must not flip.");
                for (int i = 0; i < 40; i++) rig.Step(Vector2.zero, .025f);
                Assert.That(Vector3.Distance(leg.localPosition, restPosition), Is.LessThan(.001f));
                Assert.That(Quaternion.Angle(leg.localRotation, restRotation), Is.LessThan(.05f));
                rig.Step(Vector2.right, .1f);
                Assert.That(rig.transform.localScale.x, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(keeper); }
        }
    }
}
