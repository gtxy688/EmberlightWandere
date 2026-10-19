using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>共享地面持续伤害；不要求持有燃地武器。</summary>
    public sealed class EmberBurnGround
    {
        sealed class Flame
        {
            public SpriteRenderer view;
            public float life;
            public float powerScale;
        }

        readonly List<Flame> flames = new List<Flame>(EmberWeaponTuning.BurnGround.Capacity);
        readonly Stack<Flame> reusable = new Stack<Flame>(EmberWeaponTuning.BurnGround.Capacity);
        static readonly Unity.Profiling.ProfilerMarker TickMarker = new Unity.Profiling.ProfilerMarker("Ember.BurnGround.Tick");
        EmberPool pool;
        public int Count => flames.Count;

        // 场景节点与池由战斗宿主统一回收；此处只清本局状态。
        public void Reset()
        {
            for (int i = 0; i < flames.Count; i++) Recycle(flames[i]);
            flames.Clear();
        }

        void Recycle(Flame flame)
        {
            flame.view = null;
            reusable.Push(flame);
        }

        void Remove(int index)
        {
            var flame = flames[index];
            flames.RemoveAt(index);
            if (flame.view != null)
            {
                if (pool != null) pool.Release("burn", flame.view);
                else Object.Destroy(flame.view.gameObject);
            }
            Recycle(flame);
        }

        public void SpawnPatch(Vector2 pos, Transform world, float powerScale, EmberPool p)
        {
            pool = p;
            float size = powerScale >= 0.99f ? 1.2f : 0.85f;
            float life = powerScale >= 0.99f ? EmberWeaponTuning.BurnGround.Lifetime : EmberWeaponTuning.BurnGround.SmallLifetime;
            if (flames.Count >= EmberWeaponTuning.BurnGround.Capacity) Remove(0);

            var tint = new Color(.65f, .18f, .045f, .18f);
            var r = pool != null
                ? pool.RentShape("burn", world, pos, Vector2.one * size, tint, 1)
                : EmberVisuals.Shape("Burning ground", world, pos, Vector2.one * size, tint, 1);
            if (r.transform.childCount == 0)
            {
                var edge = EmberVisuals.Shape("Smoldering edge", r.transform, Vector2.zero, Vector2.one, new Color(1, .35f, .06f, .3f), 2);
                edge.sprite = EmberArt.Ring;
                for (int k = 0; k < 3; k++)
                    EmberArt.Fire(r.transform, new Vector2((k - 1) * .28f, .05f * Mathf.Sin(k)), .20f, 2);
            }

            var flame = reusable.Count > 0 ? reusable.Pop() : new Flame();
            flame.view = r;
            flame.life = life;
            flame.powerScale = powerScale;
            flames.Add(flame);
        }

        public void Tick(float dt, EmberWeaponContext ctx)
        {
            if (ctx.Progress == null || flames.Count == 0)
                return;
            using var sample = TickMarker.Auto();
            bool extend = ctx.Progress.TrailExtend;
            float trailPow = ctx.Progress.TrailPower > 0 ? ctx.Progress.TrailPower : EmberWeaponTuning.BurnGround.DefaultPower;
            if (extend) trailPow *= EmberWeaponTuning.BurnGround.ExtendedPower;
            float damageMul = ctx.Progress.DamageMul;
            float weaponPower = 1f + ctx.Progress.WeaponMagnitude(RunProgress.WeaponTrail);
            // 敌人直到武器遍历结束才增删；位置仍逐次读取，保留同帧击退的影响。
            int enemyCount = ctx.EnemyCount();
            for (int i = flames.Count - 1; i >= 0; i--)
            {
                var f = flames[i];
                f.life -= dt;
                var shade = f.view.color;
                shade.a = .18f * Mathf.Clamp01(f.life);
                f.view.color = shade;
                float fade = Mathf.Clamp01(f.life);
                for (int k = 1; k < f.view.transform.childCount; k++)
                {
                    var child = f.view.transform.GetChild(k);
                    float pulse = 1 + .15f * Mathf.Sin(ctx.Elapsed * 12 + k);
                    child.localScale = new Vector3(.20f, .30f * pulse, 1) * fade;
                }

                float hitR = f.powerScale >= 0.99f ? EmberWeaponTuning.BurnGround.Radius : EmberWeaponTuning.BurnGround.SmallRadius;
                Vector2 position = f.view.transform.position;
                float damage = EmberWeaponTuning.BurnGround.Dps * trailPow * f.powerScale * damageMul * weaponPower * dt;
                for (int j = enemyCount - 1; j >= 0; j--)
                {
                    var e = ctx.GetEnemy(j);
                    if (e == null || !e.Targetable)
                        continue;
                    Vector2 delta = position - (Vector2)e.view.position;
                    if (Mathf.Abs(delta.x) >= hitR || Mathf.Abs(delta.y) >= hitR) continue;
                    if (delta.magnitude < hitR && ctx.DealContactDamage(j, damage, position))
                        ctx.PlaySound?.Invoke(EmberWeaponSound.BurnGround);
                }

                if (f.life <= 0)
                    Remove(i);
            }
        }
    }
}
