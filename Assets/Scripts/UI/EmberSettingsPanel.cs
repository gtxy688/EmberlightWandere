using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    /// <summary>Settings overlay: music/sfx volume + continue/back. Used from pause and main menu.</summary>
    public sealed class EmberSettingsPanel : MonoBehaviour
    {
        const float SfxPreviewInterval = 0.12f;

        TMP_FontAsset font;
        GameObject root;
        Action onClose;
        float nextSfxPreview;

        static readonly Color PanelBg = new Color(0.025f, 0.045f, 0.075f, 0.96f);
        static readonly Color BarBg = new Color(0.12f, 0.18f, 0.24f, 1f);
        static readonly Color Fill = new Color(0.95f, 0.70f, 0.34f, 1f);
        static readonly Color Handle = new Color(0.98f, 0.88f, 0.69f, 1f);
        static readonly Color Ink = new Color(0.98f, 0.88f, 0.69f, 1f);

        public void Initialize(TMP_FontAsset chineseFont) { font = chineseFont; }

        public void Hide()
        {
            if (root == null) return;
            root.SetActive(false);
            Destroy(root);
            root = null;
            onClose = null;
        }

        /// <param name="close">Continue battle or return to previous screen.</param>
        /// <param name="fromPause">true: button = continue battle; false: back.</param>
        public void Show(Action close, bool fromPause)
        {
            Hide();
            onClose = close;
            EmberAudio.Ensure();

            root = new GameObject("Settings", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            var rr = root.GetComponent<RectTransform>();
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = Vector2.one;
            rr.offsetMin = rr.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = PanelBg;
            root.transform.SetAsLastSibling();

            Label(root.transform, "\u8bbe\u7f6e", 36f, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.90f));

            Label(root.transform, "\u97f3\u4e50", 22f, new Vector2(0.08f, 0.48f), new Vector2(0.28f, 0.56f));
            MakeSlider(root.transform, new Vector2(0.30f, 0.48f), new Vector2(0.92f, 0.56f),
                EmberAudio.Ensure().MusicVolume, v => EmberAudio.Ensure().MusicVolume = v);

            Label(root.transform, "\u97f3\u6548", 22f, new Vector2(0.08f, 0.36f), new Vector2(0.28f, 0.44f));
            MakeSlider(root.transform, new Vector2(0.30f, 0.36f), new Vector2(0.92f, 0.44f),
                EmberAudio.Ensure().SfxVolume, OnSfxChanged);

            string btn = fromPause ? "\u7ee7\u7eed\u6218\u6597" : "\u8fd4\u56de";
            MakeButton(root.transform, btn, new Vector2(0.12f, 0.18f), new Vector2(0.88f, 0.30f), () =>
            {
                EmberAudio.Ensure().PlayUiClick();
                var cb = onClose;
                Hide();
                if (cb != null) cb();
            });
        }

        void OnSfxChanged(float v)
        {
            EmberAudio.Ensure().SfxVolume = v;
            if (Time.unscaledTime < nextSfxPreview) return;
            nextSfxPreview = Time.unscaledTime + SfxPreviewInterval;
            EmberAudio.Ensure().PlayUiClick();
        }

        TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 amin, Vector2 amax)
        {
            var g = new GameObject("Label", typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = Ink;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        Slider MakeSlider(Transform parent, Vector2 amin, Vector2 amax, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var g = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = r.offsetMax = Vector2.zero;

            var bg = ChildImage(g.transform, "Background", BarBg);
            Stretch(bg);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(g.transform, false);
            var fa = fillArea.GetComponent<RectTransform>();
            fa.anchorMin = new Vector2(0f, 0.25f);
            fa.anchorMax = new Vector2(1f, 0.75f);
            fa.offsetMin = new Vector2(8f, 0f);
            fa.offsetMax = new Vector2(-8f, 0f);

            var fill = ChildImage(fillArea.transform, "Fill", Fill);
            Stretch(fill);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(g.transform, false);
            var ha = handleArea.GetComponent<RectTransform>();
            ha.anchorMin = Vector2.zero;
            ha.anchorMax = Vector2.one;
            ha.offsetMin = new Vector2(8f, 0f);
            ha.offsetMax = new Vector2(-8f, 0f);

            var handle = ChildImage(handleArea.transform, "Handle", Handle);
            var hr = handle.GetComponent<RectTransform>();
            hr.sizeDelta = new Vector2(28f, 28f);

            var slider = g.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = hr;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = Mathf.Clamp01(value);
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        static Image ChildImage(Transform parent, string name, Color color)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(parent, false);
            var img = g.GetComponent<Image>();
            img.color = color;
            return img;
        }

        static void Stretch(Component c)
        {
            var r = c.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        void MakeButton(Transform parent, string label, Vector2 amin, Vector2 amax, UnityEngine.Events.UnityAction action)
        {
            var g = new GameObject("Button " + label, typeof(RectTransform), typeof(Image), typeof(Button));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = r.offsetMax = Vector2.zero;
            g.GetComponent<Image>().color = new Color(0.18f, 0.26f, 0.32f);
            g.GetComponent<Button>().onClick.AddListener(action);
            Label(g.transform, label, 24f, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
        }
    }
}
