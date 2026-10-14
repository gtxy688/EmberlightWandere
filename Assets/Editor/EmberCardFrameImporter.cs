using UnityEditor;
using UnityEngine;

namespace Emberlight.Editor
{
    public sealed class EmberCardFrameImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/Art/UI/doodle-card-frames.png") return;
            var importer = (TextureImporter)assetImporter;
            // 必须 Sprite + Multiple：切片矩形存在 .meta 里，Default 会让导入器完全忽略它们，
            // 切片（Sprite Editor / Tools/slice_atlases.py）会在下次重导入时被静默清掉。
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            // 切片按像素写死，power-of-two 缩放会让每个矩形都失效。
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
