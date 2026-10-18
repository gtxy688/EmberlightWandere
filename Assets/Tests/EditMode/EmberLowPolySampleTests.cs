using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Tests
{
    public sealed class EmberLowPolySampleTests
    {
        const string Root = "Assets/Art/LowPolySample";

        [Test]
        public void Keeper_IsPersistentSkinnedGeometryWithNoSpriteParts()
        {
            var keeper = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Keeper.prefab");
            Assert.That(keeper, Is.Not.Null, "The 3D keeper sample has not been built.");
            Assert.That(keeper.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);
            var skin = keeper.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(skin, Is.Not.Null);
            Assert.That(skin.sharedMesh, Is.Not.Null);
            Assert.That(EditorUtility.IsPersistent(skin.sharedMesh), Is.True);
            Assert.That(skin.bones.Length, Is.GreaterThan(10));
            foreach (var bone in skin.bones) Assert.That(bone, Is.Not.Null);
            foreach (var material in skin.sharedMaterials)
                Assert.That(material != null && EditorUtility.IsPersistent(material), Is.True);
        }

        [TestCase(0, .18f)]
        [TestCase(.10f, -.22f)]
        [TestCase(-.10f, .05f)]
        public void KneeSolution_BendsForwardAndPreservesBothBoneLengths(float x, float z)
        {
            var hip = new Vector3(0, .72f, 0);
            var foot = new Vector3(x, .10f, z);
            var knee = EmberLowPolyGait.Knee(hip, foot, .36f, .36f, Vector3.forward);
            Assert.That(Vector3.Distance(hip, knee), Is.EqualTo(.36f).Within(.0001f));
            Assert.That(Vector3.Distance(knee, foot), Is.EqualTo(.36f).Within(.0001f));
            Assert.That(knee.z, Is.GreaterThan((hip.z + foot.z) * .5f));
        }

        [Test]
        public void StanceFoot_CancelsForwardRootTravelAndStaysOnTheFloor()
        {
            var first = EmberLowPolyGait.Foot(.1f, .60f, .16f, .6f);
            var second = EmberLowPolyGait.Foot(.4f, .60f, .16f, .6f);
            Assert.That(first.y, Is.EqualTo(0).Within(.0001f));
            Assert.That(second.y, Is.EqualTo(0).Within(.0001f));
            Assert.That(second.z + .30f, Is.EqualTo(first.z).Within(.0001f),
                "A supporting foot must remain in place while the root advances.");
        }

        [Test]
        public void LiftOff_JoinsThePlantedFootWithoutAVelocityJump()
        {
            const float epsilon = .0001f;
            var before = EmberLowPolyGait.Foot(.6f - epsilon,.6f,.16f,.6f);
            var contact = EmberLowPolyGait.Foot(.6f,.6f,.16f,.6f);
            var after = EmberLowPolyGait.Foot(.6f + epsilon,.6f,.16f,.6f);
            var entering = (contact - before) / epsilon;
            var leaving = (after - contact) / epsilon;
            Assert.That(Vector3.Distance(entering,leaving), Is.LessThan(.02f),
                "The foot should ease out of ground contact instead of jerking into the swing.");
        }

        [Test]
        public void SampleScene_IsSavedButDoesNotReplaceTheSubmissionBuildScene()
        {
            const string scene = Root + "/LowPolyCourtyard.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene), Is.Not.Null);
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.enabled && entry.path == scene), Is.False);
        }
    }
}
