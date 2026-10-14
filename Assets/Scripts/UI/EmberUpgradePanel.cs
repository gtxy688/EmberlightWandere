using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    public sealed class EmberUpgradePanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        bool choosing;
        [SerializeField] System.Collections.Generic.List<CardView> cards = new System.Collections.Generic.List<CardView>();
        [Serializable]
        sealed class CardView
        {
            public Image Face, Inner, Shade, Icon;
            public TextMeshProUGUI Title, Description, Preview, Rarity;
            public Button Button;
            public EmberCardMotion Motion;
            [NonSerialized] public EmberOffer Offer;
        }

        [SerializeField] TextMeshProUGUI subtitle, scrollHint, refreshLabel;
        [SerializeField] ScrollRect scroll;
        [SerializeField] Image refreshFace;
        [SerializeField] Button refreshButton;
        bool listenersBound;
        Action<EmberOffer> onSelect;
        Func<bool> refresh;
        bool canRefresh;
        static readonly Color Gold = new Color(.95f, .70f, .34f);
        public void Initialize()
        {
            if (root == null || subtitle == null || scrollHint == null || refreshLabel == null || scroll == null ||
                refreshFace == null || refreshButton == null || cards.Count == 0)
                throw new InvalidOperationException("Upgrade prefab references are incomplete.");
            if (!listenersBound)
            {
                foreach (var view in cards)
                {
                    if (view.Face == null || view.Inner == null || view.Shade == null || view.Icon == null || view.Title == null ||
                        view.Description == null || view.Preview == null || view.Rarity == null || view.Button == null || view.Motion == null)
                        throw new InvalidOperationException("Upgrade card prefab references are incomplete.");
                    BindCard(view);
                }
                BindRefresh();
                listenersBound = true;
            }
            Hide();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            if (scroll != null)
                scroll.StopMovement();
            choosing = false;
            onSelect = null;
            refresh = null;
        }

        // 卡面文案规则归 EmberOfferPresentation。
        public static string Preview(int id, EmberRarity rarity, RunProgress progress, int weaponId = -1)
        {
            return EmberOfferPresentation.Preview(id, rarity, progress, weaponId);
        }

        public static string RarityLabel(EmberOffer offer, RunProgress progress)
        {
            return EmberOfferPresentation.RarityLabel(offer, progress);
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
                if (progress == null || offers == null || offers.Length == 0)
                    return;
                choosing = false;
                onSelect = select;
                refresh = onRefresh;
                string sub = "\u6ce2\u6b21\u5956\u52b1\u0020\u00b7\u0020\u9009\u4e00\u9879\u0020\u00b7\u0020\u5e78\u8fd0 " + Mathf.RoundToInt(progress.Luck) + "  \u00b7  \u5237\u65b0 " + progress.RefreshesRemaining;
                subtitle.text = sub;
                int n = offers.Length;
                if (n > cards.Count) throw new InvalidOperationException("UIRoot has too few cached upgrade cards.");
                float cardH = n > 4 ? 0.11f : (n > 3 ? 0.125f : 0.145f);
                float gap = n > 4 ? 0.015f : 0.02f;
                float top0 = 0.75f;
                RectTransform scrollContent = null;
                scroll.gameObject.SetActive(n > 4);
                scrollHint.gameObject.SetActive(n > 4);
                scroll.content.sizeDelta = new Vector2(0, n * 144f - 12f);
                scroll.content.anchoredPosition = Vector2.zero;
                scrollContent = n > 4 ? scroll.content : null;
                foreach (var cached in cards)
                    cached.Face.gameObject.SetActive(false);
                for (int i = 0; i < n; i++)
                {
                    EmberOffer offer = offers[i];
                    int id = offer.Id;
                    float top = top0 - i * (cardH + gap);
                    Color accent = EmberRarityUtil.Tint(offer.Rarity).ToColor();
                    var view = cards[i];
                    var card = view.Face;
                    var parent = scrollContent != null ? scrollContent : root.transform;
                    if (card.transform.parent != parent)
                        card.transform.SetParent(parent, false);
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
                    inner.rectTransform.offsetMin = new Vector2(24f, 8f);
                    inner.rectTransform.offsetMax = new Vector2(-24f, -8f);
                    var shadeTop = view.Shade;
                    view.Icon.sprite = EmberCardIcons.ForOffer(id);
                    var presentation = EmberOfferPresentation.Build(offer, progress, names, descriptions);
                    string titleText = presentation.Title;
                    string descText = presentation.Description;
                    var title = view.Title;
                    title.text = titleText;
                    title.alignment = TextAlignmentOptions.Left;
                    FitText(title, 13, 22, false);
                    var desc = view.Description;
                    desc.text = descText;
                    desc.alignment = TextAlignmentOptions.Left;
                    FitText(desc, 11, 14, id != RunProgress.StatShield);
                    var preview = view.Preview;
                    preview.text = presentation.NumericPreview;
                    preview.color = accent;
                    preview.alignment = TextAlignmentOptions.Left;
                    FitText(preview, 10, 15, false);
                    string label = presentation.Badge;
                    var rarityLabel = view.Rarity;
                    rarityLabel.text = label;
                    rarityLabel.color = accent;
                    rarityLabel.enableAutoSizing = true;
                    rarityLabel.fontSizeMin = 8;
                    rarityLabel.fontSizeMax = 11;
                    rarityLabel.overflowMode = TextOverflowModes.Overflow;
                    rarityLabel.enableWordWrapping = false;
                    var button = view.Button;
                    button.interactable = true;
                    button.targetGraphic = card;
                    var colors = button.colors;
                    colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
                    colors.pressedColor = new Color(.9f, .9f, .9f);
                    button.colors = colors;
                    view.Offer = offer;
                    view.Motion.Initialize(i * .05f, card);
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

        void BindCard(CardView view)
        {
            view.Button.onClick.AddListener(() =>
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

        void BindRefresh()
        {
            refreshButton.onClick.AddListener(() => { if (!choosing && canRefresh) refresh?.Invoke(); });
        }

        // 标题、两行说明与数值预览各自独立缩放，避免挤在一起互相重叠。
        static void FitText(TextMeshProUGUI text, float min, float max, bool wrap)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.enableWordWrapping = wrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

    }
}
