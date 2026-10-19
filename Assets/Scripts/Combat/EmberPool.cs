using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 战斗对象的复用池：按 key 复用 GameObject，避免每帧 new/Destroy 产生抖动。
    /// key 是短而稳定的 id（fire / burn / warn / shade / boss）。
    /// </summary>
    public sealed class EmberPool
    {
        readonly Dictionary<string, Stack<GameObject>> stacks = new Dictionary<string, Stack<GameObject>>();
        readonly List<SpriteRenderer> enemyParts = new List<SpriteRenderer>(16);
        Transform holder;
        GameObject shadePrefab;
        bool bossUsesTemplate;
        int maxPerKey = 64;

        public void Bind(Transform runWorld, int maxPerKey = 64, GameObject shadePrefab = null)
        {
            Clear(destroy: true);
            this.shadePrefab = shadePrefab;
            var silhouette = shadePrefab != null ? shadePrefab.GetComponent<EmberEnemySilhouette>() : null;
            bossUsesTemplate = silhouette != null && silhouette.HasSkinFor(3);
            this.maxPerKey = maxPerKey < 8 ? 8 : maxPerKey;
            if (runWorld == null) return;
            var go = new GameObject("EmberPool");
            holder = go.transform;
            holder.SetParent(runWorld, false);
            holder.localPosition = Vector3.zero;
        }

        public void Clear(bool destroy)
        {
            foreach (var kv in stacks)
            {
                while (kv.Value.Count > 0)
                {
                    var g = kv.Value.Pop();
                    if (g != null && destroy) UnityEngine.Object.Destroy(g);
                }
            }
            stacks.Clear();
            if (holder != null)
            {
                if (destroy) UnityEngine.Object.Destroy(holder.gameObject);
                holder = null;
            }
        }

        static readonly Unity.Profiling.ProfilerMarker CreateMarker = new Unity.Profiling.ProfilerMarker("Ember.Pool.Create");
        static readonly Unity.Profiling.ProfilerMarker ReuseMarker = new Unity.Profiling.ProfilerMarker("Ember.Pool.Reuse");

        public GameObject Rent(string key, Transform parent, Func<Transform, GameObject> create)
        {
            var cached = Take(key, parent);
            if (cached != null) return cached;
            using (CreateMarker.Auto())
                return create(parent);
        }

        GameObject Take(string key, Transform parent)
        {
            Stack<GameObject> stack;
            if (stacks.TryGetValue(key, out stack))
            {
                while (stack.Count > 0)
                {
                    var g = stack.Pop();
                    if (g == null) continue;
                    using (ReuseMarker.Auto())
                    {
                        g.transform.SetParent(parent, false);
                        g.SetActive(true);
                        return g;
                    }
                }
            }
            return null;
        }

        public SpriteRenderer RentFire(Transform parent, Vector2 position, float size, int order = 8)
        {
            var go = Take("fire", parent);
            if (go == null)
            {
                using (CreateMarker.Auto())
                    go = EmberArt.Fire(parent, position, size, order).gameObject;
            }
            var r = go.GetComponent<SpriteRenderer>();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size, size * 1.5f, 1f);
            go.transform.rotation = Quaternion.identity;
            if (r != null)
            {
                r.sortingOrder = order;
                r.color = new Color(1f, .30f, .055f);
                r.sprite = EmberArt.Flame;
                for (int i = 0; i < go.transform.childCount; i++) go.transform.GetChild(i).gameObject.SetActive(true);
            }
            return r;
        }

        public SpriteRenderer RentShape(string key, Transform parent, Vector2 position, Vector2 scale, Color color, int order)
        {
            var go = Take(key, parent);
            if (go == null)
            {
                using (CreateMarker.Auto())
                    go = EmberVisuals.Shape(key, parent, position, scale, color, order).gameObject;
            }
            var r = go.GetComponent<SpriteRenderer>();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.identity;
            if (r != null)
            {
                r.color = color;
                r.sortingOrder = order;
                if (r.sprite == null) r.sprite = EmberVisuals.Disc;
            }
            return r;
        }

        static readonly Unity.Profiling.ProfilerMarker RentEnemyMarker = new Unity.Profiling.ProfilerMarker("Ember.Pool.RentEnemy");

        public Transform RentEnemy(string key, Transform parent, Color body)
        {
            using (RentEnemyMarker.Auto())
            {
                var go = Take(key, parent);
                if (go == null)
                {
                    bool boss = key == "boss";
                    using (CreateMarker.Auto())
                        go = shadePrefab != null && (!boss || bossUsesTemplate)
                            ? UnityEngine.Object.Instantiate(shadePrefab, parent, false)
                            : EmberVisuals.Character(boss ? "Nightwarden" : "Shade", parent, body).gameObject;
                }
                go.transform.SetParent(parent, false);
                go.transform.rotation = Quaternion.identity;
                // Recolor cloak/hood if present (children Shape nodes).
                go.GetComponentsInChildren(true, enemyParts);
                for (int i = 0; i < enemyParts.Count; i++)
                {
                    var part = enemyParts[i];
                    var n = part.name;
                    if (n == "Cloak") part.color = body;
                    else if (n == "Hood") part.color = body * 1.2f;
                }
                enemyParts.Clear();
                go.SetActive(true);
                return go.transform;
            }
        }

        public void Release(string key, GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (holder != null) go.transform.SetParent(holder, false);
            Stack<GameObject> stack;
            if (!stacks.TryGetValue(key, out stack))
            {
                stack = new Stack<GameObject>();
                stacks[key] = stack;
            }
            if (stack.Count >= maxPerKey)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            stack.Push(go);
        }

        public void Release(string key, Component c)
        {
            if (c != null) Release(key, c.gameObject);
        }
    }
}
