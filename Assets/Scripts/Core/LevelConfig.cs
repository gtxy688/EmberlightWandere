using UnityEngine;

namespace Emberlight
{
    /// <summary>Run/level tunables: arena, waves, spawn cadence, boss HP. Combat reads this instead of magic numbers.</summary>
    public enum EmberDifficulty { Casual, Standard, Hard }

    public sealed class LevelConfig
    {
        public float MapLimit = 22f;
        public float BossHp = 6000f;
        public int BossWave = 25;
        public EmberDifficulty Difficulty { get; private set; } = EmberDifficulty.Standard;
        public float HealthMultiplier { get; private set; } = 1f;
        public float DamageMultiplier { get; private set; } = 1f;
        public float CountMultiplier { get; private set; } = 1f;
        public float IntervalMultiplier { get; private set; } = 1f;
        public float AffixMultiplier { get; private set; } = 1f;

        public static string DifficultyName(EmberDifficulty difficulty)
        { return difficulty == EmberDifficulty.Casual ? "休闲" : difficulty == EmberDifficulty.Hard ? "困难" : "标准"; }
        public static LevelConfig Create(EmberDifficulty difficulty, int waves)
        {
            var c = new LevelConfig();
            c.BossWave = waves == 50 ? 50 : 25;
            c.Difficulty = System.Enum.IsDefined(typeof(EmberDifficulty), difficulty) ? difficulty : EmberDifficulty.Standard;
            if (c.Difficulty == EmberDifficulty.Casual)
            { c.HealthMultiplier = .5f; c.DamageMultiplier = .7f; c.CountMultiplier = .85f; c.IntervalMultiplier = 1.15f; c.AffixMultiplier = .7f; }
            if (c.Difficulty == EmberDifficulty.Hard)
            { c.HealthMultiplier = 1.35f; c.DamageMultiplier = 1.3f; c.CountMultiplier = 1.2f; c.IntervalMultiplier = .88f; c.AffixMultiplier = 1.25f; }
            c.BossHp = (c.BossWave == 50 ? 10000f : 6000f) * c.HealthMultiplier;
            return c;
        }
        public bool IsBossWave(int wave) { return wave >= 1 && wave <= BossWave && (wave == BossWave || wave % 10 == 0); }
        public bool IsEliteWave(int wave) { return wave >= 1 && wave < BossWave && wave % 5 == 0 && !IsBossWave(wave); }
        public int PicksAfterWave(int wave)
        {
            if (wave < 1 || wave >= BossWave) return 0;
            return (wave <= 10 || wave % 2 == 0 ? 1 : 0) + (IsBossWave(wave) ? 1 : 0);
        }
        public float BossHpForWave(int wave)
        { return wave == BossWave ? BossHp : (1600f + wave * 100f) * HealthMultiplier; }
        public int BossTier(int wave) { return Mathf.Clamp(wave / 10 + (wave == BossWave ? 1 : 0), 1, 5); }
        public string WaveName(int wave)
        {
            if (wave == BossWave) return "最终决战";
            if (IsBossWave(wave)) return "守卫试炼";
            if (IsEliteWave(wave)) return "精英挑战";
            if (wave <= 5) return "渐入长夜";
            switch ((wave - 6) % 4) { case 0: return "烬虫潮涌"; case 1: return "暗烛交火"; case 2: return "铁灯阵线"; default: return "引线围猎"; }
        }
        public int EnemyLimit = 150;
        public float HealDropChance = .08f, EliteHealDropChance = .20f;
        public float HealDropAmount = 20f, HealDropLifetime = 60f;
        public float ShooterNear = 5.5f, ShooterFar = 7f, ShooterInterval = 2.6f, ShooterCharge = .4f;
        public float ShooterBulletSpeed = 6f, ShooterDamage = 8f;
        public float ShieldTurnSpeed = 65f, ShieldFrontMultiplier = .3f;
        public float BombFuse = .8f, BombRadius = 1.8f, BombDamage = 20f, BombTriggerRange = 2f;
        public int SplitCount = 3;
        public float BurningRadius = 2f, BurningDamage = 2f;
        public float WindPeriod = 3f, WindDuration = 1f, WindSpeedMultiplier = 1.8f;
        public float RebirthDelay = 2f, RebirthHpFraction = .4f;

