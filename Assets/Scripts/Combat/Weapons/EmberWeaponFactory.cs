using System.Collections.Generic;

namespace Emberlight
{
    /// <summary>武器实现的唯一注册点；列表顺序即伤害与共享燃地的结算顺序。</summary>
    public static class EmberWeaponFactory
    {
        public static List<EmberWeapon> CreateAll()
        {
            return new List<EmberWeapon>
            {
                new EmberBasicShotWeapon(),
                new EmberOrbitWeapon(),
                new EmberTrailWeapon(),
                new EmberPierceWeapon(),
                new EmberBoomerangWeapon(),
                new EmberMeteorWeapon()
            };
        }
    }
}
