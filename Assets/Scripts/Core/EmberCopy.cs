namespace Emberlight
{
    /// <summary>
    /// 公共界面文案；武器身份与说明由 EmberWeaponCatalog 提供。EmberFonts 靠这里的常量决定预烘
    /// 图集包含哪些字形，改文案前先读 07-art-presentation.md（静态图集不能追加字形，改完要重烘）。
    /// </summary>
    public static class EmberCopy
    {
        public const string BuffRefreshed = "刷新";
        public const string BuffGlyphs = BuffRefreshed;
        public const string SlotsTitle = "武器槽位";
        public const string SlotsSummary = "拿得越少强化越快，拿得越多打法越丰富 · 默认推荐 2 把";
        public const string RosterTitle = "挑选起始武器";
        public const string RosterSummary = "点选一把武器后点击确认开战 · 上下滑动查看全部";
        public static readonly string[] SlotChoices = { "1 把武器", "2 把武器（推荐）", "3 把武器", "4 把武器", "5 把武器" };
        // 玩家可见文案一律写成常量，不要内联字面量：EmberFonts 只能扫描到"可达"的字符串，
        // 内联文案会漏出烘制范围，字在游戏里渲染成空心方框。新增文案放这里（或对应面板的常量），
        // 并接进 EmberFonts.MenuGlyphSet，否则不会进图集。
        public const string MenuTitle = "\u70ec\u706f\u884c\u8005";
        public const string MenuTagline = "25\u6ce2\u6807\u51c6\u5f81\u7a0b\u00b7\u002050\u6ce2\u6f2b\u957f\u5f81\u7a0b\n" + "\u81ea\u7531\u9009\u62e9\u96be\u5ea6\uff0c\u6bcf\u5c40\u4ece\u76f8\u540c\u7684\u57fa\u7840\u5c5e\u6027\u51fa\u53d1\u3002\n" + "\u6ce2\u6b21\u5956\u52b1\u9009\u5361\uff0c\u51fb\u8d25\u6700\u7ec8\u5b88\u536b\u3002";
        public static readonly string[] MenuChoices =
        {
            "\u70b9\u4eae\u65c5\u7a0b",
            "\u8bbe\u7f6e"
        };
        public const string MenuHint = "\u89c9\u5f97\u592a\u6162\uff1f\u70b9\u5f00\u300c\u8bbe\u7f6e\u300d\uff0c" + "\u65c5\u9014\u8282\u594f\u53ef\u4ee5\u8c03\u5230 5 \u500d\u0020\u00b7\u0020\u70b9\u6b64\u5173\u95ed";
        public const string RunSetupTitle = "\u9009\u62e9\u5f81\u7a0b";
        public const string RunSetupBody = "\u5c40\u957f\u4e0e\u96be\u5ea6\u72ec\u7acb\u9009\u62e9\u3002\n" + "\u524d 10 \u6ce2\u6bcf\u6ce2\u9009\u5361\uff0c\u4e4b\u540e\u6bcf 2 \u6ce2\u9009\u5361\u3002\n" + "\u9636\u6bb5 Boss \u989d\u5916\u5956\u52b1\u4e00\u6b21\u3002\n" + "\u6bcf 10 \u6ce2\u662f\u5b88\u536b\u8bd5\u70bc\uff0c\u6700\u540e\u4e00\u6ce2\u662f\u957f\u591c\u5b88\u536b\u3002";
        public static readonly string[] MenuChoicesWithBack =
        {
            "\u6807\u51c6\u5f81\u7a0b \u00b7 25\u6ce2\uff08\u63a8\u8350\uff09",
            "\u6f2b\u957f\u5f81\u7a0b \u00b7 50\u6ce2",
            "\u8fd4\u56de\u8425\u5730"
        };
        public const string DifficultyTitle = "\u9009\u62e9\u96be\u5ea6";
        // 每种难度单独一行，避免折行后末尾留下孤字。
        public static string DifficultyBody(int waves)
        {
            return waves + " \u6ce2\u5f81\u7a0b\n" + "\u4f11\u95f2\u9002\u5408\u8f7b\u677e\u6784\u7b51\uff0c\n" + "\u6807\u51c6\u4f53\u9a8c\u5b8c\u6574\u6311\u6218\uff0c\n" + "\u56f0\u96be\u9762\u5bf9\u66f4\u591a\u7cbe\u82f1\u3002";
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
        // 结算页与局内数字拼接的片段。
        public const string ResultShape = "\u6ce2\u5f81\u7a0b\u5230\u8fbe\u7b2c\u751f\u5b58\u79d2\u51fb\u6740\u4f59\u70ec" + "\u4f11\u95f2\u6807\u51c6\u56f0\u96be";
        public static readonly string[] UpgradeNames =
        {
            "\u653b\u51fb\u529b",
            "\u653b\u901f",
            "\u5e78\u8fd0",
            "\u5168\u4f53\u4f24\u5bb3\u589e\u5e45",
            "护盾",
            "",
            "",
            "",
            "",
            "",
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Pierce).Name,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Boom).Name,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Meteor).Name,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Basic).Name,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Orbit).Name,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Trail).Name,
            "",
            "",
            "",
            "",
            "\u71ce\u539f",
            "\u65e5\u5195",
            "\u706b\u6d77",
            "\u7a7f\u6768",
            "\u6298\u8fd4\u4e0d\u5c3d",
            "\u5929\u706b",
            "",
            "",
            "",
            "",
            "\u8fde\u53d1",
            "\u6dfb\u85aa",
            "\u5ef6\u71c3",
            "\u8d2f\u7a7f",
            "\u56de\u9a6c",
            "\u591a\u843d\u70b9"
        };
        public static readonly string[] UpgradeDetails =
        {
            "提高对应武器造成的伤害\n不强化其他武器的直接伤害",
            "提高对应武器的攻击效率\n具体效果随武器类型变化",
            "提高通用卡的高品质概率\n幸运较高时，专属更容易出现",
            "提高所有武器造成的伤害\n与单武器攻击加成独立计算",
            "立即增加护盾，受伤优先抵扣\n本局可累加，耗尽后扣除生命",
            "",
            "",
            "",
            "",
            "",
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Pierce).Description,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Boom).Description,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Meteor).Description,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Basic).Description,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Orbit).Description,
            EmberWeaponCatalog.Find(EmberWeaponCatalog.Trail).Description,
            "",
            "",
            "",
            "",
            "火球变大并提高伤害\n命中后留下持续燃烧的火区",
            "扩大环绕范围并提高转速\n提升接触伤害，沿途留下燃地",
            "身周生成持续伤害火圈\n随玩家移动，原有燃地保留",
            "命中后分裂一支小火矢\n小火矢不会再次分裂",
            "返回身边后再次飞出\n额外往返仍可正常命中敌人",
            "扩大火雨爆炸范围并提高伤害\n不改变每轮的落点数量",
            "",
            "",
            "",
            "",
            "每轮发射更多火球\n围绕同一目标小角度散射",
            "增加环绕火球的数量\n扩大同一轨道上的攻击覆盖",
            "缩短铺火间隔并提高燃地伤害\n单块火焰的持续时间不变",
            "增加每支主火矢的穿透目标数\n单次命中的伤害不变",
            "去程、回程各增加一次命中机会\n飞行途中可再次伤害同一敌人",
            "每轮额外增加火雨落点\n各落点独立预警并造成范围伤害"
        };
    }
}
