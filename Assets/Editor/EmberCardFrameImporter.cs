using UnityEditor;

namespace Emberlight.Editor
{
    public sealed class EmberCardFrameImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/Art/UI/doodle-card-frames.png") return;
            EmberSpriteSheetImport.Configure((TextureImporter)assetImporter, true);
        }
    }
}
