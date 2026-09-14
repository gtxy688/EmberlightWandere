using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Combat sim: enemies, weapon ticks. Wave clear → upgrade card → next wave / Boss. No death drops.</summary>
    public sealed class EmberCombat
    {
        public sealed class Enemy
        {
            public Transform view;
            public float hp, maxHp, speed, radius, flash;
            public EmberEnemyBar bar;
            public EmberBoss ai;
            public int kind;
            public IEnemyBehavior behavior;
            public EmberEnemyVisual visual;
            public EnemyAffix affix;
            public bool dead, revived, detonating;
            public float reviveTimer, fuse, facing, charge, age;
            public Vector2 aim, moveDirection;
            public bool Targetable { get { return view != null && hp > 0 && !dead && !detonating && reviveTimer <= 0; } }
            public SpriteRenderer[] parts;
            public Color[] colors;
        }

        readonly EmberGame game;
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<EmberWeapon> weapons = new List<EmberWeapon>();
        readonly EmberTrailWeapon trailWeapon = new EmberTrailWeapon();
        readonly EmberWeaponContext weaponCtx = new EmberWeaponContext();
        readonly System.Random random = new System.Random();
        readonly EmberPool pool = new EmberPool();
        Vector2 lastPlayerPos;
        bool hasLastPlayerPos;
        readonly EmberEnemyContext enemyCtx = new EmberEnemyContext();
        readonly EmberEnemyProjectiles enemyProjectiles;
        readonly EmberHealingDrops healingDrops;
        readonly List<Vector2> pendingBroods = new List<Vector2>();
        bool eventTriggered;
        float encounterTimer, burningTimer;
        string encounterText = "";
        int extraSpawned;
        public string EncounterText { get { return encounterTimer > 0 ? encounterText : ""; } }

        /// <summary>Banner lead-in before a Boss appears, in unscaled-by-design game seconds.</summary>
        const float BossWarningSeconds = 3f;

        Transform world, player;
        Camera cam;
        EmberEffects effects;
        Enemy bossEnemy;
        LevelConfig config;
        float spawnTimer;
        bool bossSpawned, awaitingUpgrade;
        // Counts down the pre-boss banner before the Boss actually appears. Kept separate from
        // bossSpawned so ticks can tell "warning is showing" apart from "Boss is out".
        float bossWarning;
        int wave = 1;
        int waveQuota;
        int waveSpawned;
        int waveKilled;

        public int EnemyCount { get { return enemies.Count; } }
        public Enemy Boss { get { return bossEnemy; } }
        public bool BossAlive { get { return bossEnemy != null; } }
        public int Wave { get { return wave; } }
        public int WaveQuota { get { return waveQuota + extraSpawned; } }
        public int WaveSpawned { get { return waveSpawned; } }
        public int WaveKilled { get { return waveKilled; } }
        /// <summary>Diagnostic snapshot for the editor boss-state dump.</summary>
        public string DebugState()
        {
            return "wave=" + wave + " quota=" + waveQuota + " spawned=" + waveSpawned
                + " kills=" + waveKilled + " enemies=" + enemies.Count
                + " bossSpawned=" + bossSpawned + " bossWarning=" + bossWarning.ToString("F2")
                + " awaitingUpgrade=" + awaitingUpgrade + " bossEnemy=" + (bossEnemy != null)
                + " BossWave=" + (config != null ? config.BossWave : -1)
                + " IsBossWave=" + (config != null && config.IsBossWave(wave));
        }

#if UNITY_EDITOR
        /// <summary>Temporary hook so the editor can jump waves during a run.</summary>
        public void StartWaveForProbe(int next) { StartWave(next); }
#endif
        /// <summary>Remaining trash for HUD: unspawned quota + on-field count.</summary>
        public int WaveRemaining
        {
            get
            {
                if (bossSpawned || (config != null && wave >= config.BossWave)) return 0;
                return Mathf.Max(0, (waveQuota - waveSpawned) + enemies.Count);
            }
        }
        public int WaveKillsRemaining { get { return Mathf.Max(0, waveQuota + extraSpawned - waveKilled); } }

        public EmberCombat(EmberGame owner)
        {
            game = owner;
            enemyProjectiles = new EmberEnemyProjectiles(pool);
            healingDrops = new EmberHealingDrops(pool, game);
            // Register all weapon modules; Tick gates on OwnedWeapons / WavePower (Gods-Select-v3: slot weapons at Begin).
            weapons.Add(new EmberBasicShotWeapon());
            weapons.Add(new EmberOrbitWeapon());
            weapons.Add(trailWeapon);
            weapons.Add(new EmberPulseWeapon());
            weapons.Add(new EmberPierceWeapon());
            weapons.Add(new EmberBoomerangWeapon());
            weapons.Add(new EmberMeteorWeapon());
        }

        public void Begin(Transform runWorld, Transform keeper, Camera camera, EmberEffects fx)
        {
            Begin(runWorld, keeper, camera, fx, LevelConfig.Default);
        }

        public void Begin(Transform runWorld, Transform keeper, Camera camera, EmberEffects fx, LevelConfig levelConfig)
        {
            ClearLists(destroyViews: false);
            world = runWorld;
            player = keeper;
            hasLastPlayerPos = false;
            cam = camera;
            effects = fx;
            pool.Bind(runWorld, 80);
            enemyProjectiles.Bind(runWorld, camera);
            enemyCtx.Player = player;
            enemyCtx.Camera = cam;
            enemyCtx.Projectiles = enemyProjectiles;
            enemyCtx.Split = point => pendingBroods.Add(point);
            enemyCtx.Explode = Explode;
            config = levelConfig ?? LevelConfig.Default;
            config.ApplyWorld();
            enemyCtx.Config = config;
            healingDrops.Bind(world, config);
            burningTimer = 0;
            spawnTimer = .3f;
            bossSpawned = false;
            bossEnemy = null;
            awaitingUpgrade = false;
            for (int i = 0; i < weapons.Count; i++) weapons[i].Reset();
            BindWeaponContext();
            StartWave(1);
        }

        public void TearDown()
        {
            ClearLists(destroyViews: false);
            pool.Clear(destroy: true);
            world = player = null;
            effects = null;
            cam = null;
            bossEnemy = null;
            bossSpawned = false;
            awaitingUpgrade = false;
            config = null;
            for (int i = 0; i < weapons.Count; i++) weapons[i].Reset();
        }

        void BindWeaponContext()
        {
            weaponCtx.Player = player;
            weaponCtx.World = world;
            weaponCtx.Cam = cam;
            weaponCtx.Effects = effects;
            weaponCtx.Pool = pool;
            weaponCtx.Random = random;
            weaponCtx.EnemyCount = () => enemies.Count;
            weaponCtx.GetEnemy = i => enemies[i];
            weaponCtx.Damage = Damage;
            weaponCtx.DamageFrom = DamageFrom;
            weaponCtx.ContactDamageFrom = (i, amount, source) => ApplyDamageFrom(i, amount, source, false);
            weaponCtx.Intercept = enemyProjectiles.Intercept;
            weaponCtx.StillPlaying = () => game.State == EmberGame.Mode.Playing;
            weaponCtx.SpawnBurnPatch = (pos, scale) =>
            {
                if (world != null) trailWeapon.SpawnPatch(pos, world, scale, pool);
            };
        }

        void ClearLists(bool destroyViews)
        {
            healingDrops.Clear();
            enemyProjectiles.Clear();
            pendingBroods.Clear();
            encounterTimer = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e.view == null) continue;
                if (destroyViews) { if (e.visual != null) e.visual.Clear(); Object.Destroy(e.view.gameObject); }
                else ReleaseEnemyView(e);
            }
            enemies.Clear();
        }

        static string EnemyPoolKey(int kind) { return kind == 3 ? "boss" : "shade"; }

        void ReleaseEnemyView(Enemy e)
        {
            if (e == null || e.view == null) return;
            if (e.ai != null) { e.ai.Dispose(); e.ai = null; }
            if (e.visual != null) { e.visual.Clear(); e.visual = null; }
            // Strip HP bar so the next Rent + EmberEnemyBar does not stack duplicates.
            var bar = e.view.Find("Health bar");
            if (bar != null) Object.DestroyImmediate(bar.gameObject);
            pool.Release(EnemyPoolKey(e.kind), e.view.gameObject);
            e.view = null;
        }

        void StartWave(int next)
        {
            wave = next;
            bossSpawned = false;
            extraSpawned = 0;
            eventTriggered = false;
            encounterTimer = 0;
            bossWarning = 0;
            enemyProjectiles.Clear();
            awaitingUpgrade = false;
            waveSpawned = 0;
            waveKilled = 0;
            if (config == null) config = LevelConfig.Default;
            if (config.IsBossWave(wave))
            {
                waveQuota = 0;
                // Announce the Boss before it exists. Previously the Boss appeared on this very
                // frame with no banner at all, which read as a bug on the final wave and gave
                // the player no chance to reposition.
                if (!bossSpawned)
                {
                    bossWarning = BossWarningSeconds;
                    bool final = wave >= config.BossWave;
                    encounterText = final
                        ? "\u957f\u591c\u5b88\u536b\u0020\u00b7\u0020\u6700\u7ec8\u51b3\u6218\n\u6ce8\u610f\u907f\u5f00\u9884\u8b66\u5708"
                        : "\u5b88\u536b\u8bd5\u70bc\u0020\u00b7\u0020\u6697\u5f71\u6765\u88ad";
                    encounterTimer = BossWarningSeconds;
                }
                return;
            }
            waveQuota = config.QuotaForWave(wave);
            spawnTimer = .25f;
            encounterText = "第 " + wave + " 波 · " + config.WaveName(wave);
            encounterTimer = 2f;
        }

        /// <summary>After upgrade Choose: start clearedWave+1 (stage Boss or final Boss from config).</summary>
        public void AdvanceAfterUpgrade()
        {
            StartWave(wave + 1);
        }

        /// <summary>Editor-only shortcut that jumps straight to the Boss, skipping the lead-in banner.</summary>
        public void PreviewBoss()
        {
            if (game.State != EmberGame.Mode.Playing || bossSpawned) return;
            ClearLists(false);
            wave = (config ?? LevelConfig.Default).BossWave;
            extraSpawned = 0;
            waveQuota = 0;
            waveSpawned = 0;
            waveKilled = 0;
            awaitingUpgrade = false;
            bossWarning = 0;
            bossSpawned = true;
            SpawnEnemy(true);
        }

        public void Tick(float dt)
        {
            if (game.State != EmberGame.Mode.Playing) return;
            var progress = game.Progress;
            if (config == null) config = LevelConfig.Default;

            if (!awaitingUpgrade && !bossSpawned && wave < config.BossWave && waveSpawned < waveQuota && enemies.Count < config.EnemyLimit)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0)
                {
                    SpawnEnemy(false);
                    waveSpawned++;
                    spawnTimer = config.SpawnInterval(wave);
                }
            }

            healingDrops.Tick(dt, player.position);
            encounterTimer = Mathf.Max(0, encounterTimer - dt);

            // Boss lead-in: hold the banner, then bring the Boss in. Deliberately placed before
            // the horde spawner above could restart, and it never runs while a Boss is out.
            if (bossWarning > 0)
            {
                bossWarning -= dt;
                if (bossWarning <= 0)
                {
                    bossWarning = 0;
                    bossSpawned = true;
                    SpawnEnemy(true);
                }
            }

            TryEncounter();
            enemyProjectiles.Move(dt);
            bool inBurningAura = false;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                if (e.dead) continue;
                e.age += dt;
                e.flash = Mathf.Max(0, e.flash - dt);
                for (int k = 0; k < e.parts.Length; k++)
                    e.parts[k].color = e.reviveTimer > 0 ? new Color(.7f, .8f, .9f, .25f)
                        : e.flash > 0 ? Color.Lerp(e.colors[k], Color.white, .7f) : e.colors[k];
                if (e.reviveTimer > 0)
                {
                    e.reviveTimer = Mathf.Max(0, e.reviveTimer - dt);
                    if (e.reviveTimer == 0) { e.hp = e.maxHp * config.RebirthHpFraction; e.bar.Set(config.RebirthHpFraction); }
                    if (e.visual != null) e.visual.Tick(game.Elapsed, config);
                    continue;
                }
                Vector2 before = e.view.position;
                if (e.ai != null) e.ai.Tick(dt, player.position, e.hp / e.maxHp);
                else
                {
                    SetEnemyContext(e);
                    e.behavior.Tick(dt, enemyCtx);
                }
                if (game.State != EmberGame.Mode.Playing) return;
                e.moveDirection = ((Vector2)e.view.position - before).normalized;
                if (e.visual != null) e.visual.Tick(game.Elapsed, config);
                if (!e.Targetable) continue;
                float distance = Vector2.Distance(player.position, e.view.position);
                if (e.affix == EnemyAffix.Burning && distance <= config.BurningRadius) inBurningAura = true;
                if (distance < e.radius + .32f && game.HurtReady)
                {
                    game.HurtPlayer(e.kind == 3 ? 24 : 9);
                    if (game.State != EmberGame.Mode.Playing) return;
                }
            }
            // One shared pulse prevents overlapping auras scaling damage with enemy count.
            burningTimer = inBurningAura ? burningTimer + dt : 0;
            if (burningTimer >= 1f)
            {
                burningTimer -= 1f;
                game.HurtPlayer(config.BurningDamage);
                if (game.State != EmberGame.Mode.Playing) return;
            }

            // Tick weapons (basic / orbits / trail / pulse / unlockables).
            Vector2 nowPos = player != null ? (Vector2)player.position : Vector2.zero;
            if (hasLastPlayerPos && dt > 1e-5f)
                weaponCtx.PlayerSpeed = Vector2.Distance(nowPos, lastPlayerPos) / dt;
            else
                weaponCtx.PlayerSpeed = 0f;
            lastPlayerPos = nowPos;
            hasLastPlayerPos = true;

            weaponCtx.Progress = progress;
            weaponCtx.Elapsed = game.Elapsed;
            weaponCtx.Player = player;
            weaponCtx.World = world;
            weaponCtx.Cam = cam;
            weaponCtx.Effects = effects;
            weaponCtx.Pool = pool;
            for (int w = 0; w < weapons.Count; w++)
            {
                if (game.State != EmberGame.Mode.Playing) return;
                weapons[w].Tick(dt, weaponCtx);
            }
            if (game.State != EmberGame.Mode.Playing) return;
            enemyProjectiles.HitPlayer(player.position, game.HurtPlayer);
            if (game.State != EmberGame.Mode.Playing) return;
            ResolveDeaths();
        }

        void FireBossVolley(Vector2 origin, int count)
        {
            float offset = (float)random.NextDouble() * Mathf.PI * 2f;
            for (int i = 0; i < count; i++)
            {
                float a = offset + i * Mathf.PI * 2f / count;
                enemyProjectiles.Fire(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), 4.5f, 10f);
            }
        }

        public void PlayBossBurst(Vector2 point) { if (effects != null) effects.Nova(point, 1.2f); }

        void SpawnEnemy(bool boss, int forcedKind = -1, Vector2? position = null, bool forceAffix = false)
        {
            if (config == null) config = LevelConfig.Default;
            int kind = boss ? 3 : forcedKind >= 0 ? forcedKind : config.RollKind(wave, random);
            float a = (float)random.NextDouble() * Mathf.PI * 2;
            Vector2 p = (Vector2)player.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 10;
            p = position ?? EmberWorld.Clamp(p, .8f);
            if (boss) p = EmberWorld.Clamp((Vector2)player.position + new Vector2(0, 2), 1);
            Color color = kind == 4 ? new Color(.45f, .22f, .65f) : kind == 5 ? new Color(.4f, .5f, .22f) : kind == 6 ? new Color(.55f, .7f, .22f) : kind == 7 ? new Color(.25f, .4f, .49f) : kind == 8 ? new Color(.8f, .29f, .13f) : kind == 1 ? new Color(.65f, .29f, .46f) : kind == 2 ? new Color(.39f, .31f, .57f) : new Color(.29f, .30f, .39f);
            var view = pool.RentEnemy(boss ? "boss" : "shade", world, color);
            var silhouette = view.GetComponent<EmberEnemySilhouette>();
            if (silhouette == null) silhouette = view.gameObject.AddComponent<EmberEnemySilhouette>();
            silhouette.Apply(kind, color);
            view.position = p;
            float size = boss ? 2.7f : kind == 6 ? .40f : kind == 5 ? 1.25f : kind == 7 ? 1.2f : kind == 8 ? .65f : kind == 2 ? 1.3f : kind == 1 ? .65f : 1;
            view.localScale = Vector3.one * size;
            float hp = boss ? config.BossHpForWave(wave) : config.TrashHp(wave, kind);
            enemies.Add(new Enemy
            {
                view = view,
                hp = hp,
                speed = boss ? .8f : config.EnemySpeed(kind),
                radius = .32f * size,
                kind = kind,
                parts = view.GetComponentsInChildren<SpriteRenderer>()
            });
            var added = enemies[enemies.Count - 1];
            added.maxHp = added.hp;
            if (!boss)
            {
                added.behavior = CreateBehavior(kind);
                Vector2 delta = (Vector2)player.position - p;
                added.facing = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                if (kind != 6 && (forceAffix || random.NextDouble() < config.AffixChance(wave)))
                    added.affix = (EnemyAffix)(1 + random.Next(3));
                added.visual = new EmberEnemyVisual(added, world);
            }
            added.bar = new EmberEnemyBar(view, boss);
            if (boss) { added.ai = new EmberBoss(game, view, world, config.BossTier(wave), FireBossVolley); bossEnemy = added; }
            added.colors = new Color[added.parts.Length];
            for (int k = 0; k < added.parts.Length; k++) added.colors[k] = added.parts[k].color;
        }

        void SetEnemyContext(Enemy e)
        {
            enemyCtx.Enemy = e;
            enemyCtx.Config = config;
            enemyCtx.SpeedMultiplier = e.affix == EnemyAffix.Wind && e.age % config.WindPeriod < config.WindDuration
                ? config.WindSpeedMultiplier : 1f;
        }

        public static IEnemyBehavior CreateBehavior(int kind)
        {
            switch (kind)
            {
                case 4: return new ShooterBehavior();
                case 5: return new SplitBehavior();
                case 7: return new ShieldBehavior();
                case 8: return new BomberBehavior();
                default: return new ChaseBehavior();
            }
        }

        void Explode(Vector2 position, float radius, float damage)
        {
            effects.Nova(position, radius);
            if (Vector2.Distance(player.position, position) <= radius) game.HurtPlayer(damage);
        }

        void Damage(int index, float amount) { DamageFrom(index, amount, player.position); }

        void DamageFrom(int index, float amount, Vector2 source)
        { ApplyDamageFrom(index, amount, source, true); }

        void ApplyDamageFrom(int index, float amount, Vector2 source, bool playHit)
        {
            if (index < 0 || index >= enemies.Count) return;
            var e = enemies[index];
            if (!e.Targetable || !EmberWorld.Visible(cam, e.view.position)) return;
            if (e.kind == 7) amount *= ShieldBehavior.DamageMultiplier(e.facing, source - (Vector2)e.view.position, config.ShieldFrontMultiplier);
            e.hp = Mathf.Max(0, e.hp - amount);
            if (playHit) EmberAudio.Ensure().PlayHit();
            e.bar.Set(e.hp / e.maxHp);
            if (e.flash <= 0) { effects.Impact(e.view.position, false); e.flash = .10f; }
            if (amount >= 1) e.view.position += (e.view.position - player.position).normalized * (e.kind == 3 ? .015f : .06f);
            if (e.hp > 0) return;
            if (TryBeginRebirth(e, config)) return;
            SetEnemyContext(e);
            if (e.behavior != null && e.behavior.OnLethal(enemyCtx)) return;
            e.dead = true;
        }

        public static bool TryBeginRebirth(Enemy e, LevelConfig config)
        {
            if (e.affix != EnemyAffix.Rebirth || e.revived || e.kind == 3) return false;
            e.revived = true;
            e.hp = 0;
            e.dead = e.detonating = false;
            e.reviveTimer = config.RebirthDelay;
            e.charge = e.fuse = 0;
            // A reborn enemy receives a fresh behavior state (no invisible charged shot).
            e.behavior = CreateBehavior(e.kind);
            return true;
        }

        void ResolveDeaths()
        {
            // Keep indices stable while every weapon ticks; children spawn only after that pass.
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (!e.dead) continue;
                // Also covers bombers that ignite while alive and die in their own explosion.
                if (TryBeginRebirth(e, config)) continue;
                bool boss = e.kind == 3;
                SetEnemyContext(e);
                if (e.behavior != null) e.behavior.OnDeath(enemyCtx);
                effects.Impact(e.view.position, true);
                healingDrops.OnDeath(e.view.position, e.kind, e.affix, random);
                ReleaseEnemyView(e);
                enemies.RemoveAt(i);
                game.RegisterKill();
                game.Progress.AddScore(boss ? 30 : e.kind == 6 ? 0 : e.affix != EnemyAffix.None ? 3 : 1);
                if (boss)
                {
                    bossEnemy = null;
                    enemyProjectiles.Clear();
                    awaitingUpgrade = true;
                    if (wave == config.BossWave) game.EndRun(true);
                    else game.OnWaveCleared(wave);
                    return;
                }
                waveKilled++;
            }
            for (int i = 0; i < pendingBroods.Count; i++)
            {
                for (int n = 0; n < config.SplitCount; n++)
                {
                    float a = n * Mathf.PI * 2 / config.SplitCount;
                    var position = EmberWorld.Clamp(pendingBroods[i] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .45f, .3f);
                    SpawnEnemy(false, 6, position);
                    extraSpawned++;
                }
            }
            pendingBroods.Clear();
            if (!bossSpawned && !awaitingUpgrade && waveSpawned >= waveQuota && enemies.Count == 0)
            {
                awaitingUpgrade = true;
                enemyProjectiles.Clear();
                encounterTimer = 0;
                game.OnWaveCleared(wave);
            }
        }

        void TryEncounter()
        {
            if (wave < 3 || bossSpawned || awaitingUpgrade || eventTriggered || waveSpawned < (waveQuota + 1) / 2) return;
            eventTriggered = true;
            bool elite = config.IsEliteWave(wave) || random.Next(2) == 0;
            int count = Mathf.Min(elite ? (config.IsEliteWave(wave) ? 6 : 3) : 10, waveQuota - waveSpawned, config.EnemyLimit - enemies.Count);
            if (count <= 0) return;
            float offset = (float)random.NextDouble() * Mathf.PI * 2;
            for (int i = 0; i < count; i++)
            {
                float a = offset + i * Mathf.PI * 2 / count;
                Vector2 p = SafeEncounterPosition(player.position, new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                SpawnEnemy(false, elite ? 2 : 0, p, elite);
                waveSpawned++;
            }
            encounterText = elite ? "遭遇 · 精英小队" : "遭遇 · 围杀圈";
            encounterTimer = 2f;
        }

        public static Vector2 SafeEncounterPosition(Vector2 player, Vector2 direction)
        {
            Vector2 candidate = EmberWorld.Clamp(player + direction * 5f, .8f);
            if (Vector2.Distance(candidate, player) >= 3f) return candidate;
            return EmberWorld.Clamp(player - direction * 5f, .8f);
        }
    }
}
