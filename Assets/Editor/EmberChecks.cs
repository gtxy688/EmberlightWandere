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

            // Default Fireball + N=2
            Require(p.OwnsWeapon(RunProgress.WeaponBasic), "Default owns Basic");
            Require(p.WeaponSlots == 2, "Default N=2");
            Require(p.RefreshesRemaining == 2, "2 free refreshes");

            // Offer length = 3 + empty
            for (int seed = 0; seed < 80; seed++)
            {
                var c = p.Choices(new Random(seed), 1);
                Require(c.Length == p.OfferCountExpected(), "Offer count 3+empty");
                Require(c.Select(o => o.Id).Distinct().Count() == c.Length, "Unique offer ids");
                // No old generics 4-9
                Require(c.All(o => o.Id <= 4 || o.Id >= 10), "Old 5-9 out of pool; shield ID4 enabled");
                // Same screen: each generic kind at most once
                var gens = c.Where(o => EmberRarityUtil.IsGenericId(o.Id)).Select(o => o.Id).ToList();
                Require(gens.Count == gens.Distinct().Count(), "Generic kinds unique");
            }

            // Generic ATK only (no global AS — jack)
            p.OfferChoices();
            Require(p.Choose(new EmberOffer(RunProgress.StatAtk, EmberRarity.Bronze))
                && MathfApprox(p.DamageBonus, 0.10f), "Bronze ATK +10%");
            p.OfferChoices();
            Require(p.Choose(new EmberOffer(RunProgress.StatAtk, EmberRarity.Diamond))
                && MathfApprox(p.DamageBonus, 0.50f), "Stack diamond ATK");
            Require(MathfApprox(p.AttackSpeedBonus, 0f), "No global AS from generics");
            p.OfferChoices();
            Require(p.Choose(new EmberOffer(RunProgress.StatAs, EmberRarity.Bronze)), "G-AS choose"); Require(p.AttackSpeedBonus >= 0.09f, "G-AS applies");

            // Luck + Amp
            p.OfferChoices();
            Require(p.Choose(new EmberOffer(RunProgress.StatLuck, EmberRarity.Bronze)) && MathfApprox(p.Luck, 25f), "Luck +25");
            p.OfferChoices();
            Require(p.Choose(new EmberOffer(RunProgress.StatAmp, EmberRarity.Silver)) && MathfApprox(p.DamageAmp, 0.10f), "Amp +10%");

            // Luck cap 120
            var luckP = new RunProgress();
            for (int i = 0; i < 10; i++)
            {
                luckP.OfferChoices();
                luckP.Choose(new EmberOffer(RunProgress.StatLuck, EmberRarity.Diamond)); // +125 each, capped
            }
            Require(MathfApprox(luckP.Luck, RunProgress.MaxLuck), "Luck capped 120");

            // Luck rarity weights endpoints
            var w0 = EmberRarityUtil.WeightsForLuck(0);
            Require(w0[0] == 72 && w0[1] == 22 && w0[2] == 5 && w0[3] == 1, "Luck0 weights");
            var w60 = EmberRarityUtil.WeightsForLuck(60);
            Require(w60[0] == 58 && w60[1] == 28 && w60[2] == 11 && w60[3] == 3, "Luck60 weights");
            var w120 = EmberRarityUtil.WeightsForLuck(120);
            Require(w120[0] == 45 && w120[1] == 32 && w120[2] == 18 && w120[3] == 5, "Luck120 weights");

            // Legacy Magnitude lock still present
            Require(MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Bronze), 0.20f)
                && MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Silver), 0.40f)
                && MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Gold), 0.65f)
                && MathfApprox(EmberRarityUtil.Magnitude(EmberRarity.Diamond), 0.90f), "Magnitude 0.20/0.40/0.65/0.90");

            Require(EmberRarityUtil.CountBonus(EmberRarity.Bronze) == 1
                && EmberRarityUtil.CountBonus(EmberRarity.Silver) == 2
                && EmberRarityUtil.CountBonus(EmberRarity.Gold) == 2
                && EmberRarityUtil.CountBonus(EmberRarity.Diamond) == 3, "CountBonus 1/2/2/3");

            // Wave1 Roll still never Diamond (legacy gate retained, unused by generics)
            for (int seed = 0; seed < 2000; seed++)
                Require(EmberRarityUtil.Roll(new Random(seed), 1) != EmberRarity.Diamond, "Wave1 never Diamond");

            while (p.Pending > 0) p.Choose(new EmberOffer(RunProgress.StatAtk, EmberRarity.Bronze));
            Require(!p.Choose(new EmberOffer(RunProgress.StatAtk, EmberRarity.Bronze)), "Cannot choose without pending");

            p.AddScore(10);
            Require(p.Score == 10, "Score only (no XP)");

            var cfg = LevelConfig.Default;
            Require(cfg.QuotaForWave(1) == 12 && cfg.QuotaForWave(2) == 18
                && cfg.QuotaForWave(3) == 24 && cfg.QuotaForWave(4) == 30
                && cfg.QuotaForWave(5) == 36, "Wave quotas 12/18/24/30/36");
            Require(MathfApprox(cfg.BossHp, 6000f) && cfg.BossWave == 25, "Default final Boss HP6000 Wave25");
            Require(MathfApprox(cfg.MapLimit, 22f), "MapLimit 22");

            Require(typeof(RunProgress).GetProperty("Level") == null
                && typeof(RunProgress).GetProperty("Experience") == null
                && typeof(RunProgress).GetMethod("AddExperience") == null, "XP progression removed");

            ValidateBuildDepth();
            ValidateGodsSelect();

            UnityEngine.Debug.Log("Emberlight progression checks passed (Difficulty-Waves-v2 + Build-Depth + Gods-Select-v3).");
        }

        static void ValidateBuildDepth()
        {
            Require(Enum.IsDefined(typeof(EmberOfferKind), EmberOfferKind.Generic)
                && Enum.IsDefined(typeof(EmberOfferKind), EmberOfferKind.Weapon)
                && Enum.IsDefined(typeof(EmberOfferKind), EmberOfferKind.Exclusive)
                && Enum.IsDefined(typeof(EmberOfferKind), EmberOfferKind.Metamorph), "Offer Kind enum v3");

            // Metamorph available when owning weapon (no exclusive prereq / no old thresholds)
            var ready = new RunProgress();
            ready.ConfigureRun(new[] { RunProgress.WeaponBasic }, 3);
            Require(ready.CanMetamorph(RunProgress.MetaBasic), "Meta Basic with Owned Basic");
            Require(ready.CanExclusive(RunProgress.ExclBasic), "Excl Basic with Owned Basic");
            bool sawMeta = false, sawExcl = false;
            for (int seed = 0; seed < 800 && !(sawMeta && sawExcl); seed++)
            {
                var c = ready.Choices(new Random(seed), 3);
                if (c.Any(o => o.Id == RunProgress.MetaBasic && o.Rarity == EmberRarity.Diamond)) sawMeta = true;
                if (c.Any(o => o.Id == RunProgress.ExclBasic && o.Rarity == EmberRarity.Gold)) sawExcl = true;
            }
            Require(sawMeta, "MetaBasic can appear Diamond");
            Require(sawExcl, "ExclBasic can appear Gold");

            // Choose meta then gone
            ready.OfferChoices();
            Require(ready.Choose(new EmberOffer(RunProgress.MetaBasic, EmberRarity.Diamond)), "Choose MetaBasic");
            Require(ready.HasMetamorph(RunProgress.MetaBasic), "HasMetamorph set");
            for (int seed = 0; seed < 200; seed++)
                Require(ready.Choices(new Random(seed), 3).All(o => o.Id != RunProgress.MetaBasic), "Meta gone after choose");

            // Exclusive
            ready.OfferChoices();
            int shotsBefore = ready.ExtraShots;
            Require(ready.Choose(new EmberOffer(RunProgress.ExclBasic, EmberRarity.Gold)), "Choose ExclBasic");
            Require(ready.ExtraShots == shotsBefore + 1, "ExclBasic +1 shot");

            // No meta without owning weapon
            var noTrail = new RunProgress();
            noTrail.ConfigureRun(new[] { RunProgress.WeaponBasic }, 2);
            Require(!noTrail.CanMetamorph(RunProgress.MetaTrail), "MetaTrail blocked without Trail");
            for (int seed = 0; seed < 100; seed++)
                Require(noTrail.Choices(new Random(seed), 4).All(o => o.Id != RunProgress.MetaTrail), "No MetaTrail without Trail");

            // Weapon unlock
            var wp = new RunProgress();
            wp.ConfigureRun(new[] { RunProgress.WeaponBasic }, 5);
            wp.OfferChoices();
            bool newW;
            Require(wp.Choose(new EmberOffer(RunProgress.WeaponPierce, EmberRarity.Gold), out newW) && newW, "Unlock pierce");
            Require(wp.OwnsWeapon(RunProgress.WeaponPierce), "Pierce owned");

            Require(typeof(EmberWeapon).IsAbstract, "EmberWeapon abstract");
            Require(typeof(EmberBasicShotWeapon) != null
                && typeof(EmberOrbitWeapon) != null
                && typeof(EmberTrailWeapon) != null
                && typeof(EmberPulseWeapon) != null
                && typeof(EmberPierceWeapon) != null
                && typeof(EmberBoomerangWeapon) != null
                && typeof(EmberMeteorWeapon) != null, "Weapon subclasses");

            Require(EmberGame.UpgradeNames.Length > 35
                && EmberGame.UpgradeNames[0].Length > 0
                && EmberGame.UpgradeNames[RunProgress.MetaMeteor].Length > 0
                && EmberGame.UpgradeNames[RunProgress.ExclMeteor].Length > 0, "UpgradeNames cover generics/meta/excl");
        }

        static void ValidateGodsSelect()
        {
            var d = new RunProgress();
            Require(d.CoreWeaponId == RunProgress.WeaponBasic, "Default core Fireball");
            Require(d.WeaponSlots == 2, "Default N=2");
            Require(MathfApprox(d.ScoreMult, 1.0f), "ScoreMult always 1.0");
            Require(d.RefreshesRemaining == 2, "Default 2 refreshes");

            // Start path (jack): one starter + N ceiling => empty = N-1
            var start = new RunProgress();
            start.ConfigureRun(new[] { RunProgress.WeaponBasic }, 2);
            Require(start.OwnedWeaponCount == 1, "Start owned=1");
            Require(start.EmptySlots == 1, "Start empty=N-1");

            // ConfigureRun still accepts multi for tests / mid-run ownership
            var multi = new RunProgress();
            multi.ConfigureRun(new[] { RunProgress.WeaponBasic, RunProgress.WeaponOrbit, RunProgress.WeaponTrail }, 3);
            Require(multi.OwnedWeaponCount == 3, "Multi own 3");
            Require(multi.Orbits == 1, "Orbit baseline Orbits=1");
            Require(MathfApprox(multi.TrailPower, 0.20f), "Trail baseline 0.20");
            Require(multi.EmptySlots == 0, "Full slots empty=0");

            // Full slots -> offer length 3, no New Weapon
            for (int seed = 0; seed < 300; seed++)
            {
                var c = multi.Choices(new Random(seed), 3);
                Require(c.Length == 3, "Full slots offer=3");
                Require(c.All(o => o.Kind != EmberOfferKind.Weapon || multi.OwnsWeapon(o.Id)), "Full: no New Weapon");
            }

            // N=1 full after one weapon
            var n1 = new RunProgress();
            n1.ConfigureRun(new[] { RunProgress.WeaponBasic }, 1);
            Require(n1.OwnedWeaponCount >= n1.WeaponSlots, "N1 full");
            for (int seed = 0; seed < 200; seed++)
            {
                var c = n1.Choices(new Random(seed), 2);
                Require(c.Length == 3, "N1 offer=3");
                Require(c.All(o => o.Kind != EmberOfferKind.Weapon || n1.OwnsWeapon(o.Id)), "N1 no New");
            }

            // Empty slots append new weapons
            var room = new RunProgress();
            room.ConfigureRun(new[] { RunProgress.WeaponBasic }, 4);
            Require(room.EmptySlots == 3, "Empty=3");
            bool sawNew = false;
            for (int seed = 0; seed < 100 && !sawNew; seed++)
            {
                var c = room.Choices(new Random(seed), 2);
                Require(c.Length == 6, "3+3 empty offers");
                if (c.Any(o => o.Kind == EmberOfferKind.Weapon && !room.OwnsWeapon(o.Id))) sawNew = true;
            }
            Require(sawNew, "Empty slots offer New Weapon");

            // Compensation path: Choose new weapon sets grantedNewWeapon
            room.OfferChoices();
            bool granted;
            Require(room.Choose(new EmberOffer(RunProgress.WeaponOrbit, EmberRarity.Gold), out granted) && granted, "New Orbit grants flag");
            Require(room.OwnedWeaponCount == 2 && room.Orbits == 1, "Orbit baseline on mid-run grant");

            // Refresh spends
            var rf = new RunProgress();
            rf.OfferChoices();
            EmberOffer[] refreshed;
            Require(rf.TryRefresh(new Random(1), 1, out refreshed) && refreshed != null && refreshed.Length >= 3, "Refresh 1");
            Require(rf.RefreshesRemaining == 1, "Refresh remaining 1");
            Require(rf.TryRefresh(new Random(2), 1, out refreshed), "Refresh 2");
            Require(rf.RefreshesRemaining == 0, "Refresh remaining 0");
            Require(!rf.TryRefresh(new Random(3), 1, out refreshed), "No more refresh");

            // FinalScore raw
            var sc = new RunProgress();
            sc.AddScore(100);
            Require(sc.FinalScore() == 100, "FinalScore raw");

            // Old 0-9 pool exit: id 4-9 never appear; id 0-3 only as new generics
            for (int seed = 0; seed < 400; seed++)
            {
                var c = new RunProgress().Choices(new Random(seed), 3);
                Require(c.All(o => !(o.Id >= 4 && o.Id <= 9)), "Ids 4-9 exited pool");
            }

            // Six exclusives / metamorphs wiring
            Require(new RunProgress().CanMetamorph(RunProgress.MetaOrbit) == false, "MetaOrbit needs Orbit");
            var orb = new RunProgress();
            orb.ConfigureRun(new[] { RunProgress.WeaponOrbit }, 2);
            Require(orb.CanMetamorph(RunProgress.MetaOrbit) && orb.CanExclusive(RunProgress.ExclOrbit), "Orbit line ready");

            Require(typeof(EmberGodsSelectPanel) != null, "EmberGodsSelectPanel");
            Require(typeof(EmberGame).GetProperty("RefreshRemaining") != null, "API RefreshRemaining");
            Require(typeof(EmberGame).GetProperty("LuckValue") != null, "API LuckValue");
            Require(typeof(EmberGame).GetProperty("CurrentOffers") != null, "API CurrentOffers");
        }

        static bool MathfApprox(float a, float b) { return Math.Abs(a - b) < 0.001f; }
        static void Require(bool success, string name) { if (!success) throw new Exception(name); }
    }
}
