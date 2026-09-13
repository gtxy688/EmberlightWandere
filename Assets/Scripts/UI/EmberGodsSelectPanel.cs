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
        int rosterPage;
        static readonly Color Gold = new Color(.95f, .70f, .34f);
        static readonly Color Dim = new Color(.10f, .14f, .18f);
        static readonly Color Hi = new Color(.22f, .32f, .28f);
        static readonly Color Filled = new Color(.32f, .28f, .18f);

        static readonly int[] Roster = {
            RunProgress.WeaponBasic, RunProgress.WeaponOrbit, RunProgress.WeaponTrail,
            RunProgress.WeaponPierce, RunProgress.WeaponBoom, RunProgress.WeaponMeteor
        };
        static readonly string[] RosterNames = {
            "\u706b\u7403", "\u73af\u706b", "\u71c3\u5730",
            "\u7a7f\u900f\u706b\u77e2", "\u56de\u65cb\u70ec\u8776", "\u5929\u964d\u706b\u96e8"
        };
                static readonly string[] RosterDescs = {
            "\u81ea\u5df1\u4f1a\u6253\u706b\u7403\uff0c\u597d\u4e0a\u624b",
            "\u706b\u56e2\u56f4\u7740\u4f60\u8f6c\uff0c\u78b0\u5230\u5c31\u70eb",
            "\u8d70\u8fc7\u7684\u8def\u7740\u706b\uff0c\u602a\u8e29\u4e86\u6389\u8840",
            "\u7ad9\u4f4f\u624d\u5f00\u706b\uff0c\u4e00\u7bad\u7a7f\u4e00\u4e32",
            "\u4e22\u51fa\u53bb\u518d\u98de\u56de\u6765\uff0c\u6765\u56de\u90fd\u80fd\u6253",
            "\u5148\u753b\u5708\u518d\u7838\u706b\uff0c\u6253\u4e00\u7247"
        };
        const int PageSize = 3;

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
                "\u6b66\u5668\u69fd\u4f4d",
                "\u5c11\u69fd\u66f4\u7a33\u0020\u00b7\u0020\u591a\u69fd\u66f4\u82b1\u66f4\u6613\u6b6a\u0020\u00b7\u0020\u9ed8\u8ba4\u0020\u004e\u003d\u0032");

            slotCards = new Image[5];
            string[] labels = {
                "N=1  \u4e13\u7cbe\u7a33\u5065",
                "N=2  \u53cc\u6838\uff08\u63a8\u8350\uff09",
                "N=3  \u57fa\u51c6",
                "N=4  \u5bbd\u6784\u7b51",
                "N=5  \u6b66\u5668\u5e93"
            };
            for (int i = 0; i < 5; i++)
            {
                int n = i + 1;
                float top = .64f - i * .09f;
                var card = Box("Slot " + n, root.transform, new Vector2(.10f, top - .075f), new Vector2(.90f, top), Dim);
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
            rosterPage = 0;
            BuildRosterUi(onConfirm);
        }

        void BuildRosterUi(Action<int[]> onConfirm)
        {
            if (root != null) { Destroy(root); root = null; }
            root = Shade("Gods roster select");
            Title(root.transform,
                "\u6b66\u5668\u5e93",
                "\u70b9\u4e00\u5f20\u5373\u5f00\u6218\u0020\u00b7\u0020\u7a7a\u4f4d\u0020" + (selectedSlots - 1) + "\u0020\u7559\u7ed9\u5c40\u5185");

            int start = rosterPage * PageSize;
            int shown = Mathf.Min(PageSize, Roster.Length - start);
            rosterCards = new Image[shown];
            for (int i = 0; i < shown; i++)
            {
                int idx = start + i;
                int id = Roster[idx];
                float top = .62f - i * .14f;
                bool taken = filled.Contains(id);
                var card = Box("Roster " + id, root.transform, new Vector2(.08f, top - .12f), new Vector2(.92f, top), taken ? Filled : Dim);
                rosterCards[i] = card;
                TextAt(card.transform, RosterNames[idx], 24, new Vector2(.06f, .45f), new Vector2(.94f, .92f), new Color(1, .93f, .80f));
                string desc = taken ? "\u5df2\u9009\u4e2d" : RosterDescs[idx];
                TextAt(card.transform, desc, 15, new Vector2(.06f, .08f), new Vector2(.94f, .48f), new Color(.65f, .73f, .78f));
                int captured = id;
                var btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); ToggleRoster(captured, onConfirm); });
            }

            // Page controls
            if (rosterPage > 0)
            {
                var prev = Box("Prev", root.transform, new Vector2(.08f, .14f), new Vector2(.32f, .19f), new Color(.12f, .16f, .20f));
                TextAt(prev.transform, "\u4e0a\u9875", 16, Vector2.zero, Vector2.one, Gold);
                var pb = prev.gameObject.AddComponent<Button>();
                pb.targetGraphic = prev;
                pb.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); rosterPage--; BuildRosterUi(onConfirm); });
            }
            if (start + PageSize < Roster.Length)
            {
                var next = Box("Next", root.transform, new Vector2(.68f, .14f), new Vector2(.92f, .19f), new Color(.12f, .16f, .20f));
                TextAt(next.transform, "\u4e0b\u9875", 16, Vector2.zero, Vector2.one, Gold);
                var nb = next.gameObject.AddComponent<Button>();
                nb.targetGraphic = next;
                nb.onClick.AddListener(() => { EmberAudio.Ensure().PlayUiClick(); rosterPage++; BuildRosterUi(onConfirm); });
            }

        }

        void ToggleRoster(int id, Action<int[]> onConfirm)
        {
            // UI: click one card confirms starter; no multi-slot fill / OnSlotPick bar.
            filled.Clear();
            filled.Add(id);
            Hide();
            onConfirm(filled.ToArray());
        }

        void RefreshSlotHighlight()
        {
            if (slotCards == null) return;
            for (int i = 0; i < slotCards.Length; i++)
                slotCards[i].color = (i + 1 == selectedSlots) ? Hi : Dim;
        }

        void ConfirmRow(Transform parent, Action confirm)
        {
            var ok = Box("Confirm", parent, new Vector2(.18f, .06f), new Vector2(.82f, .13f), new Color(.28f, .42f, .32f));
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
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }
    }
}
