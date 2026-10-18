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
                description = id == RunProgress.StatAtk
                    ? "提高「" + weaponName + "」造成的伤害\n不强化其他武器的直接伤害"
                    : AttackSpeedDescription(offer.WeaponId, weaponName);
            }

            return new EmberOfferPresentation
            {
                Title = title,
                Description = description,
                NumericPreview = Preview(id, offer.Rarity, progress, offer.WeaponId),
                Badge = RarityLabel(offer, progress)
            };
        }

        static string AttackSpeedDescription(int weaponId, string weaponName)
        {
            if (weaponId == EmberWeaponCatalog.Orbit)
                return "提高环火的旋转速度\n仅强化环火，不改变接触伤害";
            if (weaponId == EmberWeaponCatalog.Trail)
                return "缩短燃地的铺设间隔\n不提高单块火焰的每秒伤害";
            if (weaponId == EmberWeaponCatalog.Pierce)
                return "缩短火矢的射击间隔\n移动时仍需停下才能射击";
            return "缩短「" + weaponName + "」的攻击间隔\n仅强化对应武器";
        }

        static string Number(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        static string Percent(float value) => Mathf.RoundToInt(value * 100).ToString("+0;-0;+0", System.Globalization.CultureInfo.InvariantCulture) + "%";
        static string BonusPreview(string label, float current, float added) => label + " " + Percent(current) + " → " + Percent(current + added);

        public static string Preview(int id, EmberRarity rarity, RunProgress progress, int weaponId = -1)
        {
            switch (id)
            {
                case RunProgress.StatShield:
                    return "护盾 " + Number(progress.Shield) + " → " + Number(progress.Shield + EmberRarityUtil.ShieldAmount(rarity));
                case RunProgress.StatAtk:
                {
                    int wid = weaponId >= 0 ? weaponId : progress.CoreWeaponId;
                    return BonusPreview("攻击加成", progress.WeaponMagnitude(wid), EmberRarityUtil.GenericAtkAs(rarity));
                }

                case RunProgress.StatAs:
                {
                    int wid = weaponId >= 0 ? weaponId : progress.CoreWeaponId;
                    return BonusPreview("攻速加成", progress.WeaponAttackSpeed(wid), EmberRarityUtil.GenericAttackSpeed(rarity));
                }

                case RunProgress.StatLuck:
                    return "幸运 " + Number(progress.Luck) + " → " + Number(Mathf.Min(RunProgress.MaxLuck, progress.Luck + EmberRarityUtil.GenericLuck(rarity))) + " · 上限 " + RunProgress.MaxLuck;
                case RunProgress.StatAmp:
                    return BonusPreview("伤害增幅", progress.DamageAmp, EmberRarityUtil.GenericAmp(rarity));
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
                    return "半径 " + Number(EmberWeaponTuning.Orbit.CoronaRadius) + " · 基础 " + Number(EmberWeaponTuning.Orbit.CoronaDps) + " 伤害/秒";
                case RunProgress.MetaTrail:
                    return "火圈半径 " + Number(EmberWeaponTuning.Trail.RingRadius) + " · 持续范围伤害";
                case RunProgress.MetaPierce:
                    return "分裂火矢伤害 " + Number(EmberWeaponTuning.Pierce.SplitDamage * 100) + "%";
                case RunProgress.MetaBoom:
                    return "额外往返 +" + EmberWeaponTuning.Boom.ExtraTrips;
                case RunProgress.MetaMeteor:
                    return "半径 " + Number(EmberWeaponTuning.Meteor.Radius) + " → " + Number(EmberWeaponTuning.Meteor.SkyfireRadius) + " · 伤害×" + Number(EmberWeaponTuning.Meteor.SkyfireDamage);
                case RunProgress.ExclBasic:
                    return "火球数 +" + EmberWeaponTuning.Basic.ExtraShots;
                case RunProgress.ExclOrbit:
                    return "环绕 +" + EmberWeaponTuning.Orbit.ExtraOrbs;
                case RunProgress.ExclTrail:
                {
                    float speed = 1 + progress.WeaponAttackSpeed(EmberWeaponCatalog.Trail);
                    return "铺火间隔 " + Number(EmberWeaponTuning.Trail.Interval / speed) + " → " + Number(EmberWeaponTuning.Trail.ExtendedInterval / speed) + " 秒";
                }
                case RunProgress.ExclPierce:
                    return "穿透目标 +" + EmberWeaponTuning.Pierce.ExtraTargets;
                case RunProgress.ExclBoom:
                    return "去程、回程均增加命中机会";
                case RunProgress.ExclMeteor:
                    return "火雨落点 +" + EmberWeaponTuning.Meteor.ExtraImpacts;
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
