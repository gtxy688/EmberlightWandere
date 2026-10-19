using UnityEditor;

namespace Emberlight.Editor
{
    public sealed class EmberUiArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/Art/UI/doodle-ui.png") return;
            EmberSpriteSheetImport.Configure((TextureImporter)assetImporter, true);
        }
    }
}
