using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Emberlight
{
    /// <summary>Gods-Select-v3: pick N, then roster single-pick 1 starter. Empty slots = N-1 for in-run. No skip - must pick N then 1 starter.</summary>
    public sealed class EmberGodsSelectPanel : MonoBehaviour
    {
        TMP_FontAsset font;
        GameObject root;
        int selectedSlots = 2;
        readonly List<int> filled = new List<int>();
        Image[] slotCards;
        Image[] rosterCards;
        static readonly Color Gold = new Color(.95f, .70f, .34f);
        static readonly Color Dim = new Color(.10f, .14f, .18f);
        static readonly Color Hi = new Color(.22f, .32f, .28f);
        static readonly Color Filled = new Color(.32f, .28f, .18f);

        static readonly int[] Roster = {
            RunProgress.WeaponBasic, RunProgress.WeaponOrbit, RunProgress.WeaponTrail,
            RunProgress.WeaponPierce, RunProgress.WeaponBoom, RunProgress.WeaponMeteor
        };
        // public so EmberFonts can prewarm these characters; see EmberFonts.CardGlyphSet.
        public static readonly string[] RosterNames = {
            "\u706b\u7403", "\u73af\u706b", "\u71c3\u5730",
            "\u7a7f\u900f\u706b\u77e2", "\u56de\u65cb\u70ec\u8776", "\u5929\u964d\u706b\u96e8"
        };
        // Kept byte-identical to EmberGame.UpgradeDetails[10..15]: the starter screen and the
        // in-run weapon card must read the same. Verified, not assumed.
        public static readonly string[] RosterDescs = {
            "自动发射火球\n攻击最近的敌人",
            "火球环绕身边\n灼烧靠近的敌人",
            "沿途留下火焰\n持续灼烧踏入的敌人",
            "停下脚步发射火矢\n贯穿直线上的敌人",
            "掷出回旋火蝴蝶\n往返均可命中敌人",
            "标记目标区域\n召唤火雨轰击敌人"
        };

        public void Initialize(TMP_FontAsset chineseFont) { font = chineseFont; }
        public void Hide()
        {
            if (root != null) { root.SetActive(false); Destroy(root); root = null; }
            slotCards = null;
            rosterCards = null;
        }

        /// <summary>UI API: start roster select after N is known.</summary>
        public void ShowRosterSelect(int slots, Action<int[]> onConfirm)
        {
            ShowRoster(slots, onConfirm);
        }

        public void ShowSlots(Action<int> onConfirm)
        {
            Hide();
            selectedSlots = 2;
            root = Shade("Gods slots select");
            Title(root.transform,
                "武器槽位",
                "拿得越少强化越快，拿得越多打法越丰富 · 默认推荐 2 把");

            slotCards = new Image[5];
            string[] labels = {
                "1 把武器",
                "2 把武器（推荐）",
                "3 把武器",
                "4 把武器",
                "5 把武器"
            };
            for (int i = 0; i < 5; i++)
            {
                int n = i + 1;
                float top = .64f - i * .09f;
                var card = Box("Slot " + n, root.transform, new Vector2(.10f, top - .075f), new Vector2(.90f, top), Dim);
                card.sprite = EmberUiArt.Get(EmberUiArt.Piece.Secondary);
                card.color = Color.white;
                card.pixelsPerUnitMultiplier = 3f;
                slotCards[i] = card;
                TextAt(card.transform, labels[i], 18, new Vector2(.04f, .05f), new Vector2(.96f, .95f), new Color(1, .92f, .76f));
                int captured = n;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); selectedSlots = captured; RefreshSlotHighlight(); });
            }
            RefreshSlotHighlight();
            ConfirmRow(root.transform, () => { Hide(); onConfirm(selectedSlots); });
        }

        public void ShowRoster(int slots, Action<int[]> onConfirm)
        {
            Hide();
            selectedSlots = slots < 1 ? 1 : (slots > 5 ? 5 : slots);
            filled.Clear();
            BuildRosterUi(onConfirm);
        }

        void BuildRosterUi(Action<int[]> onConfirm)
        {
            if (root != null) { root.SetActive(false); Destroy(root); root = null; }
            root = Shade("Gods roster select");
            Title(root.transform,
                "挑选起始武器",
                "点选一把武器后点击确认开战 · 上下滑动查看全部");

            // Long list, one interaction: vertical scroll, same pattern as EmberUpgradePanel.
            // This replaced a 3-per-page pager. Scrolling keeps all six options in one list and
            // -- the reason it matters -- the selection is never on another page, so tapping an
            // option cannot scroll the chosen one out of sight.
            const float cardH = 132f, step = 144f;
            var viewport = Box("Roster viewport", root.transform, new Vector2(.06f, .21f), new Vector2(.94f, .72f), Color.clear, false);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = new GameObject("Roster", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, Roster.Length * step - (step - cardH));
            content.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            rosterCards = new Image[Roster.Length];
            for (int i = 0; i < Roster.Length; i++)
            {
                int id = Roster[i];
                bool taken = filled.Contains(id);
                var card = Box("Roster " + id, content, new Vector2(0, 1), Vector2.one, taken ? Filled : Dim);
                card.rectTransform.offsetMin = new Vector2(0, -i * step - cardH);
                card.rectTransform.offsetMax = new Vector2(0, -i * step);
                card.sprite = EmberCardFrames.Get(EmberRarity.Gold);
                card.color = taken ? new Color(1f, .91f, .65f) : Color.white;
                card.pixelsPerUnitMultiplier = 4f;
                rosterCards[i] = card;
                var icon = Box("Starter doodle " + id, card.transform, new Vector2(.07f, .16f), new Vector2(.27f, .84f), Color.white, false);
                icon.sprite = EmberCardIcons.ForOffer(id);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                TextAt(card.transform, RosterNames[i], 24, new Vector2(.30f, .57f), new Vector2(.92f, .88f), new Color(1, .93f, .80f));
                string desc = taken ? "\u5df2\u9009\u4e2d" : RosterDescs[i];
                // Reserve two lines and shrink slightly on compact screens instead of truncating.
                var description = TextAt(card.transform, desc, 15, new Vector2(.30f, .10f), new Vector2(.92f, .55f), new Color(.65f, .73f, .78f));
                description.enableAutoSizing = true;
                description.fontSizeMin = 12f;
                description.fontSizeMax = 15f;
                // Explicit semantic lines prevent isolated trailing Chinese characters.
                description.enableWordWrapping = false;
                description.overflowMode = TextOverflowModes.Overflow;
                int captured = id;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); ToggleRoster(captured, onConfirm); });
            }

            string selectedName = filled.Count > 0 ? RosterNames[Array.IndexOf(Roster, filled[0])] : "";
            var confirm = Box("Confirm starter", root.transform, new Vector2(.18f,.055f), new Vector2(.82f,.125f), Color.white);
            confirm.sprite = EmberUiArt.Get(EmberUiArt.Piece.Primary);
            confirm.pixelsPerUnitMultiplier = 3f;
            TextAt(confirm.transform, filled.Count > 0 ? "确认 · " + selectedName : "请先选择武器", 20,
                new Vector2(.16f,.08f), new Vector2(.90f,.92f), Gold);
            var confirmButton = confirm.gameObject.AddComponent<Button>();
            confirmButton.targetGraphic = confirm;
            confirmButton.interactable = filled.Count > 0;
            confirmButton.onClick.AddListener(() => {
                if (!confirmButton.interactable || filled.Count == 0) return;
                confirmButton.interactable = false;
                var chosen = filled.ToArray();
                EmberAudio.Ensure().PlayUiConfirm();
                Hide();
                onConfirm(chosen);
            });
        }

        void ToggleRoster(int id, Action<int[]> onConfirm)
        {
            // Selection is retained across pages until explicit confirmation.
            filled.Clear();
            filled.Add(id);
            BuildRosterUi(onConfirm);
        }

        void RefreshSlotHighlight()
        {
            if (slotCards == null) return;
            for (int i = 0; i < slotCards.Length; i++)
            {
                slotCards[i].sprite = EmberUiArt.Get(i + 1 == selectedSlots
                    ? EmberUiArt.Piece.Primary : EmberUiArt.Piece.Secondary);
                slotCards[i].color = Color.white;
            }
        }

        void ConfirmRow(Transform parent, Action confirm)
        {
            var ok = Box("Confirm", parent, new Vector2(.18f, .06f), new Vector2(.82f, .13f), new Color(.28f, .42f, .32f));
            ok.sprite = EmberUiArt.Get(EmberUiArt.Piece.Primary);
            ok.color = Color.white;
            ok.pixelsPerUnitMultiplier = 3f;
            TextAt(ok.transform, "\u786e\u8ba4", 22, Vector2.zero, Vector2.one, Gold);
            var okBtn = ok.gameObject.AddComponent<Button>();
            okBtn.targetGraphic = ok;
            okBtn.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); confirm(); });

        }

        GameObject Shade(string name)
        {
            var shade = Box(name, transform, Vector2.zero, Vector2.one, new Color(.015f, .028f, .05f, .94f), false);
            return shade.gameObject;
        }

        void Title(Transform parent, string title, string sub)
        {
            var emblem = Box("Emblem", parent, new Vector2(.43f, .88f), new Vector2(.57f, .96f), new Color(1, .47f, .08f, .13f));
            emblem.sprite = EmberArt.Glow; emblem.preserveAspect = true;
            TextAt(parent, title, 32, new Vector2(.08f, .78f), new Vector2(.92f, .87f), new Color(1, .89f, .66f));
            TextAt(parent, sub, 15, new Vector2(.08f, .72f), new Vector2(.92f, .78f), new Color(.60f, .67f, .73f));
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
            t.enableAutoSizing = true;
            t.fontSizeMin = size * .75f;
            t.fontSizeMax = size;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
    }
}
