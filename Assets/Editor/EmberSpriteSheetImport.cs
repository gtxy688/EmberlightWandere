using UnityEditor;
using UnityEngine;

namespace Emberlight.Editor
{
    internal static class EmberSpriteSheetImport
    {
        internal static void Configure(TextureImporter importer, bool setAlphaTransparency)
        {
            // 保留 .meta 中的像素切片；Default 或 NPOT 缩放会使切片失效。
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            if (setAlphaTransparency) importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
