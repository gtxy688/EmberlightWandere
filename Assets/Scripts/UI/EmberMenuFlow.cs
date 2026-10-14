using UnityEngine;

namespace Emberlight
{
    /// <summary>开局界面流程需要的宿主能力；阶段本身归组合根持有，EmberMenuFlow 不持有 EmberGame，只通过这里回调。</summary>
    public interface IEmberMenuHost
    {
        void EnterMode(EmberRunPhase mode);
        /// <summary>回主菜单前收起上一局的世界。</summary>
        void HideRunWorld();
        void StartRun(int[] weapons, int slots);
    }

    /// <summary>开局前的界面流程：主菜单 → 设置 → 选局长 → 选难度 → 选槽位 → 选起始武器；只管显示与跳转，不碰玩法，开一局通过 IEmberMenuHost.StartRun 交回组合根。</summary>
    public sealed class EmberMenuFlow
    {
        readonly EmberMenuUi ui;
        readonly IEmberMenuHost host;

        int selectedWaves = 25;
        EmberDifficulty selectedDifficulty = EmberDifficulty.Standard;
        int pendingSlots = 2;
        int[] pendingRoster = new int[0];

        public int SelectedWaves { get { return selectedWaves; } }
        public EmberDifficulty SelectedDifficulty { get { return selectedDifficulty; } }

        public EmberMenuFlow(EmberMenuUi menuUi, IEmberMenuHost menuHost)
        {
            ui = menuUi;
            host = menuHost;
        }

        public void ShowMainMenu()
        {
            host.EnterMode(EmberRunPhase.Menu);
            EmberAudio.Ensure().PlayMenuMusic();
            host.HideRunWorld();
            ui.Show(
                EmberCopy.MenuTitle,
                EmberCopy.MenuTagline,
                EmberCopy.MenuChoices,
                new UnityEngine.Events.UnityAction[] { BeginRun, OpenSettingsFromMenu },
                true,
                // 一次性提示：只在玩家真正改过速度之前出现，点掉即视为已知晓，不必每局再提示。
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

        /// <summary>入口：先选 N 再单选 1 把起始武器，开局不可跳过。</summary>
        public void BeginRun()
        {
            if (ui == null || ui.BattleHud == null || ui.GodsSelect == null) return;
            if (ui.UpgradePanel != null) ui.UpgradePanel.Hide();
            ui.Overlay.SetActive(false);
            ui.BattleHud.Show(false);
            ui.StickBase.gameObject.SetActive(false);
            pendingSlots = 2;
            pendingRoster = new int[0];
            host.EnterMode(EmberRunPhase.SelectLength);
            // 开跑前先交代 Boss 节奏：每 10 波一个关底 Boss，最后一波是长夜守卫，避免最终波突然袭击。
            ui.Show(EmberCopy.RunSetupTitle,
                EmberCopy.RunSetupBody,
                EmberCopy.MenuChoicesWithBack,
                new UnityEngine.Events.UnityAction[] { () => ChooseLength(25), () => ChooseLength(50), ShowMainMenu });
        }

        void ChooseLength(int waves)
        {
            selectedWaves = waves;
            host.EnterMode(EmberRunPhase.SelectDifficulty);
            // 每个难度独占一行：整句换行会把「精英」挤到单独一行，并把收尾的句号一起带走。
            ui.Show(EmberCopy.DifficultyTitle, EmberCopy.DifficultyBody(selectedWaves),
                EmberCopy.DifficultyChoices,
                new UnityEngine.Events.UnityAction[] { () => ChooseDifficulty(EmberDifficulty.Casual),
                    () => ChooseDifficulty(EmberDifficulty.Standard), () => ChooseDifficulty(EmberDifficulty.Hard) });
        }

        void ChooseDifficulty(EmberDifficulty difficulty)
        {
            selectedDifficulty = difficulty;
            ui.Overlay.SetActive(false);
            host.EnterMode(EmberRunPhase.SelectSlots);
            ui.GodsSelect.ShowSlots(OnSlotsChosen);
        }

        void OnSlotsChosen(int slots)
        {
            pendingSlots = slots;
            host.EnterMode(EmberRunPhase.SelectRoster);
            ui.GodsSelect.ShowRoster(slots, OnRosterChosen);
        }

        void OnRosterChosen(int[] weapons)
        {
            pendingRoster = weapons ?? new[] { RunProgress.WeaponBasic };
            host.StartRun(pendingRoster, pendingSlots);
        }
    }
}
