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
        readonly System.Collections.Generic.List<CardView> cards = new System.Collections.Generic.List<CardView>();
        sealed class CardView
        {
            public Image Face, Inner, Shade, Icon;
            public TextMeshProUGUI Title, Description, Preview, Rarity;
            public Button Button;
            public EmberCardMotion Motion;
            public EmberOffer Offer;
        }
        TextMeshProUGUI subtitle, scrollHint, refreshLabel;
        ScrollRect scroll;
        Image refreshFace;
        Button refreshButton;
        Action<EmberOffer> onSelect;
        Func<bool> refresh;
        bool canRefresh;
        static readonly Color Gold = new Color(.95f, .70f, .34f);
        static readonly Color Ink = new Color(.024f, .042f, .066f);

        public void Initialize(TMP_FontAsset chineseFont)
        {
            font = chineseFont;
            if (root != null) return;
            // Capacity is a view cache, not a change to the candidate-generation rules.
            var warmOffers = new EmberOffer[8];
            for (int i = 0; i < warmOffers.Length; i++) warmOffers[i] = new EmberOffer(RunProgress.StatShield, EmberRarity.Gold);
            Show(warmOffers, new RunProgress(), new string[5], new string[5], null);
            Canvas.ForceUpdateCanvases();
            Hide();
        }
        public void Hide()
        {
            if (root != null) root.SetActive(false);
            if (scroll != null) scroll.StopMovement();
            choosing = false;
            onSelect = null;
            refresh = null;
        }

        public static string Preview(int id, EmberRarity rarity, RunProgress progress, int weaponId = -1)
        {
            switch (id)
            {
                case RunProgress.StatShield:
                    return "护盾 +" + EmberRarityUtil.ShieldAmount(rarity).ToString("0") + " · 当前 " + progress.Shield.ToString("0");
                case RunProgress.StatAtk:
                {
                    int wid = weaponId >= 0 ? weaponId : progress.CoreWeaponId;
                    return "\u653b\u51fb\u0020\u002b" + Mathf.RoundToInt(EmberRarityUtil.GenericAtkAs(rarity) * 100) + "%  (\u5408\u8ba1 +" + Mathf.RoundToInt(progress.WeaponMagnitude(wid) * 100) + "%)";
                }
                case RunProgress.StatAs:
                {
                    int wid = weaponId >= 0 ? weaponId : progress.CoreWeaponId;
                    return "\u653b\u901f\u0020\u002b" + Mathf.RoundToInt(EmberRarityUtil.GenericAttackSpeed(rarity) * 100) + "%  (\u5408\u8ba1 +" + Mathf.RoundToInt(progress.WeaponAttackSpeed(wid) * 100) + "%)";
                }
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

        static readonly Unity.Profiling.ProfilerMarker ShowMarker = new Unity.Profiling.ProfilerMarker("Ember.UI.Upgrade.Show");

        public void Show(EmberOffer[] offers, RunProgress progress, string[] names, string[] descriptions, Action<EmberOffer> select, Func<bool> onRefresh)
        {
            using (ShowMarker.Auto())
            {
                Hide();
                if (progress == null || offers == null || offers.Length == 0) return;
                choosing = false;
                if (root == null)
                {
                var shade = Box("Blessing veil", transform, Vector2.zero, Vector2.one, new Color(.015f, .028f, .05f, .90f), false);
                root = shade.gameObject;
                var emblem = Box("Emblem", root.transform, new Vector2(.37f, .90f), new Vector2(.63f, .98f), Color.white);
                emblem.sprite = EmberUiArt.Get(EmberUiArt.Piece.Lantern); emblem.type = Image.Type.Simple; emblem.preserveAspect = true;
                TextAt(root.transform, "\u706f\u0020\u706b\u0020\u5347\u0020\u534e", 32, new Vector2(.10f, .82f), new Vector2(.90f, .89f), new Color(1, .89f, .66f));
                }
                onSelect = select;
                refresh = onRefresh;
                string sub = "\u6ce2\u6b21\u5956\u52b1\u0020\u00b7\u0020\u9009\u4e00\u9879\u0020\u00b7\u0020\u5e78\u8fd0 " + Mathf.RoundToInt(progress.Luck)
                    + "  \u00b7  \u5237\u65b0 " + progress.RefreshesRemaining;
                if (subtitle == null) subtitle = TextAt(root.transform, "", 15, new Vector2(.08f, .775f), new Vector2(.92f, .82f), new Color(.60f, .67f, .73f));
                subtitle.text = sub;

                int n = offers != null ? offers.Length : 0;
                float cardH = n > 4 ? 0.11f : (n > 3 ? 0.125f : 0.145f);
                float gap = n > 4 ? 0.015f : 0.02f;
                float top0 = 0.75f;
                RectTransform scrollContent = null;
                if (scroll == null)
                {
                    var viewport = Box("Card viewport", root.transform, new Vector2(.055f, .075f), new Vector2(.945f, .75f), Color.clear, false);
                    viewport.gameObject.AddComponent<RectMask2D>();
                    scroll = viewport.gameObject.AddComponent<ScrollRect>();
                    scrollContent = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
                    scrollContent.SetParent(viewport.transform, false);
                    scrollContent.anchorMin = new Vector2(0, 1);
                    scrollContent.anchorMax = Vector2.one;
                    scrollContent.pivot = new Vector2(.5f, 1);
                    scrollContent.sizeDelta = new Vector2(0, n * 144f - 12f);
                    scrollContent.anchoredPosition = Vector2.zero;
                    scroll.viewport = viewport.rectTransform;
                    scroll.content = scrollContent;
                    scroll.horizontal = false;
                    scroll.vertical = true;
                    scroll.movementType = ScrollRect.MovementType.Clamped;
                    scrollHint = TextAt(root.transform, "上下滑动查看全部选项", 12, new Vector2(.1f, .055f), new Vector2(.9f, .075f), new Color(.60f, .67f, .73f));
                    FitText(scrollHint, 10, 12, false);
                }

                scroll.gameObject.SetActive(n > 4);
                scrollHint.gameObject.SetActive(n > 4);
                scroll.content.sizeDelta = new Vector2(0, n * 144f - 12f);
                scroll.content.anchoredPosition = Vector2.zero;
                scrollContent = n > 4 ? scroll.content : null;
                foreach (var cached in cards) cached.Face.gameObject.SetActive(false);
                for (int i = 0; i < n; i++)
                {
                    EmberOffer offer = offers[i];
                    int id = offer.Id;
                    float top = top0 - i * (cardH + gap);
                    Color accent = EmberRarityUtil.Color(offer.Rarity);
                    bool create = i >= cards.Count;
                    var view = create ? new CardView() : cards[i];
                    if (create) cards.Add(view);
                    var card = view.Face;
                    if (create) card = view.Face = Box("Blessing card " + id, scrollContent != null ? scrollContent : root.transform, new Vector2(.055f, top - cardH), new Vector2(.945f, top), accent * .65f);
                    var parent = scrollContent != null ? scrollContent : root.transform;
                    if (card.transform.parent != parent) card.transform.SetParent(parent, false);
                    card.rectTransform.anchorMin = new Vector2(.055f, top - cardH);
                    card.rectTransform.anchorMax = new Vector2(.945f, top);
                    card.rectTransform.offsetMin = card.rectTransform.offsetMax = Vector2.zero;
                    card.transform.SetAsLastSibling();
                    card.gameObject.SetActive(true);
                    if (scrollContent != null)
                    {
                        card.rectTransform.anchorMin = new Vector2(0, 1);
                        card.rectTransform.anchorMax = Vector2.one;
                        card.rectTransform.offsetMin = new Vector2(0, -i * 144f - 132f);
                        card.rectTransform.offsetMax = new Vector2(0, -i * 144f);
                    }
                    card.sprite = EmberCardFrames.Get(offer.Rarity);
                    card.color = Color.white;
                    card.pixelsPerUnitMultiplier = 4f;
                    var inner = view.Inner;
                    if (create) inner = view.Inner = Box("Card face", card.transform, Vector2.zero, Vector2.one, Color.clear, false);
                    inner.rectTransform.offsetMin = new Vector2(24f, 8f);
                    inner.rectTransform.offsetMax = new Vector2(-24f, -8f);
                    var shadeTop = view.Shade;
                    if (create) shadeTop = view.Shade = Box("Route tint", inner.transform, new Vector2(.01f, .03f), new Vector2(.24f, .97f), new Color(16f / 255, 28f / 255, 40f / 255, 1));
                    if (create)
                    {
                        view.Icon = Box("Doodle icon", shadeTop.transform, new Vector2(.06f, .27f), new Vector2(.94f, .97f), Color.white, false);
                        view.Icon.preserveAspect = true;
                        view.Icon.raycastTarget = false;
                    }
                    view.Icon.sprite = EmberCardIcons.ForOffer(id);
                    string titleText = (id >= 0 && id < names.Length && !string.IsNullOrEmpty(names[id])) ? names[id] : ("#" + id);
                    if ((id == RunProgress.StatAtk || id == RunProgress.StatAs) && offer.WeaponId >= 0
                        && offer.WeaponId < names.Length && !string.IsNullOrEmpty(names[offer.WeaponId]))
                        titleText = names[offer.WeaponId] + "\u00b7" + titleText;
                    string descText = (id >= 0 && id < descriptions.Length) ? descriptions[id] : "";
                    if ((id == RunProgress.StatAtk || id == RunProgress.StatAs) && offer.WeaponId >= 0
                        && offer.WeaponId < names.Length && !string.IsNullOrEmpty(names[offer.WeaponId]))
                    {
                        string wname = names[offer.WeaponId];
                        descText = id == RunProgress.StatAtk
                            ? ("\u53ea\u63d0\u9ad8\u300c" + wname + "\u300d\u7684\u4f24\u5bb3")
                            : ("\u53ea\u63d0\u9ad8\u300c" + wname + "\u300d\u7684\u653b\u51fb\u901f\u5ea6");
                    }
                    if (id == RunProgress.StatShield)
                        descText = "立即获得护盾，受伤优先消耗\n可叠加，仅本局有效";
                    var title = view.Title;
                    if (create) title = view.Title = TextAt(inner.transform, titleText, 22, new Vector2(.275f, .70f), new Vector2(.97f, .98f), new Color(1, .93f, .80f));
                    title.text = titleText;
                    title.alignment = TextAlignmentOptions.Left;
                    FitText(title, 13, 22, false);
                    var desc = view.Description;
                    if (create) desc = view.Description = TextAt(inner.transform, descText, 14, new Vector2(.275f, .25f), new Vector2(.97f, .68f), new Color(.65f, .73f, .78f));
                    desc.text = descText;
                    desc.alignment = TextAlignmentOptions.Left;
                    FitText(desc, 11, 14, id != RunProgress.StatShield);
                    var preview = view.Preview;
                    if (create) preview = view.Preview = TextAt(inner.transform, Preview(id, offer.Rarity, progress, offer.WeaponId), 15, new Vector2(.275f, .01f), new Vector2(.97f, .23f), accent);
                    preview.text = Preview(id, offer.Rarity, progress, offer.WeaponId);
                    preview.color = accent;
                    preview.alignment = TextAlignmentOptions.Left;
                    FitText(preview, 10, 15, false);
                    string label = RarityLabel(offer, progress);
                    var rarityLabel = view.Rarity;
                    if (create) rarityLabel = view.Rarity = TextAt(shadeTop.transform, label, 11, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.25f), accent);
                    rarityLabel.text = label;
                    rarityLabel.color = accent;
                    rarityLabel.enableAutoSizing = true;
                    rarityLabel.fontSizeMin = 8;
                    rarityLabel.fontSizeMax = 11;
                    rarityLabel.overflowMode = TextOverflowModes.Overflow;
                    rarityLabel.enableWordWrapping = false;

                    var button = view.Button;
                    if (create) button = view.Button = card.gameObject.AddComponent<Button>();
                    button.interactable = true;
                    button.targetGraphic = card;
                    var colors = button.colors;
                    colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
                    colors.pressedColor = new Color(.9f, .9f, .9f);
                    button.colors = colors;
                    view.Offer = offer;
                    if (create)
                    {
                        view.Motion = card.gameObject.AddComponent<EmberCardMotion>();
                        button.onClick.AddListener(() =>
                        {
                            if (choosing || !root.activeInHierarchy || !view.Button.interactable) return;
                            EmberAudio.Ensure().PlayUiClick();
                            choosing = true;
                            foreach (var other in cards) other.Button.interactable = false;
                            if (refreshButton != null) refreshButton.interactable = false;
                            var callback = onSelect;
                            var selected = view.Offer;
                            view.Motion.Select(() => callback?.Invoke(selected));
                        });
                    }
                    view.Motion.Initialize(i * .05f, card);
                }

                if (refreshFace == null)
                {
                    refreshFace = Box("Refresh", root.transform, new Vector2(.25f, .015f), new Vector2(.75f, .055f), Color.white);
                    refreshLabel = TextAt(refreshFace.transform, "", 16, Vector2.zero, Vector2.one, Gold);
                    refreshButton = refreshFace.gameObject.AddComponent<Button>();
                    refreshButton.targetGraphic = refreshFace;
                    refreshButton.onClick.AddListener(() => { if (!choosing && canRefresh) refresh?.Invoke(); });
                }
                canRefresh = onRefresh != null && progress.RefreshesRemaining > 0;
                refreshFace.gameObject.SetActive(onRefresh != null);
                refreshFace.color = canRefresh ? new Color(.20f, .28f, .36f) : new Color(.10f, .12f, .14f);
                refreshLabel.text = canRefresh ? ("刷新（剩 " + progress.RefreshesRemaining + "）") : "无刷新";
                refreshLabel.color = canRefresh ? Gold : new Color(.4f, .45f, .5f);
                refreshButton.interactable = canRefresh;
                root.transform.SetAsLastSibling();
                root.SetActive(true);
            }
        }

        // Separate title, two-line explanation and numeric preview so fitting one cannot overlap another.
        static void FitText(TextMeshProUGUI text, float min, float max, bool wrap)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.enableWordWrapping = wrap;
            text.overflowMode = TextOverflowModes.Overflow;
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
            FitText(t, size * .75f, size, true);
            return t;
        }
    }
}
