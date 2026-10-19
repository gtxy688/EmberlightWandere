using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 用 Time.timeScale 应用玩家倍速并跨局保存；界面动画与音频节流使用 unscaled 时间。
    /// </summary>
    public static class GameSpeed
    {
        public const int MinMultiplier = 1;
        public const int MaxMultiplier = 5;

        const string PrefSpeed = "ember_speed";
        const string PrefHintSeen = "ember_speed_hint_seen";

        static int multiplier = -1;

        /// <summary>当前倍率；赋值会持久化并立即应用到 Time.timeScale。</summary>
        public static int Multiplier
        {
            get
            {
                if (multiplier < 0) multiplier = Mathf.Clamp(PlayerPrefs.GetInt(PrefSpeed, 1), MinMultiplier, MaxMultiplier);
                return multiplier;
            }
            set
            {
                int clamped = Mathf.Clamp(value, MinMultiplier, MaxMultiplier);
                bool changed = clamped != multiplier;
                multiplier = clamped;
                Time.timeScale = clamped;
                if (!changed) return;
                PlayerPrefs.SetInt(PrefSpeed, clamped);
                PlayerPrefs.Save();
                // 调过速度就算用过该功能，首次提示不再出现。
                MarkHintSeen();
            }
        }

        /// <summary>是否仍应展示一次性的「可以加速」提示。</summary>
        public static bool ShouldShowHint { get { return PlayerPrefs.GetInt(PrefHintSeen, 0) == 0; } }

        public static void MarkHintSeen()
        {
            if (PlayerPrefs.GetInt(PrefHintSeen, 0) == 1) return;
            PlayerPrefs.SetInt(PrefHintSeen, 1);
            PlayerPrefs.Save();
        }

        /// <summary>把已存倍率推回 Time.timeScale。任何重置过 timeScale 的场合（收局拆场、退出播放模式）
        /// 都要调用，否则玩家选的速度会被静默打回 1x。</summary>
        public static void Apply() { Time.timeScale = Multiplier; }
    }
}
