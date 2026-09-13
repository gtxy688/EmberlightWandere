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
            switch (id)
            {
                case RunProgress.StatShield:
                    return "护盾 +" + EmberRarityUtil.ShieldAmount(rarity).ToString("0") + " · 当前 " + progress.Shield.ToString("0");
                case RunProgress.StatAtk:
                    return "\u653b\u51fb\u0020\u002b" + Mathf.RoundToInt(EmberRarityUtil.GenericAtkAs(rarity) * 100) + "%  (\u5408\u8ba1 +" + Mathf.RoundToInt(progress.DamageBonus * 100) + "%)";
                case RunProgress.StatAs:
                    return "\u653b\u901f\u5df2\u9000\u51fa\u901a\u7528\u6c60";
                case RunProgress.StatLuck:
                    return "\u5e78\u8fd0\u0020\u002b" + EmberRarityUtil.GenericLuck(rarity) + "  (\u5f53\u524d " + Mathf.RoundToInt(progress.Luck) + "/" + RunProgress.MaxLuck + ")";
                case RunProgress.StatAmp:
                    return "\u589e\u5e45\u0020\u002b" + Mathf.RoundToInt(EmberRarityUtil.GenericAmp(rarity) * 100) + "%  (\u5408\u8ba1 +" + Mathf.RoundToInt(progress.DamageAmp * 100) + "%)";
                case RunProgress.WeaponBasic:
                case RunProgress.WeaponOrbit:
                case RunProgress.WeaponTrail:
                case RunProgress.WeaponPierce:
                case RunProgress.WeaponBoom:
                case RunProgress.WeaponMeteor:
                    return progress.OwnsWeapon(id) ? "\u5df2\u62e5\u6709" : "\u89e3\u9501\u65b0\u6b66\u5668\u0020\u00b7\u0020\u88c5\u5165\u69fd\u4f4d";
                case RunProgress.MetaBasic: return "\u4f53\u79ef/\u4f24\u5bb3\u00d71.5\u0020\u00b7\u0020\u547d\u4e2d\u71c3\u5730";
                case RunProgress.MetaOrbit: return "\u534a\u5f84 2.3\u0020\u00b7\u0020\u0044\u0050\u0053 90\u0020\u00b7\u0020\u707c\u70e7\u62d6\u5c3e";
                case RunProgress.MetaTrail: return "\u8eab\u5468\u6301\u7eed\u706b\u5708";
                case RunProgress.MetaPierce: return "\u547d\u4e2d\u5206\u88c2\u5c0f\u706b\u77e2";
                case RunProgress.MetaBoom: return "\u989d\u5916\u4e00\u4e2a\u6765\u56de";
                case RunProgress.MetaMeteor: return "\u5708\u66f4\u5927\u0020\u00b7\u0020\u4f24\u66f4\u9ad8";
                case RunProgress.ExclBasic: return "\u706b\u7403\u6570\u0020\u002b2";
                case RunProgress.ExclOrbit: return "\u73af\u7ed5\u0020\u002b2";
                case RunProgress.ExclTrail: return "\u62d6\u5c3e\u66f4\u957f\u66f4\u70eb";
                case RunProgress.ExclPierce: return "\u7a7f\u900f\u76ee\u6807\u0020\u002b2";
                case RunProgress.ExclBoom: return "\u5f80\u8fd4\u5404\u591a\u4e00\u6bb5";
                case RunProgress.ExclMeteor: return "\u843d\u70b9\u0020\u002b1";
                default: return "";
            }
        }

        public static string RarityLabel(EmberOffer offer, RunProgress progress)
        {
            string rarity = EmberRarityUtil.Name(offer.Rarity);
            return offer.Kind == EmberOfferKind.Generic ? rarity : KindBadge(offer, progress) + " · " + rarity;
        }

        static string KindBadge(EmberOffer offer, RunProgress progress)
        {
            if (offer.Kind == EmberOfferKind.Metamorph) return "\u8d28\u53d8";
            if (offer.Kind == EmberOfferKind.Exclusive) return "\u4e13\u5c5e";
            if (offer.Kind == EmberOfferKind.Weapon)
                return progress != null && progress.OwnsWeapon(offer.Id) ? "\u6b66\u5668" : "\u65b0\u6b66\u5668";
            return EmberRarityUtil.Name(offer.Rarity);
        }

        public void Show(EmberOffer[] offers, RunProgress progress, string[] names, string[] descriptions, Action<EmberOffer> select)
        {
            Show(offers, progress, names, descriptions, select, null);
        }

        public void Show(EmberOffer[] offers, RunProgress progress, string[] names, string[] descriptions, Action<EmberOffer> select, Func<bool> onRefresh)
        {
            Hide();
            choosing = false;
            var shade = Box("Blessing veil", transform, Vector2.zero, Vector2.one, new Color(.015f, .028f, .05f, .90f), false);
            root = shade.gameObject;
            var emblem = Box("Emblem", root.transform, new Vector2(.43f, .90f), new Vector2(.57f, .97f), new Color(1, .47f, .08f, .13f));
            emblem.sprite = EmberArt.Glow; emblem.preserveAspect = true;
            TextAt(root.transform, "\u706f\u0020\u706b\u0020\u5347\u0020\u534e", 32, new Vector2(.10f, .82f), new Vector2(.90f, .89f), new Color(1, .89f, .66f));
            string sub = "\u6ce2\u6b21\u5956\u52b1\u0020\u00b7\u0020\u9009\u4e00\u9879\u0020\u00b7\u0020\u5e78\u8fd0 " + Mathf.RoundToInt(progress.Luck)
                + "  \u00b7  \u5237\u65b0 " + progress.RefreshesRemaining;
            TextAt(root.transform, sub, 15, new Vector2(.08f, .775f), new Vector2(.92f, .82f), new Color(.60f, .67f, .73f));

            int n = offers != null ? offers.Length : 0;
            float cardH = n > 4 ? 0.11f : (n > 3 ? 0.125f : 0.145f);
            float gap = n > 4 ? 0.015f : 0.02f;
            float top0 = 0.75f;

            for (int i = 0; i < n; i++)
            {
                EmberOffer offer = offers[i];
                int id = offer.Id;
                float top = top0 - i * (cardH + gap);
                Color accent = EmberRarityUtil.Color(offer.Rarity);
                var card = Box("Blessing card " + id, root.transform, new Vector2(.055f, top - cardH), new Vector2(.945f, top), accent * .65f);
                var inner = Box("Card face", card.transform, Vector2.zero, Vector2.one, Ink);
                inner.rectTransform.offsetMin = new Vector2(1.5f, 1.5f);
                inner.rectTransform.offsetMax = new Vector2(-1.5f, -1.5f);
                var shadeTop = Box("Route tint", inner.transform, new Vector2(.01f, .03f), new Vector2(.24f, .97f), new Color(accent.r * .15f, accent.g * .15f, accent.b * .15f, 1));
                DrawIcon(shadeTop.transform, id, accent);
                string titleText = (id >= 0 && id < names.Length && !string.IsNullOrEmpty(names[id])) ? names[id] : ("#" + id);
                string descText = (id >= 0 && id < descriptions.Length) ? descriptions[id] : "";
                var title = TextAt(inner.transform, titleText, 22, new Vector2(.275f, .55f), new Vector2(.96f, .92f), new Color(1, .93f, .80f));
                title.alignment = TextAlignmentOptions.Left;
                var desc = TextAt(inner.transform, descText, 14, new Vector2(.275f, .30f), new Vector2(.97f, .55f), new Color(.65f, .73f, .78f));
                desc.alignment = TextAlignmentOptions.Left;
                var preview = TextAt(inner.transform, Preview(id, offer.Rarity, progress), 15, new Vector2(.275f, .05f), new Vector2(.97f, .30f), accent);
                preview.alignment = TextAlignmentOptions.Left;
                string label = RarityLabel(offer, progress);
                var rarityLabel = TextAt(shadeTop.transform, label, 11, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.34f), accent);
                rarityLabel.enableAutoSizing = true;
                rarityLabel.fontSizeMin = 8;
                rarityLabel.fontSizeMax = 11;
                rarityLabel.overflowMode = TextOverflowModes.Ellipsis;
                rarityLabel.enableWordWrapping = false;

                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = inner;
                var colors = button.colors;
                colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
                colors.pressedColor = new Color(.9f, .9f, .9f);
                button.colors = colors;
                var motion = card.gameObject.AddComponent<EmberCardMotion>();
                motion.Initialize(i * .05f, inner);
                EmberOffer captured = offer;
                button.onClick.AddListener(() =>
                {
                    EmberAudio.Ensure().PlayUiClick();
                    if (choosing) return;
                    choosing = true;
                    foreach (var b in root.GetComponentsInChildren<Button>()) b.interactable = false;
                    motion.Select(() => select(captured));
                });
            }

            // Refresh button
            if (onRefresh != null)
            {
                bool can = progress.RefreshesRemaining > 0;
                var refBtn = Box("Refresh", root.transform, new Vector2(.25f, .015f), new Vector2(.75f, .055f),
                    can ? new Color(.20f, .28f, .36f) : new Color(.10f, .12f, .14f));
                TextAt(refBtn.transform,
                    can ? ("\u5237\u65b0\uff08\u5269 " + progress.RefreshesRemaining + "\uff09") : "\u65e0\u5237\u65b0",
                    16, Vector2.zero, Vector2.one, can ? Gold : new Color(.4f, .45f, .5f));
                var rb = refBtn.gameObject.AddComponent<Button>();
                rb.targetGraphic = refBtn;
                rb.interactable = can;
                rb.onClick.AddListener(() =>
                {
                    if (choosing || !can) return;
                    onRefresh();
                });
            }
        }

        void DrawIcon(Transform parent, int id, Color c)
        {
            var seal = Box("Seal", parent, new Vector2(.15f, .40f), new Vector2(.85f, .96f), new Color(c.r, c.g, c.b, .12f));
            seal.sprite = EmberVisuals.Disc; seal.preserveAspect = true;
            if (id == RunProgress.StatShield || id == RunProgress.StatAs || id == RunProgress.MetaOrbit || id == RunProgress.ExclOrbit || id == RunProgress.WeaponOrbit)
            {
                var ring = Box("Orbit glyph", parent, new Vector2(.18f, .32f), new Vector2(.82f, .84f), c);
                ring.sprite = EmberArt.Ring; ring.preserveAspect = true;
            }
            else
            {
                int count = (id == RunProgress.ExclBasic || id == RunProgress.StatAtk) ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    float x = count == 1 ? .5f : .30f + i * .4f / (count - 1);
                    float y = .55f;
                    var f = Box("Flame glyph", parent, new Vector2(x - .13f, y - .18f), new Vector2(x + .13f, y + .22f), c);
                    f.sprite = EmberArt.Flame;
                }
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
