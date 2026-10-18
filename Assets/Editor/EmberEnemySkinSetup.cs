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
    /// <summary>导入无脚怪物皮肤并原位绑定现有敌人模板；切片不改写生成原图。</summary>
    public static class EmberEnemySkinSetup
    {
        const string Root = "Assets/Art/CleanToonSample/Enemies";
        const string Prefab = "Assets/Art/CleanToonSample/Prefabs/Shade.prefab";

        [MenuItem("Emberlight/Art/Build Enemy Skins")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before editing enemy skin assets.");
            var core = Import("enemy-core", 3);
            var special = Import("enemy-special", 3);
            var guardians = Import("enemy-guardians", 3);
            var shield = Import("guard-shield", 1)[0];
            var material = SkinMaterial();
            var sprites = core.Concat(special).Concat(guardians).ToArray();
            int[] kinds = { 0, 1, 2, 4, 5, 6, 7, 8, 3 };
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var component = root.GetComponent<EmberEnemySilhouette>();
                if (component == null) throw new InvalidOperationException("Enemy prefab requires its silhouette component.");
                var view = new SerializedObject(component);
                var body = view.FindProperty("ordinaryBody").objectReferenceValue as SpriteRenderer;
                if (body == null) throw new InvalidOperationException("Enemy prefab requires a whole-body sprite renderer.");
                if (body.sharedMaterial == null || body.sharedMaterial.shader.name == "Sprites/Default")
                    body.sharedMaterial = material;
                if (!body.sharedMaterial.HasProperty("_HitFlash"))
                    throw new InvalidOperationException("Existing enemy skin material must support _HitFlash; preserve or replace it explicitly.");
                var map = view.FindProperty("skins");
                for (int i = 0; i < kinds.Length; i++)
                {
                    SerializedProperty entry = null;
                    for (int slot = 0; slot < map.arraySize; slot++)
                    {
                        var existing = map.GetArrayElementAtIndex(slot);
                        if (existing.FindPropertyRelative("kind").intValue == kinds[i]) { entry = existing; break; }
                    }
                    if (entry != null) continue; // 已有皮肤选择和高度由美术在 Prefab 编辑，重跑不覆盖。
                    int index = map.arraySize++;
                    entry = map.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("kind").intValue = kinds[i];
                    entry.FindPropertyRelative("sprite").objectReferenceValue = sprites[i];
                    entry.FindPropertyRelative("height").floatValue = kinds[i] == 3 ? .95f : 1.05f;
                }
                var shieldReference = view.FindProperty("directionalShieldSprite");
                if (shieldReference.objectReferenceValue == null) shieldReference.objectReferenceValue = shield;
                view.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save the existing enemy prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("ENEMY_SKINS_READY: Nine footless role skins and directional shield connected to existing prefab.");
        }

        static Material SkinMaterial()
        {
            string path = Root + "/EnemySkin.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/EnemySkin.shader");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Enemy skin hit-flash shader is unavailable.");
            material = new Material(shader) { name = "Enemy Skin" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Sprite[] Import(string name, int count)
        {
            string path = Root + "/" + name + ".png";
            var bounds = FindSpriteBounds(path, count);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Enemy texture importer unavailable: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = count == 1 ? 1024 : 2048;
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

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite provider unavailable: " + path);
            provider.InitSpriteEditorDataProvider();
            if (provider.spriteImportMode != SpriteImportMode.Multiple ||
                !provider.HasDataProvider(typeof(ISpriteNameFileIdDataProvider)) ||
                !provider.HasDataProvider(typeof(ITextureDataProvider)))
                throw new InvalidOperationException("Importer cannot safely persist enemy sprite identities: " + path);
            var identities = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (identities == null) throw new InvalidOperationException("Sprite identity provider unavailable: " + path);
            var previous = provider.GetSpriteRects().ToDictionary(rect => rect.name);
            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
            {
                string spriteName = name + "_" + i;
                rects[i] = new SpriteRect
                {
                    name = spriteName,
                    spriteID = previous.TryGetValue(spriteName, out var old) ? old.spriteID : GUID.Generate(),
                    rect = bounds[i], pivot = Vector2.one * .5f, alignment = SpriteAlignment.Center
                };
            }
            provider.SetSpriteRects(rects);
            identities.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
        }

        // 原稿角色宽度不同，机械均分会切到邻居；通过真正透明的列间隙确定每个完整轮廓。
        static Rect[] FindSpriteBounds(string path, int expected)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!File.Exists(path) || !image.LoadImage(File.ReadAllBytes(path)))
                    throw new InvalidOperationException("Cannot read enemy skin image: " + path);
                int width = image.width, height = image.height;
                var pixels = image.GetPixels32();
                var occupied = new bool[width];
                for (int x = 0; x < width; x++) for (int y = 0; y < height; y++)
                    if (pixels[y * width + x].a > 16) { occupied[x] = true; break; }
                var result = new List<Rect>();
                for (int x = 0; x < width;)
                {
                    if (!occupied[x]) { x++; continue; }
                    int left = x;
                    while (x < width && occupied[x]) x++;
                    int right = x;
                    if (right - left < 8) continue; // 忽略透明区的孤立边缘噪点。
                    int bottom = height, top = 0;
                    for (int px = left; px < right; px++) for (int y = 0; y < height; y++)
                        if (pixels[y * width + px].a > 16)
                        { bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1); }
                    int startX = Math.Max(0, left - 3), startY = Math.Max(0, bottom - 3);
                    result.Add(new Rect(startX, startY, Math.Min(width, right + 3) - startX,
                        Math.Min(height, top + 3) - startY));
                }
                if (result.Count != expected)
                    throw new InvalidOperationException("Expected " + expected + " separated enemy sprites but found " + result.Count + ": " + path);
                return result.ToArray();
            }
            finally { Object.DestroyImmediate(image); }
        }
    }
}
