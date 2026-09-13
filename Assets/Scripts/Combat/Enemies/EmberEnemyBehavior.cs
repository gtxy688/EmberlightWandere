using UnityEngine;

namespace Emberlight
{
    public enum EnemyAffix { None, Burning, Wind, Rebirth }

    public interface IEnemyBehavior
    {
        void Tick(float dt, EmberEnemyContext ctx);
        bool OnLethal(EmberEnemyContext ctx);
        void OnDeath(EmberEnemyContext ctx);
    }

    /// <summary>Reused by the combat owner; behaviors must not retain this context.</summary>
    public sealed class EmberEnemyContext
    {
        public EmberCombat.Enemy Enemy;
        public Transform Player;
        public Camera Camera;
        public LevelConfig Config;
        public EmberEnemyProjectiles Projectiles;
        public System.Action<Vector2> Split;
        public System.Action<Vector2, float, float> Explode;
        public float SpeedMultiplier = 1f;
        public Vector2 Position { get { return Enemy.view.position; } }
        public Vector2 Delta { get { return (Vector2)Player.position - Position; } }
        public bool Visible { get { return EmberWorld.Visible(Camera, Position); } }

        public void Move(Vector2 direction, float dt)
        {
            Enemy.view.position = EmberWorld.Clamp(Position + direction * Enemy.speed * SpeedMultiplier * dt, .3f);
        }

        // The nominal ranged band must fit the portrait viewport, including the HUD inset.
        public float ShooterNearDistance
        {
            get { return Mathf.Min(Config.ShooterNear, Camera.orthographicSize * Mathf.Min(Camera.aspect, .65f) * .70f); }
        }
    }

    public class ChaseBehavior : IEnemyBehavior
    {
        public virtual void Tick(float dt, EmberEnemyContext ctx) { ctx.Move(ctx.Delta.normalized, dt); }
        public virtual bool OnLethal(EmberEnemyContext ctx) { return false; }
        public virtual void OnDeath(EmberEnemyContext ctx) { }
    }

    public sealed class ShooterBehavior : ChaseBehavior
    {
        float cooldown = 1f, charge;
        Vector2 aim;
        public override void Tick(float dt, EmberEnemyContext ctx)
        {
            var e = ctx.Enemy;
            float distance = ctx.Delta.magnitude;
            if (!ctx.Visible)
            {
                charge = 0;
                e.charge = 0;
                ctx.Move(ctx.Delta.normalized, dt);
                return;
            }
            if (charge > 0)
            {
                charge = Mathf.Max(0, charge - dt);
                e.charge = 1f - charge / ctx.Config.ShooterCharge;
                e.aim = aim;
                if (charge == 0)
                {
                    ctx.Projectiles.Fire(ctx.Position, aim, ctx.Config.ShooterBulletSpeed, ctx.Config.ShooterDamage);
                    cooldown = ctx.Config.ShooterInterval - ctx.Config.ShooterCharge;
                    e.charge = 0;
                }
                return;
            }
            float near = ctx.ShooterNearDistance;
            if (distance > near + (ctx.Config.ShooterFar - ctx.Config.ShooterNear)) ctx.Move(ctx.Delta.normalized, dt);
            else if (distance < near) ctx.Move(-ctx.Delta.normalized, dt);
            cooldown -= dt;
            if (cooldown <= 0 && ctx.Visible)
            {
                charge = ctx.Config.ShooterCharge;
                aim = ctx.Delta.sqrMagnitude > .001f ? ctx.Delta.normalized : Vector2.down;
                e.aim = aim;
                e.charge = .01f;
            }
        }
    }

    public sealed class SplitBehavior : ChaseBehavior
    {
        public override void OnDeath(EmberEnemyContext ctx) { ctx.Split(ctx.Position); }
    }

    public sealed class ShieldBehavior : ChaseBehavior
    {
        public override void Tick(float dt, EmberEnemyContext ctx)
        {
            float target = Mathf.Atan2(ctx.Delta.y, ctx.Delta.x) * Mathf.Rad2Deg;
            ctx.Enemy.facing = Mathf.MoveTowardsAngle(ctx.Enemy.facing, target, ctx.Config.ShieldTurnSpeed * dt);
            float a = ctx.Enemy.facing * Mathf.Deg2Rad;
            ctx.Move(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), dt);
        }

        public static float DamageMultiplier(float facing, Vector2 towardSource, float frontMultiplier)
        {
            if (towardSource.sqrMagnitude < .0001f) return 1f;
            float angle = Mathf.Atan2(towardSource.y, towardSource.x) * Mathf.Rad2Deg;
            return Mathf.Abs(Mathf.DeltaAngle(facing, angle)) <= 60f ? frontMultiplier : 1f;
        }
    }

    public sealed class BomberBehavior : ChaseBehavior
    {
        public override bool OnLethal(EmberEnemyContext ctx)
        {
            Arm(ctx);
            return true;
        }

        static void Arm(EmberEnemyContext ctx)
        {
            if (ctx.Enemy.detonating) return;
            ctx.Enemy.detonating = true;
            ctx.Enemy.fuse = ctx.Config.BombFuse;
        }

        public override void Tick(float dt, EmberEnemyContext ctx)
        {
            var e = ctx.Enemy;
            if (e.dead) return;
            if (!e.detonating)
            {
                if (ctx.Delta.magnitude > ctx.Config.BombTriggerRange) { base.Tick(dt, ctx); return; }
                Arm(ctx);
                return;
            }
            e.fuse = Mathf.Max(0, e.fuse - dt);
            if (e.fuse > 0) return;
            ctx.Explode(ctx.Position, ctx.Config.BombRadius, ctx.Config.BombDamage);
            e.dead = true;
        }
    }
}
