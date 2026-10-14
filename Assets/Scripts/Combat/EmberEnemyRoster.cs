using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 敌人名册：拥有敌方实例列表，并负责刷怪与回收（M3）。
    ///
    /// 从 `EmberCombat` 搬出来的原因：刷怪一次要同时决定 8 件事——种类 roll、落点、颜色、
    /// 尺寸、血量、速度、词缀、表现部件——这些散落在 130 行的 `SpawnEnemy` 里，与波次推进、
    /// 伤害结算混在同一个类。现在"怎么造一个敌人"和"什么时候该造"分开了：
    /// 名册只管前者，后者归 <see cref="EmberWaveDirector"/>。
    ///
    /// 行为口径保持不变（`requirements.md` §3.4 / §3.5）：血量与词缀概率全部查 `LevelConfig`，
    /// 不在这里写数值。
    /// </summary>
    public sealed class EmberEnemyRoster
    {
        readonly List<EmberEnemy> enemies = new List<EmberEnemy>();
        readonly EmberPool pool;
        readonly LevelConfig config;
        readonly Transform world;
        readonly Transform player;
        readonly System.Random random;
        readonly Camera camera;

        public EmberEnemyRoster(EmberPool pool, LevelConfig config, Transform world, Transform player, System.Random random, Camera camera)
        {
            this.pool = pool;
            this.config = config;
            this.world = world;
            this.player = player;
            this.random = random;
            this.camera = camera;
        }

        /// <summary>当前在场敌人数量（含正在重生读条的）。</summary>
        public int Count { get { return enemies.Count; } }

        /// <summary>按下标访问，供武器 context 逐一挑选目标。</summary>
        public EmberEnemy At(int index) { return enemies[index]; }

        /// <summary>直接遍历用（结算器、Tick 循环）。不要改这个列表的结构。</summary>
        public List<EmberEnemy> All { get { return enemies; } }

        // ── 刷怪 ──────────────────────────────────────────────────────────────────────────

        /// <summary>对象池的 key：Boss 与杂兵分池，避免尺寸/部件互相污染。</summary>
        static string PoolKeyFor(int kind) { return kind == 3 ? "boss" : "shade"; }

        /// <summary>
        /// 造一个敌人并登记入册。
        /// </summary>
        /// <param name="boss">true 时强制为 Boss（kind 3）。</param>
        /// <param name="wave">当前波次：决定血量、词缀概率与 Boss 档位。</param>
        /// <param name="host">Boss 需要的宿主契约（非 Boss 可不传）。</param>
        /// <param name="bossVolley">Boss 暗弹齐射回调。</param>
        /// <param name="forcedKind">≥ 0 时跳过种类 roll；仅在 <paramref name="boss"/> 为 false 时生效。</param>
        /// <param name="position">请求落点；杂兵仅在地图内且屏外时采用，否则重新选取。</param>
        /// <param name="forceAffix">true 时跳过词缀概率判定（精英挑战小队用）。</param>
        public EmberEnemy Spawn(bool boss, int wave, IEmberCombatHost host = null,
            System.Action<Vector2, int> bossVolley = null, int forcedKind = -1,
            Vector2? position = null, bool forceAffix = false, bool allowInView = false)
        {
            int kind = boss ? 3 : forcedKind >= 0 ? forcedKind : config.RollKind(wave, random);

            Vector2 p;
            if (boss) p = EmberWorld.Clamp((Vector2)player.position + new Vector2(0, 2), 1);
            else if (allowInView && position.HasValue) p = position.Value;
            else if (!EmberSpawnPlacement.TryFind(camera, config, random, position, out p)) return null;

            Color body = BodyColor(kind);
            var view = pool.RentEnemy(boss ? "boss" : "shade", world, body);
            var silhouette = view.GetComponent<EmberEnemySilhouette>();
            if (silhouette == null) silhouette = view.gameObject.AddComponent<EmberEnemySilhouette>();
            silhouette.Apply(kind, body);
            view.position = p;

            float size = BodySize(boss, kind);
            view.localScale = Vector3.one * size;

            var added = new EmberEnemy
            {
                view = view,
                hp = boss ? config.BossHpForWave(wave) : config.TrashHp(wave, kind),
                speed = boss ? .8f : config.EnemySpeed(kind),
                radius = .32f * size,
                kind = kind,
                parts = view.GetComponentsInChildren<SpriteRenderer>()
            };
            added.maxHp = added.hp;
            enemies.Add(added);

            if (!boss)
            {
                added.behavior = CreateBehavior(kind);
                Vector2 delta = (Vector2)player.position - p;
                added.facing = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                // 引线虫（kind 6）不带词缀：它自己会炸。
                if (kind != 6 && (forceAffix || random.NextDouble() < config.AffixChance(wave)))
                    added.affix = (EnemyAffix)(1 + random.Next(3));
                added.visual = new EmberEnemyVisual(added, world);
            }

            added.bar = new EmberEnemyBar(view, boss);
            if (boss) added.ai = new EmberBoss(host, view, world, config.BossTier(wave), bossVolley);

            added.colors = new Color[added.parts.Length];
            for (int k = 0; k < added.parts.Length; k++) added.colors[k] = added.parts[k].color;
            return added;
        }

        // ── 回收与清理 ────────────────────────────────────────────────────────────────────

        /// <summary>把表现节点收回对象池，并清掉附着的血条与表现组件。</summary>
        public void Release(EmberEnemy e)
        {
            if (e == null || e.view == null) return;
            if (e.ai != null) { e.ai.Dispose(); e.ai = null; }
            if (e.visual != null) { e.visual.Clear(); e.visual = null; }
            // 摘掉血条，否则下次 Rent 出来会叠加第二条。
            var bar = e.view.Find("Health bar");
            if (bar != null) Object.DestroyImmediate(bar.gameObject);
            pool.Release(PoolKeyFor(e.kind), e.view.gameObject);
            e.view = null;
        }

        /// <summary>从名册移除（不回收表现——调用方可能已经处理）。</summary>
        public void RemoveAt(int index) { enemies.RemoveAt(index); }

        /// <summary>清空名册；<paramref name="destroyViews"/> 为 true 时连节点一起销毁。</summary>
        public void Clear(bool destroyViews)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e.view == null) continue;
                if (destroyViews)
                {
                    if (e.visual != null) e.visual.Clear();
                    Object.Destroy(e.view.gameObject);
                }
                else Release(e);
            }
            enemies.Clear();
        }

        // ── 外观与行为的静态查表 ──────────────────────────────────────────────────────────

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

        /// <summary>躯干配色，按种类区分（引线虫、铁灯卫、烬胎母体等各有色相）。</summary>
        static Color BodyColor(int kind)
        {
            switch (kind)
            {
                case 4: return new Color(.45f, .22f, .65f);
                case 5: return new Color(.4f, .5f, .22f);
                case 6: return new Color(.55f, .7f, .22f);
                case 7: return new Color(.25f, .4f, .49f);
                case 8: return new Color(.8f, .29f, .13f);
                case 1: return new Color(.65f, .29f, .46f);
                case 2: return new Color(.39f, .31f, .57f);
                default: return new Color(.29f, .30f, .39f);
            }
        }

        /// <summary>体型倍率：Boss 2.7，引线虫最小，其余按种类分档。</summary>
        static float BodySize(bool boss, int kind)
        {
            if (boss) return 2.7f;
            switch (kind)
            {
                case 6: return .40f;
                case 5: return 1.25f;
                case 7: return 1.2f;
                case 8: return .65f;
                case 2: return 1.3f;
                case 1: return .65f;
                default: return 1f;
            }
        }
    }
}
