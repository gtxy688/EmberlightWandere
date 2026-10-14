namespace Emberlight
{
    /// <summary>武器基础参数与升级增量。规则、伤害计算和卡面预览共用，避免文案漂移。</summary>
    public static class EmberWeaponTuning
    {
        public static class Basic
        {
            public const float Cooldown = .8f, Damage = 22f, Speed = 9f, Lifetime = 1.8f;
            public const float WildfireSize = 1.5f, WildfireDamage = 1.25f;
            public const int ExtraShots = 1;
        }

        public static class Orbit
        {
            public const float Radius = 1.6f, CoronaRadius = 2.3f;
            public const float Spin = 2.5f, CoronaSpin = 3.6f;
            public const float Dps = 65f, CoronaDps = 75f;
            public const int ExtraOrbs = 1;
        }

        public static class Trail
        {
            public const float Interval = .45f, ExtendedInterval = .28f;
            public const float RingRadius = 2.1f, RingDps = 70f, SeaPatchInterval = .35f;
            public const float ExtraPower = .25f;
        }

        public static class BurnGround
        {
            public const int Capacity = 40;
            public const float Lifetime = 2.5f, SmallLifetime = 1.6f;
            public const float Radius = .7f, SmallRadius = .55f;
            public const float Dps = 46f, DefaultPower = .4f, ExtendedPower = 1.35f;
        }

        public static class Pierce
        {
            public const float Cooldown = 1.1f, Damage = 30f, Speed = 12f, SplitSpeed = 10f;
            public const float Lifetime = 2.2f, SplitLifetime = 1f, SplitDamage = .45f;
            public const int ExtraTargets = 1;
        }

        public static class Boom
        {
            public const float Cooldown = 2f, Damage = 18f, Distance = 6f, Speed = 10f;
            public const int ExtraTrips = 1;
        }

        public static class Meteor
        {
            public const float Cooldown = 3f, Damage = 45f, Radius = 1.6f;
            public const float SkyfireRadius = 2.53f, SkyfireDamage = 1.54f, WarningTime = .6f;
            public const int ExtraImpacts = 1;
        }
    }
}
