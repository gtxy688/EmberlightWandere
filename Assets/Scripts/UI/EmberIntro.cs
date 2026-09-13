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
            var title = EmberHud.Text(bg.transform, fallback, "EmberlightWandere", 27, new Vector2(.03f, .49f), new Vector2(.97f, .60f));
            prompt = EmberHud.Text(bg.transform, fallback, "Loading...", 20, new Vector2(.1f, .24f), new Vector2(.9f, .32f));
            var progress = EmberHud.Bar(bg.transform, "Loading", new Vector2(.16f, .20f), new Vector2(.84f, .214f), new Color(.95f, .67f, .27f));
            progress.fillAmount = 0;
            float began = Time.unscaledTime;

            // Empty static SDF is unusable — bake a dynamic TMP font and pre-add every UI glyph.
            progress.fillAmount = .4f;
            yield return null;
            var font = EmberFonts.CreateChinese();
            if (font == null)
            {
                prompt.text = "Font load failed";
                yield break;
            }

            title.font = font;
            prompt.font = font;
            prompt.text = "\u70b9\u4eae\u706f\u706b\u2026";
            progress.fillAmount = .7f;

            var mark = EmberHud.Box(bg.transform, "Flame emblem", new Vector2(.43f, .63f), new Vector2(.57f, .76f), new Color(1, .61f, .19f));
            mark.sprite = EmberArt.Flame; mark.type = Image.Type.Simple; mark.preserveAspect = true;
            var core = EmberHud.Box(mark.transform, "Flame heart", new Vector2(.28f, .10f), new Vector2(.72f, .64f), new Color(1, .92f, .63f));
            core.sprite = EmberArt.Flame; core.type = Image.Type.Simple;

            var warm = new[] { EmberArt.Glow, EmberArt.Panel, EmberArt.Ring, EmberVisuals.Disc };
            yield return null;
            prepared(font);
            progress.fillAmount = 1;
            while (Time.unscaledTime - began < 1.4f) yield return null;

            title.text = "\u70ec\u0020\u706f\u0020\u884c\u0020\u8005";
            title.fontSize = 40;
            EmberHud.Text(bg.transform, font, "EmberlightWandere", 23, new Vector2(.04f, .435f), new Vector2(.96f, .50f));
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
