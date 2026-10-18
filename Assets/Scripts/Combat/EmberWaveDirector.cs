using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 波次推进（M3，`requirements.md` §3.1 / §3.2）：本波刷多少、多久刷一个、什么时候清波、
    /// 什么时候出遭遇、Boss 的预警倒计时。
    ///
    /// **本类不造敌人**：需要刷怪时通过 <see cref="Host"/> 请求，由名册执行。
    /// 数值一律查 `LevelConfig`，不在这里写。
    /// </summary>
    public sealed class EmberWaveDirector
    {
        /// <summary>Boss 出场前的预警横幅时长（秒）。</summary>
        public const float BossWarningSeconds = 3f;

        /// <summary>遭遇事件横幅时长（秒）。</summary>
        public const float EncounterBannerSeconds = 2f;

        /// <summary>清波与遭遇的结算口子由战斗根实现。</summary>
        public interface Host
        {
            /// <summary>当前在场敌人数量。</summary>
            int EnemyCount { get; }

            /// <summary>灯芯当前位置（遭遇事件以它为圆心布点）。</summary>
            Vector2 PlayerPosition { get; }

            /// <summary>按波次 roll 一个屏外杂兵；false 表示当前没有合法落点，不消耗配额。</summary>
            bool TrySpawnRolledTrash();

            /// <summary>按 Boss 伴随兵构成生成一个屏外杂兵；失败不占用名额。</summary>
            bool TrySpawnBossSupport();

            /// <summary>请求遭遇落点，名册仍保证屏外；false 不消耗配额。</summary>
            bool TrySpawnTrashAt(int kind, Vector2 position, bool forceAffix);

            /// <summary>刷 Boss。</summary>
            void SpawnBoss();

            /// <summary>清掉敌方弹幕（Boss 出场与收波时用）。</summary>
            void ClearProjectiles();

            /// <summary>一波清完，交回宿主决定发牌还是收局。</summary>
            void OnWaveCleared(int wave);

            /// <summary>最终 Boss 倒下，结算胜利。</summary>
            void OnFinalBossDefeated();
        }

        readonly Host host;
        readonly LevelConfig config;
        readonly System.Random random;

        int wave = 1;
        int waveQuota;
        int waveSpawned;
        int waveKilled;
        int extraSpawned;
        float spawnTimer;
        bool bossSpawned;
        bool awaitingUpgrade;
        bool eventTriggered;
        float encounterTimer;
        float bossWarning;
        float bossSupportTimer;
        string encounterText = "";

        public EmberWaveDirector(Host host, LevelConfig config, System.Random random)
        {
            this.host = host;
            this.config = config;
            this.random = random;
        }

        // ── 只读状态（HUD 与发牌读取）────────────────────────────────────────────────────

        public int Wave { get { return wave; } }
        public bool BossSpawned { get { return bossSpawned; } }
        public bool AwaitingUpgrade { get { return awaitingUpgrade; } }
        public int Quota { get { return waveQuota + extraSpawned; } }
        public int Spawned { get { return waveSpawned; } }
        public int Killed { get { return waveKilled; } }

        /// <summary>Boss 出场预警是否还在倒计时。</summary>
        public bool BossWarningActive { get { return bossWarning > 0f; } }

        /// <summary>HUD 用：横幅文字，非展示期为 empty。</summary>
        public string EncounterText { get { return encounterTimer > 0 ? encounterText : ""; } }

        /// <summary>HUD 用：本波剩余杂兵 = 未刷出的配额 + 场上数量。Boss 波恒为 0。</summary>
        public int Remaining
        {
            get
            {
                if (bossSpawned || wave >= config.BossWave) return 0;
                return Mathf.Max(0, (waveQuota - waveSpawned) + host.EnemyCount);
            }
        }

        /// <summary>本波还需击杀多少（含分裂产生的额外个体）。</summary>
        public int KillsRemaining { get { return Mathf.Max(0, waveQuota + extraSpawned - waveKilled); } }

        // ── 生命周期 ──────────────────────────────────────────────────────────────────────

        /// <summary>开一局：从第 1 波开始。</summary>
        public void Begin()
        {
            wave = 1;
            bossSpawned = false;
            bossWarning = 0f;
            bossSupportTimer = 0f;
            awaitingUpgrade = false;
            eventTriggered = false;
            encounterTimer = 0f;
            extraSpawned = 0;
            waveSpawned = 0;
            waveKilled = 0;
            host.ClearProjectiles();
            StartWave(1);
        }

        /// <summary>选卡结束后推进到下一波。</summary>
        public void Advance() { StartWave(wave + 1); }

        /// <summary>进入某一波：设定配额、间隔与横幅；Boss 波先起预警倒计时。</summary>
        void StartWave(int next)
        {
            wave = next;
            bossSpawned = false;
            extraSpawned = 0;
            eventTriggered = false;
            encounterTimer = 0f;
            bossWarning = 0f;
            bossSupportTimer = 0f;
            awaitingUpgrade = false;
            waveSpawned = 0;
            waveKilled = 0;
            host.ClearProjectiles();

            if (config.IsBossWave(wave))
            {
                waveQuota = 0;
                // ⚠️ bossSpawned 的语义是"这一波是 Boss 波"，不是"Boss 已经出来了"：
                // Remaining 与清波判定都看它，不置位会让空场被判为本波已清完、波次连锁跳过。
                // 真正让 SpawnBoss 等待的是 bossWarning。
                bossSpawned = true;
                bossWarning = BossWarningSeconds;
                bool final = wave >= config.BossWave;
                encounterText = final
                    ? "\u957f\u591c\u5b88\u536b\u0020\u00b7\u0020\u6700\u7ec8\u51b3\u6218\n\u6ce8\u610f\u907f\u5f00\u9884\u8b66\u5708"
                    : "\u5b88\u536b\u8bd5\u70bc\u0020\u00b7\u0020\u6697\u5f71\u6765\u88ad";
                encounterTimer = BossWarningSeconds;
                return;
            }

            waveQuota = config.QuotaForWave(wave);
            spawnTimer = .25f;
            encounterText = "第 " + wave + " 波 · " + config.WaveName(wave);
            encounterTimer = EncounterBannerSeconds;
        }

        // ── 每帧 ──────────────────────────────────────────────────────────────────────────

        /// <summary>推进刷怪节奏、Boss 预警与遭遇事件。伤害与表现不在这里。</summary>
        public void Tick(float dt)
        {
            if (dt <= 0 || awaitingUpgrade) return;
            // 常规刷怪：Boss 波、待选卡、配额刷完或到达同屏上限时停手
            if (!awaitingUpgrade && !bossSpawned && wave < config.BossWave &&
                waveSpawned < waveQuota && host.EnemyCount < config.EnemyLimit)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0)
                {
                    if (host.TrySpawnRolledTrash()) waveSpawned++;
                    spawnTimer = config.SpawnInterval(wave);
                }
            }

            encounterTimer = Mathf.Max(0f, encounterTimer - dt);

            // Boss 预警：StartWave 已把本波标记为 Boss 波，所以倒计时期间不会有别的逻辑来清波。
            if (bossWarning > 0f)
            {
                bossWarning -= dt;
                if (bossWarning <= 0f)
                {
                    bossWarning = 0f;
                    host.SpawnBoss();
                    SpawnBossSupports(config.BossSupportTargetForWave(wave));
                    bossSupportTimer = config.BossSupportInterval;
                    return;
                }
            }

            if (bossSpawned && bossWarning <= 0)
            {
                bossSupportTimer -= dt;
                if (bossSupportTimer <= 0)
                {
                    SpawnBossSupports(config.BossSupportBatch);
                    bossSupportTimer = config.BossSupportInterval;
                }
            }

            TryEncounter();
        }

        void SpawnBossSupports(int batch)
        {
            int missing = config.BossSupportTargetForWave(wave) - Mathf.Max(0, host.EnemyCount - 1);
            int available = Mathf.Max(0, config.EnemyLimit - host.EnemyCount);
            int count = Mathf.Min(batch, Mathf.Min(missing, available));
            for (int i = 0; i < count; i++)
                if (!host.TrySpawnBossSupport()) break;
        }

        /// <summary>本波刷到一半时放一次遭遇事件（精英小队或围杀圈）。</summary>
        void TryEncounter()
        {
            if (wave < 3 || bossSpawned || awaitingUpgrade || eventTriggered ||
                waveSpawned < (waveQuota + 1) / 2) return;
            eventTriggered = true;

            bool elite = config.IsEliteWave(wave) || random.Next(2) == 0;
            int count = Mathf.Min(
                elite ? (config.IsEliteWave(wave) ? 6 : 3) : 10,
                waveQuota - waveSpawned,
                config.EnemyLimit - host.EnemyCount);
            if (count <= 0) return;

            float offset = (float)random.NextDouble() * Mathf.PI * 2;
            for (int i = 0; i < count; i++)
            {
                float a = offset + i * Mathf.PI * 2 / count;
                Vector2 p = SafeEncounterPosition(host.PlayerPosition, new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                // 精英小队强制带词缀；普通围杀圈按概率
                if (host.TrySpawnTrashAt(elite ? 2 : 0, p, elite)) waveSpawned++;
            }
            encounterText = elite ? "遭遇 · 精英小队" : "遭遇 · 围杀圈";
            encounterTimer = EncounterBannerSeconds;
        }

        /// <summary>
        /// 遭遇事件的落点：以 <paramref name="player"/> 为心取 <paramref name="direction"/> 方向 5 单位处；
        /// 若因边界钳制后离玩家不足 3 单位，则反向再取一次。
        /// </summary>
        public static Vector2 SafeEncounterPosition(Vector2 player, Vector2 direction)
        {
            Vector2 candidate = EmberWorld.Clamp(player + direction * 5f, .8f);
            if (Vector2.Distance(candidate, player) >= 3f) return candidate;
            return EmberWorld.Clamp(player - direction * 5f, .8f);
        }

        // ── 死亡与清波的结果回报 ──────────────────────────────────────────────────────────

        /// <summary>记一个杂兵被击杀（分裂子体也算）。</summary>
        public void NoteKill() { waveKilled++; }

        /// <summary>分裂产生的新个体计入本波配额之外。</summary>
        public void NoteExtraSpawn(int count) { extraSpawned += count; }

        /// <summary>
        /// Boss 倒下。最终 Boss → 收局胜利；阶段 Boss → 起一次选卡。
        /// </summary>
        public void NoteBossDefeated()
        {
            awaitingUpgrade = true;
            host.ClearProjectiles();
            if (wave >= config.BossWave) host.OnFinalBossDefeated();
            else host.OnWaveCleared(wave);
        }

        /// <summary>
        /// 每帧死亡结算结束后调用：判定本波是否已清空。
        /// 条件是"配额刷完 + 场上无敌人"，且不能是 Boss 波、不能已在待选卡状态。
        /// </summary>
        public void CheckWaveCleared()
        {
            if (bossSpawned || awaitingUpgrade) return;
            if (waveSpawned < waveQuota || host.EnemyCount != 0) return;

            awaitingUpgrade = true;
            host.ClearProjectiles();
            encounterTimer = 0f;
            host.OnWaveCleared(wave);
        }

    }
}
