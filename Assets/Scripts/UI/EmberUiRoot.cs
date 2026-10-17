using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight
{
    /// <summary>Prefab 的视图引用。布局归资产，数据与交互归各面板；不持有游戏宿主。</summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class EmberUiRoot : MonoBehaviour
    {
        [Header("Menu")]
        public RectTransform Safe;
        public GameObject Overlay, Brand, Hint;
        public TextMeshProUGUI Heading, Summary, HintText;
        public Button[] MenuButtons;
        [Header("Input")]
        public RectTransform StickBase, StickKnob;
        [Header("Panels")]
        public EmberUpgradePanel Upgrade;
        public EmberGodsSelectPanel GodsSelect;
        public EmberSettingsPanel Settings;
        public HudReferences Hud;

        [Serializable]
        public sealed class HudReferences
        {
            public GameObject Root, BossRoot, EncounterRoot;
            public Image Health, Shield, WaveFill, BossFill;
            public TextMeshProUGUI HealthText, ShieldText, WaveText, Clock, BossText, EncounterLabel;
            public EmberBuffHud Buffs;
            public Button Pause;
        }

        public void ValidateReferences()
        {
            if (Safe == null || Overlay == null || Brand == null || Hint == null || HintText == null || Heading == null || Summary == null ||
                StickBase == null || StickKnob == null || Upgrade == null || GodsSelect == null || Settings == null ||
                Hud == null || Hud.Root == null || Hud.Pause == null || Hud.BossRoot == null || Hud.EncounterRoot == null ||
                Hud.Health == null || Hud.Shield == null || Hud.WaveFill == null || Hud.BossFill == null ||
                Hud.HealthText == null || Hud.ShieldText == null || Hud.WaveText == null || Hud.Clock == null ||
                Hud.BossText == null || Hud.EncounterLabel == null || Hud.Buffs == null ||
                MenuButtons == null || MenuButtons.Length < 3 || Array.Exists(MenuButtons, b => b == null))
                throw new InvalidOperationException("UIRoot references are incomplete. Run Emberlight/UI/Connect UIRoot Runtime in the Editor.");
        }
    }
}
