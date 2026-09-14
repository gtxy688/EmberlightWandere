using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    public sealed class EmberIntro : MonoBehaviour
    {
        public bool Ready { get; private set; }
        Action continueAction;
        GameObject screen;
        TextMeshProUGUI prompt;
        float readyAt;

        public void Begin(Action<TMP_FontAsset> prepared, Action enter)
        {
            continueAction = enter;
            StartCoroutine(Load(prepared));
        }

        IEnumerator Load(Action<TMP_FontAsset> prepared)
        {
            screen = new GameObject("Loading and title", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            screen.transform.SetParent(transform, false);
            screen.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            screen.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = screen.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = .5f;
            var bg = EmberHud.Box(screen.transform, "Night sky", Vector2.zero, Vector2.one, new Color(.025f, .045f, .07f));

            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            var titleEn = EmberHud.Text(bg.transform, fallback, "EmberlightWanderer", 26, new Vector2(.04f, .52f), new Vector2(.96f, .62f));
            TextMeshProUGUI titleCn = null;
            prompt = EmberHud.Text(bg.transform, fallback, "Loading...", 20, new Vector2(.1f, .30f), new Vector2(.9f, .38f));
            var progress = EmberHud.Bar(bg.transform, "Loading", new Vector2(.16f, .20f), new Vector2(.72f, .228f), new Color(.95f, .67f, .27f));
            progress.fillAmount = 0f;
            var pct = EmberHud.Text(bg.transform, fallback, "0%", 18, new Vector2(.74f, .19f), new Vector2(.92f, .24f));
            pct.alignment = TextAlignmentOptions.Left;

            // Progress is stage-driven only (no wall-clock Max). Font bake is sync and can
            // stall several seconds — the old `Max(visual, elapsed/5*0.95)` snapped to 95% after that.
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

            SetProgress(0f);
            yield return null;
            yield return SpinBar(0.15f, 0.6f);

            prompt.text = "Loading fonts...";
            yield return null;
            var font = EmberFonts.CreateChinese();
            if (font == null)
            {
                prompt.text = "Font load failed";
                yield break;
            }

            titleCn = EmberHud.Text(bg.transform, font, "\u70ec\u706f\u884c\u8005", 40, new Vector2(.03f, .58f), new Vector2(.97f, .70f));
            titleCn.ForceMeshUpdate();
            titleEn.font = font;
            titleEn.ForceMeshUpdate();
            titleEn.rectTransform.anchorMin = new Vector2(.04f, .48f);
            titleEn.rectTransform.anchorMax = new Vector2(.96f, .57f);
            prompt.font = font;
            pct.font = font;
            prompt.text = "\u70b9\u4eae\u706f\u706b\u2026";
            prompt.ForceMeshUpdate();
            pct.ForceMeshUpdate();

            // Resume from wherever we were (often still ~15% after a long font bake).
            yield return SpinBar(0.45f, 1.0f);

            var mark = EmberHud.Box(bg.transform, "Flame emblem", new Vector2(.43f, .74f), new Vector2(.57f, .86f), new Color(1, .61f, .19f));
            mark.sprite = EmberArt.Flame; mark.type = Image.Type.Simple; mark.preserveAspect = true;
            var core = EmberHud.Box(mark.transform, "Flame heart", new Vector2(.28f, .10f), new Vector2(.72f, .64f), new Color(1, .92f, .63f));
            core.sprite = EmberArt.Flame; core.type = Image.Type.Simple;

            yield return SpinBar(0.7f, 0.9f);
            prepared(font);

            float remain = Mathf.Max(1.2f, minShow - (Time.unscaledTime - began));
            yield return SpinBar(1f, remain);
            SetProgress(1f);
            yield return new WaitForSecondsRealtime(0.2f);

            EmberHud.Text(bg.transform, font, "\u63d0\u706f\u5165\u591c\uff0c\u4ee5\u706b\u7834\u6653\u3002", 21, new Vector2(.08f, .38f), new Vector2(.92f, .44f));
            progress.transform.parent.gameObject.SetActive(false);
            pct.gameObject.SetActive(false);
            prompt.text = "\u6309\u4efb\u610f\u952e\u7ee7\u7eed\u0020\u00b7\u0020\u70b9\u51fb\u5c4f\u5e55\u5f00\u59cb";
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
            screen.SetActive(false);
            continueAction();
            Destroy(screen);
            Destroy(this);
        }
    }
}