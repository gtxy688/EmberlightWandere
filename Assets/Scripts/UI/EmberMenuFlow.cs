using UnityEngine;

namespace Emberlight
{
    /// <summary>开局界面流程需要的宿主能力。EmberMenuFlow 不持有 EmberGame，只通过这里回调。</summary>
    public interface IEmberMenuHost
    {
        /// <summary>请求把局长状态切到某个阶段。状态本身仍归 EmberGame 所有。</summary>
        void EnterMode(EmberGame.Mode mode);
        /// <summary>回主菜单前，把上一局的世界收起来。</summary>
        void HideRunWorld();
        /// <summary>起始武器选完，交回宿主真正开一局。</summary>
        void StartRun(int[] weapons, int slots);
    }

    /// <summary>
    /// 开局前的界面流程：主菜单 → 设置入口 → 选局长 → 选难度 → 选槽位 → 选起始武器。
    /// 只负责「显示哪个界面、点了之后去哪」，不碰玩法；开一局通过 IEmberMenuHost.StartRun 交回 EmberGame。
    /// </summary>
    public sealed class EmberMenuFlow
    {
        readonly EmberMenuUi ui;
        readonly IEmberMenuHost host;

        int selectedWaves = 25;
        EmberDifficulty selectedDifficulty = EmberDifficulty.Standard;
        int pendingSlots = 2;
        int[] pendingRoster = new int[0];

        /// <summary>开一局时用：局长波数。</summary>
        public int SelectedWaves { get { return selectedWaves; } }
        /// <summary>开一局时用：难度。</summary>
        public EmberDifficulty SelectedDifficulty { get { return selectedDifficulty; } }

        public EmberMenuFlow(EmberMenuUi menuUi, IEmberMenuHost menuHost)
        {
            ui = menuUi;
            host = menuHost;
        }

        public void ShowMainMenu()
        {
            host.EnterMode(EmberGame.Mode.Menu);
            EmberAudio.Ensure().PlayMenuMusic();
            host.HideRunWorld();
            ui.Show(
                EmberCopy.MenuTitle,
                EmberCopy.MenuTagline,
                EmberCopy.MenuChoices,
                new UnityEngine.Events.UnityAction[] { BeginRun, OpenSettingsFromMenu },
                true,
                // Offered once, until the player actually changes the speed. Tapping it counts
                // as acknowledgement so it does not nag every session.
                GameSpeed.ShouldShowHint ? EmberCopy.MenuHint : null,
                GameSpeed.MarkHintSeen);
        }

        void OpenSettingsFromMenu()
        {
            if (ui == null || ui.SettingsPanel == null)
            {
                ShowMainMenu();
                return;
            }
            ui.Overlay.SetActive(false);
            ui.SettingsPanel.Show(ShowMainMenu, false);
        }

        /// <summary>Entry: pick N then single starter weapon (no skip).</summary>
        public void BeginRun()
        {
            if (ui == null || ui.BattleHud == null || ui.GodsSelect == null) return;
            if (ui.UpgradePanel != null) ui.UpgradePanel.Hide();
            ui.Overlay.SetActive(false);
            ui.BattleHud.Show(false);
            ui.StickBase.gameObject.SetActive(false);
            pendingSlots = 2;
            pendingRoster = new int[0];
            host.EnterMode(EmberGame.Mode.SelectLength);
            // Spells out the Boss schedule before the run starts: every 10th wave is a stage Boss
            // and the last one is the Nightwarden, so the final wave is not a surprise.
            ui.Show(EmberCopy.RunSetupTitle,
                EmberCopy.RunSetupBody,
                EmberCopy.MenuChoicesWithBack,
                new UnityEngine.Events.UnityAction[] { () => ChooseLength(25), () => ChooseLength(50), ShowMainMenu });
        }

        void ChooseLength(int waves)
        {
            selectedWaves = waves;
            host.EnterMode(EmberGame.Mode.SelectDifficulty);
            // One difficulty per line: a single wrapped sentence broke "精英" onto a line of its
            // own and pushed the closing 。with it.
            ui.Show(EmberCopy.DifficultyTitle, EmberCopy.DifficultyBody(selectedWaves),
                EmberCopy.DifficultyChoices,
                new UnityEngine.Events.UnityAction[] { () => ChooseDifficulty(EmberDifficulty.Casual),
                    () => ChooseDifficulty(EmberDifficulty.Standard), () => ChooseDifficulty(EmberDifficulty.Hard) });
        }

        void ChooseDifficulty(EmberDifficulty difficulty)
        {
            selectedDifficulty = difficulty;
            ui.Overlay.SetActive(false);
            host.EnterMode(EmberGame.Mode.SelectSlots);
            ui.GodsSelect.ShowSlots(OnSlotsChosen);
        }

        void OnSlotsChosen(int slots)
        {
            pendingSlots = slots;
            host.EnterMode(EmberGame.Mode.SelectRoster);
            ui.GodsSelect.ShowRoster(slots, OnRosterChosen);
        }

        void OnRosterChosen(int[] weapons)
        {
            pendingRoster = weapons ?? new[] { RunProgress.WeaponBasic };
            host.StartRun(pendingRoster, pendingSlots);
        }
    }
}