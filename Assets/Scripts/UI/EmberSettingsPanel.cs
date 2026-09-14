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
        Action onReturnToCamp;
        Action onSpeedChanged;
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
            onReturnToCamp = null;
            onSpeedChanged = null;
        }

        /// <param name="close">Continue battle or return to previous screen.</param>
        /// <param name="fromPause">true: button = continue battle; false: back.</param>
        /// <param name="returnToCamp">When supplied from pause, ends the current run and opens the camp.</param>
        /// <param name="onSpeedChanged">Notified after the player picks a different game speed.</param>
        public void Show(Action close, bool fromPause, Action returnToCamp = null, Action onSpeedChanged = null)
        {
            Hide();
            onClose = close;
            onReturnToCamp = returnToCamp;
            this.onSpeedChanged = onSpeedChanged;
            EmberAudio.Ensure();

            root = new GameObject("Settings", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            var rr = root.GetComponent<RectTransform>();
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = Vector2.one;
            rr.offsetMin = rr.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = PanelBg;
            root.transform.SetAsLastSibling();

            Label(root.transform, "\u8bbe\u7f6e", 36f, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.92f));

            MakeVolumeCard("背景音乐", "调节背景旋律", .615f, .745f,
                EmberAudio.Ensure().MusicVolume, v => EmberAudio.Ensure().MusicVolume = v);
            MakeVolumeCard("游戏音效", "调节攻击与点击声", .475f, .605f,
                EmberAudio.Ensure().SfxVolume, OnSfxChanged);
            MakeSpeedCard(.325f, .455f);

            string btn = fromPause ? "\u7ee7\u7eed\u6218\u6597" : "\u8fd4\u56de";
            MakeButton(root.transform, btn, new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.295f), () =>
            {
                EmberAudio.Ensure().PlayUiClick();
                var cb = onClose;
                Hide();
                if (cb != null) cb();
            });

            if (fromPause && onReturnToCamp != null)
            {
                bool awaitingConfirmation = false;
                TextMeshProUGUI campLabel = null;
                campLabel = MakeButton(root.transform, "返回营地", new Vector2(0.12f, 0.085f), new Vector2(0.88f, 0.18f), () =>
                {
                    EmberAudio.Ensure().PlayUiClick();
                    if (!awaitingConfirmation)
                    {
                        awaitingConfirmation = true;
                        campLabel.text = "再次点击确认放弃本局";
                        campLabel.color = new Color(1f, 0.65f, 0.32f);
                        return;
                    }

                    var cb = onReturnToCamp;
                    Hide();
                    if (cb != null) cb();
                });
            }
        }

        /// <summary>
        /// 1x-5x picker. A row of discrete segments rather than a slider because the feature is
        /// explicitly five fixed steps, and five tap targets read better than a continuous drag.
        /// </summary>
        void MakeSpeedCard(float bottom, float top)
        {
            var card = ChildImage(root.transform, "Game speed", Color.white);
            card.sprite = EmberUiArt.Get(EmberUiArt.Piece.Panel);
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 3f;
            card.rectTransform.anchorMin = new Vector2(.08f, bottom);
            card.rectTransform.anchorMax = new Vector2(.92f, top);
            card.rectTransform.offsetMin = card.rectTransform.offsetMax = Vector2.zero;

            var name = Label(card.transform, "游戏速度", 23, new Vector2(.10f, .60f), new Vector2(.60f, .84f));
            name.alignment = TextAlignmentOptions.Left;
            var caption = Label(card.transform, "整体时间流逝的倍率", 13, new Vector2(.10f, .37f), new Vector2(.72f, .58f));
            caption.color = new Color(.60f, .69f, .75f);
            caption.alignment = TextAlignmentOptions.Left;
            var current = Label(card.transform, "", 22, new Vector2(.74f, .60f), new Vector2(.90f, .84f));
            current.color = Fill;

            const int segments = GameSpeed.MaxMultiplier - GameSpeed.MinMultiplier + 1;
            var faces = new Image[segments];
            var texts = new TextMeshProUGUI[segments];

            Action refresh = () =>
            {
                int speed = GameSpeed.Multiplier;
                current.text = speed + "\u500d";
                for (int i = 0; i < segments; i++)
                {
                    bool active = i + GameSpeed.MinMultiplier == speed;
                    faces[i].color = active ? Fill : BarBg;
                    texts[i].color = active ? new Color(.10f, .07f, .03f) : Ink;
                }
            };

            for (int i = 0; i < segments; i++)
            {
                int value = i + GameSpeed.MinMultiplier;
                float w = 1f / segments;
                var seg = ChildImage(card.transform, "Speed " + value, BarBg);
                seg.sprite = EmberArt.Panel;
                seg.type = Image.Type.Sliced;
                var sr = seg.rectTransform;
                sr.anchorMin = new Vector2(.10f + i * w * .80f, .10f);
                sr.anchorMax = new Vector2(.10f + (i + 1) * w * .80f - .012f, .34f);
                sr.offsetMin = sr.offsetMax = Vector2.zero;
                var label = Label(seg.transform, value + "x", 18, Vector2.zero, Vector2.one);
                faces[i] = seg;
                texts[i] = label;

                var btn = seg.gameObject.AddComponent<Button>();
                btn.targetGraphic = seg;
                int captured = value;
                btn.onClick.AddListener(() =>
                {
                    EmberAudio.Ensure().PlayUiClick();
                    GameSpeed.Multiplier = captured;
                    refresh();
                    if (onSpeedChanged != null) onSpeedChanged();
                });
            }

            refresh();
        }

        void OnSfxChanged(float v)
        {
            EmberAudio.Ensure().SfxVolume = v;
            if (Time.unscaledTime < nextSfxPreview) return;
            nextSfxPreview = Time.unscaledTime + SfxPreviewInterval;
            EmberAudio.Ensure().PlayUiClick();
        }

        void MakeVolumeCard(string title, string hint, float bottom, float top, float value,
            UnityEngine.Events.UnityAction<float> changed)
        {
            var card = ChildImage(root.transform, title, Color.white);
            card.sprite = EmberUiArt.Get(EmberUiArt.Piece.Panel);
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 3f;
            card.rectTransform.anchorMin = new Vector2(.08f, bottom);
            card.rectTransform.anchorMax = new Vector2(.92f, top);
            card.rectTransform.offsetMin = card.rectTransform.offsetMax = Vector2.zero;
            var name = Label(card.transform, title, 23, new Vector2(.10f,.64f), new Vector2(.68f,.88f));
            name.alignment = TextAlignmentOptions.Left;
            var percent = Label(card.transform, "", 22, new Vector2(.70f,.64f), new Vector2(.90f,.88f));
            percent.color = Fill;
            Action<float> update = v => percent.text = v <= .001f ? "静音" : Mathf.RoundToInt(v * 100) + "%";
            update(value);
            MakeSlider(card.transform, new Vector2(.10f,.25f), new Vector2(.90f,.62f), value,
                v => { update(v); changed(v); });
            var caption = Label(card.transform, hint, 13, new Vector2(.10f,.08f), new Vector2(.90f,.25f));
            caption.color = new Color(.60f,.69f,.75f);
            caption.alignment = TextAlignmentOptions.Left;
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

            // Keep a generous invisible touch target around the thin visible track.
            g.AddComponent<Image>().color = Color.clear;
            var bg = ChildImage(g.transform, "Background", BarBg);
            Stretch(bg);
            bg.sprite = EmberArt.Panel;
            bg.type = Image.Type.Sliced;
            bg.rectTransform.anchorMin = new Vector2(0,.40f);
            bg.rectTransform.anchorMax = new Vector2(1,.60f);
            bg.rectTransform.offsetMin = new Vector2(16f, 0f);
            bg.rectTransform.offsetMax = new Vector2(-16f, 0f);
            bg.raycastTarget = false;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(g.transform, false);
            var fa = fillArea.GetComponent<RectTransform>();
            fa.anchorMin = new Vector2(0f, 0.42f);
            fa.anchorMax = new Vector2(1f, 0.58f);
            fa.offsetMin = new Vector2(16f, 0f);
            fa.offsetMax = new Vector2(-16f, 0f);

            var fill = ChildImage(fillArea.transform, "Fill", Fill);
            Stretch(fill);
            fill.sprite = EmberArt.Panel;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(g.transform, false);
            var ha = handleArea.GetComponent<RectTransform>();
            ha.anchorMin = new Vector2(0,.5f);
            ha.anchorMax = new Vector2(1,.5f);
            ha.offsetMin = new Vector2(16f, 0f);
            ha.offsetMax = new Vector2(-16f, 0f);

            var handle = ChildImage(handleArea.transform, "Handle", Handle);
            var hr = handle.GetComponent<RectTransform>();
            hr.sizeDelta = new Vector2(32f, 32f);
            handle.sprite = EmberVisuals.Disc;
            var center = ChildImage(handle.transform, "Orange center", Fill);
            Stretch(center);
            center.rectTransform.offsetMin = new Vector2(7,7);
            center.rectTransform.offsetMax = new Vector2(-7,-7);
            center.sprite = EmberVisuals.Disc;
            center.raycastTarget = false;

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

        TextMeshProUGUI MakeButton(Transform parent, string label, Vector2 amin, Vector2 amax, UnityEngine.Events.UnityAction action)
        {
            var g = new GameObject("Button " + label, typeof(RectTransform), typeof(Image), typeof(Button));
            g.transform.SetParent(parent, false);
            var r = g.GetComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = r.offsetMax = Vector2.zero;
            var face = g.GetComponent<Image>();
            face.color = Color.white;
            face.sprite = EmberUiArt.Get(EmberUiArt.Piece.Secondary);
            face.type = Image.Type.Sliced;
            face.pixelsPerUnitMultiplier = 2f;
            g.GetComponent<Button>().onClick.AddListener(action);
            return Label(g.transform, label, 24f, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
        }
    }
}
