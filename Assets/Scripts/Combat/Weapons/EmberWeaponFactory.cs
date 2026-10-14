using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>武器实现的唯一注册点。顺序保持原有伤害与共享燃地的结算时序。</summary>
    public static class EmberWeaponFactory
    {
        public static List<EmberWeapon> CreateAll()
        {
            return new List<EmberWeapon>
            {
                new EmberBasicShotWeapon(),
                new EmberOrbitWeapon(),
                new EmberTrailWeapon(),
                new EmberPulseWeapon(),
                new EmberPierceWeapon(),
                new EmberBoomerangWeapon(),
                new EmberMeteorWeapon()
            };
        }
    }
}
