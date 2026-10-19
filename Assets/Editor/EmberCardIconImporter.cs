using UnityEditor;

namespace Emberlight.Editor
{
    public sealed class EmberCardIconImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/Art/Cards/doodle-icons.png") return;
            EmberSpriteSheetImport.Configure((TextureImporter)assetImporter, false);
        }
    }
}
