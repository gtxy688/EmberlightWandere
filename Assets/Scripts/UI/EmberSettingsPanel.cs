using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    /// <summary>设置浮层：音乐/音效音量与继续/返回；暂停界面与主菜单共用。</summary>
    public sealed class EmberSettingsPanel : MonoBehaviour
    {
        const float SfxPreviewInterval = 0.12f;

        [SerializeField] GameObject root;
        Action onClose;
        Action onReturnToCamp;
        Action onSpeedChanged;
        float nextSfxPreview;
        bool awaitingConfirmation;
        [SerializeField] TextMeshProUGUI closeLabel, campLabel, musicPercent, sfxPercent, speedValue;
        [SerializeField] Slider musicSlider, sfxSlider;
        [SerializeField] Image[] speedFaces;
        [SerializeField] TextMeshProUGUI[] speedLabels;
        bool listenersBound;
        Action<float> musicDisplay, sfxDisplay;
        Action refreshSpeed;

        static readonly Color BarBg = new Color(0.12f, 0.18f, 0.24f, 1f);
        static readonly Color Fill = new Color(0.95f, 0.70f, 0.34f, 1f);
        static readonly Color Ink = new Color(0.98f, 0.88f, 0.69f, 1f);

        public void Initialize()
        {
            if (root == null || closeLabel == null || campLabel == null || musicSlider == null || sfxSlider == null ||
                musicPercent == null || sfxPercent == null || speedValue == null || speedFaces == null || speedLabels == null)
                throw new InvalidOperationException("Settings prefab references are incomplete.");
            if (root != null && !listenersBound)
            {
                musicDisplay = v => DisplayVolume(musicPercent, v);
                sfxDisplay = v => DisplayVolume(sfxPercent, v);
                musicSlider.onValueChanged.AddListener(v => { musicDisplay(v); EmberAudio.Ensure().MusicVolume = v; });
                sfxSlider.onValueChanged.AddListener(v => { sfxDisplay(v); OnSfxChanged(v); });
                closeLabel.GetComponentInParent<Button>(true).onClick.AddListener(Close);
                campLabel.GetComponentInParent<Button>(true).onClick.AddListener(ReturnToCamp);
                refreshSpeed = RefreshSpeed;
                for (int i = 0; i < speedFaces.Length; i++)
                {
                    int value = i + GameSpeed.MinMultiplier;
                    speedFaces[i].GetComponent<Button>().onClick.AddListener(() =>
                    {
                        EmberAudio.Ensure().PlayUiClick();
                        GameSpeed.Multiplier = value;
                        RefreshSpeed();
                        onSpeedChanged?.Invoke();
                    });
                }
            }
            listenersBound = true;
            Hide();
        }

        static void DisplayVolume(TextMeshProUGUI label, float value)
        {
            label.text = value <= .001f ? "静音" : Mathf.RoundToInt(value * 100) + "%";
        }

        void RefreshSpeed()
        {
            int speed = GameSpeed.Multiplier;
            speedValue.text = speed + "倍";
            for (int i = 0; i < speedFaces.Length; i++)
            {
                bool active = i + GameSpeed.MinMultiplier == speed;
                speedFaces[i].color = active ? Fill : BarBg;
                speedLabels[i].color = active ? new Color(.10f, .07f, .03f) : Ink;
            }
        }

        void Close()
        {
            EmberAudio.Ensure().PlayUiClick();
            var callback = onClose;
            Hide();
            callback?.Invoke();
        }

        void ReturnToCamp()
        {
            if (onReturnToCamp == null) return;
            EmberAudio.Ensure().PlayUiClick();
            if (!awaitingConfirmation)
            {
                awaitingConfirmation = true;
                campLabel.text = "再次点击确认放弃本局";
                campLabel.color = new Color(1f, .65f, .32f);
                return;
            }
            var callback = onReturnToCamp;
            Hide();
            callback?.Invoke();
        }

        public void Hide()
        {
            if (root == null) return;
            root.SetActive(false);
            awaitingConfirmation = false;
            onClose = null;
            onReturnToCamp = null;
            onSpeedChanged = null;
        }

        /// <param name="fromPause">true：按钮为「继续战斗」；false：「返回」。</param>
        /// <param name="returnToCamp">从暂停界面传入时，结束本局并打开营地。</param>
        public void Show(Action close, bool fromPause, Action returnToCamp = null, Action onSpeedChanged = null)
        {
            Hide();
            onClose = close;
            onReturnToCamp = returnToCamp;
            this.onSpeedChanged = onSpeedChanged;
            var audio = EmberAudio.Ensure();
            nextSfxPreview = 0;
            if (root != null)
            {
                closeLabel.text = fromPause ? "继续战斗" : "返回";
                campLabel.text = "返回营地";
                campLabel.color = Ink;
                campLabel.transform.parent.gameObject.SetActive(fromPause && returnToCamp != null);
                musicSlider.SetValueWithoutNotify(audio.MusicVolume);
                sfxSlider.SetValueWithoutNotify(audio.SfxVolume);
                musicDisplay(audio.MusicVolume);
                sfxDisplay(audio.SfxVolume);
                refreshSpeed();
                root.transform.SetAsLastSibling();
                root.SetActive(true);
                return;
            }

        }

        void OnSfxChanged(float v)
        {
            EmberAudio.Ensure().SfxVolume = v;
            if (Time.unscaledTime < nextSfxPreview) return;
            nextSfxPreview = Time.unscaledTime + SfxPreviewInterval;
            EmberAudio.Ensure().PlayUiClick();
        }

    }
}
