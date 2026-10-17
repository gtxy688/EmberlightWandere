using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>通过 Sprite Editor 数据接口导入三连图；再次运行保留已存在切片的身份。</summary>
    public static class EmberPickupArtSetup
    {
        public const string AtlasPath = "Assets/Resources/Art/Pickups/ember-pickups.png";

        public static void Configure()
        {
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Pickup atlas is missing or is not a texture.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
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
            if (provider == null) throw new InvalidOperationException("Texture importer does not support Sprite Editor data.");
            provider.InitSpriteEditorDataProvider();
            // Unity 2022 的 Sprite 包使用 HasDataProvider 查询支持情况，尚无新版本的 FrameEditCapability 接口。
            if (provider.spriteImportMode != SpriteImportMode.Multiple ||
                !provider.HasDataProvider(typeof(ISpriteNameFileIdDataProvider)))
                throw new InvalidOperationException("Importer does not support persistent multi-sprite identities.");
            var identities = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (identities == null) throw new InvalidOperationException("Sprite name/id provider is missing.");
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width < 3 || height < 1) throw new InvalidOperationException("Invalid pickup source dimensions.");
            var old = provider.GetSpriteRects();
            var names = new[] { "pickup-heal", "pickup-power", "pickup-haste" };
            var rects = new SpriteRect[3];
            for (int i = 0; i < rects.Length; i++)
            {
                int left = width * i / 3;
                int right = width * (i + 1) / 3;
                var previous = old.FirstOrDefault(rect => rect.name == names[i]);
                rects[i] = new SpriteRect
                {
                    name = names[i], spriteID = previous != null ? previous.spriteID : GUID.Generate(),
                    rect = new Rect(left, 0, right - left, height), pivot = Vector2.one * .5f, alignment = SpriteAlignment.Center
                };
            }
            provider.SetSpriteRects(rects);
            identities.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            var saved = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            if (saved.Length != 3 || names.Any(name => !saved.Any(sprite => sprite.name == name)))
                throw new InvalidOperationException("Pickup atlas did not retain all three sprites.");
            Debug.Log("Pickup art imported: three persistent centered sprites.");
        }
    }
}
