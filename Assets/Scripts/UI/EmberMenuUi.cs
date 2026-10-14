using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Emberlight
{
    public sealed class EmberMenuUi
    {
        public RectTransform Safe { get; private set; }
        public GameObject Overlay { get; private set; }
        public RectTransform StickBase { get; private set; }
        public RectTransform StickKnob { get; private set; }
        public EmberUpgradePanel UpgradePanel { get; private set; }
        public EmberGodsSelectPanel GodsSelect { get; private set; }
        public EmberHud BattleHud { get; private set; }
        public EmberSettingsPanel SettingsPanel { get; private set; }

        TextMeshProUGUI heading, summary;
        GameObject brandRoot;
        GameObject hintRoot;
        TextMeshProUGUI hintText;
        bool watchingSafeArea;
        readonly System.Collections.Generic.List<Button> menuButtons = new System.Collections.Generic.List<Button>();
        UnityEngine.Events.UnityAction hintDismissed;

        public void Bind(EmberUiRoot view, TMP_FontAsset loadedFont, System.Action pause)
        {
            view.ValidateReferences();
            foreach (var text in view.GetComponentsInChildren<TextMeshProUGUI>(true)) text.font = loadedFont;
            Safe = view.Safe;
            Overlay = view.Overlay;
            heading = view.Heading;
            summary = view.Summary;
            brandRoot = view.Brand;
            hintRoot = view.Hint;
            hintText = view.HintText;
            menuButtons.Clear();
            menuButtons.AddRange(view.MenuButtons);
            StickBase = view.StickBase;
            StickKnob = view.StickKnob;
            UpgradePanel = view.Upgrade;
            GodsSelect = view.GodsSelect;
            SettingsPanel = view.Settings;
            UpgradePanel.Initialize();
            GodsSelect.Initialize();
            SettingsPanel.Initialize();
            BattleHud = new EmberHud(view.Hud, pause);
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (hintRoot != null)
                hintRoot.GetComponent<Button>().onClick.AddListener(() =>
                {
                    EmberAudio.Ensure().PlayUiClick();
                    HideHint();
                    hintDismissed?.Invoke();
                });
            view.gameObject.SetActive(true);
            Overlay.SetActive(false);
            HideHint();
            StickBase.gameObject.SetActive(false);
            UpdateSafeArea();
        }

        public void UpdateSafeArea()
        {
            if (Safe == null)
                return;
            Rect area = Screen.safeArea;
            Safe.anchorMin = new Vector2(area.x / Screen.width, area.y / Screen.height);
            Safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            Safe.offsetMin = Safe.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 订阅安全区变化。原先这份判断每帧跑在 `EmberGame.Update()` 顶部；现在由
        /// <see cref = "EmberSafeAreaWatcher"/> 统一观察，UI 只在真的变化时才重排。
        /// </summary>
        public void WatchSafeArea()
        {
            if (watchingSafeArea)
                return;
            watchingSafeArea = true;
            EmberSafeAreaWatcher.Changed += UpdateSafeArea;
        }

        /// <summary>取消订阅，避免静态事件留住已销毁的面板。</summary>
        public void UnwatchSafeArea()
        {
            if (!watchingSafeArea)
                return;
            watchingSafeArea = false;
            EmberSafeAreaWatcher.Changed -= UpdateSafeArea;
        }

        public void SetStick(bool active, Vector2 screenOrigin, Vector2 stick)
        {
            StickBase.gameObject.SetActive(active);
            if (!active)
                return;
            StickBase.position = screenOrigin;
            StickKnob.anchoredPosition = stick * 45;
        }

        public void Show(string title, string desc, string[] labels, UnityEngine.Events.UnityAction[] actions, bool showBrand = false, string hint = null, UnityEngine.Events.UnityAction onHintDismissed = null)
        {
            if (UpgradePanel != null)
                UpgradePanel.Hide();
            if (GodsSelect != null)
                GodsSelect.Hide();
            if (SettingsPanel != null)
                SettingsPanel.Hide();
            BattleHud.Show(false);
            Overlay.SetActive(true);
            if (brandRoot != null)
                brandRoot.SetActive(showBrand);
            heading.text = title;
            summary.text = desc;
            StickBase.gameObject.SetActive(false);
            if (labels.Length > menuButtons.Count) throw new System.InvalidOperationException("UIRoot has too few menu buttons.");
            for (int i = 0; i < labels.Length; i++)
            {
                var button = menuButtons[i];
                button.name = "Button " + labels[i];
                button.GetComponentInChildren<TextMeshProUGUI>(true).text = labels[i];
                button.onClick.RemoveAllListeners();
                var action = actions[i];
                button.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); action?.Invoke(); });
                button.gameObject.SetActive(true);
            }
            for (int i = labels.Length; i < menuButtons.Count; i++) menuButtons[i].gameObject.SetActive(false);
            hintDismissed = onHintDismissed;
            // One-time discovery hint. Sits under the choice buttons, which stop at .16, and is
            // itself the dismiss control so the tip costs no extra chrome.
            if (string.IsNullOrEmpty(hint))
            {
                HideHint();
            }
            else if (hintText != null)
            {
                hintText.text = hint;
                hintRoot.SetActive(true);
            }
        }

        /// <summary>Removes the discovery hint if it is currently on screen.</summary>
        public void HideHint()
        {
            if (hintRoot == null)
                return;
            hintRoot.SetActive(false);
        }

    }
}
