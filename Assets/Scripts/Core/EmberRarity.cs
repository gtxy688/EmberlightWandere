using System;

namespace Emberlight
{
    public enum EmberRarity
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Diamond = 3
    }

    public enum EmberOfferKind
    {
        Generic = 0,
        Weapon = 1,
        Exclusive = 2,
        Metamorph = 3
    }

    /// <summary>
    /// 品质（稀有度）规则：幸运 → 权重 → 品质，以及各品质对应的通用词条数值。
    /// **零 `UnityEngine` 引用**，与 <see cref="RunProgress"/> / <see cref="EmberChoicePool"/> 同层。
    /// 权威数值见 `Docs/requirements.md` §4.4 / §4.5。
    /// </summary>
    public static class EmberRarityUtil
    {
        /// <summary>
        /// 品质名，下标即 <see cref="EmberRarity"/>。这四串字是该名称的唯一出处，
        /// <see cref="EmberFonts"/> 也读它们做字形预热。
        /// </summary>
        public static readonly string[] AllNames =
        {
            "\u9752\u94dc",  // 青铜
            "\u767d\u94f6",  // 白银
            "\u9ec4\u91d1",  // 黄金
            "\u94bb\u77f3",  // 钻石
        };

        public static string Name(EmberRarity rarity)
        {
            int index = (int)rarity;
            return index >= 0 && index < AllNames.Length ? AllNames[index] : AllNames[0];
        }

        /// <summary>
        /// 品质配色，用纯 `float[3]`（RGB, 0–1）表达而非 `UnityEngine.Color`，
        /// 这样本文件与依赖它的构筑逻辑都能脱离 Unity 单测。UI 侧用
        /// <see cref="EmberRarityTint.ToColor"/> 转换。
        /// </summary>
        public static float[] Tint(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return new[] { .78f, .85f, .94f };
                case EmberRarity.Gold: return new[] { 1f, .76f, .19f };
                case EmberRarity.Diamond: return new[] { .25f, .94f, 1f };
                default: return new[] { .86f, .49f, .28f };
            }
        }

        /// <summary>通用词条按幸运 roll 品质（`requirements.md` §4.5）。</summary>
        public static EmberRarity RollByLuck(System.Random random, float luck)
        {
            return RollWeighted(random, WeightsForLuck(luck));
        }

        public static int[] WeightsForLuck(float luck)
        {
            if (luck < 0f) luck = 0f;
            if (luck > 120f) luck = 120f;
            float t;
            float[] a, b;
            if (luck <= 60f)
            {
                t = luck / 60f;
                a = new[] { 72f, 22f, 5f, 1f };
                b = new[] { 58f, 28f, 11f, 3f };
            }
            else
            {
                t = (luck - 60f) / 60f;
                a = new[] { 58f, 28f, 11f, 3f };
                b = new[] { 45f, 32f, 18f, 5f };
            }
            return new[]
            {
                LerpWeight(a[0], b[0], t),
                LerpWeight(a[1], b[1], t),
                LerpWeight(a[2], b[2], t),
                LerpWeight(a[3], b[3], t)
            };
        }

        /// <summary>线性插值后四舍五入（远离零方向），下限 0。用 `System.Math` 以免引入 `UnityEngine`。</summary>
        static int LerpWeight(float from, float to, float t)
        {
            float v = from + (to - from) * t;
            return Math.Max(0, (int)Math.Round(v, MidpointRounding.AwayFromZero));
        }

        static EmberRarity RollWeighted(System.Random random, int[] weights)
        {
            int total = 0;
            for (int i = 0; i < weights.Length; i++) total += weights[i];
            if (total <= 0) return EmberRarity.Bronze;
            int roll = random.Next(total);
            int acc = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                acc += weights[i];
                if (roll < acc) return (EmberRarity)i;
            }
            return EmberRarity.Bronze;
        }

        // 通用词条数值（攻击力/攻速为单武器加算比例）。见 `requirements.md` §4.4。

        public static float GenericAtkAs(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return 0.20f;
                case EmberRarity.Gold: return 0.30f;
                case EmberRarity.Diamond: return 0.40f;
                default: return 0.10f;
            }
        }

        public static float GenericAttackSpeed(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return 0.20f;
                case EmberRarity.Gold: return 0.30f;
                case EmberRarity.Diamond: return 0.50f;
                default: return 0.10f;
            }
        }

        public static int GenericLuck(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return 50;
                case EmberRarity.Gold: return 75;
                case EmberRarity.Diamond: return 125;
                default: return 25;
            }
        }

        public static float GenericAmp(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return 0.10f;
                case EmberRarity.Gold: return 0.15f;
                case EmberRarity.Diamond: return 0.25f;
                default: return 0.05f;
            }
        }

        public static float ShieldAmount(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return 500;
                case EmberRarity.Gold: return 750;
                case EmberRarity.Diamond: return 1250;
                default: return 250;
            }
        }

        /// <summary>按 Id 分区判定候选类型（`requirements.md` §4.7）。</summary>
        public static EmberOfferKind KindOf(int id)
        {
            if (id >= 30 && id <= 39) return EmberOfferKind.Exclusive;
            if (id >= 20 && id <= 29) return EmberOfferKind.Metamorph;
            if (id >= 10 && id <= 19) return EmberOfferKind.Weapon;
            return EmberOfferKind.Generic;
        }

        /// <summary>通用词条 Id：攻击力 / 攻速 / 幸运 / 增幅 / 护盾。</summary>
        public static bool IsGenericId(int id) { return id == 0 || id == 1 || id == 2 || id == 3 || id == 4; }
    }

    public struct EmberOffer
    {
        public int Id;
        public EmberRarity Rarity;
        public EmberOfferKind Kind;
        /// <summary>-1 表示全局项（幸运/增幅/护盾）；攻击力/攻速指向某把已拥有武器。</summary>
        public int WeaponId;

        public EmberOffer(int id, EmberRarity rarity, int weaponId = -1)
        {
            Id = id;
            Rarity = rarity;
            Kind = EmberRarityUtil.KindOf(id);
            WeaponId = weaponId;
        }
    }
}
