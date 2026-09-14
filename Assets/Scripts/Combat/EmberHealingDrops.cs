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
            v.sprite = EmberWorldArt.Get(9);
            v.color = Color.white;
            // Preserve the pickup size regardless of atlas pixel dimensions.
            v.transform.localScale = Vector3.one * (.62f / v.sprite.bounds.size.x);
            for (int child = 0; child < v.transform.childCount; child++)
                v.transform.GetChild(child).gameObject.SetActive(false);
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
                d.view.transform.localScale = Vector3.one * (.62f * pulse / d.view.sprite.bounds.size.x);
                d.view.color = new Color(1f, 1f, 1f, d.life < 5f ? .55f + .45f * Mathf.Sin(d.life * 10f) : 1f);
                // Always attract/pick while playing; overflow heal discarded (no shield)
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
