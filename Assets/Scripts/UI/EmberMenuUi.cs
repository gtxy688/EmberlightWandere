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

        TMP_FontAsset font;
        TextMeshProUGUI heading, summary;
        Transform buttons;
        GameObject brandRoot;
        GameObject hintRoot;
        TextMeshProUGUI hintText;

        public void Build(Transform host, TMP_FontAsset loadedFont, System.Action pause)
        {
            font = loadedFont;
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvas = new GameObject("Emberlight UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(host, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = .5f;

            Safe = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            Safe.SetParent(canvas.transform, false);
            UpdateSafeArea();
            UpgradePanel = Safe.gameObject.AddComponent<EmberUpgradePanel>();
            UpgradePanel.Initialize(font);
            GodsSelect = Safe.gameObject.AddComponent<EmberGodsSelectPanel>();
            GodsSelect.Initialize(font);
            BattleHud = new EmberHud(Safe, font, pause);
            SettingsPanel = Safe.gameObject.AddComponent<EmberSettingsPanel>();
            SettingsPanel.Initialize(font);

            StickBase = new GameObject("Joystick", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            StickBase.SetParent(Safe, false);
            StickBase.sizeDelta = Vector2.one * 120;
            StickBase.GetComponent<Image>().sprite = EmberVisuals.Disc;
            StickBase.GetComponent<Image>().color = new Color(1, 1, 1, .12f);
            StickBase.GetComponent<Image>().raycastTarget = false;
            StickKnob = new GameObject("Knob", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            StickKnob.SetParent(StickBase, false);
            StickKnob.sizeDelta = Vector2.one * 40;
            StickKnob.GetComponent<Image>().sprite = EmberVisuals.Disc;
            StickKnob.GetComponent<Image>().color = new Color(1, .7f, .2f, .7f);
            StickKnob.GetComponent<Image>().raycastTarget = false;
            StickBase.gameObject.SetActive(false);

            Overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            Overlay.transform.SetParent(Safe, false);
            var r = Overlay.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            Overlay.GetComponent<Image>().color = new Color(.025f, .045f, .075f, .95f);
            brandRoot = new GameObject("Brand", typeof(RectTransform));
            brandRoot.transform.SetParent(Overlay.transform, false);
            var brandRt = brandRoot.GetComponent<RectTransform>();
            brandRt.anchorMin = new Vector2(.42f, .88f);
            brandRt.anchorMax = new Vector2(.58f, .98f);
            brandRt.offsetMin = brandRt.offsetMax = Vector2.zero;
            var brandBg = brandRoot.AddComponent<Image>();
            brandRt.anchorMin = new Vector2(.34f, .88f);
            brandRt.anchorMax = new Vector2(.66f, .98f);
            brandBg.color = Color.white;
            brandBg.raycastTarget = false;
            brandBg.sprite = EmberWorldArt.Get(8);
            brandBg.preserveAspect = true;
            brandRoot.SetActive(false);

            heading = Label("Title", Overlay.transform, "", 34, new Vector2(.06f, .72f), new Vector2(.94f, .86f));
            summary = Label("Description", Overlay.transform, "", 19, new Vector2(.06f, .52f), new Vector2(.94f, .70f));
            buttons = new GameObject("Choices", typeof(RectTransform)).transform;
            buttons.SetParent(Overlay.transform, false);
            var b = (RectTransform)buttons;
            b.anchorMin = Vector2.zero; b.anchorMax = Vector2.one; b.offsetMin = b.offsetMax = Vector2.zero;
            Label("Footer", Overlay.transform, "\u70ec\u706f\u884c\u8005\n\u62d6\u52a8\u5c4f\u5e55\u79fb\u52a8\uff0c\u6e05\u6ce2\u9009\u62e9\u5f3a\u5316", 16, new Vector2(.05f, .025f), new Vector2(.95f, .12f));
        }

        public void UpdateSafeArea()
        {
            if (Safe == null) return;
            Rect area = Screen.safeArea;
            Safe.anchorMin = new Vector2(area.x / Screen.width, area.y / Screen.height);
            Safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            Safe.offsetMin = Safe.offsetMax = Vector2.zero;
        }

        public void SetStick(bool active, Vector2 screenOrigin, Vector2 stick)
        {
            StickBase.gameObject.SetActive(active);
            if (!active) return;
            StickBase.position = screenOrigin;
            StickKnob.anchoredPosition = stick * 45;
        }

        public void Show(string title, string desc, string[] labels, UnityEngine.Events.UnityAction[] actions, bool showBrand = false,
            string hint = null, UnityEngine.Events.UnityAction onHintDismissed = null)
        {
            if (UpgradePanel != null) UpgradePanel.Hide();
            if (GodsSelect != null) GodsSelect.Hide();
            if (SettingsPanel != null) SettingsPanel.Hide();
            BattleHud.Show(false);
            Overlay.SetActive(true);
            if (brandRoot != null) brandRoot.SetActive(showBrand);
            heading.text = title;
            summary.text = desc;
            StickBase.gameObject.SetActive(false);
            foreach (Transform child in buttons)
            {
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
            for (int i = 0; i < labels.Length; i++)
                MakeButton(buttons, labels[i], new Vector2(.09f, .43f - i * .135f), new Vector2(.91f, .54f - i * .135f), actions[i]);

            // One-time discovery hint. Sits under the choice buttons, which stop at .16, and is
            // itself the dismiss control so the tip costs no extra chrome.
            if (string.IsNullOrEmpty(hint))
            {
                HideHint();
            }
            else if (hintRoot == null)
            {
                var bar = new GameObject("Hint", typeof(RectTransform), typeof(Image), typeof(Button));
                bar.transform.SetParent(Overlay.transform, false);
                hintRoot = bar;
                var hr = bar.GetComponent<RectTransform>();
                hr.anchorMin = new Vector2(.07f, .045f);
                hr.anchorMax = new Vector2(.93f, .15f);
                hr.offsetMin = hr.offsetMax = Vector2.zero;
                var face = bar.GetComponent<Image>();
                face.color = new Color(.13f, .19f, .25f, .96f);
                face.sprite = EmberUiArt.Get(EmberUiArt.Piece.Panel);
                face.type = Image.Type.Sliced;
                face.pixelsPerUnitMultiplier = 3f;
                hintText = Label("Hint text", bar.transform, hint, 16, new Vector2(.06f, .16f), new Vector2(.94f, .84f));
                hintText.color = new Color(1f, .89f, .66f);
                bar.GetComponent<Button>().onClick.AddListener(() =>
                {
                    EmberAudio.Ensure().PlayUiClick();
                    HideHint();
                    if (onHintDismissed != null) onHintDismissed();
                });
            }
            else if (hintText != null)
            {
                hintText.text = hint;
            }
        }

        /// <summary>Removes the discovery hint if it is currently on screen.</summary>
        public void HideHint()
        {
            if (hintRoot == null) return;
            Object.Destroy(hintRoot);
            hintRoot = null;
            hintText = null;
        }

        TextMeshProUGUI Label(string name, Transform parent, string text, float size, Vector2 anchorMin, Vector2 anchorMax)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size;
            t.color = new Color(.98f, .88f, .69f);
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            return t;
        }

        void MakeButton(Transform parent, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var g = new GameObject("Button " + label, typeof(RectTransform), typeof(Image), typeof(Button));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var face = g.GetComponent<Image>();
            face.sprite = EmberUiArt.Get(parent.childCount == 1 ? EmberUiArt.Piece.Primary : EmberUiArt.Piece.Secondary);
            face.type = Image.Type.Sliced;
            face.pixelsPerUnitMultiplier = 2f;
            face.color = Color.white;
            g.GetComponent<Button>().onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); if (action != null) action(); });
            Label("Label", g.transform, label, 23, new Vector2(.18f, .12f), new Vector2(.88f, .88f));
        }
    }
}
