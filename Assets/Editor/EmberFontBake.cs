using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Emberlight.Editor
{
    /// <summary>
    /// Bakes the Chinese TMP atlas ahead of time and writes it to Resources, so the loading
    /// screen no longer builds a 2048x2048 SDF atlas from the 16 MB otf at startup.
    ///
    /// This replaces an earlier attempt (commit b87f568, reverted in 745dd98) and supersedes a
    /// third attempt that shipped an asset which threw on the first frame that drew. Four things
    /// were wrong across those, and this version is built around each:
    ///
    /// 1. NO MULTI-ATLAS. 2048x2048 at 90 px sampling with 9 px padding does not hold the whole
    ///    glyph set, so TMP allocated a SECOND atlas and set m_AtlasTextureIndex = 1. The asset
    ///    then carried `m_AtlasTextures: [{fileID: ...}, {fileID: 0}]` with the font pointing at
    ///    the null slot, and TMP_MaterialManager.GetFallbackMaterial dereferences
    ///    atlasTextures[atlasIndex] without checking -- NullReferenceException from inside TMP.
    ///    The bake now preflights with enableMultiAtlasSupport: false and drops the sampling point
    ///    size until everything fits one atlas, and ApplySingleAtlas enforces the invariant before
    ///    anything is written.
    ///
    /// 2. WRITE ORDER. The old tool called AssetDatabase.CreateAsset(font) and only afterwards
    ///    AddObjectToAsset(font.atlasTexture, font). CreateAsset serialises the object graph as it
    ///    stands, so the atlas had no pixel data on disk. Sub-assets go in first here.
    ///
    /// 3. NO VERIFICATION. It counted characterTable.Count and stopped. The only operation that
    ///    actually fails on a malformed atlas is drawing, so this reloads from disk and draws.
    ///
    /// 4. includeFontFeatures: true on TryAddCharacters, which pulls optional font features into
    ///    the atlas. This font needs none of them.
    ///
    /// The asset is additive: EmberFonts.CreateChinese prefers it only after ValidateBaked passes,
    /// and otherwise falls back to the runtime bake. A bad bake costs a console warning.
    ///
    /// Menu:   Emberlight/Bake Chinese font atlas
    /// Verify: -executeMethod Emberlight.Editor.EmberFontBake.CheckRuntimeLoad
    /// </summary>
    public static class EmberFontBake
    {
        const string SourcePath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular.otf";
        const string OutPath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular SDF.asset";

        const int AtlasPadding = 9;
        const int AtlasSize = 2048;

        // Largest first. The loop stops at the first size whose glyph set fits a single atlas.
        static readonly int[] CandidatePointSizes = { 90, 80, 72, 64, 56, 48, 40 };

        [MenuItem("Emberlight/Bake Chinese font atlas", false, 212)]
        public static void Bake()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            if (source == null)
            {
                Debug.LogError("[EmberFontBake] missing source font at " + SourcePath);
                return;
            }

            string glyphs = EmberFonts.AllGlyphs;
            var total = System.Diagnostics.Stopwatch.StartNew();

            TMP_FontAsset font = null;
            int pointSize = 0;
            double createMs = 0, addMs = 0;
            string missing = null;

            foreach (int candidate in CandidatePointSizes)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var attempt = TMP_FontAsset.CreateFontAsset(
                    source, candidate, AtlasPadding, GlyphRenderMode.SDFAA,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic,
                    false); // enableMultiAtlasSupport -- must stay false, see class comment

                if (attempt == null)
                {
                    Debug.LogWarning("[EmberFontBake] CreateFontAsset returned null at " + candidate + " px");
                    continue;
                }
                double attemptCreateMs = sw.Elapsed.TotalMilliseconds;

                sw.Restart();
                string attemptMissing;
                attempt.TryAddCharacters(glyphs, out attemptMissing);
                sw.Stop();
                double attemptAddMs = sw.Elapsed.TotalMilliseconds;

                int spilled;
                bool fits = FitsOneAtlas(attempt, out spilled);
                // Coverage, not just placement. With multi-atlas off, a character that does not fit
                // is simply not added -- glyphTable never sees it, so "no glyph points past atlas 0"
                // stays true while characters silently go missing. That is exactly how the first
                // single-atlas bake reported fits=True with 109 characters dropped.
                string dropped;
                bool complete = CoversEveryCharacter(attempt, glyphs, out dropped);
                Debug.Log("[EmberFontBake] " + candidate + " px -> " + attempt.characterTable.Count
                    + " chars, " + attempt.glyphTable.Count + " glyphs, "
                    + attempt.atlasTextures.Length + " atlas texture(s), "
                    + spilled + " glyphs past atlas 0, fits=" + fits + ", complete=" + complete
                    + (complete ? "" : ", dropped " + dropped.Length + ": " + Truncate(dropped, 60)));

                if (fits && complete)
                {
                    font = attempt;
                    pointSize = candidate;
                    createMs = attemptCreateMs;
                    addMs = attemptAddMs;
                    missing = attemptMissing;
                    break;
                }

                Object.DestroyImmediate(attempt);
            }

            if (font == null)
            {
                Debug.LogError("[EmberFontBake] no candidate sampling size fit " + glyphs.Length
                    + " characters into one " + AtlasSize + "x" + AtlasSize + " atlas. "
                    + "Lower CandidatePointSizes further, or trim EmberFonts.AllGlyphs.");
                return;
            }

            font.name = "NotoSansCJKsc-Regular Static";

            if (!ApplySingleAtlas(font))
            {
                Debug.LogError("[EmberFontBake] refusing to write: could not reduce the font to a "
                    + "single atlas without losing glyphs. See the glyph dump above.");
                return;
            }

            int glyphCount = font.characterTable.Count;
            int atlasW = font.atlasTextures[0].width, atlasH = font.atlasTextures[0].height;

            // Freeze before saving: a Static atlas cannot accept new glyphs, which is exactly what
            // is wanted at runtime -- there is nothing left to bake on the loading screen.
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            Write(font);
            total.Stop();

            Debug.Log(string.Format(
                "[EmberFontBake] baked {0} glyphs of {1} requested ({2} distinct) at {3} px into ONE {4}x{5} atlas\n"
                + "  atlas textures      {6}\n"
                + "  CreateFontAsset     {7,8:F1} ms\n"
                + "  TryAddCharacters    {8,8:F1} ms\n"
                + "  total               {9,8:F1} ms\n"
                + "  missing             {10}\n"
                + "  written to          {11}",
                glyphCount, glyphs.Length, new System.Collections.Generic.HashSet<char>(glyphs).Count,
                pointSize, atlasW, atlasH,
                font.atlasTextures.Length,
                createMs, addMs, total.Elapsed.TotalMilliseconds,
                string.IsNullOrEmpty(missing) ? "none" : missing.Length + " code points",
                OutPath));

            VerifyReloaded();
        }

        /// <summary>
        /// True when every distinct character in <paramref name="requested"/> is present in the
        /// atlas. Placement being valid says nothing about coverage: a character that did not fit
        /// is absent from the table entirely rather than present-and-invalid.
        /// </summary>
        static bool CoversEveryCharacter(TMP_FontAsset font, string requested, out string dropped)
        {
            var seen = new System.Collections.Generic.HashSet<char>();
            var gaps = new System.Text.StringBuilder();
            for (int i = 0; i < requested.Length; i++)
            {
                char c = requested[i];
                if (!seen.Add(c)) continue;
                if (!font.HasCharacter(c)) gaps.Append(c);
            }
            dropped = gaps.ToString();
            return dropped.Length == 0;
        }

        static string Truncate(string value, int max)
        {
            return value.Length <= max ? value : value.Substring(0, max) + "...";
        }

        /// <summary>
        /// True when every glyph lives in atlas 0 and inside its bounds.
        ///
        /// NOTE this alone is not sufficient to accept a bake -- see CoversEveryCharacter. With
        /// multi-atlas off, an overflowing glyph is never added, so the table can be entirely
        /// valid while characters are missing.
        /// </summary>
        static bool FitsOneAtlas(TMP_FontAsset font, out int spilled)
        {
            spilled = 0;
            if (font.atlasTextures == null || font.atlasTextures.Length == 0 || font.atlasTextures[0] == null)
                return false;

            var texture = font.atlasTextures[0];
            for (int i = 0; i < font.glyphTable.Count; i++)
            {
                var glyph = font.glyphTable[i];
                if (glyph == null) continue;
                if (glyph.atlasIndex != 0)
                {
                    spilled++;
                    continue;
                }
                float right = glyph.glyphRect.x + glyph.glyphRect.width;
                float top = glyph.glyphRect.y + glyph.glyphRect.height;
                if (glyph.glyphRect.x < 0 || glyph.glyphRect.y < 0 || right > texture.width || top > texture.height)
                    spilled++;
            }
            return spilled == 0;
        }

        /// <summary>
        /// Enforces "exactly one atlas texture" on the in-memory font, so serialising it cannot
        /// reproduce the null-slot asset. Returns false rather than guessing if any glyph still
        /// references a higher atlas.
        /// </summary>
        static bool ApplySingleAtlas(TMP_FontAsset font)
        {
            if (font.atlasTextures == null || font.atlasTextures.Length == 0 || font.atlasTextures[0] == null)
            {
                Debug.LogError("[EmberFontBake] no atlas 0 to keep");
                return false;
            }

            int offenders = 0;
            for (int i = 0; i < font.glyphTable.Count; i++)
                if (font.glyphTable[i] != null && font.glyphTable[i].atlasIndex != 0) offenders++;
            if (offenders > 0)
            {
                Debug.LogError("[EmberFontBake] " + offenders + " glyphs still point past atlas 0; "
                    + "shipping this asset would make TMP dereference a null atlas texture at draw time");
                return false;
            }

            // Collapse the array. A null or extra slot left in place is what produced the crash this
            // whole routine exists to prevent, so it is removed rather than merely ignored.
            //
            // atlasTextures has a setter; atlasTexture does NOT (get-only, and it lazily caches
            // atlasTextures[0] the first time it is read). Nothing here reads the singular property
            // while the array is mid-repair, so the cache cannot latch onto a stale texture.
            font.atlasTextures = new[] { font.atlasTextures[0] };

            font.atlasTextures[0].name = font.name + " Atlas";
            EditorUtility.SetDirty(font.atlasTextures[0]);
            EditorUtility.SetDirty(font);
            return true;
        }

        /// <summary>
        /// Writes the atlas texture, the material, and finally the font asset.
        ///
        /// Sub-assets go in first on purpose: CreateAsset serialises the object graph as it stands,
        /// so the atlas has to be present with its pixel data before the font asset is written.
        /// </summary>
        static void Write(TMP_FontAsset font)
        {
            // Rebake in place. Deleting first is safe because the asset is generated and never
            // hand-edited; the old tool refused instead, which made iterating needlessly manual.
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath) != null)
            {
                AssetDatabase.DeleteAsset(OutPath);
                AssetDatabase.Refresh();
            }

            var texture = font.atlasTextures[0];
            texture.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.CreateAsset(texture, OutPath);

            if (font.material != null)
            {
                font.material.name = font.name + " Material";
                font.material.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(font.material, texture);
            }

            AssetDatabase.AddObjectToAsset(font, texture);
            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Reloads the asset from disk -- not the in-memory object -- and forces TMP to lay out and
        /// mesh a sample of glyphs through the real text pipeline.
        ///
        /// This is the check the earlier attempts lacked, and it is the only one that would have
        /// caught any of their failures: all of them shipped assets whose serialised tables looked
        /// healthy and which threw only when something was drawn.
        /// </summary>
        static void VerifyReloaded()
        {
            var reloaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath);
            if (reloaded == null)
            {
                Debug.LogError("[EmberFontBake] VERIFY FAILED: the written asset does not reload");
                return;
            }

            if (reloaded.atlasTextures == null || reloaded.atlasTextures.Length == 0 || reloaded.atlasTextures[0] == null
                || reloaded.characterTable == null || reloaded.characterTable.Count == 0
                || reloaded.material == null)
            {
                Debug.LogError("[EmberFontBake] VERIFY FAILED: the reloaded asset is missing its atlas, "
                    + "characters or material. EmberFonts will fall back to the runtime bake.");
                return;
            }

            // The exact invariant whose violation crashed the game: every glyph must index a slot
            // that actually has a texture.
            int offenders = 0, maxIndex = -1;
            for (int i = 0; i < reloaded.glyphTable.Count; i++)
            {
                var glyph = reloaded.glyphTable[i];
                if (glyph == null) continue;
                if (glyph.atlasIndex > maxIndex) maxIndex = glyph.atlasIndex;
                if (glyph.atlasIndex < 0 || glyph.atlasIndex >= reloaded.atlasTextures.Length) offenders++;
            }
            if (offenders > 0)
            {
                Debug.LogError("[EmberFontBake] VERIFY FAILED: " + offenders + " reloaded glyphs index a "
                    + "missing atlas slot (max atlasIndex " + maxIndex + ", "
                    + reloaded.atlasTextures.Length + " textures). This is the crash condition.");
                return;
            }

            // A bounded sample, spread across the set. Long strings make TMP lay out thousands of
            // characters and page through mesh buffers, which tests the text pipeline rather than
            // the atlas; 64 characters is enough to prove the texture is readable.
            string sample = Sample(EmberFonts.AllGlyphs, 64);
            var go = new GameObject("EmberFontBake verify", typeof(RectTransform));
            try
            {
                // A bare TextMeshProUGUI on a RectTransform produces no geometry: with no Canvas
                // and a zero-sized rect, TMP has nothing to lay out into. An earlier revision of
                // this check did exactly that and reported a false failure.
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(1024, 1024);

                var text = go.AddComponent<TextMeshProUGUI>();
                text.font = reloaded;
                text.fontSize = 24;
                text.text = sample;
                text.ForceMeshUpdate();

                int drawn = text.textInfo != null ? text.textInfo.characterCount : 0;
                int withGeometry = 0;
                if (text.textInfo != null)
                    for (int i = 0; i < text.textInfo.characterCount; i++)
                        if (text.textInfo.characterInfo[i].isVisible) withGeometry++;

                if (drawn == 0 || withGeometry == 0)
                {
                    Debug.LogError("[EmberFontBake] VERIFY FAILED: " + drawn + " characters laid out, "
                        + withGeometry + " with geometry. EmberFonts will fall back to the runtime bake.");
                    return;
                }

                Debug.Log(string.Format(
                    "[EmberFontBake] VERIFY OK: reloaded {0} glyphs / {1} atlas texture(s) / max atlasIndex {2}; "
                    + "drew {3}/{4} sampled characters with geometry",
                    reloaded.glyphTable.Count, reloaded.atlasTextures.Length, maxIndex,
                    withGeometry, drawn));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Evenly spaced slice of <paramref name="source"/>, at most <paramref name="count"/> long.</summary>
        static string Sample(string source, int count)
        {
            if (string.IsNullOrEmpty(source) || source.Length <= count) return source ?? "";
            var keep = new System.Text.StringBuilder(count);
            for (int i = 0; i < count; i++)
                keep.Append(source[(int)((long)i * source.Length / count)]);
            return keep.ToString();
        }

        /// <summary>
        /// Read-only end-to-end check, runnable from the command line:
        ///   Unity.exe -batchmode -quit -executeMethod Emberlight.Editor.EmberFontBake.CheckRuntimeLoad
        ///
        /// Asks the question the bake itself cannot answer, because it writes the asset rather than
        /// consuming it: does EmberFonts accept what Resources.Load actually returns? Deliberately
        /// renders nothing, so it works headless.
        /// </summary>
        public static void CheckRuntimeLoad()
        {
            var font = EmberFonts.CreateChinese();
            if (font == null)
            {
                Debug.LogError("[EmberFontCheck] FAILED: CreateChinese returned null -- neither the "
                    + "pre-baked asset nor the runtime path produced a font");
                return;
            }

            bool baked = font.atlasPopulationMode == AtlasPopulationMode.Static;
            Debug.Log(string.Format(
                "[EmberFontCheck] {0}\n"
                + "  atlasPopulationMode {1}\n"
                + "  glyphs              {2}\n"
                + "  atlas textures      {3}\n"
                + "  atlas size          {4}\n"
                + "  material            {5}",
                baked ? "PASS: the pre-baked atlas is in use (no runtime bake, drawable on frame 0)"
                      : "FALLBACK: the runtime dynamic bake is in use (check the warnings above)",
                font.atlasPopulationMode,
                font.characterTable != null ? font.characterTable.Count : 0,
                font.atlasTextures != null ? font.atlasTextures.Length : 0,
                font.atlasTexture != null ? font.atlasTexture.width + "x" + font.atlasTexture.height : "none",
                font.material != null ? font.material.name : "MISSING"));
        }
    }
}
