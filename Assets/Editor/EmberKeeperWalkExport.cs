using System;
using System.IO;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Emberlight.Editor
{
    /// <summary>用 Unity 相机渲染关节动画，不重绘 PNG。导出资源随源码交付。</summary>
    public static class EmberKeeperWalkExport
    {
        public const string FrameFolder = EmberKeeperWalkSetup.Root + "/Animations/KeeperWalkFrames";

        [MenuItem("Emberlight/Art/Export Keeper Walk Frames")]
        public static void Export()
        {
            EmberKeeperWalkSetup.Build();
            if (!AssetDatabase.IsValidFolder(FrameFolder))
                AssetDatabase.CreateFolder(EmberKeeperWalkSetup.Root + "/Animations", "KeeperWalkFrames");
            string previewFolder = Path.GetFullPath("Library/TestRun");
            Directory.CreateDirectory(previewFolder);
            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = null;
            Texture2D image = null;
            var previous = RenderTexture.active;
            try
            {
                var cameraHost = new GameObject("Walk export camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraHost, scene);
                var camera = cameraHost.GetComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 1.1f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.transform.position = new Vector3(0, .31f, -10);
                target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
                image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                camera.targetTexture = target;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EmberKeeperWalkSetup.PrefabPath);
                var keeper = Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(keeper, scene);
                keeper.transform.Find("Shadow").gameObject.SetActive(false);
                keeper.transform.Find("Ground light").gameObject.SetActive(false);
                var visual = keeper.transform.Find("Keeper artwork").gameObject;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(EmberKeeperWalkSetup.ClipPath);
                for (int i = 0; i < 8; i++)
                {
                    clip.SampleAnimation(visual, i * clip.length / 8);
                    Read(camera, target, image);
                    File.WriteAllBytes(FrameFolder + "/keeper-walk-" + i.ToString("00") + ".png", image.EncodeToPNG());
                }

                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                target = new RenderTexture(512, 640, 24);
                image = new Texture2D(512, 640, TextureFormat.RGBA32, false);
                camera.targetTexture = target;
                camera.aspect = 512f / 640;
                camera.backgroundColor = new Color(.035f, .046f, .075f, 1);
                keeper.transform.Find("Shadow").gameObject.SetActive(true);
                keeper.transform.Find("Ground light").gameObject.SetActive(true);
                using (var encoder = new MediaEncoder(Path.Combine(previewFolder, "keeper-walk-preview.mp4"),
                    new VideoTrackAttributes { frameRate = new MediaRational(24), width = 512, height = 640, includeAlpha = false }))
                {
                    for (int frame = 0; frame < 96; frame++)
                    {
                        clip.SampleAnimation(visual, Mathf.Repeat(frame / 24f * 2, 1) * clip.length);
                        Read(camera, target, image);
                        if (!encoder.AddFrame(image)) throw new InvalidOperationException("Could not encode the walk preview.");
                        if (frame == 3) File.WriteAllBytes(Path.Combine(previewFolder, "keeper-walk-preview.png"), image.EncodeToPNG());
                    }
                }
                Object.DestroyImmediate(keeper);

                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                target = new RenderTexture(1024, 640, 24);
                image = new Texture2D(1024, 640, TextureFormat.RGBA32, false);
                camera.targetTexture = target;
                camera.aspect = 1024f / 640;
                camera.orthographicSize = 2.4f;
                camera.transform.position = new Vector3(0, .31f, -10);
                for (int i = 0; i < 8; i++)
                {
                    var pose = Object.Instantiate(prefab);
                    SceneManager.MoveGameObjectToScene(pose, scene);
                    pose.transform.position = new Vector3((i % 4 - 1.5f) * 1.85f, i < 4 ? 1.05f : -1.05f, 0);
                    pose.transform.Find("Shadow").gameObject.SetActive(false);
                    pose.transform.Find("Ground light").gameObject.SetActive(false);
                    clip.SampleAnimation(pose.transform.Find("Keeper artwork").gameObject, i * clip.length / 8);
                }
                Read(camera, target, image);
                File.WriteAllBytes(Path.Combine(previewFolder, "keeper-walk-contact.png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
                if (image != null) Object.DestroyImmediate(image);
                EditorSceneManager.ClosePreviewScene(scene);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for (int i = 0; i < 8; i++) ImportFrame(FrameFolder + "/keeper-walk-" + i.ToString("00") + ".png");
            AssetDatabase.SaveAssets();
            Debug.Log("KEEPER_WALK_EXPORTED: 8 transparent frames and 96-frame preview.");
        }

        static void Read(Camera camera, RenderTexture target, Texture2D image)
        {
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
        }

        static void ImportFrame(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing rendered frame: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f / 2.2f;
            importer.spritePivot = Vector2.one * .5f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 256;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
    }
}
