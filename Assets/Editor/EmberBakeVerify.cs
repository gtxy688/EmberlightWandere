using TMPro;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Headless verification that the baked assets actually load and carry usable data.
    /// A font asset that serialises with zero glyphs or a null atlas still "bakes" without
    /// throwing, which is exactly how the previous pre-baked font ended up empty, so this
    /// checks the loaded object rather than trusting the bake log.
    ///
    /// Run via: -executeMethod Emberlight.Editor.EmberBakeVerify.Run
    /// </summary>
    public static class EmberBakeVerify
    {
        public static void Run()
        {
            bool ok = true;

            // --- baked art ---
            var names = new[] { "art-flame", "art-ring", "art-panel", "art-glow", "art-disc" };
            foreach (var n in names)
            {
                var t = Resources.Load<Texture2D>("Art/Baked/" + n);
                if (t == null) { Debug.LogError("[EmberBakeVerify] MISSING Art/Baked/" + n); ok = false; continue; }
                Debug.Log(string.Format("[EmberBakeVerify] Art/Baked/{0}: {1}x{2} fmt={3}", n, t.width, t.height, t.format));
            }

            // --- baked font ---
            var font = Resources.Load<TMP_FontAsset>("Fonts/NotoSansCJKsc-Regular SDF");
            if (font == null)
            {
                Debug.LogError("[EmberBakeVerify] MISSING Fonts/NotoSansCJKsc-Regular SDF");
                ok = false;
            }
            else
            {
                int chars = font.characterTable != null ? font.characterTable.Count : -1;
                int glyphs = font.glyphTable != null ? font.glyphTable.Count : -1;
                int slots = font.atlasTextures != null ? font.atlasTextures.Length : -1;
                int aw = font.atlasTexture != null ? font.atlasTexture.width : -1;
                int ah = font.atlasTexture != null ? font.atlasTexture.height : -1;
                Debug.Log(string.Format(
                    "[EmberBakeVerify] font: chars={0} glyphs={1} mode={2} atlasTextures={3} atlas={4}x{5} material={6}",
                    chars, glyphs, font.atlasPopulationMode, slots, aw, ah, font.material != null ? "yes" : "NULL"));

                // atlasTextures is the one that actually breaks the game: TMP_FontAsset.atlasTexture
                // reads slot 0 of it, and any label drawn with a font whose array was never
                // serialised throws UnassignedReferenceException out of GetFallbackMaterial.
                // Checking characterTable alone passed a font that could not render a thing.
                if (slots <= 0 || font.atlasTextures[0] == null)
                {
                    Debug.LogError("[EmberBakeVerify] font.atlasTextures is empty or slot 0 is null - "
                        + "this font will throw UnassignedReferenceException when text is drawn");
                    ok = false;
                }
                if (chars <= 0 || glyphs <= 0) { Debug.LogError("[EmberBakeVerify] font has no glyphs"); ok = false; }
                if (aw <= 0 || ah <= 0) { Debug.LogError("[EmberBakeVerify] font atlas is empty"); ok = false; }
                if (font.material == null) { Debug.LogError("[EmberBakeVerify] font material is null"); ok = false; }

                // Prove every character the UI can draw resolves to a real glyph. This is the
                // check that catches a Static atlas dropping something the hand-written glyph
                // set never listed.
                var used = EmberGlyphSet.CollectGameCharacters();
                var missing = new System.Collections.Generic.List<char>();
                foreach (var cp in used)
                {
                    TMP_Character c;
                    if (!font.characterLookupTable.TryGetValue(cp, out c) || c == null) missing.Add(cp);
                }
                missing.Sort();
                if (missing.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("[EmberBakeVerify] " + missing.Count + " character(s) used by the UI have no glyph:");
                    foreach (var cp in missing)
                        sb.AppendLine(string.Format("    U+{0:X4} {1}", (int)cp, char.IsControl(cp) || char.IsWhiteSpace(cp) ? "(whitespace/control)" : "'" + cp + "'"));
                    Debug.LogError(sb.ToString());
                    ok = false;
                }
                Debug.Log("[EmberBakeVerify] probed all " + used.Count + " characters reachable from game text");
            }

            Debug.Log(ok ? "[EmberBakeVerify] RESULT: PASS" : "[EmberBakeVerify] RESULT: FAIL");
            if (!ok) EditorApplication.Exit(1);
        }
    }
}
