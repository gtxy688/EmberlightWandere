#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Emberlight.Tests
{
    public sealed class EmberLowPolyCaptureTests
    {
        [UnityTest]
        public IEnumerator Courtyard_RecordsTrue3DWalkingTurningStoppingAndPause()
        {
            int initialAntialiasing = QualitySettings.antiAliasing;
            var initialResolution = QualitySettings.shadowResolution;
            EditorSceneManager.LoadSceneInPlayMode("Assets/Art/LowPolySample/LowPolyCourtyard.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var preview = Object.FindObjectOfType<EmberLowPolyPreview>();
            Assert.That(preview, Is.Not.Null);
            preview.Initialize();
            preview.enabled = false; // 同一 Step 接收回放输入，避免物理键盘污染录像。
            var keeper = preview.Keeper;
            var leftFoot = keeper.transform.Find("Pelvis/Left thigh/Left shin/Left foot");
            var rightFoot = keeper.transform.Find("Pelvis/Right thigh/Right shin/Right foot");
            Assert.That(keeper.GetComponent<SkinnedMeshRenderer>(), Is.Not.Null);
            Assert.That(keeper.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
            int oldRate = Application.targetFrameRate, oldVSync = QualitySettings.vSyncCount;
            var oldCapture = Time.captureDeltaTime;
            var oldShadows = QualitySettings.shadows;
            float oldShadowDistance = QualitySettings.shadowDistance;
            try
            {
                Application.targetFrameRate = 30; QualitySettings.vSyncCount = 0;
                Time.captureDeltaTime = 1f / 30;
                QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowDistance = 45;
                using (var camera = new PolyCapture(preview.View,768,576))
                {
                    yield return Record(preview,camera,"low-poly-courtyard",leftFoot,rightFoot);
                }
                preview.View.orthographicSize = 2.25f;
                using (var camera = new PolyCapture(preview.View,512,640))
                {
                    yield return Record(preview,camera,"low-poly-walk-close",leftFoot,rightFoot);
                    var facing = Vector3.ProjectOnPlane(preview.View.transform.position - keeper.transform.position,Vector3.up);
                    keeper.transform.rotation = Quaternion.LookRotation(facing);
                    for (int i = 0; i < 30; i++) { preview.Step(Vector2.zero,1f / 30); yield return null; }
                    camera.Screenshot("low-poly-keeper-detail");
                }

                preview.Paused = true;
                var rootPosition = keeper.transform.position;
                var footPosition = leftFoot.position;
                for (int i = 0; i < 6; i++) { preview.Step(Vector2.right,1f / 30); yield return null; }
                Assert.That(keeper.transform.position, Is.EqualTo(rootPosition));
                Assert.That(leftFoot.position, Is.EqualTo(footPosition));
                Assert.That(Mathf.Abs(leftFoot.position.y - .10f), Is.LessThan(.001f), "Idle foot must settle on the floor.");
            }
            finally
            {
                Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldVSync;
                Time.captureDeltaTime = oldCapture; QualitySettings.shadows = oldShadows;
                QualitySettings.shadowDistance = oldShadowDistance;
            }
            Object.Destroy(preview.gameObject);
            yield return null;
            Assert.That(QualitySettings.antiAliasing, Is.EqualTo(initialAntialiasing));
            Assert.That(QualitySettings.shadowResolution, Is.EqualTo(initialResolution));
        }

        static IEnumerator Record(EmberLowPolyPreview preview, PolyCapture capture, string name, Transform left, Transform right)
        {
            var origin = preview.Keeper.transform.position;
            float largestLift = 0;
            float largestTurn = 0;
            var initialFacing = preview.Keeper.transform.forward;
            using (var encoder = capture.Encoder(name))
            {
                for (int frame = 0; frame < 180; frame++)
                {
                    var input = frame < 45 ? Vector2.right : frame < 90 ? Vector2.up : frame < 135 ? Vector2.left : Vector2.zero;
                    preview.Step(input,1f / 30);
                    yield return null;
                    largestLift = Mathf.Max(largestLift,left.position.y,right.position.y);
                    largestTurn = Mathf.Max(largestTurn,Vector3.Angle(initialFacing,preview.Keeper.transform.forward));
                    Assert.That(Mathf.Min(left.position.y,right.position.y), Is.GreaterThan(.098f), "A foot penetrated the ground.");
                    capture.Frame(encoder);
                    if (frame == 20) capture.Screenshot(name);
                }
            }
            Assert.That(Vector3.Distance(origin,preview.Keeper.transform.position), Is.GreaterThan(.2f));
            Assert.That(largestTurn, Is.GreaterThan(10));
            Assert.That(largestLift, Is.GreaterThan(.20f), "The swing leg never lifted.");
            Assert.That(new FileInfo(capture.PathFor(name,".mp4")).Length, Is.GreaterThan(10000));
        }

        sealed class PolyCapture : IDisposable
        {
            readonly Camera camera;
            readonly RenderTexture previous, target;
            readonly Texture2D image;
            readonly float aspect;
            readonly string directory = Path.GetFullPath("Library/TestRun");

            public PolyCapture(Camera camera, int width, int height)
            {
                this.camera = camera; previous = camera.targetTexture; aspect = camera.aspect;
                target = new RenderTexture(width,height,24) { antiAliasing = 4 };
                image = new Texture2D(width,height,TextureFormat.RGBA32,false);
                camera.targetTexture = target; camera.aspect = (float)width / height;
                Directory.CreateDirectory(directory);
            }
            public string PathFor(string name, string extension) => Path.Combine(directory,name + extension);
            public MediaEncoder Encoder(string name) => new MediaEncoder(PathFor(name,".mp4"),new VideoTrackAttributes {
                frameRate = new MediaRational(30),width = (uint)target.width,height = (uint)target.height,includeAlpha = false });
            void Read()
            {
                var active = RenderTexture.active;
                try
                {
                    camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                }
                finally { RenderTexture.active = active; }
            }
            public void Frame(MediaEncoder encoder) { Read(); Assert.That(encoder.AddFrame(image), Is.True); }
            public void Screenshot(string name) { Read(); File.WriteAllBytes(PathFor(name,".png"),image.EncodeToPNG()); }
            public void Dispose()
            {
                camera.targetTexture = previous; camera.aspect = aspect;
                target.Release(); Object.Destroy(target); Object.Destroy(image);
            }
        }
    }
}
#endif
