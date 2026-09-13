using System;
using UnityEditor;
using UnityEngine;

namespace Emberlight
{
    /// <summary>Isolated rule checks. Does not enter Play Mode or modify saved scenes.</summary>
    public static class EmberEnemyChecks
    {
        [MenuItem("Emberlight/Validate enemy rules")]
        public static void Validate()
        {
            var config = new LevelConfig();
            var random = new System.Random(413);
            for (int wave = 1; wave <= 5; wave++)
            {
                var weights = config.WeightTable(wave);
                int sum = 0;
                for (int i = 0; i < weights.Count; i++) sum += weights[i];
                Require(sum == 100, "Spawn weights sum to 100");
                for (int i = 0; i < 1000; i++)
                {
                    int kind = config.RollKind(wave, random);
                    Require(kind != 3 && kind != 6, "Boss and offspring excluded from regular rolls");
                    Require(wave > 1 || kind == 0, "Wave 1 only ordinary enemies");
                    Require(wave >= 3 || kind < 4, "No early special enemies");
                    Require(wave >= 4 || kind < 5, "Brood and shield gates");
                    Require(wave >= 5 || kind != 8, "Bomber gate");
                }
            }
            Require(config.AffixChance(2) == 0 && Mathf.Approximately(config.AffixChance(3), .12f)
                && Mathf.Approximately(config.AffixChance(5), .18f), "Affix wave gates");
            Require(Mathf.Approximately(ShieldBehavior.DamageMultiplier(0, Vector2.right, .3f), .3f), "Shield front reduction");
            Require(ShieldBehavior.DamageMultiplier(0, Vector2.left, .3f) == 1f, "Shield back full damage");
            Require(ShieldBehavior.DamageMultiplier(0, Vector2.up, .3f) == 1f, "Shield side full damage");
            Require(EmberEnemyProjectiles.DistanceToSegment(Vector2.zero, Vector2.left, Vector2.right) == 0, "Fast crossing intersection");
            Require(EmberEnemyProjectiles.DistanceToSegment(Vector2.up, Vector2.left, Vector2.right) == 1, "Non-intersecting segment");

            var root = new GameObject("Enemy rule checks") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var body = new GameObject("Enemy"); body.transform.SetParent(root.transform);
                var player = new GameObject("Target"); player.transform.SetParent(root.transform);
                var e = new EmberCombat.Enemy { view = body.transform, hp = 10, maxHp = 10, speed = 1, kind = 8 };
                var ctx = new EmberEnemyContext { Enemy = e, Player = player.transform, Config = config };
                int explosions = 0;
                ctx.Explode = (p, radius, damage) => { explosions++; Require(radius == 1.8f && damage == 20, "Bomb payload"); };
                var bomber = new BomberBehavior();
                bomber.Tick(.1f, ctx);
                Require(e.detonating && !e.Targetable && explosions == 0, "Bomb arms without instant explosion");
                bomber.Tick(.7f, ctx);
                Require(explosions == 0, "Fuse remains pending");
                bomber.Tick(.11f, ctx);
                Require(explosions == 1 && e.dead, "Bomb detonates after fuse");
                bomber.Tick(1f, ctx);
                Require(explosions == 1, "Bomb cannot explode twice");

                e.affix = EnemyAffix.Rebirth;
                Require(EmberCombat.TryBeginRebirth(e, config), "Rebirth triggers on explosion death");
                Require(!e.dead && !e.detonating && !e.Targetable && e.reviveTimer == 2f, "Dormant rebirth state");
                Require(!EmberCombat.TryBeginRebirth(e, config), "Rebirth only once");

                int children = 0;
                ctx.Split = p => children += config.SplitCount;
                new SplitBehavior().OnDeath(ctx);
                Require(children == 3, "Brood produces exactly three children");
                e.facing = 0; e.speed = .7f;
                player.transform.position = Vector2.left * 5;
                new ShieldBehavior().Tick(.1f, ctx);
                Require(Mathf.Abs(e.facing) <= config.ShieldTurnSpeed * .1f + .001f, "Guard turn rate bounded");

                var camObject = new GameObject("Test camera"); camObject.transform.SetParent(root.transform);
                var camera = camObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true; camera.orthographicSize = 8; camera.aspect = .5625f;
                camera.transform.position = new Vector3(0, 0, -10);
                ctx.Camera = camera;
                var pool = new EmberPool();
                var projectiles = new EmberEnemyProjectiles(pool);
                projectiles.Bind(root.transform, camera);
                ctx.Projectiles = projectiles;
                e.view.position = Vector2.zero; e.dead = false; e.speed = 0;
                player.transform.position = Vector2.right * 3;
                var shooter = new ShooterBehavior();
                try
                {
                    shooter.Tick(1f, ctx);
                    Require(projectiles.Count == 0 && e.charge > 0, "Shooter begins with visible warning");
                    shooter.Tick(.3f, ctx);
                    Require(projectiles.Count == 0, "Shooter does not fire during charge");
                    shooter.Tick(.11f, ctx);
                    Require(projectiles.Count == 1, "Shooter fires after charge");
                    projectiles.Move(.1f);
                    Require(projectiles.Intercept(new Vector2(.3f, -1), new Vector2(.3f, 1), .2f), "Crossing projectile intercepted");
                    Require(projectiles.Count == 0, "Intercepted bolt removed");
                    e.view.position = Vector2.right * 50;
                    shooter.Tick(5f, ctx);
                    Require(projectiles.Count == 0 && e.charge == 0, "Offscreen shooter cannot fire");
                }
                // All temporary views remain under root; the outer finally destroys them immediately in Edit Mode.
                finally { projectiles.Clear(); pool.Clear(false); }

                float limit = EmberWorld.Limit;
                for (int corner = 0; corner < 4; corner++)
                {
                    var pos = new Vector2(corner % 2 == 0 ? limit : -limit, corner < 2 ? limit : -limit);
                    for (int i = 0; i < 36; i++)
                    {
                        float a = i * 10f * Mathf.Deg2Rad;
                        var spawn = EmberCombat.SafeEncounterPosition(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
                        Require(Vector2.Distance(spawn, pos) >= 3f, "Encounter avoids unavoidable wall overlap");
                        Require(Mathf.Abs(spawn.x) <= limit && Mathf.Abs(spawn.y) <= limit, "Encounter inside arena");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            Debug.Log("Emberlight enemy rule checks passed: weights, gates, shield, swept collision, fuse, rebirth, split and encounter placement.");
        }

        static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Enemy rules: " + message); }
    }
}
