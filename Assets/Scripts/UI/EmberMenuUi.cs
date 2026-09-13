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
        public EmberHud BattleHud { get; private set; }

        TMP_FontAsset font;
        TextMeshProUGUI heading, summary;
        Transform buttons;

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
            BattleHud = new EmberHud(Safe, font, pause);

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
            heading = Label("Title", Overlay.transform, "", 38, new Vector2(.06f, .71f), new Vector2(.94f, .88f));
            summary = Label("Description", Overlay.transform, "", 21, new Vector2(.06f, .58f), new Vector2(.94f, .72f));
            buttons = new GameObject("Choices", typeof(RectTransform)).transform;
            buttons.SetParent(Overlay.transform, false);
            var b = (RectTransform)buttons;
            b.anchorMin = Vector2.zero; b.anchorMax = Vector2.one; b.offsetMin = b.offsetMax = Vector2.zero;
            Label("Footer", Overlay.transform, "烬灯行者\n拖动屏幕移动，拾取金色余烬升级", 16, new Vector2(.05f, .025f), new Vector2(.95f, .12f));
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

        public void Show(string title, string desc, string[] labels, UnityEngine.Events.UnityAction[] actions)
        {
            if (UpgradePanel != null) UpgradePanel.Hide();
            BattleHud.Show(false);
            Overlay.SetActive(true);
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
            g.GetComponent<Button>().onClick.AddListener(action);
            Label("Label", g.transform, label, 23, new Vector2(.035f, .05f), new Vector2(.965f, .95f));
        }
    }
}
