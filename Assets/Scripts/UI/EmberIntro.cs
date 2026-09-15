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

            var mark = EmberHud.Box(bg.transform, "Wanderer emblem", new Vector2(.37f,.74f),new Vector2(.63f,.89f),Color.white);
            mark.sprite=EmberWorldArt.Get(8);mark.type=Image.Type.Simple;mark.preserveAspect=true;

            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            var titleEn = EmberHud.Text(bg.transform, fallback, "EmberlightWanderer", 26, new Vector2(.04f, .48f), new Vector2(.96f, .57f));
            TextMeshProUGUI titleCn = EmberHud.Text(bg.transform, fallback, "", 40, new Vector2(.03f, .58f), new Vector2(.97f, .70f));
            prompt = EmberHud.Text(bg.transform, fallback, "Loading...", 20, new Vector2(.1f, .30f), new Vector2(.9f, .38f));
            var progress = EmberHud.Bar(bg.transform, "Loading", new Vector2(.16f, .20f), new Vector2(.72f, .228f), new Color(.95f, .67f, .27f));
            progress.fillAmount = 0f;
            var pct = EmberHud.Text(bg.transform, fallback, "0%", 18, new Vector2(.74f, .19f), new Vector2(.92f, .24f));
            pct.alignment = TextAlignmentOptions.Left;

            // The labels are created here with the built-in Latin font and re-pointed at the
            // Chinese atlas a few lines below, before any frame has been presented. There is no
            // reveal step to hold them back: with the pre-baked atlas the font is a serialized
            // asset, so the swap costs nothing and the wording is correct on frame one.

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

            // ── Font first, animation second ─────────────────────────────────────────────────
            // This used to call CreateChinese() only after a 0.5 s SpinBar, so the earliest the
            // wording could appear was half a second in -- which is exactly what read as "the
            // Chinese lags by half a beat". Nothing was slow; the text was simply scheduled after
            // an animation. With the pre-baked atlas the font is a serialized asset, so it is
            // loaded here, on the first frame, and every wait below happens with the text already
            // on screen.
            prompt.text = "Loading fonts...";
            var font = EmberFonts.CreateChinese();

            // ── Wording ──────────────────────────────────────────────────────────────────────
            // Set once here rather than inside a reveal action: the fallback and the success path
            // differ only in what the text says, so there is no reason for two code paths to own
            // the same three labels.
            titleCn.font = font != null ? font : fallback;
            titleCn.text = font != null ? "\u70ec\u706f\u884c\u8005" : "EmberlightWanderer";
            titleCn.fontSize = font != null ? 40 : 28;
            titleEn.font = titleCn.font;
            prompt.font = titleCn.font;
            pct.font = titleCn.font;
            prompt.text = font != null ? "\u70b9\u4eae\u706f\u706b\u2026" : "Font load failed \u2014 tap to continue";
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
                progress.transform.parent.gameObject.SetActive(false);
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
