using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Combat sim: enemies, shots, drops, orbits, trail. Wave clear → upgrade card → next wave / Boss.</summary>
    public sealed class EmberCombat
    {
        public sealed class Enemy
        {
            public Transform view;
            public float hp, maxHp, speed, radius, flash;
            public EmberEnemyBar bar;
            public EmberBoss ai;
            public int kind;
            public SpriteRenderer[] parts;
            public Color[] colors;
        }

        sealed class Shot
        {
            public SpriteRenderer view;
            public Vector2 velocity;
            public float life, damage, trail;
        }

        sealed class Drop
        {
            public SpriteRenderer view;
            public int value;
        }

        sealed class Flame
        {
            public SpriteRenderer view;
            public float life;
        }

        readonly EmberGame game;
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Drop> drops = new List<Drop>();
        readonly List<Flame> flames = new List<Flame>();
        readonly List<SpriteRenderer> orbs = new List<SpriteRenderer>();
        readonly System.Random random = new System.Random();

        Transform world, player;
        Camera cam;
        EmberEffects effects;
        Enemy bossEnemy;
        LevelConfig config;
        float spawnTimer, fireTimer, trailTimer, pulseTimer;
        bool bossSpawned, awaitingUpgrade;
        int wave = 1;
        int waveQuota;
        int waveSpawned;
        int waveKilled;

        public int EnemyCount { get { return enemies.Count; } }
        public Enemy Boss { get { return bossEnemy; } }
        public bool BossAlive { get { return bossEnemy != null; } }
        public int Wave { get { return wave; } }
        public int WaveQuota { get { return waveQuota; } }
        public int WaveSpawned { get { return waveSpawned; } }
        public int WaveKilled { get { return waveKilled; } }
        /// <summary>Remaining trash for HUD: unspawned quota + on-field count.</summary>
        public int WaveRemaining
        {
            get
            {
                if (bossSpawned || (config != null && wave >= config.BossWave)) return 0;
                return Mathf.Max(0, (waveQuota - waveSpawned) + enemies.Count);
            }
        }
        public int WaveKillsRemaining { get { return Mathf.Max(0, waveQuota - waveKilled); } }

        public EmberCombat(EmberGame owner) { game = owner; }

        public void Begin(Transform runWorld, Transform keeper, Camera camera, EmberEffects fx)
        {
            Begin(runWorld, keeper, camera, fx, LevelConfig.Default);
        }

        public void Begin(Transform runWorld, Transform keeper, Camera camera, EmberEffects fx, LevelConfig levelConfig)
        {
            ClearLists(destroyViews: false);
            world = runWorld;
            player = keeper;
            cam = camera;
            effects = fx;
            config = levelConfig ?? LevelConfig.Default;
            config.ApplyWorld();
            spawnTimer = .3f;
            fireTimer = trailTimer = pulseTimer = 0;
            bossSpawned = false;
            bossEnemy = null;
            awaitingUpgrade = false;
            StartWave(1);
        }

        public void TearDown()
        {
            ClearLists(destroyViews: false);
            world = player = null;
            effects = null;
            cam = null;
            bossEnemy = null;
            bossSpawned = false;
            awaitingUpgrade = false;
            config = null;
        }

        void ClearLists(bool destroyViews)
        {
            if (destroyViews)
            {
                for (int i = 0; i < enemies.Count; i++) if (enemies[i].view != null) Object.Destroy(enemies[i].view.gameObject);
                for (int i = 0; i < shots.Count; i++) if (shots[i].view != null) Object.Destroy(shots[i].view.gameObject);
                for (int i = 0; i < drops.Count; i++) if (drops[i].view != null) Object.Destroy(drops[i].view.gameObject);
                for (int i = 0; i < flames.Count; i++) if (flames[i].view != null) Object.Destroy(flames[i].view.gameObject);
                for (int i = 0; i < orbs.Count; i++) if (orbs[i] != null) Object.Destroy(orbs[i].gameObject);
            }
            enemies.Clear();
            shots.Clear();
            drops.Clear();
            flames.Clear();
            orbs.Clear();
        }

        void StartWave(int next)
        {
            wave = next;
            awaitingUpgrade = false;
            waveSpawned = 0;
            waveKilled = 0;
            if (config == null) config = LevelConfig.Default;
            if (wave >= config.BossWave)
            {
                waveQuota = 0;
                if (!bossSpawned)
                {
                    bossSpawned = true;
                    SpawnEnemy(true);
                }
                return;
            }
            waveQuota = config.QuotaForWave(wave);
            spawnTimer = .25f;
        }

        /// <summary>After upgrade Choose: start clearedWave+1 (Boss at Wave6).</summary>
        public void AdvanceAfterUpgrade()
        {
            StartWave(wave + 1);
        }

        public void PreviewBoss()
        {
            if (game.State != EmberGame.Mode.Playing || bossSpawned) return;
            // Force Wave6-equivalent boss spawn for debug.
            wave = config != null ? config.BossWave : 6;
            waveQuota = 0;
            waveSpawned = 0;
            waveKilled = 0;
            awaitingUpgrade = false;
            bossSpawned = true;
            SpawnEnemy(true);
        }

        public void Tick(float dt)
        {
            if (game.State != EmberGame.Mode.Playing) return;
            var progress = game.Progress;
            if (config == null) config = LevelConfig.Default;

            if (!awaitingUpgrade && !bossSpawned && wave < config.BossWave && waveSpawned < waveQuota && enemies.Count < 150)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0)
                {
                    SpawnEnemy(false);
                    waveSpawned++;
                    spawnTimer = config.SpawnInterval(wave);
                }
            }

            fireTimer -= dt;
            if (fireTimer <= 0 && enemies.Count > 0)
            {
                Fire();
                fireTimer = .8f / (1 + progress.AttackSpeedBonus);
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                Vector2 delta = (Vector2)player.position - (Vector2)e.view.position;
                e.flash = Mathf.Max(0, e.flash - dt);
                for (int k = 0; k < e.parts.Length; k++)
                    e.parts[k].color = e.flash > 0 ? Color.Lerp(e.colors[k], Color.white, .7f) : e.colors[k];
                if (e.ai != null) e.ai.Tick(dt, player.position, e.hp / e.maxHp);
                else e.view.position = EmberWorld.Clamp((Vector2)e.view.position + delta.normalized * e.speed * dt, .3f);
                if (game.State != EmberGame.Mode.Playing) return;
                if (delta.magnitude < e.radius + .32f && game.HurtReady)
                {
                    game.HurtPlayer(e.kind == 3 ? 24 : 9);
                    if (game.State != EmberGame.Mode.Playing) return;
                }
            }

            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var s = shots[i];
                s.life -= dt;
                s.view.transform.position += (Vector3)(s.velocity * dt);
                if (!EmberWorld.Visible(cam, s.view.transform.position))
                {
                    Object.Destroy(s.view.gameObject);
                    shots.RemoveAt(i);
                    continue;
                }
                s.trail -= dt;
                if (s.trail <= 0) { effects.Trail(s.view.transform.position); s.trail = .045f; }
                for (int j = enemies.Count - 1; j >= 0; j--)
                    if (Vector2.Distance(s.view.transform.position, enemies[j].view.position) < enemies[j].radius + .18f)
                    {
                        Damage(j, s.damage);
                        s.life = 0;
                        break;
                    }
                if (s.life <= 0) { Object.Destroy(s.view.gameObject); shots.RemoveAt(i); }
            }
            if (game.State != EmberGame.Mode.Playing) return;

            UpdateOrbits(dt);
            if (game.State != EmberGame.Mode.Playing) return;
            UpdateTrail(dt);
            if (game.State != EmberGame.Mode.Playing) return;

            pulseTimer -= dt;
            if (progress.WavePower > 0 && pulseTimer <= 0)
            {
                pulseTimer = 4;
                float radius = 3 + progress.WavePower * 1.5f;
                Burst(player.position, radius);
                for (int j = enemies.Count - 1; j >= 0; j--)
                    if (Vector2.Distance(player.position, enemies[j].view.position) < radius)
                        Damage(j, 50 * progress.WavePower);
            }

            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                float distance = Vector2.Distance(d.view.transform.position, player.position);
                if (distance < 2 + progress.PickupBonus)
                    d.view.transform.position = Vector2.MoveTowards(d.view.transform.position, player.position, 8 * dt);
                if (distance < .45f)
                {
                    progress.AddScore(d.value);
                    Object.Destroy(d.view.gameObject);
                    drops.RemoveAt(i);
                }
            }
        }

        public void PlayBossBurst(Vector2 point) { if (effects != null) effects.Nova(point, 1.2f); }

        void SpawnEnemy(bool boss)
        {
            if (config == null) config = LevelConfig.Default;
            // Wave1: kind0 only; Wave2+: mix; Boss: kind3
            int kind = boss ? 3 : (wave <= 1 ? 0 : random.Next(3));
            float a = (float)random.NextDouble() * Mathf.PI * 2;
            Vector2 p = (Vector2)player.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 10;
            p = EmberWorld.Clamp(p, .8f);
            if (boss) p = EmberWorld.Clamp((Vector2)player.position + new Vector2(0, 2), 1);
            Color color = kind == 1 ? new Color(.65f, .29f, .46f) : kind == 2 ? new Color(.39f, .31f, .57f) : new Color(.29f, .30f, .39f);
            var view = EmberVisuals.Character(boss ? "Nightwarden" : "Shade", world, color, false);
            view.position = p;
            float size = boss ? 2.7f : kind == 2 ? 1.3f : kind == 1 ? .65f : 1;
            view.localScale = Vector3.one * size;
            float hp = boss ? config.BossHp : config.TrashHp(wave, kind);
            enemies.Add(new Enemy
            {
                view = view,
                hp = hp,
                speed = boss ? .8f : kind == 1 ? 1.9f : 1.0f,
                radius = .32f * size,
                kind = kind,
                parts = view.GetComponentsInChildren<SpriteRenderer>()
            });
            var added = enemies[enemies.Count - 1];
            added.maxHp = added.hp;
            added.bar = new EmberEnemyBar(view, boss);
            if (boss) { added.ai = new EmberBoss(game, view, world); bossEnemy = added; }
            added.colors = new Color[added.parts.Length];
            for (int k = 0; k < added.parts.Length; k++) added.colors[k] = added.parts[k].color;
        }

        void Fire()
        {
            Enemy nearest = null;
            float best = 10000;
            foreach (var e in enemies)
            {
                if (!EmberWorld.Visible(cam, e.view.position)) continue;
                float d = (e.view.position - player.position).sqrMagnitude;
                if (d < best) { best = d; nearest = e; }
            }
            if (nearest == null) return;
            Vector2 dir = ((Vector2)nearest.view.position - (Vector2)player.position).normalized;
            int count = 1 + game.Progress.ExtraShots;
            for (int i = 0; i < count; i++)
            {
                Vector2 v = Quaternion.Euler(0, 0, (i - (count - 1) * .5f) * 9) * dir;
                var r = EmberArt.Fire(world, player.position, .32f);
                r.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90);
                shots.Add(new Shot { view = r, velocity = v * 9, life = 1.8f, damage = 22 * (1 + game.Progress.DamageBonus) });
            }
        }

        void Damage(int index, float amount)
        {
            var e = enemies[index];
            if (!EmberWorld.Visible(cam, e.view.position)) return;
            e.hp -= amount;
            e.bar.Set(e.hp / e.maxHp);
            if (e.flash <= 0) { effects.Impact(e.view.position, false); e.flash = .10f; }
            if (amount >= 1) e.view.position += (e.view.position - player.position).normalized * (e.kind == 3 ? .015f : .06f);
            if (e.hp > 0) return;
            effects.Impact(e.view.position, true);
            Vector2 p = e.view.position;
            bool boss = e.kind == 3;
            Object.Destroy(e.view.gameObject);
            enemies.RemoveAt(index);
            game.RegisterKill();
            if (!boss && !awaitingUpgrade && wave < (config != null ? config.BossWave : 6))
            {
                waveKilled++;
                // Clear = quota fully spawned AND enemies on field == 0. No 1.5s rest.
                if (waveSpawned >= waveQuota && enemies.Count == 0)
                {
                    awaitingUpgrade = true;
                    game.OnWaveCleared(wave);
                }
            }
            if (drops.Count < 200)
            {
                var r = EmberVisuals.Shape("Ember", world, p, Vector2.one * .16f, new Color(1, .65f, .18f), 2);
                drops.Add(new Drop { view = r, value = boss ? 30 : 1 });
            }
            else drops[0].value++;
            if (boss) { e.ai.Clear(); bossEnemy = null; game.EndRun(true); }
        }

        void UpdateOrbits(float dt)
        {
            int count = game.Progress.Orbits;
            while (orbs.Count < count) orbs.Add(EmberArt.Fire(world, player.position, .33f));
            for (int i = 0; i < orbs.Count; i++)
            {
                float a = game.Elapsed * 2.5f + i * Mathf.PI * 2 / orbs.Count;
                Vector2 p = (Vector2)player.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.6f;
                orbs[i].transform.position = p;
                orbs[i].transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                if ((int)(game.Elapsed * 24) != (int)((game.Elapsed - dt) * 24)) effects.Trail(p);
                for (int j = enemies.Count - 1; j >= 0; j--)
                    if (Vector2.Distance(p, enemies[j].view.position) < enemies[j].radius + .35f)
                        Damage(j, 65 * dt);
            }
        }

        void UpdateTrail(float dt)
        {
            trailTimer -= dt;
            if (game.Progress.TrailPower > 0 && trailTimer <= 0)
            {
                trailTimer = .45f;
                var r = EmberVisuals.Shape("Burning ground", world, player.position, Vector2.one * 1.2f, new Color(.65f, .18f, .045f, .18f), 1);
                var edge = EmberVisuals.Shape("Smoldering edge", r.transform, Vector2.zero, Vector2.one, new Color(1, .35f, .06f, .3f), 2);
                edge.sprite = EmberArt.Ring;
                for (int k = 0; k < 3; k++) EmberArt.Fire(r.transform, new Vector2((k - 1) * .28f, .05f * Mathf.Sin(k)), .20f, 2);
                flames.Add(new Flame { view = r, life = 2.5f });
            }
            for (int i = flames.Count - 1; i >= 0; i--)
            {
                var f = flames[i];
                f.life -= dt;
                var shade = f.view.color;
                shade.a = .18f * Mathf.Clamp01(f.life);
                f.view.color = shade;
                for (int k = 1; k < f.view.transform.childCount; k++)
                {
                    var child = f.view.transform.GetChild(k);
                    float pulse = 1 + .15f * Mathf.Sin(game.Elapsed * 12 + k);
                    child.localScale = new Vector3(.20f, .30f * pulse, 1) * Mathf.Clamp01(f.life);
                }
                for (int j = enemies.Count - 1; j >= 0; j--)
                    if (Vector2.Distance(f.view.transform.position, enemies[j].view.position) < .7f)
                        Damage(j, 40 * game.Progress.TrailPower * dt);
                if (f.life <= 0) { Object.Destroy(f.view.gameObject); flames.RemoveAt(i); }
            }
        }

        void Burst(Vector2 p, float radius) { effects.Nova(p, radius); }
    }
}
