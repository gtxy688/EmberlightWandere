using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Emberlight
{
    public static class EmberFonts
    {
        // 字形预热表，分两批加入：FirstScreenGlyphSet 同步加（加载页与主菜单要立刻能读），
        // 其余交给 PrewarmInBackground 分帧处理，避免把约 90 ms 的图集构建压到首帧。
        // 表不追求穷尽：UI 里多出来的字由动态图集按需补齐，不会显示成方框。
        public const string GlyphSet = " %()+-./0123456789:DFLPS[]adefgilnot\u00b7\u00d7\u2026\u2192\u3001\u3002\u3010\u3011\u4e00\u4e0a\u4e0b\u4e0e\u4e14\u4e24\u4e2a\u4e2d\u4e30\u4e32\u4e45\u4e4b\u4eae\u4eba\u4ece\u4ee5\u4efb\u4f10\u4f11\u4f18\u4f1a\u4f24\u4f4d\u4f53\u4f59\u4f5c\u4fa7\u500d\u505c\u50cf\u5148\u5149\u5165\u5173\u518d\u51b2\u51c6\u51fa\u51fb\u5206\u5229\u5230\u5237\u523b\u524d\u5269\u52a0\u52a8\u52b1\u5316\u5347\u534a\u534e\u5355\u5361\u536b\u5373\u53bb\u53cc\u53d1\u53d6\u53d7\u53d8\u53e0\u53ea\u53ef\u5404\u5408\u540c\u540e\u5468\u547d\u548c\u54c1\u54ea\u5668\u56de\u56f4\u5708\u5728\u5730\u573a\u57df\u57fa\u5806\u589e\u5907\u590d\u5916\u591a\u591c\u5927\u5929\u5956\u597d\u59cb\u5b58\u5b88\u5bb3\u5bb9\u5bcc\u5c04\u5c0f\u5c11\u5c31\u5c3e\u5c40\u5c42\u5c4f\u5c5e\u5e55\u5e76\u5e78\u5e87\u5ea6\u5f00\u5f20\u5f3a\u5f53\u5f55\u5f71\u5f80\u5f81\u5f84\u5f97\u5feb\u6012\u6027\u602a\u6062\u606f\u610f\u6210\u6218\u6240\u624d\u6253\u6254\u6263\u6269\u628a\u62a4\u62b5\u62c9\u62d6\u62e9\u62fe\u62ff\u6301\u6309\u6311\u6389\u6392\u63a5\u63a8\u63d0\u653b\u653e\u654c\u6563\u6570\u6597\u65b0\u65b9\u65c5\u65cb\u65e0\u65f6\u6613\u661f\u6625\u6653\u6682\u6696\u6697\u66b4\u66f4\u6700\u6709\u671d\u671f\u6740\u6761\u6765\u679a\u6807\u6837\u69fd\u6b21\u6b47\u6b65\u6b66\u6bcf\u6bd4\u6cd5\u6ce2\u6d41\u6d6a\u6e05\u6ee1\u6ee9\u6f2b\u706b\u706d\u706f\u707c\u70b8\u70b9\u70bd\u70c8\u70e7\u70eb\u70ec\u70ed\u7130\u7184\u71c3\u7206\u7247\u724c\u7269\u72c2\u730e\u73af\u7403\u751f\u7528\u7531\u7554\u7559\u75bc\u75be\u767d\u767e\u7684\u76c8\u76ee\u76f4\u76f8\u76fe\u7740\u77e2\u77f3\u7834\u7838\u7840\u795d\u798f\u79cd\u79d2\u79ef\u79fb\u7a0b\u7a0d\u7a7a\u7a7f\u7acb\u7b2c\u7b49\u7b80\u7bad\u7ea2\u7ea7\u7eaa\u7ebf\u7ec8\u7ed5\u7ed9\u7ee7\u7eed\u7efd\u7ffb\u8005\u800c\u80dc\u811a\u81ea\u81f3\u8272\u8303\u8350\u83b7\u8425\u843d\u8774\u8776\u8840\u884c\u8865\u88c2\u88c5\u88f9\u89e3\u8b66\u8ba1\u8ba4\u8bb0\u8c01\u8d25\u8d28\u8d34\u8d70\u8d74\u8d77\u8d8a\u8def\u8e0f\u8e29\u8eab\u8f6c\u8f7b\u8fb9\u8fc7\u8fce\u8fd0\u8fd1\u8fd4\u8fd8\u8fd9\u8fdb\u8ffd\u9000\u9009\u900f\u9014\u901a\u901f\u9020\u9053\u91ca\u91cc\u91cf\u91d1\u94bb\u94dc\u94f6\u9501\u950b\u952e\u9556\u957f\u964d\u9650\u968f\u96be\u96e8\u9752\u9760\u9762\u9879\u9884\u9897\u989d\u98ce\u98de\u9ad8\u9ec4\u9ed8\u9f50\uff01\uff08\uff09\uff0c\uff1a\u300c\u300d\u592a\u594f\u60a8\u6162\u620f\u6574\u6b64\u6e38\u7387\u7f6e\u8282\u89c9\u8bbe\u8c03\u901d\u95ed\u95f4\uff1f\u0078\u4e09\u51b3\u5c06\u6bb5\u6ce8\u70bc\u767b\u80fd\u88ad\u8bd5\u907f\u9636\u0042\u0073\u662f\u72ec\uff1b";
        // 玩家读到第一帧之前就必须存在的字：加载页、主菜单标题/标语/按钮、一次性倍速提示。
        // 故意收窄——每个字都占图集空间与光栅化时间；漏掉的字由动态图集按需补齐。
        public const string FirstScreenGlyphSet = "\u70ec\u706f\u884c\u8005" // 烬灯行者
 + "\u63d0\u706f\u5165\u591c\uff0c\u4ee5\u706b\u7834\u6653\u3002" // 提灯入夜，以火破晓。
 + "\u70b9\u4eae\u706f\u706b\u2026" // 点亮灯火…
 + "\u6309\u4efb\u610f\u952e\u7ee7\u7eed\u70b9\u51fb\u5c4f\u5e55\u5f00\u59cb" // 按任意键继续·点击屏幕开始
 + "\u70b9\u4eae\u65c5\u7a0b" // 点亮旅程
 + "\u8bbe\u7f6e" // 设置
 + "\u6f2b\u957f\u5f81\u7a0b" // 漫长征程
 + "\u81ea\u7531\u9009\u62e9\u96be\u5ea6\uff0c\u6bcf\u5c40\u4ece\u76f8\u540c\u7684\u57fa\u7840\u5c5e\u6027\u51fa\u53d1\u3002" + "\u6ce2\u6b21\u5956\u52b1\u9009\u5361\uff0c\u51fb\u8d25\u6700\u7ec8\u5b88\u536b\u3002" + "\u89c9\u5f97\u592a\u6162\uff1f\u8bbe\u7f6e\u91cc\u65c5\u9014\u8282\u594f\u53ef\u4ee5\u8c03\u5230\u500d\u70b9\u6b64\u5173\u95ed" + " %()+-.0123456789:\u00b7\u00d7\u2026"; // digits / units the loading bar and menu show
        const string SourcePath = "Fonts/NotoSansCJKsc-Regular";
        /// <summary>
        /// 可选：预烘静态图集，由 `Emberlight/Bake Chinese font atlas` 生成。
        /// 加载并通过 <see cref="ValidateBaked"/> 后完全替代运行时构建（不读 16 MB otf、
        /// 不跑 CreateFontAsset、不做字形光栅化）；缺失或校验失败时回落到动态图集。
        /// </summary>
        const string BakedAssetPath = "Fonts/NotoSansCJKsc-Regular SDF";
        /// <summary>运行时应当覆盖的全部字符；与烘制工具共用。</summary>
        public static string AllGlyphs
        {
            get
            {
                return GlyphSet + CardGlyphSet() + MenuGlyphSet()
                    + EmberBuffCatalog.Power.Name + EmberBuffCatalog.Haste.Name + EmberBuffCatalog.Slow.Name + EmberCopy.BuffGlyphs;
            }
        }

        public static TMP_FontAsset CreateChinese()
        {
            var baked = Resources.Load<TMP_FontAsset>(BakedAssetPath);
            if (baked != null && ValidateBaked(baked))
            {
                Debug.Log("[EmberFonts] using pre-baked atlas (" + baked.characterTable.Count + " glyphs, " + baked.atlasTexture.width + "x" + baked.atlasTexture.height + ")");
                return baked;
            }

            return CreateChineseDynamic();
        }

        /// <summary>
        /// 预烘图集只有在 TMP 绘制时真正解引用的部分齐全且自洽时才可用。
        /// 这里逐项校验：图集纹理、字符表、材质，以及每个字符都有对应 glyph、
        /// 每个 glyph 的 atlasIndex 都落在图集数组范围内——缺任何一项都回落动态图集。
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

            // 每个字符都必须指向存在的 glyph，否则 TMP 会在首次绘制该字的帧上解引用 null。
            for (int i = 0; i < font.characterTable.Count; i++)
            {
                var record = font.characterTable[i];
                if (record == null)
                    continue;
                uint index = record.glyphIndex;
                if (!font.glyphLookupTable.ContainsKey(index))
                {
                    Debug.LogWarning("[EmberFonts] pre-baked atlas has a character with no glyph (U+" + record.unicode.ToString("X4") + "); falling back to runtime bake");
                    return false;
                }
            }

            // 每个 glyph 的 atlasIndex 必须是 atlasTextures 的合法下标：TMP 的
            // TMP_MaterialManager.GetFallbackMaterial 会直接做 atlasTextures[atlasIndex]，
            // 越界会在首次绘制时从 TMP 内部抛 IndexOutOfRangeException。
            int atlasCount = font.atlasTextures.Length;
            int worst = -1;
            for (int i = 0; i < font.glyphTable.Count; i++)
            {
                var glyph = font.glyphTable[i];
                if (glyph == null)
                    continue;
                if (glyph.atlasIndex > worst)
                    worst = glyph.atlasIndex;
                if (glyph.atlasIndex < 0 || glyph.atlasIndex >= atlasCount)
                {
                    Debug.LogWarning("[EmberFonts] pre-baked atlas glyph index " + glyph.index + " wants atlas " + glyph.atlasIndex + " but the asset has " + atlasCount + " atlas texture(s); falling back to runtime bake");
                    return false;
                }
            }

            Debug.Log("[EmberFonts] pre-baked atlas validated: " + font.atlasTextures.Length + " atlas texture(s), " + font.characterTable.Count + " characters, " + font.glyphTable.Count + " glyphs, max atlasIndex " + worst);
            return true;
        }

        static TMP_FontAsset CreateChineseDynamic()
        {
            // 这里刻意不读预烘资产：动态图集由 TMP 自己持有纹理并在内存里构建，
            // 永远内部自洽；预烘资产一旦字段不齐就会在首次绘制时抛异常。
            // 只同步加入 FirstScreenGlyphSet，其余走 PrewarmInBackground。
            var source = Resources.Load<Font>(SourcePath);
            if (source == null)
            {
                Debug.LogError("[EmberFonts] missing font Resources/" + SourcePath);
                return null;
            }

            var font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
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
        /// 分帧加入 GlyphSet 的剩余部分，外加卡面可能画出的全部字符，每帧
        /// <paramref name = "perFrame"/> 个。TMP 3.0.7 的 TryAddCharacters 全是同步的，
        /// 一次性推入会卡住首帧；加载页至少展示 5 秒，足够在玩家进菜单前铺完。
        /// </summary>
        public static System.Collections.IEnumerator PrewarmInBackground(TMP_FontAsset font, int perFrame = 48)
        {
            if (font == null)
                yield break;
            // 预烘静态图集本身已含全部字形，且按设计不能再追加——直接跳过。
            if (font.atlasPopulationMode == AtlasPopulationMode.Static)
                yield break;
            string remaining = Complement(AllGlyphs, FirstScreenGlyphSet);
            if (remaining.Length == 0)
                yield break;
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
        /// 玩家界面可能渲染的每一个字符。
        /// 从实时数组与命名的文案常量**推导**得出，不手写，避免改文案后预热表失同步
        /// （漏字会静默显示成空心方框）。按字符去重，因为 TMP 按不同字形占图集空间。
        /// </summary>
        public static string CardGlyphSet()
        {
            var seen = new System.Collections.Generic.HashSet<char>();
            var keep = new System.Text.StringBuilder();
            Collect(keep, seen, EmberCopy.UpgradeNames);
            Collect(keep, seen, EmberCopy.UpgradeDetails);
            Collect(keep, seen, EmberRarityUtil.AllNames);
            Collect(keep, seen, ExtraCardText);
            return keep.ToString();
        }

        /// <summary>
        /// 菜单与开局流程的文案：标题、标语、按钮、难度说明、暂停与结算页。
        /// 从 <see cref="EmberCopy"/> 的命名常量读取，而不是抄字面量，否则改文案会悄悄漏出预热表。
        /// </summary>
        public static string MenuGlyphSet()
        {
            var seen = new System.Collections.Generic.HashSet<char>();
            var keep = new System.Text.StringBuilder();
            Collect(keep, seen, FirstScreenGlyphSet);
            Collect(keep, seen, EmberCopy.MenuTitle);
            Collect(keep, seen, EmberCopy.SlotsTitle);
            Collect(keep, seen, EmberCopy.SlotsSummary);
            Collect(keep, seen, EmberCopy.RosterTitle);
            Collect(keep, seen, EmberCopy.RosterSummary);
            Collect(keep, seen, EmberCopy.SlotChoices);
            Collect(keep, seen, EmberCopy.MenuTagline);
            Collect(keep, seen, EmberCopy.MenuChoices);
            Collect(keep, seen, EmberCopy.MenuHint);
            Collect(keep, seen, EmberCopy.RunSetupTitle);
            Collect(keep, seen, EmberCopy.RunSetupBody);
            Collect(keep, seen, EmberCopy.MenuChoicesWithBack);
            Collect(keep, seen, EmberCopy.DifficultyTitle);
            Collect(keep, seen, EmberCopy.DifficultyBodyShape);
            Collect(keep, seen, EmberCopy.DifficultyChoices);
            Collect(keep, seen, EmberCopy.PauseTitle);
            Collect(keep, seen, EmberCopy.PauseBody);
            Collect(keep, seen, EmberCopy.PauseChoice);
            Collect(keep, seen, EmberCopy.WinTitle);
            Collect(keep, seen, EmberCopy.LoseTitle);
            Collect(keep, seen, EmberCopy.RestartChoice);
            Collect(keep, seen, EmberCopy.ReturnChoice);
            Collect(keep, seen, EmberCopy.ResultShape);
            Collect(keep, seen, ExtraVisibleGlyphs);
            return keep.ToString();
        }

        /// <summary>
        /// 图集必须持有、但没有任何字符串字面量会要求的字符：
        /// 1. TMP 特殊字符 U+005F（下划线）与 U+2026（省略号）——TMP 每次给
        ///    TextMeshProUGUI 赋字体都会查这两个，缺一个就每个 label 报一条警告；
        ///    下划线是 ASCII，看着"显然存在"，且没有任何游戏文案含它，所以只能在这里兜。
        /// 2. 仍写在方法体里的文案（设置页、敌人名册、部分卡面与结算串）。
        /// 由 `Tools/atlas_glyph_check.py` 从编译产物的字符串堆推导；加文案后重跑该脚本，
        /// 它非零退出时会打印需要补进来的字面量。
        /// </summary>
        const string ExtraVisibleGlyphs = // TMP 特殊字符，必须存在，不是装饰。
        "\u005f\u2026" // 音乐 音效 静音 / 铁灯卫 引线虫 / 放弃 遭遇 / 阵亡 队伍 / 渐涌 潮 烛 背景 确 交 乐 声 律 —
        + "\u2014\u4e50\u4ea4\u58f0\u5f03\u5f15\u5f8b\u666f\u6d8c\u6e10\u6f6e\u70db" + "\u786e\u80cc\u866b\u8bf7\u9047\u906d\u94c1\u961f\u9635\u9759\u97f3";
        // 卡面在运行时拼出来的字面量：选项徽章、卡片标题、选卡面板自身的文字、数值预览措辞。
        const string ExtraCardText = "\u4e13\u5c5e\u8d28\u53d8\u65b0\u6b66\u5668\u6b66\u5668" // 专属 质变 新武器 武器
 + "\u706f\u706b\u5347\u534e\u6ce2\u6b21\u5956\u52b1\u9009\u4e00\u9879\u5237\u65b0\u65e0\u4e0a\u4e0b\u6ed1\u52a8\u67e5\u770b\u5168\u90e8\u9009\u9879" // 灯火升华 波次奖励 选一项 刷新 无 上下滑动查看全部选项
 + "\u62e5\u6709\u89e3\u9501\u88c5\u5165\u69fd\u4f4d\u5df2" // 拥有 解锁 装入槽位 已
 + "\u62a4\u76fe\u5f53\u524d\u5408\u8ba1\u4f24\u5bb3\u653b\u51fb\u901f\u5ea6\u5e78\u8fd0\u589e\u5e45" // 护盾 当前 合计 伤害 攻击 速度 幸运 增幅
 + "\u53ea\u63d0\u9ad8\u300c\u300d\u7684\u7acb\u5373\u83b7\u5f97\u53ef\u53e0\u52a0\u4ec5\u672c\u5c40\u6709\u6548\u53d7\u4f24\u4f18\u5148\u6d88\u8017\r\n" // 只提高「」的 立即获得 可叠加 仅本局有效 受伤优先消耗
 + " +-%()0123456789/.";
        static void Collect(System.Text.StringBuilder keep, System.Collections.Generic.HashSet<char> seen, string[] values)
        {
            if (values == null)
                return;
            for (int i = 0; i < values.Length; i++)
                Collect(keep, seen, values[i]);
        }

        static void Collect(System.Text.StringBuilder keep, System.Collections.Generic.HashSet<char> seen, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            for (int i = 0; i < value.Length; i++)
                if (seen.Add(value[i]))
                    keep.Append(value[i]);
        }

        /// <summary>返回 <paramref name = "characters"/> 中被图集拒收的字数。</summary>
        static int AddCharacters(TMP_FontAsset font, string characters)
        {
            string missing;
            font.TryAddCharacters(characters, out missing);
            return string.IsNullOrEmpty(missing) ? 0 : missing.Length;
        }

        /// <summary>
        /// `all` 减去 `already` 中已有的字符，避免同步批与分帧批把同一个字形光栅化两次、重复占图集空间。
        /// </summary>
        static string Complement(string all, string already)
        {
            var keep = new System.Text.StringBuilder(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                char c = all[i];
                if (already.IndexOf(c) < 0)
                    keep.Append(c);
            }

            return keep.ToString();
        }
    }
}
