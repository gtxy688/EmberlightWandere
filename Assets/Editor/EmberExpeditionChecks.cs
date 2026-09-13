using System;
using UnityEditor;
using UnityEngine;

namespace Emberlight
{
    public static class EmberExpeditionChecks
    {
        [MenuItem("Emberlight/Validate expeditions")]
        public static void Validate()
        {
            foreach (int length in new[] { 25, 50 })
            foreach (EmberDifficulty difficulty in Enum.GetValues(typeof(EmberDifficulty)))
            {
                var c = LevelConfig.Create(difficulty, length);
                int bosses = 0, picks = 0;
                for (int wave = 1; wave <= length; wave++)
                {
                    if (c.IsBossWave(wave))
                    {
                        bosses++;
                        Require(c.QuotaForWave(wave) == 0 && c.BossHpForWave(wave) > 0, "Boss replaces trash quota");
                        Require(wave == length || c.PicksAfterWave(wave) == 2, "Stage Boss grants regular and bonus choice");
                    }
                    else Require(c.QuotaForWave(wave) > 0 && c.QuotaForWave(wave) <= c.EnemyLimit, "All trash waves have bounded quota");
                    Require(!c.IsBossWave(wave) || !c.IsEliteWave(wave), "Boss and elite wave mutually exclusive");
                    Require(c.SpawnInterval(wave) >= .14f, "Spawn cadence bounded");
                    Require(c.AffixChance(wave) >= 0 && c.AffixChance(wave) <= .5f, "Affix probability bounded");
                    int sum = 0; foreach (int weight in c.WeightTable(wave)) sum += weight;
                    Require(sum == 100, "All encounter compositions normalized");
                    picks += c.PicksAfterWave(wave);
                }
                Require(bosses == (length == 25 ? 3 : 5), "Expected stage and final Boss count");
                Require(picks == (length == 25 ? 19 : 33), "Scheduled choice count excludes final reward");
                Require(c.PicksAfterWave(length) == 0 && c.PicksAfterWave(11) == 0 && c.PicksAfterWave(12) == 1, "Final and late-wave cadence");
                Require(c.IsEliteWave(5) && c.IsEliteWave(15), "Elite challenges every five waves");
            }
            var easy = LevelConfig.Create(EmberDifficulty.Casual, 25);
            var normal = LevelConfig.Create(EmberDifficulty.Standard, 25);
            var hard = LevelConfig.Create(EmberDifficulty.Hard, 25);
            Require(easy.TrashHp(15, 7) < normal.TrashHp(15, 7) && normal.TrashHp(15, 7) < hard.TrashHp(15, 7), "Difficulty health scaling");
            Require(easy.DamageMultiplier < normal.DamageMultiplier && normal.DamageMultiplier < hard.DamageMultiplier, "Difficulty damage scaling");
            Require(easy.QuotaForWave(9) < normal.QuotaForWave(9) && normal.QuotaForWave(9) < hard.QuotaForWave(9), "Difficulty population scaling");
            Require(easy.SpawnInterval(9) > normal.SpawnInterval(9) && normal.SpawnInterval(9) > hard.SpawnInterval(9), "Difficulty spawn cadence");
            Require(easy.AffixChance(9) < normal.AffixChance(9) && normal.AffixChance(9) < hard.AffixChance(9), "Difficulty affix scaling");
            hard.WaveQuotas[0] = 999;
            Require(normal.QuotaForWave(1) == 12 && LevelConfig.Default.QuotaForWave(1) == 12, "Run configs cannot leak into each other");
            var p = new RunProgress();
            Require(p.DamageBonus == 0 && p.BonusMaxHealth == 0 && p.MoveSpeedBonus == 0, "Equal starting baseline without meta");
            Require(typeof(RunProgress).GetMethod("ApplyMeta") == null, "Meta entry point removed");
            Debug.Log("Emberlight expedition checks passed: all 6 combinations, 25/50 waves, stage Bosses, 19/33 scheduled picks, elite waves and equal starting stats.");
        }
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException("Expeditions: " + message); }
    }
}
