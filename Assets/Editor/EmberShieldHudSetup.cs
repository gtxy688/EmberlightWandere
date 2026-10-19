using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    /// <summary>原位迁移独立护盾条；重复连接只绑定引用，保留 Prefab 的人工排版。</summary>
    public static class EmberShieldHudSetup
    {
        [MenuItem("Emberlight/UI/Update Shield HUD")]
        public static void UpdatePrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");
            EmberUiPrefabSetup.ConfigurePrefab();
            Debug.Log("Shield HUD configured. Existing shield rules and HUD height preserved.");
        }

        public static void Configure(EmberUiRoot.HudReferences view)
        {
            var track = (RectTransform)view.Shield.transform.parent;
            bool migrated = view.ShieldText.transform.parent == track;
            var oldIcon = track.Find("Shield icon");
            if (oldIcon != null)
            {
                UnityEngine.Object.DestroyImmediate(oldIcon.gameObject);
                if (migrated) FitContents(view);
            }
            if (migrated) return;

            // 压缩条间留白，在现有战斗栏高度内为护盾留出独立的一行。
            Place(view.Health.transform.parent, new Vector2(0, 1), Vector2.one,
                new Vector2(0, -57), new Vector2(-48, 26));
            Place(track, new Vector2(0, 1), Vector2.one, new Vector2(0, -83), new Vector2(-52, 20));
            track.GetComponent<Image>().color = new Color(.045f, .085f, .14f);
            view.Shield.color = new Color(.12f, .51f, .82f);
            view.Shield.type = Image.Type.Filled;
            view.Shield.fillMethod = Image.FillMethod.Horizontal;
            view.Shield.fillOrigin = 0;
            view.Shield.fillAmount = 0;
            view.Shield.raycastTarget = false;
            var label = view.ShieldText;
            label.transform.SetParent(track, false);
            label.transform.SetAsLastSibling();
            FitContents(view);
            label.color = new Color(.78f, .92f, 1);
            Text(label, 14);

            Place(view.WaveFill.transform.parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(55, -108), new Vector2(66, 8));
            Place(view.WaveText.transform, new Vector2(0, 1), Vector2.one,
                new Vector2(40, -108), new Vector2(-132, 20));
            Text(view.WaveText, 13);
        }

        static void FitContents(EmberUiRoot.HudReferences view)
        {
            var fill = view.Shield.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(3, 2);
            fill.offsetMax = new Vector2(-3, -2);
            var label = view.ShieldText.rectTransform;
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = new Vector2(3, 0);
            label.offsetMax = new Vector2(-3, 0);
        }

        static void Text(TextMeshProUGUI text, float size)
        {
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size - 2;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
        }

        static void Place(Transform node, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)node;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
