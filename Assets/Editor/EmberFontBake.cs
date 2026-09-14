using System.Diagnostics;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Emberlight.Editor
{
    /// <summary>
    /// Builds the Chinese TMP font atlas ahead of time.
    ///
    /// EmberFonts.CreateChinese() currently runs TMP_FontAsset.CreateFontAsset at startup
    /// and bakes a 2048x2048 SDF atlas from the 16 MB .otf, then pushes the whole GlyphSet
    /// through TryAddCharacters. That is the single largest one-off cost on the loading
    /// screen. This tool does the same work in the editor and writes a Static atlas to
    /// Resources, which EmberFonts prefers when present.
    ///
    /// Re-bake: delete Assets/Resources/Fonts/NotoSansCJKsc-Regular SDF.asset first.
    /// A Static atlas cannot take new glyphs, so baking over one is refused rather than
    /// silently producing an empty asset (which is what happened to the previous attempt).
    /// </summary>
    public static class EmberFontBake
    {
        const string SourcePath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular.otf";
        const string OutPath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular SDF.asset";

        [MenuItem("Emberlight/Bake Chinese font atlas", false, 212)]
        public static void Bake()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            if (source == null)
            {
                UnityEngine.Debug.LogError("[EmberFontBake] missing source font at " + SourcePath);
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath);
            if (existing != null)
            {
                if (existing.atlasPopulationMode == AtlasPopulationMode.Static)
                {
                    UnityEngine.Debug.LogError("[EmberFontBake] " + OutPath + " is already a Static atlas. "
                        + "Delete it (and its .meta) before re-baking, or the glyphs cannot be added.");
                    return;
                }
                AssetDatabase.DeleteAsset(OutPath);
            }

            var total = Stopwatch.StartNew();

            var sw = Stopwatch.StartNew();
            var font = TMP_FontAsset.CreateFontAsset(
                source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            sw.Stop();
            var createMs = sw.Elapsed.TotalMilliseconds;

            if (font == null)
            {
                UnityEngine.Debug.LogError("[EmberFontBake] TMP_FontAsset.CreateFontAsset returned null");
                return;
            }
            font.name = "NotoSansCJKsc-Regular Static";

            sw.Restart();
            string missing;
            font.TryAddCharacters(EmberFonts.GlyphSet, out missing, true);
            sw.Stop();
            var bakeMs = sw.Elapsed.TotalMilliseconds;

            int glyphCount = font.characterTable != null ? font.characterTable.Count : 0;
            int atlasW = 0, atlasH = 0;
            if (font.atlasTexture != null) { atlasW = font.atlasTexture.width; atlasH = font.atlasTexture.height; }

            // Freeze: no runtime population, so nothing is baked on the loading screen.
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(font, OutPath);
            if (font.atlasTexture != null)
            {
                font.atlasTexture.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            }
            if (font.material != null)
            {
                font.material.name = font.name + " Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
            }
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            total.Stop();

            UnityEngine.Debug.Log(string.Format(
                "[EmberFontBake] baked {0} glyphs into a {1}x{2} atlas\n"
                + "  CreateFontAsset {3,8:F1} ms\n  TryAddCharacters {4,8:F1} ms\n  total {5,8:F1} ms\n"
                + "  missing glyphs: {6}\n  written to {7}",
                glyphCount, atlasW, atlasH, createMs, bakeMs, total.Elapsed.TotalMilliseconds,
                string.IsNullOrEmpty(missing) ? "none" : missing.Length + " code points",
                OutPath));
        }
    }
}
