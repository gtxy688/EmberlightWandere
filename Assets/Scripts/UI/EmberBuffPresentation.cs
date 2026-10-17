using System.Globalization;

namespace Emberlight
{
    /// <summary>从同一份 Buff 配置生成效果与时间说明，不另存一套展示数值。</summary>
    public static class EmberBuffPresentation
    {
        public static string ColorHex(BuffConfig config) => config.IsDebuff ? "#BDACFF" :
            config.Attribute == BuffAttribute.Damage ? "#FFC66D" : "#78E5E4";

        public static string Notice(BuffConfig config, bool refreshed)
        {
            return (refreshed ? EmberCopy.BuffRefreshed + " · " : "") + config.Name + " " + Percent(config);
        }

        static string Percent(BuffConfig config) => (config.Bonus * 100).ToString("+0.#;-0.#;0", CultureInfo.InvariantCulture) + "%";
    }
}
