using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Emberlight.Editor
{
    /// <summary>
    /// 预烘中文 TMP 图集写入 Resources，避免加载页启动时用 16 MB otf 现场烘 2048x2048 SDF。
    /// 三条硬约束：
    /// 1. 禁止多图集：2048x2048 / 90 px / 9 px padding 装不下全部字形，TMP 会开第二张图集并把字体指向
    ///    空槽，随后 TMP 解引用 atlasTextures[atlasIndex] 抛 NullReferenceException。预检固定
    ///    enableMultiAtlasSupport: false 并逐档降采样点，写盘前由 ApplySingleAtlas 兜住该不变量。
    /// 2. 写入顺序：先 AddObjectToAsset 贴图与材质、最后写字体资产；CreateAsset 按当时的对象图序列化，
    ///    顺序反了盘上的图集就没有像素数据。
    /// 3. 必须真渲染校验：畸形图集只在绘制时才报错，故要从盘上重新加载并真画一遍（VerifyReloaded），
    ///    只数字形数不算验证；多图集关闭时装不下的字根本不会入表，另需 CoversEveryCharacter 查覆盖率。
    /// 另外 TryAddCharacters 的 includeFontFeatures 必须为 false，否则会引入本字体不需要的可选特性。
    ///
    /// 资产是可加的：EmberFonts.CreateChinese 只在 ValidateBaked 通过后采用，否则回落运行时动态烘制。
    /// Menu: Emberlight/Bake Chinese font atlas
    /// Verify: -executeMethod Emberlight.Editor.EmberFontBake.CheckRuntimeLoad
    /// </summary>
    public static class EmberFontBake
    {
        const string SourcePath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular.otf";
        const string OutPath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular SDF.asset";

        const int AtlasPadding = 9;
        const int AtlasSize = 2048;

        // 从大到小试，遇到第一个装得进单图集的尺寸就停。
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
                    false); // enableMultiAtlasSupport，必须为 false，理由见类注释

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
                // 还要查覆盖率：多图集关闭时装不下的字根本不入表，glyphTable 里看不到它，
                // 于是「没有字形越过 atlas 0」依然成立，字却静默丢失。
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

            // 存盘前冻结为 Static：静态图集不再接受新字形，运行时加载页也就无需再烘任何字。
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

        /// <summary><paramref name="requested"/> 里的每个不同字符都在图集中时为真。
        /// 位置合法不等于覆盖完整：装不下的字符是整条缺失，而不是存在但非法。</summary>
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

        /// <summary>每个字形都在 atlas 0 且落在其边界内时为真。
        /// 注意：这一项不足以接受一次烘制，还需 CoversEveryCharacter——多图集关闭时溢出的字形不会入表，
        /// 表可以完全合法却缺字。</summary>
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

        /// <summary>在内存字体上强制「只有一张图集贴图」，避免序列化出带空槽的资产；
        /// 仍有字形指向更高图集时返回 false，不做猜测。</summary>
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

            // 收拢数组：残留的空槽或多余槽正是本方法要防的崩溃条件，必须删掉而不是忽略。
            // atlasTextures 有 setter，atlasTexture 没有（只读，且首次读取会缓存 atlasTextures[0]）；
            // 数组修复期间不要读那个单数属性，否则缓存会锁住过期贴图。
            font.atlasTextures = new[] { font.atlasTextures[0] };

            font.atlasTextures[0].name = font.name + " Atlas";
            EditorUtility.SetDirty(font.atlasTextures[0]);
            EditorUtility.SetDirty(font);
            return true;
        }

        /// <summary>依次写入图集贴图、材质，最后写字体资产。
        /// 子资产必须先行：CreateAsset 按当时的对象图序列化，图集得先带上像素数据。</summary>
        static void Write(TMP_FontAsset font)
        {
            // 原地重烘：该资产是生成物、从不手改，先删再写是安全的。
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

        /// <summary>从磁盘（而不是内存对象）重新加载资产，并让 TMP 走真实文本管线排版、生成网格。
        /// 这是唯一能发现问题的一步：坏资产的序列化表看起来总是健康的，只有绘制时才抛异常。</summary>
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

            // 关键不变量：每个字形索引到的图集槽位必须真的有贴图，违反即崩溃。
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

            // 抽样要限量且分布均匀：长字符串会让 TMP 排版上千字符并反复翻页网格缓冲，
            // 那测的是文本管线而不是图集；64 个字符足以证明贴图可读。
            string sample = Sample(EmberFonts.AllGlyphs, 64);
            var go = new GameObject("EmberFontBake verify", typeof(RectTransform));
            try
            {
                // 光秃秃的 RectTransform + TextMeshProUGUI 画不出几何：既没有 Canvas 又没有尺寸，
                // TMP 无从排版，会误报失败。
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

        /// <summary>等距抽取 <paramref name="source"/> 的片段，长度不超过 <paramref name="count"/>。</summary>
        static string Sample(string source, int count)
        {
            if (string.IsNullOrEmpty(source) || source.Length <= count) return source ?? "";
            var keep = new System.Text.StringBuilder(count);
            for (int i = 0; i < count; i++)
                keep.Append(source[(int)((long)i * source.Length / count)]);
            return keep.ToString();
        }

        /// <summary>
        /// 只读的端到端检查，可命令行执行：
        ///   Unity.exe -batchmode -quit -executeMethod Emberlight.Editor.EmberFontBake.CheckRuntimeLoad
        ///
        /// 回答烘制本身回答不了的问题（它只写资产、不消费资产）：EmberFonts 是否接受
        /// Resources.Load 真正返回的对象？刻意不渲染任何东西，因此可无头运行。
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
