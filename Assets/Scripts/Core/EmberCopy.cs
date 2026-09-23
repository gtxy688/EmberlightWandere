namespace Emberlight
{
    /// <summary>
    /// 玩家可见文案的唯一出处。UI 面板不直接引用它——文案经方法参数传入。
    /// EmberFonts 依赖这里的常量决定预烘图集要包含哪些字形；改文案前先读 07-art-presentation.md。
    /// </summary>
    public static class EmberCopy
    {
        // ── Player-facing copy ────────────────────────────────────────────────────────────────
        // Named constants, not inline literals, for one specific reason: EmberFonts must bake a
        // glyph for every character it can be asked to draw, and it can only see strings that are
        // reachable. The difficulty blurb used to be an inline literal inside ChooseLength, so no
        // amount of scanning could find it, the bake never requested 闲/验/精/英/困/低, and the
        // atlas had no glyphs for them -- they rendered as hollow boxes in game.
        //
        // Keep new player-facing copy here (or in the matching panel's constants) and wire it into
        // EmberFonts.MenuGlyphSet, or it will not be in the atlas.
        public const string MenuTitle = "\u70ec\u706f\u884c\u8005";
        public const string MenuTagline =
            "25\u6ce2\u6807\u51c6\u5f81\u7a0b\u00b7\u002050\u6ce2\u6f2b\u957f\u5f81\u7a0b\n"
            + "\u81ea\u7531\u9009\u62e9\u96be\u5ea6\uff0c\u6bcf\u5c40\u4ece\u76f8\u540c\u7684\u57fa\u7840\u5c5e\u6027\u51fa\u53d1\u3002\n"
            + "\u6ce2\u6b21\u5956\u52b1\u9009\u5361\uff0c\u51fb\u8d25\u6700\u7ec8\u5b88\u536b\u3002";
        public static readonly string[] MenuChoices = { "\u70b9\u4eae\u65c5\u7a0b", "\u8bbe\u7f6e" };
        public const string MenuHint =
            "\u89c9\u5f97\u592a\u6162\uff1f\u70b9\u5f00\u300c\u8bbe\u7f6e\u300d\uff0c"
            + "\u65c5\u9014\u8282\u594f\u53ef\u4ee5\u8c03\u5230 5 \u500d\u0020\u00b7\u0020\u70b9\u6b64\u5173\u95ed";

        public const string RunSetupTitle = "\u9009\u62e9\u5f81\u7a0b";
        public const string RunSetupBody =
            "\u5c40\u957f\u4e0e\u96be\u5ea6\u72ec\u7acb\u9009\u62e9\u3002\n"
            + "\u524d 10 \u6ce2\u6bcf\u6ce2\u9009\u5361\uff0c\u4e4b\u540e\u6bcf 2 \u6ce2\u9009\u5361\u3002\n"
            + "\u9636\u6bb5 Boss \u989d\u5916\u5956\u52b1\u4e00\u6b21\u3002\n"
            + "\u6bcf 10 \u6ce2\u662f\u5b88\u536b\u8bd5\u70bc\uff0c\u6700\u540e\u4e00\u6ce2\u662f\u957f\u591c\u5b88\u536b\u3002";
        public static readonly string[] MenuChoicesWithBack =
        {
            "\u6807\u51c6\u5f81\u7a0b \u00b7 25\u6ce2\uff08\u63a8\u8350\uff09",
            "\u6f2b\u957f\u5f81\u7a0b \u00b7 50\u6ce2",
            "\u8fd4\u56de\u8425\u5730"
        };

        public const string DifficultyTitle = "\u9009\u62e9\u96be\u5ea6";
        // The blurb that exposed the whole problem. Each difficulty gets its own line so nothing
        // wraps onto an orphaned trailing character.
        public static string DifficultyBody(int waves)
        {
            return waves + " \u6ce2\u5f81\u7a0b\n"
                + "\u4f11\u95f2\u9002\u5408\u8f7b\u677e\u6784\u7b51\uff0c\n"
                + "\u6807\u51c6\u4f53\u9a8c\u5b8c\u6574\u6311\u6218\uff0c\n"
                + "\u56f0\u96be\u9762\u5bf9\u66f4\u591a\u7cbe\u82f1\u3002";
        }
        public static readonly string[] DifficultyChoices =
        {
            "\u4f11\u95f2 \u00b7 \u602a\u7269\u66f4\u5c11\uff0c\u4f24\u5bb3\u66f4\u4f4e",
            "\u6807\u51c6 \u00b7 \u5747\u8861\u6311\u6218",
            "\u56f0\u96be \u00b7 \u66f4\u591a\u602a\u7269\u4e0e\u7cbe\u82f1"
        };
        public const string DifficultyBodyShape = "\u4f11\u95f2\u9002\u5408\u8f7b\u677e\u6784\u7b51\u6807\u51c6\u4f53\u9a8c\u5b8c\u6574\u6311\u6218\u56f0\u96be\u9762\u5bf9\u66f4\u591a\u7cbe\u82f1\u6ce2\u5f81\u7a0b";

        public const string PauseTitle = "\u6682\u61a9\u706f\u7554";
        public const string PauseBody = "\u7a0d\u4f5c\u4f11\u606f\uff0c\u518d\u8d74\u957f\u591c\u3002";
        public const string PauseChoice = "\u7ee7\u7eed\u6218\u6597";
        public const string WinTitle = "\u957f\u591c\u7834\u6653";
        public const string LoseTitle = "\u706f\u706b\u6682\u7184";
        public const string RestartChoice = "\u518d\u6218\u4e00\u6b21";
        public const string ReturnChoice = "\u8fd4\u56de\u8425\u5730";
        // Fragments the result screen concatenates with run numbers.
        public const string ResultShape =
            "\u6ce2\u5f81\u7a0b\u5230\u8fbe\u7b2c\u751f\u5b58\u79d2\u51fb\u6740\u4f59\u70ec"
            + "\u4f11\u95f2\u6807\u51c6\u56f0\u96be";

        public static readonly string[] UpgradeNames =
        {
            "\u653b\u51fb\u529b",
            "\u653b\u901f",
            "\u5e78\u8fd0",
            "\u5168\u4f53\u4f24\u5bb3\u589e\u5e45",
            "护盾", "", "", "", "", "",
            "\u7a7f\u900f\u706b\u77e2",
            "\u56de\u65cb\u70ec\u8776",
            "\u5929\u964d\u706b\u96e8",
            "\u706b\u7403",
            "\u73af\u706b",
            "\u71c3\u5730",
            "", "", "", "",
            "\u71ce\u539f",
            "\u65e5\u5195",
            "\u706b\u6d77",
            "\u7a7f\u6768",
            "\u6298\u8fd4\u4e0d\u5c3d",
            "\u5929\u706b",
            "", "", "", "",
            "\u8fde\u53d1",
            "\u6dfb\u85aa",
            "\u5ef6\u71c3",
            "\u8d2f\u7a7f",
            "\u56de\u9a6c",
            "\u591a\u843d\u70b9"
        };
                public static readonly string[] UpgradeDetails =
        {
            "\u53ea\u63d0\u9ad8\u5bf9\u5e94\u6b66\u5668\u7684\u4f24\u5bb3",
            "\u53ea\u63d0\u9ad8\u5bf9\u5e94\u6b66\u5668\u7684\u653b\u901f",
            "\u66f4\u5bb9\u6613\u5237\u51fa\u597d\u5361",
            "\u6240\u6709\u6b66\u5668\u4f24\u5bb3\u518d\u4e58\u4e00\u622a",
            "立即获得护盾，受伤时优先消耗；可叠加，仅本局有效",
            "",
            "",
            "",
            "",
            "",
            // Six weapon descriptions (13..18). Deliberately byte-identical to RosterDescs in
            // EmberGodsSelectPanel: the player reads the weapon here for the first time, so the
            // run-in card must not paraphrase it. Two lines each, with the break written in
            // explicitly -- letting TMP wrap a single long sentence is what left orphaned
            // trailing characters on the starter screen.
            "\u81ea\u52a8\u53d1\u5c04\u706b\u7403\n\u653b\u51fb\u6700\u8fd1\u7684\u654c\u4eba",
            "\u706b\u7403\u73af\u7ed5\u8eab\u8fb9\n\u707c\u70e7\u9760\u8fd1\u7684\u654c\u4eba",
            "\u6cbf\u9014\u7559\u4e0b\u706b\u7130\n\u6301\u7eed\u707c\u70e7\u8e0f\u5165\u7684\u654c\u4eba",
            "\u505c\u4e0b\u811a\u6b65\u53d1\u5c04\u706b\u77e2\n\u8d2f\u7a7f\u76f4\u7ebf\u4e0a\u7684\u654c\u4eba",
            "\u63b7\u51fa\u56de\u65cb\u706b\u8774\u8776\n\u5f80\u8fd4\u5747\u53ef\u547d\u4e2d\u654c\u4eba",
            "\u6807\u8bb0\u76ee\u6807\u533a\u57df\n\u53ec\u5524\u706b\u96e8\u8f70\u51fb\u654c\u4eba",
            "",
            "",
            "",
            "",
            "\u706b\u7403\u66f4\u5927\u66f4\u70eb\uff0c\u6253\u4e2d\u4f1a\u70e7\u5730",
            "\u706b\u56e2\u8f6c\u5f97\u66f4\u5927\u66f4\u5feb\u66f4\u70eb",
            "\u8eab\u8fb9\u4e00\u76f4\u6709\u4e00\u5708\u706b",
            "\u5c04\u4e2d\u540e\u6e85\u51fa\u4e00\u5c0f\u7bad",
            "\u591a\u98de\u4e00\u4e2a\u6765\u56de",
            "\u706b\u5708\u66f4\u5927\uff0c\u7838\u5f97\u66f4\u75bc",
            "",
            "",
            "",
            "",
            "\u591a\u6253\u51fa\u4e00\u4e2a\u706b\u7403",
            "\u591a\u4e00\u9897\u56f4\u7740\u8f6c\u7684\u706b",
            "\u706b\u70e7\u5f97\u66f4\u4e45\u66f4\u70eb",
            "\u80fd\u591a\u7a7f\u51e0\u4e2a\u602a",
            "\u53bb\u548c\u56de\u591a\u6253\u4e00\u4e0b",
            "\u591a\u7838\u4e00\u5904"
        };
    }
}
