using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    /// <summary>仅在 Editor 中创建 Prefab 控件；运行时只绑定序列化视图。</summary>
    public static class EmberUiFactory
    {
        static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return node;
        }

        public static Image Box(string name, Transform parent, Vector2 min, Vector2 max, Color color, bool rounded = true)
        {
            var image = Rect(name, parent, min, max).AddComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = EmberArt.Panel;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value, float size, Vector2 min, Vector2 max, Color color, bool wrap)
        {
            var text = Rect(name, parent, min, max).AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableWordWrapping = wrap;
            return text;
        }

        public static Image Bar(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var track = Box(name + " track", parent, min, max, new Color(.04f, .075f, .105f));
            var fill = Box(name + " fill", track.transform, Vector2.zero, Vector2.one, color);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.raycastTarget = false;
            return fill;
        }
    }
}
