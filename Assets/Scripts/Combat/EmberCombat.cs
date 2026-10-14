using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 战斗根（M3）：本局战斗的装配点与每帧顺序。
    ///
    /// 只做四件事——拥有武器实例、装配各战斗单元、驱动每帧顺序、把单元之间的回调接起来。
    /// 具体规则分别属于：波次推进 <see cref="EmberWaveDirector"/>、敌人实例化与回收
    /// <see cref="EmberEnemyRoster"/>、伤害与死亡结算 <see cref="EmberCombatDamage"/>、
    /// 敌方弹幕 <see cref="EmberEnemyProjectiles"/>、治疗掉落 <see cref="EmberHealingDrops"/>。
    /// 对宿主的依赖只有 <see cref="IEmberCombatHost"/>，不认识 `EmberGame`。
    /// </summary>
    public sealed class EmberCombat : EmberWaveDirector.Host
    {
        readonly List<EmberWeapon> weapons = EmberWeaponFactory.CreateAll();
        readonly EmberBurnGround burnGround = new EmberBurnGround();
        readonly EmberWeaponContext weaponCtx = new EmberWeaponContext();
        readonly IEmberCombatHost host;
        readonly System.Random random = new System.Random();
        readonly EmberPool pool = new EmberPool();
        readonly EmberEnemyProjectiles enemyProjectiles;
        readonly EmberHealingDrops healingDrops;
        readonly EmberEnemyContext enemyCtx = new EmberEnemyContext();
        readonly EmberCombatDamage.Context damageCtx = new EmberCombatDamage.Context();
        readonly List<Vector2> pendingBroods = new List<Vector2>();
        EmberEnemyRoster roster;
        EmberWaveDirector waves;
        Transform world, player;
        Camera cam;
        EmberEffects effects;
        LevelConfig config;
        EmberEnemy bossEnemy;
        Vector2 lastPlayerPos;
        bool hasLastPlayerPos;
        float burningTimer;
        bool inBurningAura;

        public EmberCombat(IEmberCombatHost owner)
        {
            host = owner;
            enemyProjectiles = new EmberEnemyProjectiles(pool);
            healingDrops = new EmberHealingDrops(pool, host);
        }

        // 只读状态，供 HUD 与武器 context 读取。

        public int EnemyCount { get { return roster != null ? roster.Count : 0; } }
        public EmberEnemy Boss { get { return bossEnemy; } }
        public bool BossAlive { get { return bossEnemy != null; } }
        public int Wave { get { return waves != null ? waves.Wave : 1; } }
        public int WaveQuota { get { return waves != null ? waves.Quota : 0; } }
        public int WaveSpawned { get { return waves != null ? waves.Spawned : 0; } }
        public int WaveKilled { get { return waves != null ? waves.Killed : 0; } }
        public int WaveRemaining { get { return waves != null ? waves.Remaining : 0; } }
        public int WaveKillsRemaining { get { return waves != null ? waves.KillsRemaining : 0; } }
        public string EncounterText { get { return waves != null ? waves.EncounterText : ""; } }

        int EmberWaveDirector.Host.EnemyCount { get { return EnemyCount; } }

        Vector2 EmberWaveDirector.Host.PlayerPosition
        {
            get { return player != null ? (Vector2)player.position : Vector2.zero; }
        }

        bool EmberWaveDirector.Host.TrySpawnRolledTrash()
        {
            return roster.Spawn(false, Wave, host, FireBossVolley) != null;
        }

        bool EmberWaveDirector.Host.TrySpawnTrashAt(int kind, Vector2 position, bool forceAffix)
        {
            return roster.Spawn(false, Wave, host, FireBossVolley, kind, position, forceAffix) != null;
        }

        void EmberWaveDirector.Host.SpawnBoss()
        {
            bossEnemy = roster.Spawn(true, Wave, host, FireBossVolley);
        }

        void EmberWaveDirector.Host.ClearProjectiles()
        {
            enemyProjectiles.Clear();
        }

        void EmberWaveDirector.Host.OnWaveCleared(int wave)
        {
            host.OnWaveCleared(wave);
        }

        void EmberWaveDirector.Host.OnFinalBossDefeated()
        {
            host.EndRun(true);
        }

        public void Begin(Transform runWorld, Transform keeper, Camera camera, EmberEffects fx)
        {
            Begin(runWorld, keeper, camera, fx, LevelConfig.Default);
        }

        public void Begin(Transform runWorld, Transform keeper, Camera camera, EmberEffects fx, LevelConfig levelConfig)
        {
            TearDown();
            world = runWorld;
            player = keeper;
            hasLastPlayerPos = false;
            cam = camera;
            effects = fx;
            config = levelConfig ?? LevelConfig.Default;
            pool.Bind(runWorld, 80);
            enemyProjectiles.Bind(runWorld, camera);
            enemyCtx.Player = player;
            enemyCtx.Camera = cam;
            enemyCtx.Projectiles = enemyProjectiles;
            enemyCtx.Split = point => pendingBroods.Add(point);
            enemyCtx.Explode = Explode;
            config.ApplyWorld();
            enemyCtx.Config = config;
            healingDrops.Bind(world, config);
            roster = new EmberEnemyRoster(pool, config, world, player, random, cam);
            waves = new EmberWaveDirector(this, config, random);
            burningTimer = 0f;
            inBurningAura = false;
            for (int i = 0; i < weapons.Count; i++)
                weapons[i].Reset();
            burnGround.Reset();
            BindWeaponContext();
            BindDamageContext();
            waves.Begin();
        }

        public void TearDown()
        {
            if (roster != null)
                roster.Clear(destroyViews: false);
            healingDrops.Clear();
            enemyProjectiles.Clear();
            pendingBroods.Clear();
            pool.Clear(destroy: true);
            world = player = null;
            effects = null;
            cam = null;
            bossEnemy = null;
            config = null;
            roster = null;
            waves = null;
            burningTimer = 0f;
            inBurningAura = false;
            for (int i = 0; i < weapons.Count; i++)
                weapons[i].Reset();
            burnGround.Reset();
        }

        void BindWeaponContext()
        {
            weaponCtx.Player = player;
            weaponCtx.World = world;
            weaponCtx.Cam = cam;
            weaponCtx.Effects = effects;
            weaponCtx.Pool = pool;
            weaponCtx.Random = random;
            weaponCtx.EnemyCount = () => EnemyCount;
            weaponCtx.GetEnemy = i => roster.At(i);
            weaponCtx.Damage = (i, amount) => EmberCombatDamage.Apply(damageCtx, i, amount);
            weaponCtx.DamageFrom = (i, amount, source) => EmberCombatDamage.Apply(damageCtx, i, amount, source);
            weaponCtx.ContactDamageFrom = (i, amount, source) => EmberCombatDamage.Apply(damageCtx, i, amount, source, false);
            weaponCtx.Intercept = enemyProjectiles.Intercept;
            weaponCtx.StillPlaying = () => host.Phase == EmberRunPhase.Playing;
            weaponCtx.PlaySound = PlayWeaponSound;
            weaponCtx.SpawnBurnPatch = (pos, scale) =>
            {
                if (world != null)
                    burnGround.SpawnPatch(pos, world, scale, pool);
            };
        }

        void BindDamageContext()
        {
            damageCtx.Roster = roster;
            damageCtx.Config = config;
            damageCtx.Effects = effects;
            damageCtx.Player = player;
            damageCtx.Camera = cam;
            damageCtx.EnemyContext = enemyCtx;
            damageCtx.PlayHit = () => EmberAudio.Ensure().PlayHit();
            damageCtx.AddScore = amount => host.Progress.AddScore(amount);
            damageCtx.NoteSplitPoint = point => pendingBroods.Add(point);
            damageCtx.HealingDrops = healingDrops;
            damageCtx.Random = random;
        }

        static void PlayWeaponSound(EmberWeaponSound sound)
        {
            var audio = EmberAudio.Ensure();
            switch (sound)
            {
                case EmberWeaponSound.Fireball:
                    audio.PlayWeaponFireball();
                    break;
                case EmberWeaponSound.OrbitContact:
                    audio.PlayWeaponOrbitContact();
                    break;
                case EmberWeaponSound.BurnGround:
                    audio.PlayWeaponBurnGround();
                    break;
                case EmberWeaponSound.PierceArrow:
                    audio.PlayWeaponPierceArrow();
                    break;
                case EmberWeaponSound.Boomerang:
                    audio.PlayWeaponBoomerang();
                    break;
                case EmberWeaponSound.MeteorImpact:
                    audio.PlayMeteorImpact();
                    break;
            }
        }

        /// <summary>选卡结束后推进到下一波。</summary>
        public void AdvanceAfterUpgrade()
        {
            if (waves != null)
                waves.Advance();
        }

        /// <summary>
        /// 一帧的战斗顺序：波次 → 掉落 → 弹幕 → 敌人 → 灼热光环 → 武器 → 弹幕命中 → 死亡结算。
        /// 每个阶段之后都重新确认仍在 Playing，因为受伤可能当场收局。
        /// </summary>
        public void Tick(float dt)
        {
            if (host.Phase != EmberRunPhase.Playing)
                return;
            if (waves == null || roster == null || player == null)
                return;
            var progress = host.Progress;
            waves.Tick(dt);
            healingDrops.Tick(dt, player.position);
            // Boss 不需要单独 Tick：它也在名册里，由 TickEnemies 的 ai 分支推进。
            enemyProjectiles.Move(dt);
            TickEnemies(dt);
            if (host.Phase != EmberRunPhase.Playing)
                return;
            // 灼热光环：多个光环共用一次脉冲，避免按敌人数叠加伤害。
            if (inBurningAura)
                burningTimer += dt;
            else
                burningTimer = 0f;
            if (burningTimer >= 1f)
            {
                burningTimer -= 1f;
                host.HurtPlayer(config.BurningDamage);
                if (host.Phase != EmberRunPhase.Playing)
                    return;
            }

            TickWeapons(dt, progress);
            if (host.Phase != EmberRunPhase.Playing)
                return;
            enemyProjectiles.HitPlayer(player.position, host.HurtPlayer);
            if (host.Phase != EmberRunPhase.Playing)
                return;
            ResolveDeaths();
        }

        /// <summary>逐个推进敌人：闪白、重生读条、行为/招式、接触伤害与灼热检测。</summary>
        void TickEnemies(float dt)
        {
            var enemies = roster.All;
            inBurningAura = false;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EmberEnemy e = enemies[i];
                if (e.dead)
                    continue;
                e.age += dt;
                e.flash = Mathf.Max(0f, e.flash - dt);
                for (int k = 0; k < e.parts.Length; k++)
                    e.parts[k].color = e.reviveTimer > 0 ? new Color(.7f, .8f, .9f, .25f) : e.flash > 0 ? Color.Lerp(e.colors[k], Color.white, .7f) : e.colors[k];
                if (e.reviveTimer > 0)
                {
                    e.reviveTimer = Mathf.Max(0f, e.reviveTimer - dt);
                    if (e.reviveTimer == 0f)
                    {
                        e.hp = e.maxHp * config.RebirthHpFraction;
                        e.bar.Set(config.RebirthHpFraction);
                    }

                    if (e.visual != null)
                        e.visual.Tick(host.Elapsed, config);
                    continue;
                }

                Vector2 before = e.view.position;
                if (e.ai != null)
                    e.ai.Tick(dt, player.position, e.hp / e.maxHp);
                else
                {
                    SetEnemyContext(e);
                    e.behavior.Tick(dt, enemyCtx);
                }

                if (host.Phase != EmberRunPhase.Playing)
                    return;
                e.moveDirection = ((Vector2)e.view.position - before).normalized;
                if (e.visual != null)
                    e.visual.Tick(host.Elapsed, config);
                if (!e.Targetable)
                    continue;
                float distance = Vector2.Distance(player.position, e.view.position);
                if (e.affix == EnemyAffix.Burning && distance <= config.BurningRadius)
                    inBurningAura = true;
                if (distance < e.radius + .32f && host.HurtReady)
                {
                    host.HurtPlayer(e.IsBoss ? 24 : 9);
                    if (host.Phase != EmberRunPhase.Playing)
                        return;
                }
            }
        }

        void TickWeapons(float dt, RunProgress progress)
        {
            Vector2 nowPos = player != null ? (Vector2)player.position : Vector2.zero;
            if (hasLastPlayerPos && dt > 1e-5f)
                weaponCtx.PlayerSpeed = Vector2.Distance(nowPos, lastPlayerPos) / dt;
            else
                weaponCtx.PlayerSpeed = 0f;
            lastPlayerPos = nowPos;
            hasLastPlayerPos = true;
            weaponCtx.Progress = progress;
            weaponCtx.Elapsed = host.Elapsed;
            weaponCtx.Player = player;
            weaponCtx.World = world;
            weaponCtx.Cam = cam;
            weaponCtx.Effects = effects;
            weaponCtx.Pool = pool;
            for (int w = 0; w < weapons.Count; w++)
            {
                if (host.Phase != EmberRunPhase.Playing)
                    return;
                weapons[w].Tick(dt, weaponCtx);
                // 共享燃地仍在原 Trail 时隙结算，火球/环火本帧生成的燃地立即生效。
                if (weapons[w] is EmberTrailWeapon)
                    burnGround.Tick(dt, weaponCtx);
            }
        }

        /// <summary>结算本帧死亡的敌人，并把结果分派给波次与分裂生成。</summary>
        void ResolveDeaths()
        {
            var outcome = EmberCombatDamage.ResolveDeaths(damageCtx);
            for (int i = 0; i < outcome.Kills; i++)
            {
                waves.NoteKill();
                host.RegisterKill();
            }
            SpawnPendingBroods();
            if (outcome.BossDefeated)
            {
                bossEnemy = null;
                waves.NoteBossDefeated();
                return;
            }

            waves.CheckWaveCleared();
        }

        /// <summary>烬胎母体死亡后在其落点周围补生成子体（计入本波额外配额）。</summary>
        void SpawnPendingBroods()
        {
            if (pendingBroods.Count == 0)
                return;
            int perPosition = config.SplitCount;
            for (int i = 0; i < pendingBroods.Count; i++)
            {
                for (int n = 0; n < perPosition; n++)
                {
                    float a = n * Mathf.PI * 2 / perPosition;
                    var position = EmberWorld.Clamp(pendingBroods[i] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .45f, .3f);
                    roster.Spawn(false, Wave, host, FireBossVolley, 6, position, false, allowInView: true);
                    waves.NoteExtraSpawn(1);
                }
            }

            pendingBroods.Clear();
        }

        void SetEnemyContext(EmberEnemy e)
        {
            enemyCtx.Enemy = e;
            enemyCtx.Config = config;
            enemyCtx.SpeedMultiplier = e.affix == EnemyAffix.Wind && e.age % config.WindPeriod < config.WindDuration ? config.WindSpeedMultiplier : 1f;
        }

        void Explode(Vector2 position, float radius, float damage)
        {
            if (effects != null)
                effects.Nova(position, radius);
            if (player != null && Vector2.Distance(player.position, position) <= radius)
                host.HurtPlayer(damage);
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

        /// <summary>Boss 招式落点的爆发特效。</summary>
        public void PlayBossBurst(Vector2 point)
        {
            if (effects != null)
                effects.Nova(point, 1.2f);
        }

        /// <summary>遭遇事件的环形落点：以玩家为心，保证不与玩家重叠。</summary>
        public static Vector2 SafeEncounterPosition(Vector2 player, Vector2 direction)
        {
            return EmberWaveDirector.SafeEncounterPosition(player, direction);
        }
    }
}
