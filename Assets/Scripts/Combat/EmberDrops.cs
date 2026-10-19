using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>管理治疗和 Buff 掉落的生成、吸附与回收；效果经宿主施加，不直接修改玩家属性。</summary>
    public sealed class EmberDrops
    {
        sealed class Drop
        {
            public SpriteRenderer View;
            public float Life;
            public BuffConfig Buff;
            public string PoolKey => Buff == null ? "healing-drop" : "buff-drop";
        }

        const int Capacity = 24;
        readonly List<Drop> drops = new List<Drop>(Capacity);
        readonly EmberPool pool;
        readonly IEmberCombatHost host;
        Transform world;
        LevelConfig config;

        public EmberDrops(EmberPool pool, IEmberCombatHost host) { this.pool = pool; this.host = host; }
        public void Bind(Transform root, LevelConfig level) { Clear(); world = root; config = level; }

        public static bool ShouldDrop(int kind, EnemyAffix affix, double roll, LevelConfig config)
        {
            return kind != 6 && roll < (kind == 3 ? 1f : affix != EnemyAffix.None ? config.EliteHealDropChance : config.HealDropChance);
        }

        public void OnDeath(Vector2 position, int kind, EnemyAffix affix, System.Random random)
        {
            if (kind == 6) return;
            if (ShouldDrop(kind, affix, random.NextDouble(), config)) Spawn(position, null);
            float chance = kind == 3 ? 1f : affix != EnemyAffix.None ? config.EliteBuffDropChance : config.BuffDropChance;
            if (random.NextDouble() < chance)
                Spawn(EmberWorld.Clamp(position + Vector2.right * .35f),
                    random.NextDouble() < .5 ? EmberBuffCatalog.Power : EmberBuffCatalog.Haste);
        }

        void Spawn(Vector2 position, BuffConfig buff)
        {
            if (drops.Count >= Capacity) Remove(0);
            var drop = new Drop { Buff = buff, Life = config.HealDropLifetime };
            drop.View = pool.RentShape(drop.PoolKey, world, position, Vector2.one, Color.white, 12);
            var kind = buff == null ? EmberPickupArt.Kind.Healing : buff == EmberBuffCatalog.Power ? EmberPickupArt.Kind.Power : EmberPickupArt.Kind.Haste;
            drop.View.sprite = EmberPickupArt.Get(kind);
            drop.View.transform.localScale = Vector3.one * (.62f / drop.View.sprite.bounds.size.x);
            for (int child = 0; child < drop.View.transform.childCount; child++)
                drop.View.transform.GetChild(child).gameObject.SetActive(false);
            drops.Add(drop);
        }

        public void Tick(float dt, Vector2 player)
        {
            if (host.Phase != EmberRunPhase.Playing) return;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var drop = drops[i];
                drop.Life -= dt;
                if (drop.Life <= 0) { Remove(i); continue; }
                float pulse = 1f + .08f * Mathf.Sin(drop.Life * 5f);
                drop.View.transform.localScale = Vector3.one * (.62f * pulse / drop.View.sprite.bounds.size.x);
                var color = Color.white;
                color.a = drop.Life < 5f ? .55f + .45f * Mathf.Sin(drop.Life * 10f) : 1f;
                drop.View.color = color;
                float attract = host.NeedsHealing ? 2.2f : 1.5f;
                float speed = host.NeedsHealing ? 7f : 5f;
                if (Vector2.Distance(drop.View.transform.position, player) <= attract)
                    drop.View.transform.position = Vector2.MoveTowards(drop.View.transform.position, player, dt * speed);
                if (Vector2.Distance(drop.View.transform.position, player) > .5f) continue;
                bool picked = drop.Buff == null ? host.PickHealingDrop(config.HealDropAmount) : host.TryAddBuff(drop.Buff);
                if (picked) Remove(i);
            }
        }

        void Remove(int index)
        {
            var drop = drops[index];
            pool.Release(drop.PoolKey, drop.View);
            drops.RemoveAt(index);
        }

        public void Clear() { for (int i = drops.Count - 1; i >= 0; i--) Remove(i); }
    }
}
