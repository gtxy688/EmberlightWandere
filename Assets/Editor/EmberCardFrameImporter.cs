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
            // Sprite, not Default. These atlases are cut into sub-sprites whose rects live in
            // the .meta; under Default the importer ignores them entirely, so any slicing done
            // in the Sprite Editor -- or written by Tools/slice_atlases.py -- was silently
            // reverted on the next reimport. Multiple is the mode slice_atlases.py writes.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            // The sliced rects are authored in pixels; letting Unity rescale the source with a
            // power-of-two fit would invalidate every one of them.
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
