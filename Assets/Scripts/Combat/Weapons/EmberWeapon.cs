using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Per-tick combat services for weapons. Weapons must not reverse-depend on EmberGame.</summary>
    public sealed class EmberWeaponContext
    {
        public Transform Player;
        public Transform World;
        public Camera Cam;
        public EmberEffects Effects;
        public EmberPool Pool;
        public RunProgress Progress;
        public float Elapsed;
        public System.Random Random;

        public Func<int> EnemyCount;
        public Func<int, EmberCombat.Enemy> GetEnemy;
        public Action<int, float> Damage;
        public Action<int, float, Vector2> DamageFrom;
        public Func<Vector2, Vector2, float, bool> Intercept;
        public void DealDamage(int index, float amount, Vector2 source)
        { if (DamageFrom != null) DamageFrom(index, amount, source); else Damage(index, amount); }
        public Action PlayingGate; // no-op helper reserved
        public Func<bool> StillPlaying;

        /// <summary>Shared burn patches (trail weapon + evo hit trails).</summary>
        public Action<Vector2, float> SpawnBurnPatch;

        public Vector2 PlayerPos { get { return Player != null ? (Vector2)Player.position : Vector2.zero; } }
        /// <summary>World units/sec this frame (set by combat).</summary>
        public float PlayerSpeed;
        /// <summary>True when nearly still (Hotfix pierce sniper gate).</summary>
        public bool PlayerStanding { get { return PlayerSpeed < 0.15f; } }

        public bool Visible(Vector2 worldPos)
        {
            return Cam != null && EmberWorld.Visible(Cam, worldPos);
        }

        public int FindNearestVisibleEnemy()
        {
            int best = -1;
            float bestD = 1e10f;
            int n = EnemyCount();
            Vector2 p = PlayerPos;
            for (int i = 0; i < n; i++)
            {
                var e = GetEnemy(i);
                if (e == null || !e.Targetable) continue;
                if (!Visible(e.view.position)) continue;
                float d = ((Vector2)e.view.position - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        readonly List<int> visibleScratch = new List<int>(64);

        public int FindRandomVisibleEnemy()
        {
            visibleScratch.Clear();
            int n = EnemyCount();
            for (int i = 0; i < n; i++)
            {
                var e = GetEnemy(i);
                if (e == null || !e.Targetable) continue;
                if (Visible(e.view.position)) visibleScratch.Add(i);
            }
            if (visibleScratch.Count == 0) return -1;
            return visibleScratch[Random.Next(visibleScratch.Count)];
        }
    }

    public abstract class EmberWeapon
    {
        public abstract void Tick(float dt, EmberWeaponContext ctx);
        public virtual void Reset() { }
    }
}
