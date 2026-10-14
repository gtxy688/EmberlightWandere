using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace Emberlight
{
    public sealed class EmberIntro : MonoBehaviour
    {
        public const string ChineseTitle = "烬灯行者";
        public const string EnglishTitle = "EmberlightWanderer";
        public const string LoadingPrompt = "点亮灯火…";
        public const string Tagline = "提灯入夜，以火破晓。";
        public const string ContinuePrompt = "按任意键继续 · 点击屏幕开始";

        public bool Ready { get; private set; }
        Action continueAction;
        EmberIntroView view;
        TextMeshProUGUI prompt;
        float readyAt;

        public void Begin(EmberIntroView loadingView, Action<TMP_FontAsset> prepared, Action enter)
        {
            loadingView.ValidateReferences();
            view = loadingView;
            view.gameObject.SetActive(false);
            prompt = view.Prompt;
            continueAction = enter;
            StartCoroutine(Load(prepared));
        }

        IEnumerator Load(Action<TMP_FontAsset> prepared)
        {
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            var titleEn = view.EnglishTitle;
            var titleCn = view.ChineseTitle;
            var progress = view.ProgressFill;
            var pct = view.Percent;
            view.Tagline.gameObject.SetActive(false);
            view.ProgressRoot.SetActive(true);
            pct.gameObject.SetActive(true);
            progress.fillAmount = 0f;
            pct.text = "0%";

            const float minShow = 5f;
            float began = Time.unscaledTime;
            float visual = 0f;

            void SetProgress(float amount)
            {
                amount = Mathf.Clamp01(amount);
                visual = amount;
                progress.fillAmount = amount;
                pct.text = Mathf.RoundToInt(amount * 100f) + "%";
            }

            IEnumerator SpinBar(float target, float holdSeconds)
            {
                float start = visual;
                target = Mathf.Clamp01(target);
                float t0 = Time.unscaledTime;
                if (holdSeconds <= 0f)
                {
                    SetProgress(target);
                    yield break;
                }
                while (true)
                {
                    float u = Mathf.Clamp01((Time.unscaledTime - t0) / holdSeconds);
                    SetProgress(Mathf.Lerp(start, target, u));
                    if (u >= 1f) break;
                    yield return null;
                }
                SetProgress(target);
            }

            // 保留 ValidateBaked：先校验/回落字体，再激活序列化视图，不直接信任资产中的字体。
            prompt.text = "Loading fonts...";
            var font = EmberFonts.CreateChinese();
            titleCn.font = font != null ? font : fallback;
            titleCn.text = font != null ? ChineseTitle : EnglishTitle;
            titleEn.font = titleCn.font;
            titleEn.text = EnglishTitle;
            prompt.font = titleCn.font;
            pct.font = titleCn.font;
            view.Tagline.font = titleCn.font;
            view.Tagline.text = Tagline;
            prompt.text = font != null ? LoadingPrompt : "Font load failed \u2014 tap to continue";
            view.gameObject.SetActive(true);
            titleCn.ForceMeshUpdate();
            titleEn.ForceMeshUpdate();
            prompt.ForceMeshUpdate();
            pct.ForceMeshUpdate();

            // Everything the first screen needs is already in the atlas (CreateChinese adds it
            // synchronously). The remainder goes in 48 at a time while this screen is still held
            // open; for a Static atlas this returns immediately.
            yield return EmberFonts.PrewarmInBackground(font);

            // Now the bar. These waits fill the minimum on-screen time with the text already
            // visible instead of before it exists.
            SetProgress(0f);
            yield return null;
            yield return SpinBar(0.15f, 0.5f);

            if (font == null)
            {
                prepared(fallback);
                yield return SpinBar(1f, 0.8f);
                view.ProgressRoot.SetActive(false);
                pct.gameObject.SetActive(false);
                Ready = true;
                readyAt = Time.unscaledTime;
                yield break;
            }

            yield return SpinBar(0.55f, 0.8f);
            prepared(font);

            float remain = Mathf.Max(1.0f, minShow - (Time.unscaledTime - began));
            yield return SpinBar(1f, remain);
            SetProgress(1f);
            yield return new WaitForSecondsRealtime(0.15f);

            view.Tagline.gameObject.SetActive(true);
            view.ProgressRoot.SetActive(false);
            pct.gameObject.SetActive(false);
            prompt.text = ContinuePrompt;
            prompt.ForceMeshUpdate();
            Ready = true;
            readyAt = Time.unscaledTime;
        }

        void Update()
        {
            if (!Ready) return;
            var c = prompt.color;
            c.a = .65f + .35f * Mathf.Sin(Time.unscaledTime * 2);
            prompt.color = c;
            if (Time.unscaledTime - readyAt < .25f) return;
            if (Input.anyKeyDown || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) Continue();
        }

        public void Continue()
        {
            if (!Ready) return;
            Ready = false;
            view.gameObject.SetActive(false);
            continueAction();
            Destroy(this);
        }
    }
}
