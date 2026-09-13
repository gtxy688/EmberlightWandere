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

    public static class EmberRarityUtil
    {
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
            // Planner lock: 1 / 2 / 2 / 3
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
            // Unicode escapes keep source encoding-safe across tools.
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

        public static EmberRarity Roll(System.Random random, int wave)
        {
            int[] weights = WeightsForWave(wave);
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

        static int[] WeightsForWave(int wave)
        {
            // Wave 1-2: 70/25/5/0; 3-4: 55/30/12/3; 5+: 50/28/15/7
            if (wave <= 2) return new[] { 70, 25, 5, 0 };
            if (wave <= 4) return new[] { 55, 30, 12, 3 };
            return new[] { 50, 28, 15, 7 };
        }
    }

    public struct EmberOffer
    {
        public int Id;
        public EmberRarity Rarity;
        public EmberOffer(int id, EmberRarity rarity) { Id = id; Rarity = rarity; }
    }
}
