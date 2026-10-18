using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Emberlight.Editor
{
    /// <summary>创建分件角色和可编辑曲线。已有 Prefab 与动画保留人工调整。</summary>
    public static class EmberKeeperWalkSetup
    {
        public const string Root = EmberArtSampleSetup.Root;
        public const string ClipPath = Root + "/Animations/KeeperWalk.anim";
        public const string PrefabPath = EmberArtSampleSetup.Prefabs + "/KeeperWalk.prefab";
        const string TexturePath = Root + "/Textures/keeper-parts.png";
        static readonly string[] PartNames =
            { "head", "torso", "cape", "far-leg", "near-leg", "free-arm", "upper-arm", "forearm", "lantern" };

        [MenuItem("Emberlight/Art/Build Keeper Walk Prototype")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before preparing animation assets.");
            if (!AssetDatabase.IsValidFolder(Root + "/Animations")) AssetDatabase.CreateFolder(Root, "Animations");
            var sprites = ImportParts();
            CreatePrefab(sprites);
            AssetDatabase.SaveAssets();
            Debug.Log("Keeper walk prototype ready; existing prefab and clip edits were preserved.");
        }

        [MenuItem("Emberlight/Art/Use Keeper Walk Prototype")]
        public static void UsePrototype()
        {
            Build();
            var set = AssetDatabase.LoadAssetAtPath<EmberArtSet>(EmberArtSampleSetup.SetPath);
            if (set == null) throw new InvalidOperationException("Build the art sample first.");
            var view = new SerializedObject(set);
            view.FindProperty("keeperPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            view.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Emberlight/Art/Use Original Keeper")]
        public static void UseOriginal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before changing art references.");
            var set = AssetDatabase.LoadAssetAtPath<EmberArtSet>(EmberArtSampleSetup.SetPath);
            if (set == null) throw new InvalidOperationException("Build the art sample first.");
            var view = new SerializedObject(set);
            view.FindProperty("keeperPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(
                EmberArtSampleSetup.Prefabs + "/Keeper.prefab");
            view.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Emberlight/Art/Reset Keeper Walk Curves")]
        public static void ResetCurves()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before editing animation assets.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (prefab == null || clip == null) throw new InvalidOperationException("Build the walking Keeper first.");
            var visual = prefab.transform.Find("Keeper artwork");
            var upper = visual.Find("Rig/Upper body");
            var arm = upper.Find("Lantern arm");
            var elbow = arm.Find("Elbow");
            var replacement = CreateClip(visual, upper, upper.Find("Cape"), visual.Find("Rig/Far leg"),
                visual.Find("Rig/Near leg"), upper.Find("Head"), upper.Find("Free arm"), arm, elbow, elbow.Find("Grip"));
            try
            {
                Undo.RecordObject(clip, "Reset keeper walk curves");
                EditorUtility.CopySerialized(replacement, clip);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(replacement); }
        }

        static Dictionary<string, Sprite> ImportParts()
        {
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Keeper part sheet is missing.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite Editor provider is unavailable.");
            provider.InitSpriteEditorDataProvider();
            var identities = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (identities == null) throw new InvalidOperationException("Persistent sprite identities are unavailable.");
            var old = provider.GetSpriteRects().ToDictionary(rect => rect.name);
            var source = new Texture2D(2, 2);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(TexturePath))) throw new InvalidOperationException("Invalid part sheet.");
                var pixels = source.GetPixels32();
                var pivots = new[] { new Vector2(.55f, .06f), new Vector2(.5f, .5f), new Vector2(.88f, .88f),
                    new Vector2(.5f, .92f), new Vector2(.5f, .92f), new Vector2(.36f, .88f),
                    new Vector2(.38f, .88f), new Vector2(.08f, .58f), new Vector2(.48f, .94f) };
                var rects = new SpriteRect[PartNames.Length];
                for (int i = 0; i < rects.Length; i++)
                {
                    int column = i % 3, row = 2 - i / 3;
                    var cell = new RectInt(column * source.width / 3, row * source.height / 3,
                        (column + 1) * source.width / 3 - column * source.width / 3,
                        (row + 1) * source.height / 3 - row * source.height / 3);
                    string name = "keeper-" + PartNames[i];
                    rects[i] = new SpriteRect { name = name,
                        spriteID = old.TryGetValue(name, out var previous) ? previous.spriteID : GUID.Generate(),
                        rect = VisibleBounds(pixels, source.width, cell),
                        pivot = pivots[i], alignment = SpriteAlignment.Custom };
                }
                provider.SetSpriteRects(rects);
                identities.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
                provider.Apply();
                importer.SaveAndReimport();
            }
            finally { Object.DestroyImmediate(source); }
            return AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        }

        // 只定义 Sprite 范围；保留原 PNG 和透明通道，不做像素重绘。
        static Rect VisibleBounds(Color32[] pixels, int width, RectInt cell)
        {
            int left = cell.xMax, right = cell.xMin, bottom = cell.yMax, top = cell.yMin;
            for (int y = cell.yMin; y < cell.yMax; y++) for (int x = cell.xMin; x < cell.xMax; x++)
            {
                if (pixels[y * width + x].a <= 16) continue;
                left = Math.Min(left, x); right = Math.Max(right, x + 1);
                bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1);
            }
            if (right <= left || top <= bottom) throw new InvalidOperationException("Empty animation cell.");
            left = Math.Max(cell.xMin, left - 3); bottom = Math.Max(cell.yMin, bottom - 3);
            right = Math.Min(cell.xMax, right + 3); top = Math.Min(cell.yMax, top + 3);
            return new Rect(left, bottom, right - left, top - bottom);
        }

        static void CreatePrefab(Dictionary<string, Sprite> sprites)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(EmberArtSampleSetup.Prefabs + "/Keeper.prefab");
            if (original == null) throw new InvalidOperationException("Build the original Keeper prefab first.");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(original);
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                root.name = "KeeperWalk";
                Object.DestroyImmediate(root.transform.Find("Keeper artwork").gameObject);
                var visual = Bone("Keeper artwork", root.transform, Vector2.zero);
                var rig = Bone("Rig", visual, Vector2.zero);
                var upper = Bone("Upper body", rig, Vector2.zero);
                var cape = Bone("Cape", upper, new Vector2(-.03f, .28f));
                Picture(cape, sprites["keeper-cape"], .67f, 3);
                var far = Bone("Far leg", rig, new Vector2(.10f, -.12f));
                Picture(far, sprites["keeper-far-leg"], .46f, 5);
                var near = Bone("Near leg", rig, new Vector2(-.07f, -.12f));
                Picture(near, sprites["keeper-near-leg"], .46f, 6);
                var torso = Bone("Torso", upper, new Vector2(0, .09f));
                var body = Picture(torso, sprites["keeper-torso"], .60f, 8);
                var head = Bone("Head", upper, new Vector2(0, .31f));
                Picture(head, sprites["keeper-head"], .95f, 10);
                var free = Bone("Free arm", upper, new Vector2(-.20f, .26f));
                Picture(free, sprites["keeper-free-arm"], .38f, 9);
                var arm = Bone("Lantern arm", upper, new Vector2(.23f, .27f));
                Picture(arm, sprites["keeper-upper-arm"], .22f, 7);
                var elbow = Bone("Elbow", arm, new Vector2(.06f, -.12f));
                Picture(elbow, sprites["keeper-forearm"], .24f, 9);
                var grip = Bone("Grip", elbow, new Vector2(.32f, .02f));
                Picture(grip, sprites["keeper-lantern"], .54f, 8);

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
                if (clip == null)
                {
                    clip = CreateClip(visual, upper, cape, far, near, head, free, arm, elbow, grip);
                    AssetDatabase.CreateAsset(clip, ClipPath);
                }
                var editorAnimation = visual.gameObject.AddComponent<Animation>();
                editorAnimation.playAutomatically = false;
                editorAnimation.AddClip(clip, "Walk");
                editorAnimation.clip = clip;
                editorAnimation.enabled = false; // Runtime 手动采样，阶段与位移决定时间。
                var rigView = visual.gameObject.AddComponent<EmberKeeperRig>();
                var view = new SerializedObject(rigView);
                view.FindProperty("walk").objectReferenceValue = clip;
                var joints = new[] { upper, cape, far, near, head, free, arm, elbow, grip };
                var list = view.FindProperty("joints");
                list.arraySize = joints.Length;
                for (int i = 0; i < joints.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = joints[i];
                view.ApplyModifiedPropertiesWithoutUndo();
                var controller = new SerializedObject(root.GetComponent<EmberKeeperAnimation>());
                controller.FindProperty("body").objectReferenceValue = body;
                controller.FindProperty("rig").objectReferenceValue = rigView;
                controller.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save the walking Keeper prefab.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static AnimationClip CreateClip(Transform visual, Transform upper, Transform cape, Transform far, Transform near,
            Transform head, Transform free, Transform arm, Transform elbow, Transform grip)
        {
            var clip = new AnimationClip { name = "KeeperWalk", legacy = true, frameRate = 24, wrapMode = WrapMode.Loop };
            Curve(clip, visual, upper, "localPosition.y", t => .016f * (1 - Mathf.Cos(t * 4 * Mathf.PI)));
            Angle(clip, visual, upper, t => 2 * Sin(t));
            Angle(clip, visual, cape, t => -3 + 6 * Sin(t - .16f));
            Curve(clip, visual, cape, "localPosition.x", t => cape.localPosition.x + .025f * Sin(t - .16f));
            Leg(clip, visual, near, 0);
            Leg(clip, visual, far, .5f);
            Angle(clip, visual, head, t => -1.8f * Sin(t - .08f));
            Angle(clip, visual, free, t => -14 * Sin(t));
            Angle(clip, visual, arm, t => 8 * Sin(t));
            Angle(clip, visual, elbow, t => -6 * Sin(t - .1f));
            Angle(clip, visual, grip, t => 11 * Sin(t - .22f));
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        static void Leg(AnimationClip clip, Transform visual, Transform leg, float offset)
        {
            Angle(clip, visual, leg, t => 22 * Mathf.Cos((t + offset) * 2 * Mathf.PI));
            Curve(clip, visual, leg, "localPosition.y", t => leg.localPosition.y +
                .10f * Mathf.Max(0, -Sin(t + offset)) - .025f * Mathf.Abs(Mathf.Cos((t + offset) * 2 * Mathf.PI)));
        }

        static float Sin(float t) => Mathf.Sin(t * 2 * Mathf.PI);
        static void Angle(AnimationClip clip, Transform root, Transform joint, Func<float, float> value) =>
            Curve(clip, root, joint, "localEulerAnglesRaw.z", value);
        static void Curve(AnimationClip clip, Transform root, Transform joint, string property, Func<float, float> value)
        {
            const float step = 1f / 8;
            var keys = new Keyframe[9];
            for (int i = 0; i < keys.Length; i++)
            {
                float t = i * step, slope = (value(t + .001f) - value(t - .001f)) / .002f;
                keys[i] = new Keyframe(t, value(t), slope, slope);
            }
            clip.SetCurve(AnimationUtility.CalculateTransformPath(joint, root), typeof(Transform), property, new AnimationCurve(keys));
            // Unity 将位置曲线序列化为完整 Vector3。未写的轴会变成零，必须保留装配位置。
            if (property.StartsWith("localPosition.", StringComparison.Ordinal))
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    string other = "localPosition." + "xyz"[axis];
                    if (other == property) continue;
                    clip.SetCurve(AnimationUtility.CalculateTransformPath(joint, root), typeof(Transform), other,
                        AnimationCurve.Constant(0, 1, joint.localPosition[axis]));
                }
            }
        }

        static Transform Bone(string name, Transform parent, Vector2 position)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(parent, false);
            bone.localPosition = position;
            return bone;
        }
        static SpriteRenderer Picture(Transform bone, Sprite sprite, float height, int order)
        {
            var artwork = new GameObject("Artwork", typeof(SpriteRenderer));
            artwork.transform.SetParent(bone, false);
            artwork.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            var renderer = artwork.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
