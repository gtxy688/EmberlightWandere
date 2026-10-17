using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    /// <summary>绑定 HUD 视图；条形值随状态更新，文字仅在对应显示数据变化时刷新。</summary>
    public sealed class EmberHud
    {
        public GameObject Root { get; }
        readonly Image health, shield, waveFill, bossFill;
        readonly TextMeshProUGUI healthText, shieldText, waveText, clock, bossText, encounterLabel;
        readonly GameObject bossRoot, encounterRoot;
        readonly EmberBuffHud buffs;
        bool hasStats;
        float lastHealth, lastMaxHealth, lastShield;
        int lastSecond, lastKills, lastOwned, lastSlots, lastLuck;
        int lastWave = -1, lastTotal = -1;
        string lastDifficulty, lastWaveName, lastBossPhase;

        public EmberHud(EmberUiRoot.HudReferences view, System.Action pause)
        {
            Root = view.Root;
            health = view.Health;
            shield = view.Shield;
            waveFill = view.WaveFill;
            bossFill = view.BossFill;
            healthText = view.HealthText;
            shieldText = view.ShieldText;
            waveText = view.WaveText;
            clock = view.Clock;
            bossText = view.BossText;
            bossRoot = view.BossRoot;
            encounterRoot = view.EncounterRoot;
            encounterLabel = view.EncounterLabel;
            buffs = view.Buffs;
            buffs.Initialize();
            view.Pause.onClick.AddListener(() => pause?.Invoke());
            Show(false);
            encounterRoot.SetActive(false);
        }

        public void Set(float hp, float max, RunProgress progress, float seconds, int kills, int wave, int waveRemaining, int waveQuota)
        {
            float shieldValue = progress != null ? progress.Shield : 0;
            float capacity = progress != null ? progress.ShieldCapacity : 0;
            shield.fillAmount = capacity > 0 ? shieldValue / capacity : 0;
            health.fillAmount = max > 0 ? Mathf.Clamp01(hp / max) : 0;
            waveFill.fillAmount = waveQuota > 0 ? 1f - Mathf.Clamp01((float)waveRemaining / waveQuota) : 1f;
            if (!hasStats || shieldValue != lastShield)
            {
                shieldText.text = "护盾 " + shieldValue.ToString("0");
                lastShield = shieldValue;
            }
            if (!hasStats || hp != lastHealth || max != lastMaxHealth)
            {
                healthText.text = "生命  " + Mathf.Max(0, hp).ToString("0") + " / " + max.ToString("0");
                lastHealth = hp;
                lastMaxHealth = max;
            }
            int second = Mathf.Max(0, (int)seconds);
            int owned = progress != null ? progress.OwnedWeaponCount : -1;
            int slots = progress != null ? progress.WeaponSlots : -1;
            int luck = progress != null ? Mathf.RoundToInt(progress.Luck) : -1;
            if (!hasStats || second != lastSecond || kills != lastKills || owned != lastOwned || slots != lastSlots || luck != lastLuck)
            {
                string build = progress != null ? "     武器 " + owned + "/" + slots + "     幸运 " + luck : "";
                clock.text = (second / 60).ToString("00") + ":" + (second % 60).ToString("00") + "     击杀 " + kills + build;
                lastSecond = second;
                lastKills = kills;
                lastOwned = owned;
                lastSlots = slots;
                lastLuck = luck;
            }
            hasStats = true;
        }

        /// <summary>波次文字的唯一写入点。</summary>
        public void Stage(int wave, int total, string difficulty, string name)
        {
            if (wave == lastWave && total == lastTotal && difficulty == lastDifficulty && name == lastWaveName) return;
            waveText.text = difficulty + " · " + wave + "/" + total + " 波 · " + name;
            lastWave = wave;
            lastTotal = total;
            lastDifficulty = difficulty;
            lastWaveName = name;
        }

        public void Encounter(string message)
        {
            bool show = !string.IsNullOrEmpty(message);
            if (encounterRoot.activeSelf != show) encounterRoot.SetActive(show);
            if (show && encounterLabel.text != message) encounterLabel.text = message;
        }

        public void Buffs(System.Collections.Generic.IReadOnlyList<Buff> active) => buffs.Present(active);

        public void NotifyBuff(BuffConfig config, bool refreshed) => buffs.Notify(config, refreshed);

        public void PositionBuffFeedback(Camera camera, Vector3 playerPosition) => buffs.PositionFeedback(camera, playerPosition);

        public void TickFeedback(float unscaledDeltaTime) => buffs.TickFeedback(unscaledDeltaTime);

        public void Show(bool visible)
        {
            if (Root.activeSelf != visible) Root.SetActive(visible);
            if (!visible)
            {
                bossRoot.SetActive(false);
                buffs.ClearFeedback();
            }
        }

        public void Boss(float ratio, string phase, bool show)
        {
            if (bossRoot.activeSelf != show) bossRoot.SetActive(show);
            if (!show) return;
            bossFill.fillAmount = Mathf.Clamp01(ratio);
            if (phase == lastBossPhase) return;
            bossText.text = "长夜守卫  ·  " + phase;
            lastBossPhase = phase;
        }
    }
}
