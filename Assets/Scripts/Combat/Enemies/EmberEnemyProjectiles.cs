using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Combat-owned dark bolts: bounded, pooled, swept collision, paused with combat.</summary>
    public sealed class EmberEnemyProjectiles
    {
        sealed class Bolt
        {
            public SpriteRenderer view;
            public Vector2 velocity, previous;
            public float life, damage;
        }
        readonly List<Bolt> bolts = new List<Bolt>(48);
        readonly Stack<Bolt> spare = new Stack<Bolt>(48);
        readonly EmberPool pool;
        Transform world;
        Camera camera;
        public int Count { get { return bolts.Count; } }

        public EmberEnemyProjectiles(EmberPool pool) { this.pool = pool; }
        public void Bind(Transform root, Camera cam) { Clear(); world = root; camera = cam; }

        public void Fire(Vector2 origin, Vector2 direction, float speed, float damage)
        {
            if (bolts.Count >= 48 || !EmberWorld.Visible(camera, origin)) return;
            var b = spare.Count > 0 ? spare.Pop() : new Bolt();
            b.view = pool.RentShape("dark-bolt", world, origin, new Vector2(.24f, .40f), new Color(.85f, .40f, 1f), 10);
            b.view.sprite = EmberArt.Flame;
            b.view.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            b.previous = origin;
            b.velocity = direction.normalized * speed;
            b.damage = damage;
            b.life = 4f;
            bolts.Add(b);
        }

        public void Move(float dt)
        {
            for (int i = bolts.Count - 1; i >= 0; i--)
            {
                var b = bolts[i];
                b.previous = b.view.transform.position;
                b.view.transform.position += (Vector3)(b.velocity * dt);
                b.life -= dt;
                if (b.life <= 0) Remove(i);
            }
        }

        public void HitPlayer(Vector2 player, System.Action<float> hurt)
        {
            for (int i = bolts.Count - 1; i >= 0; i--)
            {
                var b = bolts[i];
                if (DistanceToSegment(player, b.previous, b.view.transform.position) <= .43f)
                {
                    float damage = b.damage;
                    Remove(i);
                    hurt(damage);
                }
                else if (!EmberWorld.Visible(camera, b.view.transform.position)) Remove(i);
            }
        }

        /// <summary>Relative swept motion prevents fast crossing projectiles tunneling through each other.</summary>
        public bool Intercept(Vector2 from, Vector2 to, float radius)
        {
            bool hit = false;
            for (int i = bolts.Count - 1; i >= 0; i--)
            {
                var b = bolts[i];
                if (DistanceToSegment(Vector2.zero, from - b.previous, to - (Vector2)b.view.transform.position) > radius + .15f) continue;
                Remove(i);
                hit = true;
            }
            return hit;
        }

        public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float t = segment.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude) : 0;
            return Vector2.Distance(point, a + segment * t);
        }

        void Remove(int index)
        {
            var b = bolts[index];
            if (b.view != null) pool.Release("dark-bolt", b.view);
            b.view = null;
            bolts.RemoveAt(index);
            spare.Push(b);
        }

        public void Clear() { for (int i = bolts.Count - 1; i >= 0; i--) Remove(i); }
    }
}
