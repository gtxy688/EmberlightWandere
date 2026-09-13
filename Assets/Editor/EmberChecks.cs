using System;
using System.Linq;
using UnityEditor;

namespace Emberlight
{
    public static class EmberChecks
    {
        [MenuItem("Emberlight/Validate progression")]
        public static void Validate()
        {
            var p = new RunProgress();
            p.OfferChoices();
            Require(p.Pending == 1, "Wave offer sets Pending=1");

            for (int seed = 0; seed < 100; seed++)
            {
                var c = p.Choices(new Random(seed), 1);
                Require(c.Length == 3, "Three offers");
                Require(c.Select(o => o.Id).Distinct().Count() == 3, "Unique offer ids");
            }

            // Magnitude lock: bronze 0.20; bronze+diamond stack = 1.10
            var bronze = new EmberOffer(1, EmberRarity.Bronze);
            Require(p.Choose(bronze) && MathfApprox(p.AttackSpeedBonus, 0.20f), "Bronze attack speed +20%");
            p.OfferChoices();
            Require(p.Choose(new EmberOffer(1, EmberRarity.Diamond)) && MathfApprox(p.AttackSpeedBonus, 1.10f), "Stack diamond attack speed");

            Require(EmberRarityUtil.CountBonus(EmberRarity.Bronze) == 1
                && EmberRarityUtil.CountBonus(EmberRarity.Silver) == 2
                && EmberRarityUtil.CountBonus(EmberRarity.Gold) == 2
                && EmberRarityUtil.CountBonus(EmberRarity.Diamond) == 3, "CountBonus 1/2/2/3");

            Require(MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Bronze), 0.20f)
                && MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Silver), 0.40f)
                && MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Gold), 0.65f)
                && MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Diamond), 0.90f), "Magnitude 0.20/0.40/0.65/0.90");

            // Wave1 rolls never Diamond (sample many seeds)
            for (int seed = 0; seed < 2000; seed++)
            {
                var r = EmberRarityUtil.Roll(new Random(seed), 1);
                Require(r != EmberRarity.Diamond, "Wave1 never Diamond");
            }

            // Spend remaining pending then ensure choose fails
            while (p.Pending > 0) p.Choose(new EmberOffer(9, EmberRarity.Bronze));
            Require(!p.Choose(new EmberOffer(9, EmberRarity.Bronze)), "Cannot choose without pending");

            p.AddScore(10);
            Require(p.Score == 10, "Score only (no XP)");
            p.OfferChoices();
            var offers = p.Choices(new Random(1), 3);
            Require(offers.Length == 3, "Still offers after score");
            Require(offers.All(o => o.Id >= 0 && o.Id <= 8), "Offers are upgrade ids");

            // Choices signature takes wave (compile-time); also Wave2 never Diamond
            for (int seed = 0; seed < 2000; seed++)
                Require(EmberRarityUtil.Roll(new Random(seed), 2) != EmberRarity.Diamond, "Wave2 never Diamond");

            var cfg = LevelConfig.Default;
            Require(cfg.QuotaForWave(1) == 12 && cfg.QuotaForWave(2) == 18
                && cfg.QuotaForWave(3) == 24 && cfg.QuotaForWave(4) == 30
                && cfg.QuotaForWave(5) == 36, "Wave quotas 12/18/24/30/36");
            Require(MathfApprox(cfg.BossHp, 2200f) && cfg.BossWave == 6, "Boss HP 2200 Wave6");
            Require(MathfApprox(cfg.MapLimit, 22f), "MapLimit 22");

            // No XP API
            Require(typeof(RunProgress).GetProperty("Level") == null
                && typeof(RunProgress).GetProperty("Experience") == null
                && typeof(RunProgress).GetProperty("Required") == null
                && typeof(RunProgress).GetMethod("AddExperience") == null, "XP progression removed");

            UnityEngine.Debug.Log("Emberlight progression checks passed (Difficulty-Waves-v2: wave-clear upgrades, score-only embers).");
        }

        static bool MathfApprox(float a, float b) { return Math.Abs(a - b) < 0.001f; }
        static void Require(bool success, string name) { if (!success) throw new Exception(name); }
    }
}
