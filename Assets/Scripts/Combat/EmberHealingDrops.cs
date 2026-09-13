using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    public sealed class EmberHealingDrops
    {
        sealed class Drop { public SpriteRenderer view; public float life; }
        readonly List<Drop> drops = new List<Drop>(24);
        readonly EmberPool pool;
        readonly EmberGame game;
        Transform world;
        LevelConfig config;
        public EmberHealingDrops(EmberPool pool, EmberGame game) { this.pool = pool; this.game = game; }
        public void Bind(Transform root, LevelConfig level) { Clear(); world = root; config = level; }
        public static bool ShouldDrop(int kind, EnemyAffix affix, double roll, LevelConfig config)
        { return kind != 6 && roll < (kind == 3 ? 1f : affix != EnemyAffix.None ? config.EliteHealDropChance : config.HealDropChance); }
        public void OnDeath(Vector2 position, int kind, EnemyAffix affix, System.Random random)
        {
            if (!ShouldDrop(kind, affix, random.NextDouble(), config)) return;
            if (drops.Count >= 24) Remove(0);
            var v = pool.RentShape("healing-drop", world, position, Vector2.one * .48f, new Color(.16f, .8f, .46f), 12);
            v.sprite = EmberArt.Panel;
            if (v.transform.childCount == 0)
            {
                var h = EmberVisuals.Shape("Healing cross horizontal", v.transform, Vector2.zero, new Vector2(.65f, .20f), Color.white, 13);
                var t = EmberVisuals.Shape("Healing cross vertical", v.transform, Vector2.zero, new Vector2(.20f, .65f), Color.white, 13);
                h.sprite = t.sprite = EmberArt.Panel;
            }
            drops.Add(new Drop { view = v, life = config.HealDropLifetime });
        }
        public void Tick(float dt, Vector2 player)
        {
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.life -= dt;
                if (d.life <= 0) { Remove(i); continue; }
                float pulse = 1f + .08f * Mathf.Sin(d.life * 5f);
                d.view.transform.localScale = Vector3.one * (.48f * pulse);
                d.view.color = new Color(.16f, .8f, .46f, d.life < 5f ? .55f + .45f * Mathf.Sin(d.life * 10f) : 1f);
                // Always attract/pick while playing (残血/满血都能吃; 满血多余转护盾)
                if (game.State != EmberGame.Mode.Playing) continue;
                float attract = game.NeedsHealing ? 2.2f : 1.5f; // 残血吸得更远
                float speed = game.NeedsHealing ? 7f : 5f;
                if (Vector2.Distance(d.view.transform.position, player) <= attract)
                    d.view.transform.position = Vector2.MoveTowards(d.view.transform.position, player, dt * speed);
                if (Vector2.Distance(d.view.transform.position, player) <= .5f && game.PickHealingDrop(config.HealDropAmount)) Remove(i);
            }
        }
        void Remove(int index) { pool.Release("healing-drop", drops[index].view); drops.RemoveAt(index); }
        public void Clear() { for (int i = drops.Count - 1; i >= 0; i--) Remove(i); }
    }
}
