using UnityEngine;

namespace Emberlight
{
    /// <summary>Run/level tunables: arena, waves, spawn cadence, boss HP. Combat reads this instead of magic numbers.</summary>
    public sealed class LevelConfig
    {
        public float MapLimit = 22f;
        public float BossHp = 2200f;
        public int BossWave = 6;
        /// <summary>Trash clear quotas for Wave 1..5.</summary>
        public int[] WaveQuotas = { 12, 18, 24, 30, 36 };
        /// <summary>Spawn interval seconds per wave index 1..5 (Boss wave unused).</summary>
        public float[] SpawnIntervals = { 0.90f, 0.75f, 0.60f, 0.48f, 0.36f };

        public static LevelConfig Default { get; } = new LevelConfig();

        public int QuotaForWave(int wave)
        {
            if (wave < 1 || wave > WaveQuotas.Length) return 0;
            return WaveQuotas[wave - 1];
        }

        public float SpawnInterval(int wave)
        {
            if (wave < 1 || wave > SpawnIntervals.Length) return 0.90f;
            return Mathf.Max(0.14f, SpawnIntervals[wave - 1]);
        }

        public float TrashHp(int wave, int kind)
        {
            if (kind == 2) return 40f + wave * 10f;
            return 16f + wave * 6f;
        }

        public void ApplyWorld()
        {
            EmberWorld.Limit = MapLimit;
        }
    }
}
