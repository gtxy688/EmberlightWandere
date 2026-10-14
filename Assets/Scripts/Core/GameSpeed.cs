using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 玩家可选的游戏速度，1x–5x，直接实现为 Time.timeScale：所有已按 Time.deltaTime 驱动的系统
    /// （局内计时、波次、敌人移动、武器节奏、粒子、灯芯动画）自动跟随，无需逐个改动。
    /// 不该加速的部分已经写在 unscaled 时间上：EmberAudio 的节流、EmberCardMotion 的面板淡入、
    /// 设置页点击试听与开场序列都用 Time.unscaledTime / unscaledDeltaTime，故 5x 下界面与音效稳定。
    /// 速度是玩家偏好，跨局保留，收局或中途退出都不重置。
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
