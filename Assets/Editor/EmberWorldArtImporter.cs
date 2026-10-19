using UnityEditor;

namespace Emberlight.Editor
{
    public sealed class EmberWorldArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/Art/World/wanderer-sheet.png") return;
            EmberSpriteSheetImport.Configure((TextureImporter)assetImporter, true);
        }
    }
}
