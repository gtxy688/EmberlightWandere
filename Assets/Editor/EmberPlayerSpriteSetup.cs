using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Emberlight.Editor
{
    /// <summary>离线导出两帧动画、持久切片和 Animator，原位连接现有玩家 Prefab。</summary>
    public static class EmberPlayerSpriteSetup
    {
        public const string Root = "Assets/Art/CleanToonSample/Player";
        const string KeeperPath = "Assets/Art/CleanToonSample/Prefabs/Keeper.prefab";
        const string ControllerPath = Root + "/Player.controller";
        const int FrameSize = 512, Ground = 48, CharacterHeight = 384, EyeX = 224;
        const float PixelsPerUnit = 100, IdleFps = 5, MoveFps = 8;

        [MenuItem("Emberlight/Art/Build Player Sprite Animations")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before editing player animation assets.");
            EnsureFolder(Root);
            foreach (string mode in new[] { "Idle", "Move" })
            {
                if (!File.Exists(SheetPath(mode)) || Enumerable.Range(0, 2).Any(i => !File.Exists(FramePath(mode, i))))
                    Export(mode);
                Import(SheetPath(mode), true, mode);
                for (int i = 0; i < 2; i++) Import(FramePath(mode, i), false, mode);
            }
            var idle = CreateClip("Idle", IdleFps);
            var move = CreateClip("Move", MoveFps);
            var controller = CreateController(idle, move);
            BindKeeper(controller, Sprites("Idle")[0]);
            AssetDatabase.SaveAssets();
            Debug.Log("PLAYER_TWO_FRAME_READY: Existing keeper prefab connected to two-frame Idle/Move animations.");
        }

        // 只作画布归一、对齐和导出；真实脚步与摆灯已经画在源稿中，不靠整体变换伪造动作。
        static void Export(string mode)
        {
            var source = ReadImage(Root + "/Source/Player_" + mode + "_Generated.png");
            var sheet = new Texture2D(FrameSize * 2, FrameSize, TextureFormat.RGBA32, false);
            try
            {
                int columns = 2, rows = mode == "Move" ? 2 : 1;
                if (source.width % columns != 0 || source.height % rows != 0)
                    throw new InvalidOperationException("Animation source must contain equal grid cells.");
                int width = source.width / columns, height = source.height / rows;
                var pixels = source.GetPixels32();
                for (int i = 0; i < 2; i++)
                {
                    // Move 源稿为四格：取左上、左下两次相反落脚；待机源稿为横向两格。
                    var cell = mode == "Move"
                        ? new RectInt(0, (1 - i) * height, width, height)
                        : new RectInt(i * width, 0, width, height);
                    var bounds = OpaqueBounds(pixels, source.width, cell);
                    float scale = CharacterHeight / (float)bounds.height;
                    float eyeX = EyeCenter(pixels, source.width, bounds);
                    var frame = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);
                    try
                    {
                        var output = new Color[FrameSize * FrameSize];
                        for (int y = 0; y < FrameSize; y++) for (int x = 0; x < FrameSize; x++)
                        {
                            float sx = (x - EyeX) / scale + eyeX;
                            float sy = (y - Ground) / scale + bounds.yMin;
                            if (sx < cell.xMin || sx >= cell.xMax - 1 || sy < cell.yMin || sy >= cell.yMax - 1) continue;
                            output[y * FrameSize + x] = source.GetPixelBilinear(
                                (sx + .5f) / source.width, (sy + .5f) / source.height);
                        }
                        frame.SetPixels(output);
                        frame.Apply();
                        File.WriteAllBytes(FramePath(mode, i), frame.EncodeToPNG());
                        sheet.SetPixels(i * FrameSize, 0, FrameSize, FrameSize, output);
                    }
                    finally { Object.DestroyImmediate(frame); }
                }
                sheet.Apply();
                File.WriteAllBytes(SheetPath(mode), sheet.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(sheet);
                Object.DestroyImmediate(source);
            }
        }

        static RectInt OpaqueBounds(Color32[] pixels, int width, RectInt cell)
        {
            int left = cell.xMax, right = cell.xMin, bottom = cell.yMax, top = cell.yMin;
            for (int y = cell.yMin; y < cell.yMax; y++) for (int x = cell.xMin; x < cell.xMax; x++)
            {
                if (pixels[y * width + x].a <= 16) continue;
                left = Math.Min(left, x); right = Math.Max(right, x + 1);
                bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1);
            }
            if (right <= left || top <= bottom) throw new InvalidOperationException("Animation cell has no visible pixels.");
            return new RectInt(left, bottom, right - left, top - bottom);
        }

        static float EyeCenter(Color32[] pixels, int width, RectInt bounds)
        {
            double sum = 0;
            int count = 0;
            for (int y = bounds.yMin + bounds.height / 2; y < bounds.yMax; y++)
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var color = pixels[y * width + x];
                    if (color.a < 128 || color.r < 235 || color.g < 160 || color.b > 115) continue;
                    sum += x; count++;
                }
            if (count == 0) throw new InvalidOperationException("Cannot locate the reference character's warm eyes.");
            return (float)(sum / count);
        }

        static Texture2D ReadImage(string path)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (File.Exists(path) && image.LoadImage(File.ReadAllBytes(path))) return image;
            Object.DestroyImmediate(image);
            throw new InvalidOperationException("Cannot read character image: " + path);
        }

        static void Import(string path, bool sheet, string mode)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer unavailable: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = sheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = sheet ? 1024 : FrameSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, Ground / (float)FrameSize);
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = importer.maxTextureSize;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            if (!sheet) return;

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite slicing provider unavailable: " + path);
            provider.InitSpriteEditorDataProvider();
            // 本工程的 Unity 2022 / Sprite 1.0 API 通过 provider 能力查询维护持久切片。
            if (provider.spriteImportMode != SpriteImportMode.Multiple ||
                !provider.HasDataProvider(typeof(ISpriteNameFileIdDataProvider)) ||
                !provider.HasDataProvider(typeof(ITextureDataProvider)))
                throw new InvalidOperationException("Importer cannot safely persist sprite rectangles and identities: " + path);
            var identities = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (identities == null) throw new InvalidOperationException("Sprite identity provider unavailable: " + path);
            var previous = provider.GetSpriteRects().ToDictionary(rect => rect.name);
            var rects = Enumerable.Range(0, 2).Select(i =>
            {
                string name = "Player_" + mode + "_" + i;
                return new SpriteRect
                {
                    name = name,
                    spriteID = previous.TryGetValue(name, out var old) ? old.spriteID : GUID.Generate(),
                    rect = new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                    alignment = SpriteAlignment.Custom, pivot = settings.spritePivot
                };
            }).ToArray();
            provider.SetSpriteRects(rects);
            identities.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        static Sprite[] Sprites(string mode) => AssetDatabase.LoadAllAssetsAtPath(SheetPath(mode))
            .OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();

        static AnimationClip CreateClip(string mode, float fps)
        {
            string path = Root + "/Player_" + mode + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip; // 后续由 Animation 窗口编辑，不覆盖人工曲线。
            var sprites = Sprites(mode);
            if (sprites.Length != 2) throw new InvalidOperationException("Expected exactly two persistent animation sprites.");
            clip = new AnimationClip { name = "Player_" + mode, frameRate = fps };
            var keys = Enumerable.Range(0, 3).Select(i => new ObjectReferenceKeyframe
                { time = i / fps, value = sprites[i % 2] }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static AnimatorController CreateController(AnimationClip idle, AnimationClip move)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle", new Vector3(200, 80));
            var moveState = machine.AddState("Move", new Vector3(460, 80));
            idleState.motion = idle;
            moveState.motion = move;
            machine.defaultState = idleState;
            Transition(idleState, moveState, AnimatorConditionMode.If);
            Transition(moveState, idleState, AnimatorConditionMode.IfNot);
            return controller;
        }

        static void Transition(AnimatorState source, AnimatorState destination, AnimatorConditionMode condition)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.AddCondition(condition, 0, "IsMoving");
        }

        static void BindKeeper(AnimatorController controller, Sprite standing)
        {
            const string backup = "Assets/Art/CleanToonSample/Prefabs/KeeperStatic.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(backup) == null && !AssetDatabase.CopyAsset(KeeperPath, backup))
                throw new InvalidOperationException("Could not preserve the previous static player prefab.");
            var root = PrefabUtility.LoadPrefabContents(KeeperPath);
            try
            {
                var body = root.transform.Find("Keeper artwork").GetComponent<SpriteRenderer>();
                var animator = body.GetComponent<Animator>();
                if (animator == null)
                {
                    // 保留旧图实际可见高度与脚底位置，不因新画布的透明留白改变角色大小。
                    var original = ReadImage(AssetDatabase.GetAssetPath(body.sprite));
                    try
                    {
                        var visible = OpaqueBounds(original.GetPixels32(), original.width,
                            new RectInt(0, 0, original.width, original.height));
                        float canvasHeight = body.sprite.bounds.size.y * body.transform.localScale.y;
                        float height = canvasHeight * visible.height / original.height;
                        float bottom = body.transform.localPosition.y + canvasHeight *
                            (visible.yMin / (float)original.height - body.sprite.pivot.y / body.sprite.rect.height);
                        body.transform.localScale = Vector3.one * (height * PixelsPerUnit / CharacterHeight);
                        body.transform.localPosition = new Vector3(body.transform.localPosition.x, bottom, body.transform.localPosition.z);
                    }
                    finally { Object.DestroyImmediate(original); }
                    animator = body.gameObject.AddComponent<Animator>();
                    body.transform.localRotation = Quaternion.identity;
                }
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                body.sprite = standing;
                var view = new SerializedObject(root.GetComponent<EmberKeeperAnimation>());
                view.FindProperty("spriteAnimator").objectReferenceValue = animator;
                view.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, KeeperPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save the existing keeper prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static string FramePath(string mode, int index) => Root + "/Player_" + mode + "_" + index + ".png";
        static string SheetPath(string mode) => Root + "/Player_" + mode + "_Sheet.png";

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
