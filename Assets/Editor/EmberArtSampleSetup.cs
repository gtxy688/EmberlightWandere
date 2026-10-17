using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Emberlight.Editor
{
    /// <summary>生成可编辑的美术样板资产；重复执行保留已存在的 Prefab 与手动布局。</summary>
    public static class EmberArtSampleSetup
    {
        public const string Root = "Assets/Art/HandPaintedSample";
        public const string Prefabs = Root + "/Prefabs";
        public const string SetPath = Root + "/ArtSet.asset";
        const string ScenePath = "Assets/Scenes/Emberlight.unity";

        [MenuItem("Emberlight/Art/Build Hand Painted Sample")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before preparing art assets.");
            EnsureFolder(Prefabs);
            EnsureFolder(Root + "/Sprites");
            var keeper = Import("keeper");
            var shade = Import("shade");
            var floor = Import("courtyard-floor", true);
            var lantern = Import("lantern-pillar");
            var rubble = Import("ruin-rubble");
            var candles = Import("candle-cluster");
            var panel = Import("hud-panel", false, true);
            CreateKeeper(keeper);
            CreateShade(shade);
            CreateProp("Lantern", lantern, 1.6f);
            CreateProp("Rubble", rubble, .65f);
            CreateProp("Candles", candles, .9f);
            CreateCourtyard(floor);

            var set = AssetDatabase.LoadAssetAtPath<EmberArtSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<EmberArtSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            var serialized = new SerializedObject(set);
            Bind(serialized, "keeperPrefab", LoadPrefab("Keeper"));
            Bind(serialized, "shadePrefab", LoadPrefab("Shade"));
            Bind(serialized, "courtyardPrefab", LoadPrefab("Courtyard"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            set.ValidateReferences();
            EmberArtSampleHudSetup.Configure(panel);
            AssetDatabase.SaveAssets();
            var scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath) Connect(scene, set);
            Debug.Log("Hand-painted sample ready. Existing prefab edits were preserved.");
        }

        // 批处理显式打开并保存主场景；菜单入口只标记已打开的主场景，不丢弃其他场景编辑。
        public static void BuildProject()
        {
            Build();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            Connect(scene, AssetDatabase.LoadAssetAtPath<EmberArtSet>(SetPath));
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save art references.");
        }

        static void Connect(Scene scene, EmberArtSet set)
        {
            var game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EmberGame>(true)).Single();
            Undo.RecordObject(game, "Connect art set");
            var serialized = new SerializedObject(game);
            Bind(serialized, "artSet", set);
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static Sprite Import(string name, bool floor = false, bool panel = false)
        {
            string path = Root + "/Textures/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing sample texture: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = floor ? 313.5f : 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = !floor;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = floor ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.maxTextureSize = floor || panel ? 4096 : 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = importer.maxTextureSize;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite provider unavailable: " + path);
            provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().FirstOrDefault(rect => rect.name == name);
            var rect = new SpriteRect
            {
                name = name, spriteID = old != null ? old.spriteID : GUID.Generate(),
                rect = panel ? PanelBounds(path) : new Rect(0, 0, width, height),
                pivot = Vector2.one * .5f, alignment = SpriteAlignment.Center,
                border = panel ? new Vector4(120, 70, 120, 70) : Vector4.zero
            };
            provider.SetSpriteRects(new[] { rect });
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                new[] { new SpriteNameFileIdPair(name, rect.spriteID) });
            provider.Apply();
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        }

        // 只定义切片范围，不修改原图像素；忽略透明边缘的极低 alpha，保留完整装饰。
        static Rect PanelBounds(string path)
        {
            var source = new Texture2D(2, 2);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid panel image.");
                var pixels = source.GetPixels32();
                int left = source.width, bottom = source.height, right = 0, top = 0;
                for (int y = 0; y < source.height; y++) for (int x = 0; x < source.width; x++)
                {
                    if (pixels[y * source.width + x].a <= 8) continue;
                    left = Math.Min(left, x); right = Math.Max(right, x + 1);
                    bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1);
                }
                if (right <= left || top <= bottom) throw new InvalidOperationException("Panel has no visible pixels.");
                return new Rect(left, bottom, right - left, top - bottom);
            }
            finally { Object.DestroyImmediate(source); }
        }

        static void CreateKeeper(Sprite sprite)
        {
            if (LoadPrefab("Keeper") != null) return;
            var root = new GameObject("Keeper");
            try
            {
                var shadow = Picture("Shadow", root.transform, Shared("shape-disc"), new Vector2(0, -.52f), 1);
                shadow.transform.localScale = new Vector3(.95f, .28f, 1);
                shadow.color = new Color(0, 0, 0, .36f);
                var glow = Picture("Ground light", root.transform, Shared("shape-glow"), new Vector2(.08f, -.45f), 2);
                glow.transform.localScale = Vector3.one * (2.4f / glow.sprite.bounds.size.x);
                glow.color = new Color(1, .51f, .13f, .23f);
                var body = Picture("Keeper artwork", root.transform, sprite, new Vector2(0, .18f), 7);
                body.transform.localScale = Vector3.one * (1.65f / sprite.bounds.size.y);
                var animation = root.AddComponent<EmberKeeperAnimation>();
                var serialized = new SerializedObject(animation);
                Bind(serialized, "body", body);
                Bind(serialized, "glow", glow);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Save(root, "Keeper");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void CreateShade(Sprite sprite)
        {
            if (LoadPrefab("Shade") != null) return;
            var root = EmberVisuals.Character("Shade", null, new Color(.29f, .30f, .39f)).gameObject;
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>()) renderer.sprite = Shared("shape-disc");
                var silhouette = root.AddComponent<EmberEnemySilhouette>();
                var serialized = new SerializedObject(silhouette);
                Bind(serialized, "ordinarySprite", sprite);
                Bind(serialized, "ordinaryBody", root.transform.Find("Cloak").GetComponent<SpriteRenderer>());
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // 资产保存中性模板；运行时由 Apply 按本次兵种选择手绘或原有轮廓。
                Save(root, "Shade");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void CreateProp(string name, Sprite sprite, float height)
        {
            if (LoadPrefab(name) != null) return;
            var root = new GameObject(name);
            try
            {
                var body = Picture("Artwork", root.transform, sprite, Vector2.zero, -2);
                body.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
                Save(root, name);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void CreateCourtyard(Sprite floor)
        {
            if (LoadPrefab("Courtyard") != null) return;
            var root = new GameObject("Courtyard");
            try
            {
                var ground = Picture("Stone floor", root.transform, floor, Vector2.zero, -20);
                ground.drawMode = SpriteDrawMode.Tiled;
                ground.size = new Vector2(80, 80);
                ground.color = new Color(.68f, .74f, .82f);
                Place(root.transform, "Lantern", new Vector2(3.3f, 3.8f), 1.35f);
                Place(root.transform, "Lantern", new Vector2(-3.4f, -5.5f), 1.1f);
                Place(root.transform, "Rubble", new Vector2(-3.6f, 2.4f), 1.5f);
                Place(root.transform, "Rubble", new Vector2(3.5f, -3.4f), 1.5f);
                Place(root.transform, "Candles", new Vector2(-3.2f, -3.4f), 1.25f);
                Place(root.transform, "Candles", new Vector2(3.2f, 1.1f), .7f);
                var random = new System.Random(73);
                for (int i = 0; i < 50; i++)
                {
                    var point = new Vector2((float)random.NextDouble() * 44 - 22, (float)random.NextDouble() * 44 - 22);
                    if (Mathf.Abs(point.x) < 4 && Mathf.Abs(point.y) < 7) continue;
                    Place(root.transform, i % 7 == 0 ? "Lantern" : i % 3 == 0 ? "Candles" : "Rubble", point, .55f + (float)random.NextDouble() * .5f);
                }
                // 样板保留现有地图边界外观；完整遗迹边界在风格确认后替换。
                EmberWorld.BuildBoundary(root.transform);
                PersistShapes(root);
                Save(root, "Courtyard");
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void Place(Transform parent, string name, Vector2 position, float scale)
        {
            var prop = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(name));
            prop.transform.SetParent(parent, false);
            prop.transform.localPosition = position;
            prop.transform.localScale = Vector3.one * scale;
        }

        static void PersistShapes(GameObject root)
        {
            var cache = new Dictionary<Sprite, Sprite>();
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || EditorUtility.IsPersistent(renderer.sprite)) continue;
                var original = renderer.sprite;
                if (!cache.TryGetValue(original, out var saved))
                {
                    saved = EmberUiPrefabRepair.PersistSprite(original, "sample-shape-" + cache.Count, Root + "/Sprites");
                    cache.Add(original, saved);
                }
                renderer.sprite = saved;
            }
        }

        static Sprite Shared(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EmberUiPrefabRepair.SpriteDirectory + "/" + name + ".asset");
            if (sprite == null) throw new InvalidOperationException("Missing persistent shape: " + name);
            return sprite;
        }

        static SpriteRenderer Picture(string name, Transform parent, Sprite sprite, Vector2 point, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = point;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        static GameObject LoadPrefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + name + ".prefab");
        static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab", out bool saved);
            if (!saved) throw new InvalidOperationException("Could not save sample prefab: " + name);
        }
        static void Bind(SerializedObject view, string name, Object value) => view.FindProperty(name).objectReferenceValue = value;
        static void EnsureFolder(string path)
        {
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
