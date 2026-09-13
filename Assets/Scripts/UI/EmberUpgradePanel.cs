using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    public sealed class EmberUpgradePanel : MonoBehaviour
    {
        TMP_FontAsset font;
        GameObject root;
        bool choosing;
        static readonly Color Gold = new Color(.95f, .70f, .34f);
        static readonly Color Ink = new Color(.024f, .042f, .066f);

        public void Initialize(TMP_FontAsset chineseFont) { font = chineseFont; }
        public void Hide() { if (root != null) { root.SetActive(false); Destroy(root); root = null; } }

        public static string Preview(int id, EmberRarity rarity, RunProgress progress)
        {
            float mag = EmberRarityUtil.Magnitude(rarity);
            int pct = EmberRarityUtil.Percent(rarity);
            int count = EmberRarityUtil.CountBonus(rarity);
            switch (id)
            {
                case 0: return "\u706b\u7403\u6570\u91cf\u0020\u0020\u002b" + count + "\u0020\u679a";
                case 1: return "\u653b\u901f\u0020\u0020\u002b" + pct + "%  \u0020\uff08\u5f53\u524d\u5408\u8ba1\u0020\u002b" + Mathf.RoundToInt(progress.AttackSpeedBonus * 100) + "\u0025\uff09";
                case 2: return "\u4f24\u5bb3\u0020\u0020\u002b" + pct + "%  \u0020\uff08\u5f53\u524d\u5408\u8ba1\u0020\u002b" + Mathf.RoundToInt(progress.DamageBonus * 100) + "\u0025\uff09";
                case 3: return "\u73af\u7ed5\u706b\u79cd\u0020\u0020\u002b" + count + "\u0020\u9897";
                case 4: return "\u71c3\u5730\u5f3a\u5ea6\u0020\u0020\u002b" + pct + "%";
                case 5: return "\u62fe\u53d6\u8303\u56f4\u0020\u0020\u002b" + (mag * 2f).ToString("0.0");
                case 6: return "\u79fb\u901f\u0020\u0020\u002b" + pct + "%";
                case 7: return "\u751f\u547d\u4e0a\u9650\u0020\u0020\u002b" + Mathf.RoundToInt(100f * mag) + "\uff0c\u5e76\u6062\u590d\u0020" + Mathf.RoundToInt(progress.HealAmount(rarity));
                case 8: return "\u706b\u6d6a\u5f3a\u5ea6\u0020\u0020\u002b" + pct + "%";
                default: return "\u7acb\u5373\u6062\u590d\u0020" + Mathf.RoundToInt(progress.HealAmount(rarity)) + "\u0020\u70b9\u751f\u547d";
            }
        }

        public void Show(EmberOffer[] offers, RunProgress progress, string[] names, string[] descriptions, Action<EmberOffer> select)
        {
            Hide();
            choosing = false;
            var shade = Box("Blessing veil", transform, Vector2.zero, Vector2.one, new Color(.015f, .028f, .05f, .90f), false);
            root = shade.gameObject;
            var emblem = Box("Emblem", root.transform, new Vector2(.43f, .84f), new Vector2(.57f, .92f), new Color(1, .47f, .08f, .13f));
            emblem.sprite = EmberArt.Glow; emblem.preserveAspect = true;
            var flame = Box("Title flame", root.transform, new Vector2(.475f, .85f), new Vector2(.525f, .905f), Gold);
            flame.sprite = EmberArt.Flame;
            TextAt(root.transform, "\u706f\u0020\u706b\u0020\u5347\u0020\u534e", 36, new Vector2(.10f, .755f), new Vector2(.90f, .83f), new Color(1, .89f, .66f));
            TextAt(root.transform, "\u6ce2\u6b21\u5956\u52b1\u0020\u00b7\u0020\u9009\u62e9\u4e00\u9879\u706f\u706b\u795d\u798f", 18, new Vector2(.1f, .715f), new Vector2(.9f, .76f), new Color(.60f, .67f, .73f));
            Box("Gold divider", root.transform, new Vector2(.35f, .697f), new Vector2(.65f, .699f), new Color(.75f, .53f, .25f, .6f), false);

            for (int i = 0; i < offers.Length; i++)
            {
                EmberOffer offer = offers[i];
                int id = offer.Id;
                float top = .665f - i * .175f;
                Color accent = EmberRarityUtil.Color(offer.Rarity);
                var card = Box("Blessing card " + id, root.transform, new Vector2(.055f, top - .15f), new Vector2(.945f, top), accent * .65f);
                var inner = Box("Card face", card.transform, Vector2.zero, Vector2.one, Ink);
                inner.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
                inner.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
                var shadeTop = Box("Route tint", inner.transform, new Vector2(.01f, .03f), new Vector2(.24f, .97f), new Color(accent.r * .15f, accent.g * .15f, accent.b * .15f, 1));
                DrawIcon(shadeTop.transform, id, accent);
                var title = TextAt(inner.transform, names[id], 25, new Vector2(.275f, .59f), new Vector2(.96f, .92f), new Color(1, .93f, .80f));
                title.alignment = TextAlignmentOptions.Left;
                var desc = TextAt(inner.transform, descriptions[id], 16, new Vector2(.275f, .34f), new Vector2(.97f, .59f), new Color(.65f, .73f, .78f));
                desc.alignment = TextAlignmentOptions.Left;
                var preview = TextAt(inner.transform, Preview(id, offer.Rarity, progress), 18, new Vector2(.275f, .085f), new Vector2(.97f, .34f), accent);
                preview.alignment = TextAlignmentOptions.Left;
                string badge;
                if (id == 0 || id == 3) badge = "+" + EmberRarityUtil.CountBonus(offer.Rarity);
                else if (id == 7) badge = "+" + Mathf.RoundToInt(100f * EmberRarityUtil.Magnitude(offer.Rarity));
                else if (id == 9) badge = "+" + Mathf.RoundToInt(progress.HealAmount(offer.Rarity));
                else badge = "+" + EmberRarityUtil.Percent(offer.Rarity) + "%";
                string label = (id == 9 ? "\u8865\u7ed9" : EmberRarityUtil.Name(offer.Rarity)) + " " + badge;
                var rarityLabel = TextAt(shadeTop.transform, label, 12, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.34f), accent);
                rarityLabel.enableAutoSizing = true;
                rarityLabel.fontSizeMin = 8;
                rarityLabel.fontSizeMax = 12;
                rarityLabel.overflowMode = TextOverflowModes.Ellipsis;
                rarityLabel.enableWordWrapping = false;

                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = inner;
                var colors = button.colors;
                colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
                colors.pressedColor = new Color(.9f, .9f, .9f);
                button.colors = colors;
                var motion = card.gameObject.AddComponent<EmberCardMotion>();
                motion.Initialize(i * .075f, inner);
                EmberOffer captured = offer;
                button.onClick.AddListener(() =>
                {
                    if (choosing) return;
                    choosing = true;
                    foreach (var b in root.GetComponentsInChildren<Button>()) b.interactable = false;
                    motion.Select(() => select(captured));
                });
            }
            TextAt(root.transform, "\u706b\u5149\u6240\u81f3\uff0c\u957f\u591c\u9000\u6563", 17, new Vector2(.1f, .09f), new Vector2(.9f, .14f), new Color(.67f, .55f, .39f));
            TextAt(root.transform, "\u9752\u94dc\u0020\u0032\u0030\u0025\u0020\u00b7\u0020\u767d\u94f6\u0020\u0034\u0030\u0025\u0020\u00b7\u0020\u9ec4\u91d1\u0020\u0036\u0035\u0025\u0020\u00b7\u0020\u94bb\u77f3\u0020\u0039\u0030\u0025\uff08\u8d8a\u9ad8\u8d8a\u7a00\u6709\uff09", 13, new Vector2(.05f, .045f), new Vector2(.95f, .09f), new Color(.40f, .49f, .56f));
        }

        void DrawIcon(Transform parent, int id, Color c)
        {
            var seal = Box("Seal", parent, new Vector2(.15f, .40f), new Vector2(.85f, .96f), new Color(c.r, c.g, c.b, .12f));
            seal.sprite = EmberVisuals.Disc; seal.preserveAspect = true;
            if (id == 3 || id == 8 || id == 5)
            {
                var ring = Box("Orbit glyph", parent, new Vector2(.18f, .32f), new Vector2(.82f, .84f), c);
                ring.sprite = EmberArt.Ring; ring.preserveAspect = true;
            }
            if (id == 7 || id == 9)
            {
                Box("Healing vertical", parent, new Vector2(.44f, .38f), new Vector2(.56f, .78f), c);
                Box("Healing horizontal", parent, new Vector2(.28f, .52f), new Vector2(.72f, .64f), c);
            }
            else
            {
                int count = id == 0 ? 2 : id == 3 ? 3 : 1;
                for (int i = 0; i < count; i++)
                {
                    float x = count == 1 ? .5f : .30f + i * .4f / (count - 1);
                    float y = id == 3 ? .5f + Mathf.Sin(i * 2f) * .13f : .55f;
                    var f = Box("Flame glyph", parent, new Vector2(x - .13f, y - .18f), new Vector2(x + .13f, y + .22f), c);
                    f.sprite = EmberArt.Flame;
                    var core = Box("Glyph core", f.transform, new Vector2(.3f, .1f), new Vector2(.7f, .63f), new Color(1, .93f, .65f));
                    core.sprite = EmberArt.Flame;
                }
                if (id == 4 || id == 6)
                    for (int i = 0; i < 3; i++)
                        Box("Speed stroke", parent, new Vector2(.2f + i * .07f, .3f + i * .08f), new Vector2(.43f + i * .07f, .32f + i * .08f), c);
            }
        }

        Image Box(string name, Transform parent, Vector2 min, Vector2 max, Color color, bool rounded = true)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var image = g.GetComponent<Image>();
            image.color = color;
            if (rounded) { image.sprite = EmberArt.Panel; image.type = Image.Type.Sliced; }
            return image;
        }

        TextMeshProUGUI TextAt(Transform parent, string value, float size, Vector2 min, Vector2 max, Color color)
        {
            var g = new GameObject("Text", typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = g.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = value; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }
    }
}
