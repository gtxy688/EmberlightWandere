using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// Player-selectable game speed, 1x to 5x.
    ///
    /// Implemented as Time.timeScale, which is exactly "the whole flow of time gets faster":
    /// every system already driven by Time.deltaTime (run clock, waves, enemy movement, weapon
    /// cadence, particles, the keeper animation) scales with it and needs no changes.
    ///
    /// Everything that must NOT speed up is already written against unscaled time: the audio
    /// throttles in EmberAudio, the panel fade in EmberCardMotion, the settings click preview
    /// and the intro sequence all use Time.unscaledTime / unscaledDeltaTime. So no UI or sound
    /// work was needed to keep the shell stable at 5x.
    ///
    /// The speed is a player preference, so it persists across runs and is deliberately kept
    /// when a run ends or is abandoned.
    /// </summary>
    public static class GameSpeed
    {
        public const int MinMultiplier = 1;
        public const int MaxMultiplier = 5;

        const string PrefSpeed = "ember_speed";
        const string PrefHintSeen = "ember_speed_hint_seen";

        static int multiplier = -1;

        /// <summary>Current multiplier. Assigning stores it and applies it to Time.timeScale.</summary>
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
                // Adjusting the speed is what counts as having used the feature, so the
                // first-time hint stops appearing.
                MarkHintSeen();
            }
        }

        /// <summary>Whether the one-time "you can speed this up" hint should still be offered.</summary>
        public static bool ShouldShowHint { get { return PlayerPrefs.GetInt(PrefHintSeen, 0) == 0; } }

        public static void MarkHintSeen()
        {
            if (PlayerPrefs.GetInt(PrefHintSeen, 0) == 1) return;
            PlayerPrefs.SetInt(PrefHintSeen, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Pushes the stored multiplier into Time.timeScale. Call this whenever something else
        /// has reset the time scale (game teardown, leaving play mode) so the player's chosen
        /// speed is restored rather than silently dropped back to 1x.
        /// </summary>
        public static void Apply() { Time.timeScale = Multiplier; }
    }
}