        // Columns: ordinary, fast, tough, shooter, brood, guard, bomber. Boss/children never roll here.
        public static readonly int[] SpawnKinds = { 0, 1, 2, 4, 5, 7, 8 };
        readonly int[][] weights = {
            new[] { 100, 0, 0, 0, 0, 0, 0 }, new[] { 60, 25, 15, 0, 0, 0, 0 },
            new[] { 50, 20, 15, 15, 0, 0, 0 }, new[] { 35, 15, 15, 15, 12, 8, 0 },
            new[] { 20, 15, 15, 15, 15, 10, 10 }
        };
        readonly int[][] lateWeights = {
            new[] { 20, 30, 5, 10, 20, 5, 10 }, new[] { 15, 10, 10, 30, 10, 15, 10 },
            new[] { 10, 10, 20, 15, 10, 25, 10 }, new[] { 10, 20, 10, 15, 10, 15, 20 }
        };
        readonly int[] eliteWeights = { 10, 10, 30, 15, 10, 20, 5 };
        public System.Collections.Generic.IReadOnlyList<int> WeightTable(int wave)
        { return IsEliteWave(wave) && wave > 5 ? eliteWeights : wave > 5 ? lateWeights[(wave - 6) % lateWeights.Length] : weights[Mathf.Clamp(wave - 1, 0, weights.Length - 1)]; }
        public int RollKind(int wave, System.Random random)
        {
            var table = WeightTable(wave);
            int roll = random.Next(100);
            for (int i = 0; i < table.Count; i++) { roll -= table[i]; if (roll < 0) return SpawnKinds[i]; }
            return 0;
        }
        public float AffixChance(int wave) { return Mathf.Min(.5f, (wave < 3 ? 0 : wave >= 5 ? .18f + Mathf.Min(.12f, (wave - 5) * .003f) : .12f) * AffixMultiplier); }
        public float EnemySpeed(int kind)
        {
            switch (kind) { case 1: return 1.9f; case 4: return 1.1f; case 5: return .8f;
                case 6: return 2.2f; case 7: return .7f; case 8: return 1.6f; default: return 1f; }
        }
        /// <summary>Trash clear quotas for Wave 1..5.</summary>
        public int[] WaveQuotas = { 12, 18, 24, 30, 36 };
        /// <summary>Spawn interval seconds per wave index 1..5 (Boss wave unused).</summary>
        public float[] SpawnIntervals = { 0.90f, 0.75f, 0.60f, 0.48f, 0.36f };

        public static LevelConfig Default { get { return Create(EmberDifficulty.Standard, 25); } }

        public int QuotaForWave(int wave)
        {
            if (wave < 1 || wave > BossWave || IsBossWave(wave)) return 0;
            int count = wave <= WaveQuotas.Length ? WaveQuotas[wave - 1] : Mathf.Min(70, 36 + (wave - 5) * 2);
            return Mathf.Max(1, Mathf.RoundToInt(count * CountMultiplier));
        }

        public float SpawnInterval(int wave)
        {
            float interval = wave >= 1 && wave <= SpawnIntervals.Length ? SpawnIntervals[wave - 1] : Mathf.Max(.22f, .36f - (wave - 5) * .006f);
            return Mathf.Max(.14f, interval * IntervalMultiplier);
        }

        public float TrashHp(int wave, int kind)
        {
            float normal = (16f + wave * 6f) * HealthMultiplier, tough = (40f + wave * 10f) * HealthMultiplier;
            switch (kind) { case 2: return tough; case 4: return normal * .8f; case 5: return tough * .6f;
                case 6: return normal * .25f; case 7: return tough * 1.2f; case 8: return normal * .6f; default: return normal; }
        }

        public void ApplyWorld()
        {
            EmberWorld.Limit = MapLimit;
        }
    }
}
