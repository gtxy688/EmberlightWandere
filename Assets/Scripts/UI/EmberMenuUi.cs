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
            brandBg.color = new Color(1f, .61f, .19f, 1f);
            brandBg.raycastTarget = false;
            if (EmberArt.Flame != null)
            {
                brandBg.sprite = EmberArt.Flame;
                brandBg.type = Image.Type.Simple;
                brandBg.preserveAspect = true;
                var coreGo = new GameObject("Flame heart", typeof(RectTransform), typeof(Image));
                coreGo.transform.SetParent(brandRoot.transform, false);
                var cr = coreGo.GetComponent<RectTransform>();
                cr.anchorMin = new Vector2(.28f, .10f);
                cr.anchorMax = new Vector2(.72f, .64f);
                cr.offsetMin = cr.offsetMax = Vector2.zero;
                var ci = coreGo.GetComponent<Image>();
                ci.sprite = EmberArt.Flame;
                ci.color = new Color(1f, .92f, .63f, 1f);
                ci.type = Image.Type.Simple;
                ci.raycastTarget = false;
            }
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

        public void Show(string title, string desc, string[] labels, UnityEngine.Events.UnityAction[] actions, bool showBrand = false)
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
            g.GetComponent<Image>().color = new Color(.18f, .26f, .32f);
            g.GetComponent<Button>().onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); if (action != null) action(); });
            Label("Label", g.transform, label, 23, new Vector2(.035f, .05f), new Vector2(.965f, .95f));
        }
    }
}
