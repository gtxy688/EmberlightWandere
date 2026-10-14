using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    /// <summary>仅供旧 UI 快照迁移：补齐开局选择页的静态控件，不包含运行时交互。</summary>
    public static class EmberGodsViewBuilder
    {
        static readonly Color Gold = new Color(.95f, .70f, .34f);

        public static void EnsurePanels(Transform parent, TMP_FontAsset font)
        {
            if (parent.Find("Gods slots select") == null) CreateSlots(parent, font);
            if (parent.Find("Gods roster select") == null) CreateRoster(parent, font);
        }

        static void CreateSlots(Transform parent, TMP_FontAsset font)
        {
            var root = Shade(parent, "Gods slots select");
            Title(root.transform, font, EmberCopy.SlotsTitle, EmberCopy.SlotsSummary);
            for (int i = 0; i < 5; i++)
            {
                float top = .64f - i * .09f;
                var card = EmberUiFactory.Box("Slot " + (i + 1), root.transform, new Vector2(.10f, top - .075f), new Vector2(.90f, top), Color.white);
                card.sprite = EmberUiArt.Get(i == 1 ? EmberUiArt.Piece.Primary : EmberUiArt.Piece.Secondary);
                card.pixelsPerUnitMultiplier = 3;
                Text(card.transform, font, EmberCopy.SlotChoices[i], 18, new Vector2(.04f, .05f), new Vector2(.96f, .95f), new Color(1, .92f, .76f));
                card.gameObject.AddComponent<Button>().targetGraphic = card;
            }
            var confirm = EmberUiFactory.Box("Confirm", root.transform, new Vector2(.18f, .06f), new Vector2(.82f, .13f), Color.white);
            confirm.sprite = EmberUiArt.Get(EmberUiArt.Piece.Primary);
            confirm.pixelsPerUnitMultiplier = 3;
            Text(confirm.transform, font, "确认", 22, Vector2.zero, Vector2.one, Gold);
            confirm.gameObject.AddComponent<Button>().targetGraphic = confirm;
            root.SetActive(false);
        }

        static void CreateRoster(Transform parent, TMP_FontAsset font)
        {
            var root = Shade(parent, "Gods roster select");
            Title(root.transform, font, EmberCopy.RosterTitle, EmberCopy.RosterSummary);
            const float cardH = 132, step = 144;
            var viewport = EmberUiFactory.Box("Roster viewport", root.transform, new Vector2(.06f, .21f), new Vector2(.94f, .72f), Color.clear, false);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var content = new GameObject("Roster", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            var roster = EmberWeaponCatalog.RosterCopy();
            content.sizeDelta = new Vector2(0, roster.Length * step - (step - cardH));
            content.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            for (int i = 0; i < roster.Length; i++)
            {
                int id = roster[i];
                var card = EmberUiFactory.Box("Roster " + id, content, new Vector2(0, 1), Vector2.one, Color.white);
                card.rectTransform.offsetMin = new Vector2(0, -i * step - cardH);
                card.rectTransform.offsetMax = new Vector2(0, -i * step);
                card.sprite = EmberCardFrames.Get(EmberRarity.Gold);
                card.pixelsPerUnitMultiplier = 4;
                var icon = EmberUiFactory.Box("Starter doodle " + id, card.transform, new Vector2(.07f, .16f), new Vector2(.27f, .84f), Color.white, false);
                icon.sprite = EmberCardIcons.ForOffer(id);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Text(card.transform, font, EmberWeaponCatalog.Find(id).Name, 24, new Vector2(.30f, .57f), new Vector2(.92f, .88f), new Color(1, .93f, .80f));
                var description = Text(card.transform, font, EmberWeaponCatalog.Find(id).Description, 15, new Vector2(.30f, .10f), new Vector2(.92f, .55f), new Color(.65f, .73f, .78f));
                description.fontSizeMin = 12;
                description.enableWordWrapping = false;
                card.gameObject.AddComponent<Button>().targetGraphic = card;
            }
            var confirm = EmberUiFactory.Box("Confirm starter", root.transform, new Vector2(.18f, .055f), new Vector2(.82f, .125f), Color.white);
            confirm.sprite = EmberUiArt.Get(EmberUiArt.Piece.Primary);
            confirm.pixelsPerUnitMultiplier = 3;
            Text(confirm.transform, font, "请先选择武器", 20, new Vector2(.16f, .08f), new Vector2(.90f, .92f), Gold);
            var button = confirm.gameObject.AddComponent<Button>();
            button.targetGraphic = confirm;
            button.interactable = false;
            root.SetActive(false);
        }

        static GameObject Shade(Transform parent, string name) => EmberUiFactory.Box(name, parent, Vector2.zero, Vector2.one, new Color(.015f, .028f, .05f, .94f), false).gameObject;

        static void Title(Transform parent, TMP_FontAsset font, string title, string summary)
        {
            var emblem = EmberUiFactory.Box("Emblem", parent, new Vector2(.43f, .88f), new Vector2(.57f, .96f), new Color(1, .47f, .08f, .13f));
            emblem.sprite = EmberArt.Glow;
            emblem.preserveAspect = true;
            Text(parent, font, title, 32, new Vector2(.08f, .78f), new Vector2(.92f, .87f), new Color(1, .89f, .66f));
            Text(parent, font, summary, 15, new Vector2(.08f, .72f), new Vector2(.92f, .78f), new Color(.60f, .67f, .73f));
        }

        static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string value, float size, Vector2 min, Vector2 max, Color color)
        {
            var text = EmberUiFactory.Text("Text", parent, font, value, size, min, max, color, true);
            text.enableAutoSizing = true;
            text.fontSizeMin = size * .75f;
            text.fontSizeMax = size;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
    }
}
