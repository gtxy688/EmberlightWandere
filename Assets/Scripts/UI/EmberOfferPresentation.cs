using UnityEngine;

namespace Emberlight
{
    /// <summary>卡牌的只读展示结果。与层级创建、按钮监听和动画分开。</summary>
    public sealed class EmberOfferPresentation
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string NumericPreview { get; private set; }
        public string Badge { get; private set; }

        public static EmberOfferPresentation Build(EmberOffer offer, RunProgress progress, string[] names, string[] descriptions)
        {
            int id = offer.Id;
            string title = id >= 0 && id < names.Length && !string.IsNullOrEmpty(names[id]) ? names[id] : "#" + id;
            string description = id >= 0 && id < descriptions.Length ? descriptions[id] : "";
            if ((id == RunProgress.StatAtk || id == RunProgress.StatAs) && EmberWeaponCatalog.IsRosterWeapon(offer.WeaponId))
            {
                string weaponName = EmberWeaponCatalog.Find(offer.WeaponId).Name;
                title = weaponName + "·" + title;
                description = id == RunProgress.StatAtk ? "只提高「" + weaponName + "」的伤害" : "只提高「" + weaponName + "」的攻击速度";
            }

            if (id == RunProgress.StatShield)
                description = "立即获得护盾，受伤优先消耗\n可叠加，仅本局有效";
            return new EmberOfferPresentation
            {
                Title = title,
                Description = description,
                NumericPreview = Preview(id, offer.Rarity, progress, offer.WeaponId),
                Badge = RarityLabel(offer, progress)
            };
        }

        public static string Preview(int id, EmberRarity rarity, RunProgress progress, int weaponId = -1)
        {
            switch (id)
            {
                case RunProgress.StatShield:
                    return "护盾 +" + EmberRarityUtil.ShieldAmount(rarity).ToString("0") + " · 当前 " + progress.Shield.ToString("0");
                case RunProgress.StatAtk:
                {
                    int wid = weaponId >= 0 ? weaponId : progress.CoreWeaponId;
                    return "\u653b\u51fb\u0020\u002b" + Mathf.RoundToInt(EmberRarityUtil.GenericAtkAs(rarity) * 100) + "%  (\u5408\u8ba1 +" + Mathf.RoundToInt(progress.WeaponMagnitude(wid) * 100) + "%)";
                }

                case RunProgress.StatAs:
                {
                    int wid = weaponId >= 0 ? weaponId : progress.CoreWeaponId;
                    return "\u653b\u901f\u0020\u002b" + Mathf.RoundToInt(EmberRarityUtil.GenericAttackSpeed(rarity) * 100) + "%  (\u5408\u8ba1 +" + Mathf.RoundToInt(progress.WeaponAttackSpeed(wid) * 100) + "%)";
                }

                case RunProgress.StatLuck:
                    return "\u5e78\u8fd0\u0020\u002b" + EmberRarityUtil.GenericLuck(rarity) + "  (\u5f53\u524d " + Mathf.RoundToInt(progress.Luck) + "/" + RunProgress.MaxLuck + ")";
                case RunProgress.StatAmp:
                    return "\u589e\u5e45\u0020\u002b" + Mathf.RoundToInt(EmberRarityUtil.GenericAmp(rarity) * 100) + "%  (\u5408\u8ba1 +" + Mathf.RoundToInt(progress.DamageAmp * 100) + "%)";
                case RunProgress.WeaponBasic:
                case RunProgress.WeaponOrbit:
                case RunProgress.WeaponTrail:
                case RunProgress.WeaponPierce:
                case RunProgress.WeaponBoom:
                case RunProgress.WeaponMeteor:
                    return progress.OwnsWeapon(id) ? "\u5df2\u62e5\u6709" : "\u89e3\u9501\u65b0\u6b66\u5668\u0020\u00b7\u0020\u88c5\u5165\u69fd\u4f4d";
                case RunProgress.MetaBasic:
                    return "体积×" + EmberWeaponTuning.Basic.WildfireSize.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " · 伤害×" + EmberWeaponTuning.Basic.WildfireDamage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " · 命中燃地";
                case RunProgress.MetaOrbit:
                    return "半径 " + EmberWeaponTuning.Orbit.CoronaRadius.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " · DPS " + EmberWeaponTuning.Orbit.CoronaDps.ToString("0") + " · 灼烧拖尾";
                case RunProgress.MetaTrail:
                    return "\u8eab\u5468\u6301\u7eed\u706b\u5708";
                case RunProgress.MetaPierce:
                    return "\u547d\u4e2d\u5206\u88c2\u5c0f\u706b\u77e2";
                case RunProgress.MetaBoom:
                    return "\u989d\u5916\u4e00\u4e2a\u6765\u56de";
                case RunProgress.MetaMeteor:
                    return "\u5708\u66f4\u5927\u0020\u00b7\u0020\u4f24\u66f4\u9ad8";
                case RunProgress.ExclBasic:
                    return "火球数 +" + EmberWeaponTuning.Basic.ExtraShots;
                case RunProgress.ExclOrbit:
                    return "环绕 +" + EmberWeaponTuning.Orbit.ExtraOrbs;
                case RunProgress.ExclTrail:
                    return "\u62d6\u5c3e\u66f4\u957f\u66f4\u70eb";
                case RunProgress.ExclPierce:
                    return "穿透目标 +" + EmberWeaponTuning.Pierce.ExtraTargets;
                case RunProgress.ExclBoom:
                    return "\u5f80\u8fd4\u5404\u591a\u4e00\u6bb5";
                case RunProgress.ExclMeteor:
                    return "\u843d\u70b9\u0020\u002b1";
                default:
                    return "";
            }
        }

        public static string RarityLabel(EmberOffer offer, RunProgress progress)
        {
            string rarity = EmberRarityUtil.Name(offer.Rarity);
            return offer.Kind == EmberOfferKind.Generic ? rarity : KindBadge(offer, progress) + " · " + rarity;
        }

        static string KindBadge(EmberOffer offer, RunProgress progress)
        {
            if (offer.Kind == EmberOfferKind.Metamorph)
                return "\u8d28\u53d8";
            if (offer.Kind == EmberOfferKind.Exclusive)
                return "\u4e13\u5c5e";
            if (offer.Kind == EmberOfferKind.Weapon)
                return progress != null && progress.OwnsWeapon(offer.Id) ? "\u6b66\u5668" : "\u65b0\u6b66\u5668";
            return EmberRarityUtil.Name(offer.Rarity);
        }
    }
}
