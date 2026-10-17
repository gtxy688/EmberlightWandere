using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    /// <summary>将战斗 HUD 迁移为紧凑样板；相同面板已绑定时保留用户排版。</summary>
    public static class EmberArtSampleHudSetup
    {
        public static void Configure(Sprite panel)
        {
            const string path = "Assets/UI/UIRoot.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var ui = root.GetComponent<EmberUiRoot>();
                var view = ui.Hud;
                var image = view.Root.GetComponent<Image>();
                if (image.sprite == panel) return;
                image.sprite = panel;
                image.color = Color.white;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 4;
                image.raycastTarget = false;
                var outline = image.GetComponent<Outline>();
                if (outline != null) outline.enabled = false;
                Rect(view.Root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -8), new Vector2(-24, 128), true);
                Rect(view.Clock.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-25, -24), new Vector2(-100, 24));
                Text(view.Clock, 14, TextAlignmentOptions.Center);
                Rect(view.Pause.transform, Vector2.one, Vector2.one, new Vector2(-34, -25), new Vector2(40, 32));
                foreach (var label in view.Pause.GetComponentsInChildren<TextMeshProUGUI>()) Text(label, 13, TextAlignmentOptions.Center);
                Rect(view.Health.transform.parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -61), new Vector2(-48, 26));
                view.Health.color = new Color(.75f, .16f, .17f);
                Text(view.HealthText, 19, TextAlignmentOptions.Center);
                Rect(view.Shield.transform.parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80), new Vector2(-52, 3));
                Rect(view.WaveFill.transform.parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(55, -105), new Vector2(66, 8));
                Rect(view.WaveText.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(5, -105), new Vector2(-164, 24));
                Text(view.WaveText, 13, TextAlignmentOptions.Center);
                Rect(view.ShieldText.transform, Vector2.one, Vector2.one, new Vector2(-48, -105), new Vector2(70, 22));
                Text(view.ShieldText, 13, TextAlignmentOptions.Center);
                Rect(view.EncounterRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -84), new Vector2(-40, 32), true);
                Rect(view.BossRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -270), new Vector2(-64, 48), true);
                ui.ValidateReferences();
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save compact HUD sample.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void Text(TextMeshProUGUI text, float size, TextAlignmentOptions alignment)
        {
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size - 2;
            text.fontSizeMax = size;
            text.alignment = alignment;
        }

        static void Rect(Transform node, Vector2 min, Vector2 max, Vector2 position, Vector2 size, bool top = false)
        {
            var rect = (RectTransform)node;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, top ? 1 : .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
