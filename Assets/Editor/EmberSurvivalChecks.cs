using System;
using UnityEditor;
using UnityEngine;
namespace Emberlight
{
    public static class EmberSurvivalChecks
    {
        [MenuItem("Emberlight/Validate survival")]
        public static void Validate()
        {
            float[] amounts = { 250, 500, 750, 1250 };
            for (int i = 0; i < amounts.Length; i++)
            {
                var p = new RunProgress(); var rarity = (EmberRarity)i;
                p.OfferChoices();
                Require(p.Choose(new EmberOffer(RunProgress.StatShield, rarity)), "Shield selectable");
                Require(p.Shield == amounts[i], "Requested shield rarity amount");
                Require(p.AbsorbDamage(20) == 0 && p.Shield == amounts[i] - 20, "Shield absorbs before HP");
                Require(p.AbsorbDamage(amounts[i]) == 20 && p.Shield == 0, "Excess damage passes to HP");
                Require(p.AbsorbDamage(-10) == 0 && p.Shield == 0, "Negative hit cannot grant shield");
                p.OfferChoices(); p.Choose(new EmberOffer(RunProgress.StatShield, rarity));
                Require(p.Shield == amounts[i], "Repeated choice adds fresh shield");
                Require(EmberUpgradePanel.RarityLabel(new EmberOffer(0, rarity), p) == EmberRarityUtil.Name(rarity), "Generic badge contains rarity exactly once");
                p.ConfigureRun(RunProgress.WeaponBasic, 2);
                Require(p.Shield == 0 && p.ShieldCapacity == 0, "Shield does not persist into next run");
            }
            var offers = new RunProgress(); bool seen = false;
            for (int seed = 0; seed < 500; seed++)
                foreach (var offer in offers.Choices(new System.Random(seed), 5))
                    if (offer.Id == RunProgress.StatShield) seen = true;
            Require(seen, "Shield appears in normal offer generation");
            Require(EmberGame.HealedHealth(90, 100, 20) == 100 && EmberGame.HealedHealth(30, 100, 20) == 50, "Healing capped at max health");
            var config = LevelConfig.Default;
            Require(EmberHealingDrops.ShouldDrop(0, EnemyAffix.None, .079, config), "Normal drop below threshold");
            Require(!EmberHealingDrops.ShouldDrop(0, EnemyAffix.None, .081, config), "Normal drop above threshold");
            Require(EmberHealingDrops.ShouldDrop(2, EnemyAffix.Wind, .19, config), "Elite bonus chance");
            Require(!EmberHealingDrops.ShouldDrop(6, EnemyAffix.None, 0, config), "Children cannot farm healing drops");
            Require(EmberHealingDrops.ShouldDrop(3, EnemyAffix.None, .999, config), "Boss guaranteed healing drop");
            Debug.Log("Emberlight survival checks passed: shield values, absorption, overflow, reset, card pool, rarity labels, healing cap and drop chances.");
        }
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    }
}
