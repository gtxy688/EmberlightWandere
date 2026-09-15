using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Emberlight
{
    public static class EmberFonts
    {
        // Full pre-warm list for the dynamic atlas. Note it is NOT exhaustive: characters the UI
        // draws but that are absent here (设置, 确认, 放弃, ...) are added on demand by the
        // dynamic atlas at first render, which is safe but bakes on the first frame that shows
        // them. The tail below is the game-speed feature.
        //
        // This whole set used to be pushed in one synchronous TryAddCharacters during the loading
        // screen (~90 ms measured, on top of ~9 ms for CreateFontAsset). That stall is what the
        // loading screen was padded to cover. It is now split: FirstScreenGlyphSet is added
        // synchronously because the loading page and the main menu cannot render without it, and
        // the remainder is handed to TMP's asynchronous population while the minimum loading
        // time is still on screen. See CreateChinese / PrewarmInBackground.
        public const string GlyphSet = " %()+-./0123456789:DFLPS[]adefgilnot\u00b7\u00d7\u2026\u2192\u3001\u3002\u3010\u3011\u4e00\u4e0a\u4e0b\u4e0e\u4e14\u4e24\u4e2a\u4e2d\u4e30\u4e32\u4e45\u4e4b\u4eae\u4eba\u4ece\u4ee5\u4efb\u4f10\u4f11\u4f18\u4f1a\u4f24\u4f4d\u4f53\u4f59\u4f5c\u4fa7\u500d\u505c\u50cf\u5148\u5149\u5165\u5173\u518d\u51b2\u51c6\u51fa\u51fb\u5206\u5229\u5230\u5237\u523b\u524d\u5269\u52a0\u52a8\u52b1\u5316\u5347\u534a\u534e\u5355\u5361\u536b\u5373\u53bb\u53cc\u53d1\u53d6\u53d7\u53d8\u53e0\u53ea\u53ef\u5404\u5408\u540c\u540e\u5468\u547d\u548c\u54c1\u54ea\u5668\u56de\u56f4\u5708\u5728\u5730\u573a\u57df\u57fa\u5806\u589e\u5907\u590d\u5916\u591a\u591c\u5927\u5929\u5956\u597d\u59cb\u5b58\u5b88\u5bb3\u5bb9\u5bcc\u5c04\u5c0f\u5c11\u5c31\u5c3e\u5c40\u5c42\u5c4f\u5c5e\u5e55\u5e76\u5e78\u5e87\u5ea6\u5f00\u5f20\u5f3a\u5f53\u5f55\u5f71\u5f80\u5f81\u5f84\u5f97\u5feb\u6012\u6027\u602a\u6062\u606f\u610f\u6210\u6218\u6240\u624d\u6253\u6254\u6263\u6269\u628a\u62a4\u62b5\u62c9\u62d6\u62e9\u62fe\u62ff\u6301\u6309\u6311\u6389\u6392\u63a5\u63a8\u63d0\u653b\u653e\u654c\u6563\u6570\u6597\u65b0\u65b9\u65c5\u65cb\u65e0\u65f6\u6613\u661f\u6625\u6653\u6682\u6696\u6697\u66b4\u66f4\u6700\u6709\u671d\u671f\u6740\u6761\u6765\u679a\u6807\u6837\u69fd\u6b21\u6b47\u6b65\u6b66\u6bcf\u6bd4\u6cd5\u6ce2\u6d41\u6d6a\u6e05\u6ee1\u6ee9\u6f2b\u706b\u706d\u706f\u707c\u70b8\u70b9\u70bd\u70c8\u70e7\u70eb\u70ec\u70ed\u7130\u7184\u71c3\u7206\u7247\u724c\u7269\u72c2\u730e\u73af\u7403\u751f\u7528\u7531\u7554\u7559\u75bc\u75be\u767d\u767e\u7684\u76c8\u76ee\u76f4\u76f8\u76fe\u7740\u77e2\u77f3\u7834\u7838\u7840\u795d\u798f\u79cd\u79d2\u79ef\u79fb\u7a0b\u7a0d\u7a7a\u7a7f\u7acb\u7b2c\u7b49\u7b80\u7bad\u7ea2\u7ea7\u7eaa\u7ebf\u7ec8\u7ed5\u7ed9\u7ee7\u7eed\u7efd\u7ffb\u8005\u800c\u80dc\u811a\u81ea\u81f3\u8272\u8303\u8350\u83b7\u8425\u843d\u8774\u8776\u8840\u884c\u8865\u88c2\u88c5\u88f9\u89e3\u8b66\u8ba1\u8ba4\u8bb0\u8c01\u8d25\u8d28\u8d34\u8d70\u8d74\u8d77\u8d8a\u8def\u8e0f\u8e29\u8eab\u8f6c\u8f7b\u8fb9\u8fc7\u8fce\u8fd0\u8fd1\u8fd4\u8fd8\u8fd9\u8fdb\u8ffd\u9000\u9009\u900f\u9014\u901a\u901f\u9020\u9053\u91ca\u91cc\u91cf\u91d1\u94bb\u94dc\u94f6\u9501\u950b\u952e\u9556\u957f\u964d\u9650\u968f\u96be\u96e8\u9752\u9760\u9762\u9879\u9884\u9897\u989d\u98ce\u98de\u9ad8\u9ec4\u9ed8\u9f50\uff01\uff08\uff09\uff0c\uff1a\u300c\u300d\u592a\u594f\u60a8\u6162\u620f\u6574\u6b64\u6e38\u7387\u7f6e\u8282\u89c9\u8bbe\u8c03\u901d\u95ed\u95f4\uff1f\u0078\u4e09\u51b3\u5c06\u6bb5\u6ce8\u70bc\u767b\u80fd\u88ad\u8bd5\u907f\u9636\u0042\u0073\u662f\u72ec\uff1b";

        // Characters that must exist before the first frame the player can read. Everything the
        // loading page draws, plus the main menu title / tagline / two buttons / the one-time
        // speed hint. Deliberately narrow: each character costs atlas space and rasterisation
        // time on the critical path, and anything missed here is still filled in on demand by the
        // dynamic atlas rather than rendering as a blank box.
        //
        // "Loading fonts..." / "Loading..." are Latin and already covered by the build-time
        // default font, but they are cheap so they stay for clarity.
        public const string FirstScreenGlyphSet =
            "\u70ec\u706f\u884c\u8005"                 // 烬灯行者
            + "\u63d0\u706f\u5165\u591c\uff0c\u4ee5\u706b\u7834\u6653\u3002" // 提灯入夜，以火破晓。
            + "\u70b9\u4eae\u706f\u706b\u2026"          // 点亮灯火…
            + "\u6309\u4efb\u610f\u952e\u7ee7\u7eed\u70b9\u51fb\u5c4f\u5e55\u5f00\u59cb" // 按任意键继续·点击屏幕开始
            + "\u70b9\u4eae\u65c5\u7a0b"               // 点亮旅程
            + "\u8bbe\u7f6e"                          // 设置
            + "\u6f2b\u957f\u5f81\u7a0b"               // 漫长征程
            + "\u81ea\u7531\u9009\u62e9\u96be\u5ea6\uff0c\u6bcf\u5c40\u4ece\u76f8\u540c\u7684\u57fa\u7840\u5c5e\u6027\u51fa\u53d1\u3002"
            + "\u6ce2\u6b21\u5956\u52b1\u9009\u5361\uff0c\u51fb\u8d25\u6700\u7ec8\u5b88\u536b\u3002"
            + "\u89c9\u5f97\u592a\u6162\uff1f\u8bbe\u7f6e\u91cc\u65c5\u9014\u8282\u594f\u53ef\u4ee5\u8c03\u5230\u500d\u70b9\u6b64\u5173\u95ed"
            + " %()+-.0123456789:\u00b7\u00d7\u2026";   // digits / units the loading bar and menu show


        const string SourcePath = "Fonts/NotoSansCJKsc-Regular";

        /// <summary>
        /// Optional pre-baked Static atlas, written by Emberlight/Bake Chinese font atlas.
        ///
        /// When this loads and passes ValidateBaked it replaces the runtime build entirely:
        /// no 16 MB otf read, no CreateFontAsset, no glyph rasterisation, and the text is
        /// drawable on the very first frame. When it is absent or fails validation the runtime
        /// path below takes over unchanged, so a bad bake degrades to the current behaviour
        /// instead of to a crash.
        /// </summary>
        const string BakedAssetPath = "Fonts/NotoSansCJKsc-Regular SDF";

        /// <summary>Distinct characters the runtime would populate. Shared with the baker.</summary>
        public static string AllGlyphs { get { return GlyphSet + CardGlyphSet() + MenuGlyphSet(); } }

        public static TMP_FontAsset CreateChinese()
        {
            var baked = Resources.Load<TMP_FontAsset>(BakedAssetPath);
            if (baked != null && ValidateBaked(baked))
            {
                Debug.Log("[EmberFonts] using pre-baked atlas (" + baked.characterTable.Count
                    + " glyphs, " + baked.atlasTexture.width + "x" + baked.atlasTexture.height + ")");
                return baked;
            }
            return CreateChineseDynamic();
        }

        /// <summary>
        /// Assigns <paramref name="font"/> to every TextMeshProUGUI under <paramref name="root"/>.
        ///
        /// TMP_Settings.defaultFontAsset cannot be used for this: in TMP 3.0.7 it is a get-only
        /// property over a private serialized field, so there is no runtime way to change the
        /// project default. Passing the font in explicitly is therefore the only mechanism, and
        /// this is the helper that will make editor-authored hierarchies work once they exist --
        /// a font created at runtime cannot be referenced from a scene or prefab.
        ///
        /// The loading screen and every panel built by EmberMenuUi already receive the font
        /// directly, so nothing depends on this yet.
        /// </summary>
        public static void ApplyFont(Transform root, TMP_FontAsset font)
        {
            if (root == null || font == null) return;
            var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].font = font;
        }

        /// <summary>
        /// A pre-baked atlas is only usable if the parts TMP actually dereferences at draw time
        /// are present and coherent. The first attempt at this shipped an asset that passed every
        /// serialised-field check and then threw on the first label drawn -- once with an
        /// unresolved m_AtlasTextures slot, once with glyphs pointing at an atlas index that did
        /// not exist. Both are cheap to detect here, so they are checked rather than trusted.
        /// </summary>
        static bool ValidateBaked(TMP_FontAsset font)
        {
            if (font.atlasTextures == null || font.atlasTextures.Length == 0 || font.atlasTextures[0] == null)
            {
                Debug.LogWarning("[EmberFonts] pre-baked atlas has no atlas texture; falling back to runtime bake");
                return false;
            }
            var texture = font.atlasTextures[0];
            if (texture.width <= 0 || texture.height <= 0)
            {
                Debug.LogWarning("[EmberFonts] pre-baked atlas texture is empty; falling back to runtime bake");
                return false;
            }
            if (font.characterTable == null || font.characterTable.Count == 0)
            {
                Debug.LogWarning("[EmberFonts] pre-baked atlas has no characters; falling back to runtime bake");
                return false;
            }
            if (font.material == null)
            {
                Debug.LogWarning("[EmberFonts] pre-baked atlas has no material; falling back to runtime bake");
                return false;
            }
            // Every entry must point at a glyph record that exists, or TMP dereferences a null
            // mid-draw on whichever frame first shows the character.
            for (int i = 0; i < font.characterTable.Count; i++)
            {
                var record = font.characterTable[i];
                if (record == null) continue;
                uint index = record.glyphIndex;
                if (!font.glyphLookupTable.ContainsKey(index))
                {
                    Debug.LogWarning("[EmberFonts] pre-baked atlas has a character with no glyph (U+"
                        + record.unicode.ToString("X4") + "); falling back to runtime bake");
                    return false;
                }
            }

            // Each glyph's atlasIndex must be a valid index into atlasTextures. This is the check
            // whose absence let the previous bake ship: TMP_MaterialManager.GetFallbackMaterial
            // does `atlasTextures[atlasIndex]` unguarded, so a glyph pointing at atlas 1 of a
            // 1-atlas asset throws IndexOutOfRangeException from deep inside TMP, on the first
            // frame that draws. The array itself being non-empty is not sufficient.
            int atlasCount = font.atlasTextures.Length;
            int worst = -1;
            for (int i = 0; i < font.glyphTable.Count; i++)
            {
                var glyph = font.glyphTable[i];
                if (glyph == null) continue;
                if (glyph.atlasIndex > worst) worst = glyph.atlasIndex;
                if (glyph.atlasIndex < 0 || glyph.atlasIndex >= atlasCount)
                {
                    Debug.LogWarning("[EmberFonts] pre-baked atlas glyph index " + glyph.index
                        + " wants atlas " + glyph.atlasIndex + " but the asset has " + atlasCount
                        + " atlas texture(s); falling back to runtime bake");
                    return false;
                }
            }

            Debug.Log("[EmberFonts] pre-baked atlas validated: " + font.atlasTextures.Length
                + " atlas texture(s), " + font.characterTable.Count + " characters, "
                + font.glyphTable.Count + " glyphs, max atlasIndex " + worst);
            return true;
        }

        static TMP_FontAsset CreateChineseDynamic()
        {
            // Deliberately no pre-baked asset path here. Two attempts at saving a Static TMP
            // atlas for this project produced assets that loaded fine and then threw on the
            // first label drawn -- one with an unresolved m_AtlasTextures slot, one with
            // glyphs pointing at an atlas index that did not exist. Both shipped past
            // asset-field checks because the tables looked healthy. The dynamic atlas cannot
            // fail this way: TMP owns the texture and builds it in memory, so it is always
            // internally consistent. The measured cost is small (CreateFontAsset ~9 ms in the
            // editor), which does not buy back a class of crash that is invisible until
            // something draws text.
            //
            // Only FirstScreenGlyphSet is added here. The rest of GlyphSet goes through
            // PrewarmInBackground once the loading page is already up, so the ~90 ms of atlas
            // population is no longer on the critical path to the first readable frame.
            var source = Resources.Load<Font>(SourcePath);
            if (source == null)
            {
                Debug.LogError("[EmberFonts] missing font Resources/" + SourcePath);
                return null;
            }
            var font = TMP_FontAsset.CreateFontAsset(
                source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            if (font == null)
            {
                Debug.LogError("[EmberFonts] TMP_FontAsset.CreateFontAsset failed");
                return null;
            }
            font.name = "NotoSansCJKsc-Regular Dynamic";
            int missing = AddCharacters(font, FirstScreenGlyphSet);
            if (missing > 0)
                Debug.LogWarning("[EmberFonts] first-screen missing glyphs: " + missing);
            return font;
        }

        /// <summary>
        /// Adds the rest of GlyphSet -- plus every character the upgrade / starter cards can
        /// draw -- across several frames, <paramref name="perFrame"/> characters at a time.
        ///
        /// TMP 3.0.7 has no asynchronous TryAddCharacters; every overload is synchronous. The
        /// previous code pushed all 2498 characters of GlyphSet in one call, which measured
        /// ~90 ms and was the reason the loading screen had to be padded out. Slicing it keeps
        /// the same total work but spreads it over frames, and the loading screen already holds
        /// for at least five seconds (EmberIntro minShow), so it finishes long before the menu
        /// is reachable.
        ///
        /// The card text is included because it is the one place the player reads a lot of
        /// characters they have never seen: GlyphSet was missing 38 of them (不乘仅全冕几力区原
        /// 召唤团均处对尽幅应延截折掷效日本杨沿海消添溅燎耗薪贯轰连马), and each first appearance
        /// rasterised a glyph and rebuilt the whole atlas texture mid-run. Late-run content that
        /// still is not listed here falls back to on-demand population as before.
        /// </summary>
        public static System.Collections.IEnumerator PrewarmInBackground(TMP_FontAsset font, int perFrame = 48)
        {
            if (font == null) yield break;
            // A pre-baked Static atlas already holds every glyph, and by design cannot accept
            // more -- pushing characters into it is the failure the previous bake attempt hit.
            // Nothing to do; the whole point of the bake is that this coroutine disappears.
            if (font.atlasPopulationMode == AtlasPopulationMode.Static) yield break;
            string remaining = Complement(AllGlyphs, FirstScreenGlyphSet);
            if (remaining.Length == 0) yield break;
            for (int start = 0; start < remaining.Length; start += perFrame)
            {
                int take = Mathf.Min(perFrame, remaining.Length - start);
                int missing = AddCharacters(font, remaining.Substring(start, take));
                if (missing > 0)
                    Debug.LogWarning("[EmberFonts] background prewarm missing glyphs: " + missing);
                yield return null;
            }
        }

        /// <summary>
        /// Every character any player-facing screen can render.
        ///
        /// Derived from the live arrays and the named copy constants rather than typed out by
        /// hand, so editing text cannot silently fall out of sync with the prewarm -- which is
        /// precisely how the difficulty screen shipped with hollow boxes: its copy lives in
        /// EmberGame.ChooseLength and was never part of the glyph set, so the bake never asked
        /// for 休闲/验证/精英/困难/降低 and the atlas had no glyphs for them.
        ///
        /// Deduplicated because TMP charges atlas space per distinct glyph.
        /// </summary>
        public static string CardGlyphSet()
        {
            var seen = new System.Collections.Generic.HashSet<char>();
            var keep = new System.Text.StringBuilder();
            Collect(keep, seen, EmberGame.UpgradeNames);
            Collect(keep, seen, EmberGame.UpgradeDetails);
            Collect(keep, seen, EmberGodsSelectPanel.RosterNames);
            Collect(keep, seen, EmberGodsSelectPanel.RosterDescs);
            Collect(keep, seen, EmberRarityUtil.AllNames);
            Collect(keep, seen, ExtraCardText);
            return keep.ToString();
        }

        /// <summary>
        /// Menu and run-setup copy: title, taglines, button labels, difficulty blurbs, pause and
        /// result screens.
        ///
        /// These read from named constants on EmberGame rather than from the literals themselves,
        /// because the strings they replace were inline inside method bodies -- 困难面对更多精英
        /// lived in ChooseLength, reachable by neither reflection nor array-walking, so the bake
        /// never requested those glyphs and the difficulty screen drew hollow boxes.
        ///
        /// DifficultyBody is a method (it prefixes the wave count), so its fixed wording is covered
        /// through DifficultyBodyShape. The varying part is digits, which the Latin set covers.
        /// </summary>
        public static string MenuGlyphSet()
        {
            var seen = new System.Collections.Generic.HashSet<char>();
            var keep = new System.Text.StringBuilder();
            Collect(keep, seen, FirstScreenGlyphSet);
            Collect(keep, seen, EmberGame.MenuTitle);
            Collect(keep, seen, EmberGame.MenuTagline);
            Collect(keep, seen, EmberGame.MenuChoices);
            Collect(keep, seen, EmberGame.MenuHint);
            Collect(keep, seen, EmberGame.RunSetupTitle);
            Collect(keep, seen, EmberGame.RunSetupBody);
            Collect(keep, seen, EmberGame.MenuChoicesWithBack);
            Collect(keep, seen, EmberGame.DifficultyTitle);
            Collect(keep, seen, EmberGame.DifficultyBodyShape);
            Collect(keep, seen, EmberGame.DifficultyChoices);
            Collect(keep, seen, EmberGame.PauseTitle);
            Collect(keep, seen, EmberGame.PauseBody);
            Collect(keep, seen, EmberGame.PauseChoice);
            Collect(keep, seen, EmberGame.WinTitle);
            Collect(keep, seen, EmberGame.LoseTitle);
            Collect(keep, seen, EmberGame.RestartChoice);
            Collect(keep, seen, EmberGame.ReturnChoice);
            Collect(keep, seen, EmberGame.ResultShape);
            Collect(keep, seen, ExtraVisibleGlyphs);
            return keep.ToString();
        }

        /// <summary>
        /// Characters the atlas must hold but that no string literal asks for.
        ///
        /// Two categories:
        ///
        /// 1. TMP's special characters. TMP_Text.GetSpecialCharacters resolves U+005F (underline)
        ///    and U+2026 (ellipsis) against the font asset on every font assignment, and logs a
        ///    warning per TextMeshProUGUI when either is absent. U+005F is the one that bites: it
        ///    is ASCII, so it looks "obviously present", and no game string contains it -- which
        ///    means neither the hand-written ASCII whitelist in GlyphSet nor any scan of string
        ///    literals can find the gap. Tools/atlas_glyph_check.py checks these codepoints
        ///    explicitly for that reason.
        ///
        /// 2. Copy still living inside method bodies -- mostly the settings screen, the enemy
        ///    roster and a few card and result strings. Reachable by neither reflection nor
        ///    array-walking, which is how 休闲适合轻松构筑 came to render as hollow boxes.
        ///
        /// Produced by Tools/atlas_glyph_check.py, which reads the compiled Assembly-CSharp.dll
        /// #US string heap -- so it sees every literal in the game regardless of where it sits.
        /// Re-run that script after adding copy; it exits non-zero when characters are missing and
        /// prints the exact literal to paste here.
        /// </summary>
        const string ExtraVisibleGlyphs =
            // TMP special characters. Required, not decorative: see the class comment above.
            "\u005f\u2026"
            // 音乐 音效 静音 / 铁灯卫 引线虫 / 放弃 遭遇 / 阵亡 队伍 / 渐涌 潮 烛 背 景 确 交 乐 声 律 —
            + "\u2014\u4e50\u4ea4\u58f0\u5f03\u5f15\u5f8b\u666f\u6d8c\u6e10\u6f6e\u70db"
            + "\u786e\u80cc\u866b\u8bf7\u9047\u906d\u94c1\u961f\u9635\u9759\u97f3";

        // Literals the cards build at runtime, so they cannot be read off an array: the option
        // badges, card headings, the offer panel's own chrome, and the stat-preview wording.
        const string ExtraCardText =
            "\u4e13\u5c5e\u8d28\u53d8\u65b0\u6b66\u5668\u6b66\u5668"          // 专属 质变 新武器 武器
            + "\u706f\u706b\u5347\u534e\u6ce2\u6b21\u5956\u52b1\u9009\u4e00\u9879\u5237\u65b0\u65e0\u4e0a\u4e0b\u6ed1\u52a8\u67e5\u770b\u5168\u90e8\u9009\u9879" // 灯火升华 波次奖励 选一项 刷新 无 上下滑动查看全部选项
            + "\u62e5\u6709\u89e3\u9501\u88c5\u5165\u69fd\u4f4d\u5df2"           // 拥有 解锁 装入槽位 已
            + "\u62a4\u76fe\u5f53\u524d\u5408\u8ba1\u4f24\u5bb3\u653b\u51fb\u901f\u5ea6\u5e78\u8fd0\u589e\u5e45" // 护盾 当前 合计 伤害 攻击 速度 幸运 增幅
            + "\u53ea\u63d0\u9ad8\u300c\u300d\u7684\u7acb\u5373\u83b7\u5f97\u53ef\u53e0\u52a0\u4ec5\u672c\u5c40\u6709\u6548\u53d7\u4f24\u4f18\u5148\u6d88\u8017\r\n" // 只提高「」的 立即获得 可叠加 仅本局有效 受伤优先消耗
            + " +-%()0123456789/.";

        static void Collect(System.Text.StringBuilder keep, System.Collections.Generic.HashSet<char> seen, string[] values)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                Collect(keep, seen, values[i]);
        }

        static void Collect(System.Text.StringBuilder keep, System.Collections.Generic.HashSet<char> seen, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            for (int i = 0; i < value.Length; i++)
                if (seen.Add(value[i])) keep.Append(value[i]);
        }

        /// <summary>Returns how many characters of <paramref name="characters"/> the atlas refused.</summary>
        static int AddCharacters(TMP_FontAsset font, string characters)
        {
            string missing;
            font.TryAddCharacters(characters, out missing);
            return string.IsNullOrEmpty(missing) ? 0 : missing.Length;
        }

        /// <summary>
        /// <paramref name="all"/> minus every character present in <paramref name="already"/>.
        /// Used to keep the synchronous and chunked passes from rasterising the same glyph twice
        /// and double-counting its atlas space.
        /// </summary>
        static string Complement(string all, string already)
        {
            var keep = new System.Text.StringBuilder(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                char c = all[i];
                if (already.IndexOf(c) < 0) keep.Append(c);
            }
            return keep.ToString();
        }
    }
}
