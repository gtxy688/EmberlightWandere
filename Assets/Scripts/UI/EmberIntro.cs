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
            // Show CN + EN together the whole time
            var titleCn = EmberHud.Text(bg.transform, fallback, "\u70ec\u706f\u884c\u8005", 40, new Vector2(.03f, .50f), new Vector2(.97f, .62f));
            var titleEn = EmberHud.Text(bg.transform, fallback, "EmberlightWanderer", 22, new Vector2(.04f, .44f), new Vector2(.96f, .51f));
            prompt = EmberHud.Text(bg.transform, fallback, "Loading...", 20, new Vector2(.1f, .26f), new Vector2(.9f, .34f));
            var progress = EmberHud.Bar(bg.transform, "Loading", new Vector2(.16f, .20f), new Vector2(.84f, .214f), new Color(.95f, .67f, .27f));
            progress.fillAmount = 0;
            float began = Time.unscaledTime;
            const float minShow = 1.75f;

            progress.fillAmount = .15f;
            yield return null;

            var font = EmberFonts.CreateChinese();
            if (font == null)
            {
                prompt.text = "Font load failed";
                yield break;
            }

            titleCn.font = font;
            titleEn.font = font;
            prompt.font = font;
            prompt.text = "\u70b9\u4eae\u706f\u706b\u2026";
            progress.fillAmount = .45f;
            yield return null;

            var mark = EmberHud.Box(bg.transform, "Flame emblem", new Vector2(.43f, .65f), new Vector2(.57f, .78f), new Color(1, .61f, .19f));
            mark.sprite = EmberArt.Flame; mark.type = Image.Type.Simple; mark.preserveAspect = true;
            var core = EmberHud.Box(mark.transform, "Flame heart", new Vector2(.28f, .10f), new Vector2(.72f, .64f), new Color(1, .92f, .63f));
            core.sprite = EmberArt.Flame; core.type = Image.Type.Simple;

            progress.fillAmount = .7f;
            yield return null;
            prepared(font);

            // Ease bar to full over remaining time so it never pops in instantly
            while (true)
            {
                float t = (Time.unscaledTime - began) / minShow;
                progress.fillAmount = Mathf.Clamp01(Mathf.Lerp(.7f, 1f, Mathf.InverseLerp(.7f, 1f, Mathf.Max(t, .7f))));
                if (t >= 1f) break;
                progress.fillAmount = Mathf.Clamp01(Mathf.Lerp(0.15f, 1f, t));
                yield return null;
            }
            progress.fillAmount = 1f;
            yield return null;

            EmberHud.Text(bg.transform, font, "\u63d0\u706f\u5165\u591c\uff0c\u4ee5\u706b\u7834\u6653\u3002", 21, new Vector2(.08f, .35f), new Vector2(.92f, .41f));
            progress.transform.parent.gameObject.SetActive(false);
            prompt.text = "\u6309\u4efb\u610f\u952e\u7ee7\u7eed\u0020\u00b7\u0020\u70b9\u51fb\u5c4f\u5e55\u5f00\u59cb";
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