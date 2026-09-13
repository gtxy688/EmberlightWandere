using UnityEngine;

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

    public static class EmberRarityUtil
    {
        // Legacy Magnitude retained for non-generic systems / Validate continuity.
        public static float Magnitude(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return 0.40f;
                case EmberRarity.Gold: return 0.65f;
                case EmberRarity.Diamond: return 0.90f;
                default: return 0.20f;
            }
        }

        public static int CountBonus(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return 2;
                case EmberRarity.Gold: return 2;
                case EmberRarity.Diamond: return 3;
                default: return 1;
            }
        }

        public static int Percent(EmberRarity rarity)
        {
            return Mathf.RoundToInt(Magnitude(rarity) * 100f);
        }

        public static string Name(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return "\u767d\u94f6";
                case EmberRarity.Gold: return "\u9ec4\u91d1";
                case EmberRarity.Diamond: return "\u94bb\u77f3";
                default: return "\u9752\u94dc";
            }
        }

        public static Color Color(EmberRarity rarity)
        {
            switch (rarity)
            {
                case EmberRarity.Silver: return new Color(.78f, .85f, .94f);
                case EmberRarity.Gold: return new Color(1f, .76f, .19f);
                case EmberRarity.Diamond: return new Color(.25f, .94f, 1f);
                default: return new Color(.86f, .49f, .28f);
            }
        }

        /// <summary>Legacy wave gate (not used for Gods-Select-v3 generics).</summary>
        public static EmberRarity Roll(System.Random random, int wave)
        {
            int[] weights = WeightsForWave(wave);
            return RollWeighted(random, weights);
        }

        /// <summary>Luck-driven rarity for G-ATK/AS/LUCK/AMP. Luck 0..120.</summary>
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
                a = new[] { 72f, 22f, 5f, 1f }; // rarity nerf 2026-09-13
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
                Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(a[0], b[0], t))),
                Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(a[1], b[1], t))),
                Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(a[2], b[2], t))),
                Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(a[3], b[3], t)))
            };
        }

        static int[] WeightsForWave(int wave)
        {
            if (wave <= 2) return new[] { 70, 25, 5, 0 };
            if (wave <= 4) return new[] { 55, 30, 12, 3 };
            return new[] { 50, 28, 15, 7 };
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

        // Gods-Select-v3 generic magnitudes (ATK/AS as additive fractions).
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

        public static EmberOfferKind KindOf(int id)
        {
            if (id >= 30 && id <= 39) return EmberOfferKind.Exclusive;
            if (id >= 20 && id <= 29) return EmberOfferKind.Metamorph;
            if (id >= 10 && id <= 19) return EmberOfferKind.Weapon;
            return EmberOfferKind.Generic;
        }

        public static float ShieldAmount(EmberRarity rarity)
        { switch (rarity) { case EmberRarity.Silver: return 500; case EmberRarity.Gold: return 750; case EmberRarity.Diamond: return 1250; default: return 250; } }

        public static bool IsGenericId(int id) { return id == 0 || id == 1 || id == 2 || id == 3 || id == 4; } // ATK/AS/Luck/Amp/Shield
    }

    public struct EmberOffer
    {
        public int Id;
        public EmberRarity Rarity;
        public EmberOfferKind Kind;
        public EmberOffer(int id, EmberRarity rarity)
        {
            Id = id;
            Rarity = rarity;
            Kind = EmberRarityUtil.KindOf(id);
        }
    }
}
