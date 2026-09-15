using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    public sealed class EmberHud
    {
        public readonly GameObject Root;
        readonly Image health, waveFill, bossFill, shield;
        readonly TextMeshProUGUI shieldText;
        readonly TextMeshProUGUI healthText, waveText, clock, bossText;
        readonly GameObject bossRoot, encounterRoot;
        readonly TextMeshProUGUI encounterLabel;
        readonly TMP_FontAsset font;

        public EmberHud(Transform parent, TMP_FontAsset fontAsset, System.Action pause)
        {
            font = fontAsset;
            Root = Box(parent, "Battle HUD", new Vector2(.04f, .83f), new Vector2(.96f, .995f), new Color(.02f, .035f, .055f, .9f)).gameObject;
            var frame = Root.GetComponent<Image>();
            frame.sprite = EmberUiArt.Get(EmberUiArt.Piece.Panel);
            frame.color = Color.white;
            frame.pixelsPerUnitMultiplier = 4f;
            clock = Text(Root.transform, font, "", 14, new Vector2(.055f, .76f), new Vector2(.80f, .95f));
            clock.enableAutoSizing = true;
            clock.fontSizeMin = 10;
            clock.fontSizeMax = 14;
            clock.overflowMode = TextOverflowModes.Ellipsis;
            health = Bar(Root.transform, "Life", new Vector2(.06f, .46f), new Vector2(.94f, .66f), new Color(.72f, .22f, .22f));
            healthText = Text(health.transform.parent, font, "", 16, Vector2.zero, Vector2.one);
            shield = Bar(Root.transform, "Shield", new Vector2(.06f, .35f), new Vector2(.94f, .40f), new Color(.20f, .72f, .96f));
            shieldText = Text(Root.transform, font, "", 12, new Vector2(.60f, .235f), new Vector2(.94f, .35f));
            shieldText.alignment = TextAlignmentOptions.Right;
            waveFill = Bar(Root.transform, "Wave", new Vector2(.06f, .08f), new Vector2(.94f, .115f), new Color(.94f, .66f, .22f));
            waveText = Text(Root.transform, font, "", 13, new Vector2(.06f, .115f), new Vector2(.94f, .235f));
            var b = Box(Root.transform, "Pause", new Vector2(.82f, .74f), new Vector2(.95f, .95f), new Color(.12f, .19f, .25f));
            b.sprite = EmberCardFrames.Get(EmberRarity.Silver);
            b.color = Color.white;
            b.pixelsPerUnitMultiplier = 7f;
            Text(b.transform, font, "\u6682\u505c", 13, Vector2.zero, Vector2.one);
            b.gameObject.AddComponent<Button>().onClick.AddListener(() => pause());
            bossRoot = Box(parent, "Boss HUD", new Vector2(.10f, .745f), new Vector2(.90f, .815f), new Color(.025f, .02f, .045f, .93f)).gameObject;
            var bossFrame = bossRoot.GetComponent<Image>();
            bossFrame.sprite = EmberCardFrames.Get(EmberRarity.Gold);
            bossFrame.color = Color.white;
            bossFrame.pixelsPerUnitMultiplier = 5f;
            bossText = Text(bossRoot.transform, font, "", 16, new Vector2(.02f, .47f), new Vector2(.98f, 1));
            bossFill = Bar(bossRoot.transform, "Boss life", new Vector2(.04f, .13f), new Vector2(.96f, .42f), new Color(.83f, .27f, .58f));
            bossRoot.SetActive(false);
            encounterRoot = Box(Root.transform, "Encounter banner", new Vector2(.06f, -.40f), new Vector2(.94f, -.06f), new Color(.12f, .04f, .10f, .92f)).gameObject;
            encounterRoot.GetComponent<Image>().raycastTarget = false;
            encounterLabel = Text(encounterRoot.transform, font, "", 21, Vector2.zero, Vector2.one);
            encounterRoot.SetActive(false);
        }

        public void Set(float hp, float max, RunProgress progress, float seconds, int kills, int wave, int waveRemaining, int waveQuota)
        {
            shield.fillAmount = progress.ShieldCapacity > 0 ? progress.Shield / progress.ShieldCapacity : 0;
            shieldText.text = "护盾 " + progress.Shield.ToString("0");
            health.fillAmount = Mathf.Clamp01(hp / max);
            healthText.text = "\u751f\u547d\u0020\u0020" + Mathf.Max(0, hp).ToString("0") + " / " + max.ToString("0");
            float progress01 = waveQuota > 0 ? 1f - Mathf.Clamp01((float)waveRemaining / waveQuota) : 1f;
            if (waveQuota <= 0) progress01 = 1f;
            waveFill.fillAmount = progress01;
            if (waveQuota > 0)
                waveText.text = "\u7b2c\u0020" + wave + "\u0020\u6ce2\u0020\u0020\u00b7\u0020\u0020\u5269\u4f59\u0020" + waveRemaining + "\u0020/\u0020" + waveQuota;
            else
                waveText.text = "\u7b2c\u0020" + wave + "\u0020\u6ce2\u0020\u0020\u00b7\u0020\u0020Boss";
            string slots = progress != null
                ? ("     \u6b66\u5668 " + progress.OwnedWeaponCount + "/" + progress.WeaponSlots
                   + "     \u5e78\u8fd0 " + Mathf.RoundToInt(progress.Luck))
                : "";
            clock.text = ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00")
                + "     \u51fb\u6740 " + kills

                + slots;
        }

        public void Stage(int wave, int total, string difficulty, string name)
        {
            waveText.text = difficulty + " · " + wave + "/" + total + " 波 · " + name;
        }

        public void Encounter(string message)
        {
            bool show = !string.IsNullOrEmpty(message);
            encounterRoot.SetActive(show);
            if (show && encounterLabel.text != message) encounterLabel.text = message;
        }

        public void Show(bool visible) { Root.SetActive(visible); if (!visible) bossRoot.SetActive(false); }
        public void Boss(float ratio, string phase, bool show)
        {
            bossRoot.SetActive(show);
            bossFill.fillAmount = Mathf.Clamp01(ratio);
            bossText.text = "\u957f\u591c\u5b88\u536b\u0020\u0020\u00b7\u0020\u0020" + phase;
        }

        public static Image Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var im = g.GetComponent<Image>();
            im.sprite = EmberArt.Panel; im.type = Image.Type.Sliced; im.color = color;
            return im;
        }

        public static Image Bar(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var track = Box(parent, name + " track", min, max, new Color(.04f, .075f, .105f));
            var fill = Box(track.transform, name + " fill", Vector2.zero, Vector2.one, color);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.raycastTarget = false;
            return fill;
        }

        public static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string value, float size, Vector2 min, Vector2 max)
        {
            var g = new GameObject("Label", typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = value; t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(1, .92f, .76f);
            t.raycastTarget = false;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
    }
}
