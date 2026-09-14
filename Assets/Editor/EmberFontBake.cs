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
        public const string OutAssetPath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular SDF.asset";
        const string OutPath = OutAssetPath;

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
            // EmberFonts.GlyphSet is hand-maintained and was already missing characters the
            // UI draws (U+3000). Bake the union of that set and everything the loaded
            // assembly can actually render, so a Static atlas cannot silently drop a glyph.
            string glyphs = EmberGlyphSet.EffectiveGlyphSet();
            string missing;
            font.TryAddCharacters(glyphs, out missing, true);
            sw.Stop();
            var bakeMs = sw.Elapsed.TotalMilliseconds;

            int glyphCount = font.characterTable != null ? font.characterTable.Count : 0;
            int atlasW = 0, atlasH = 0;
            if (font.atlasTexture != null) { atlasW = font.atlasTexture.width; atlasH = font.atlasTexture.height; }
            int slots = font.atlasTextures != null ? font.atlasTextures.Length : 0;

            // Freeze: no runtime population, so nothing is baked on the loading screen.
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(font, OutPath);
            if (font.atlasTexture != null)
            {
                font.atlasTexture.name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(font.atlasTexture, font);

                // Adding the texture as a sub-asset is NOT enough on its own. The font
                // serialises a separate m_AtlasTextures array, and TMP_FontAsset.atlasTexture
                // reads slot 0 of it. If that array is left empty in the asset file the font
                // loads as a non-null object with no texture, and the first label drawn
                // throws UnassignedReferenceException out of GetFallbackMaterial, which takes
                // the whole UI down. Assign the slot explicitly and dirty it so the reference
                // is actually written, then assert it survived the save.
                if (font.atlasTextures == null || font.atlasTextures.Length == 0)
                    font.atlasTextures = new Texture2D[] { font.atlasTexture };
                else
                    font.atlasTextures[0] = font.atlasTexture;

                EditorUtility.SetDirty(font.atlasTexture);
            }
            else
            {
                UnityEngine.Debug.LogError("[EmberFontBake] font has no atlas texture; refusing to save a broken asset");
                return;
            }
            if (font.material != null)
            {
                font.material.name = font.name + " Material";
                AssetDatabase.AddObjectToAsset(font.material, font);
                EditorUtility.SetDirty(font.material);
            }
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Re-read from disk so this checks what was serialised, not the live object.
            var saved = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath);
            int savedSlots = saved != null && saved.atlasTextures != null ? saved.atlasTextures.Length : 0;
            bool slotOk = saved != null && savedSlots > 0 && saved.atlasTextures[0] != null;
            total.Stop();

            if (!slotOk)
            {
                UnityEngine.Debug.LogError("[EmberFontBake] SERIALISATION FAILED: the saved atlas has "
                    + savedSlots + " atlas texture slot(s). This asset would crash the UI at runtime; "
                    + "deleting it so the runtime bake takes over.");
                AssetDatabase.DeleteAsset(OutPath);
                AssetDatabase.Refresh();
                return;
            }

            UnityEngine.Debug.Log(string.Format(
                "[EmberFontBake] baked {0} glyphs into a {1}x{2} atlas (requested {3} characters)\n"
                + "  CreateFontAsset {4,8:F1} ms\n  TryAddCharacters {5,8:F1} ms\n  total {6,8:F1} ms\n"
                + "  missing glyphs: {7}\n"
                + "  atlas texture slots in memory {8}, after save {9} (must be > 0)\n"
                + "  written to {10}",
                glyphCount, atlasW, atlasH, glyphs.Length, createMs, bakeMs, total.Elapsed.TotalMilliseconds,
                string.IsNullOrEmpty(missing) ? "none" : missing.Length + " code points",
                slots, savedSlots,
                OutPath));
        }
    }
}
