using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight
{
    /// <summary>绑定 Prefab 的 Buff 图标与有限浮字槽位；只展示传入的效果，不推进玩法计时。</summary>
    public sealed class EmberBuffHud : MonoBehaviour
    {
        public const float NoticeSeconds = 1.35f;
        public const float NoticeFadeStart = .6f;
        public const float NoticeRise = 26f;
        public const float NoticeOffset = 58f;
        public const float NoticeStackStep = 24f;
        [SerializeField] IconView[] icons;
        [SerializeField] RectTransform floatingRoot;
        [SerializeField] FloatingView[] floating;
        // Editor 布局迁移版本；不参与运行时显示与玩法。
        [SerializeField, HideInInspector] int layoutVersion;
        Canvas canvas;

        [Serializable]
        sealed class IconView
        {
            public string Id;
            public GameObject Root;
            public Image Icon, Ring;
            public TextMeshProUGUI Time;
            [NonSerialized] public Buff Shown;
            [NonSerialized] public int Second = -1;
        }

        [Serializable]
        sealed class FloatingView
        {
            public TextMeshProUGUI Text;
            public CanvasGroup Fade;
            [NonSerialized] public float Age;
        }

        public void Initialize()
        {
            if (icons == null || icons.Length == 0 || floatingRoot == null || floating == null || floating.Length == 0)
                throw new InvalidOperationException("Buff HUD prefab references are incomplete.");
            var ids = new HashSet<string>();
            foreach (var icon in icons)
            {
                if (string.IsNullOrWhiteSpace(icon.Id) || !ids.Add(icon.Id) || icon.Root == null ||
                    icon.Icon == null || icon.Icon.sprite == null || icon.Ring == null || icon.Ring.sprite == null || icon.Time == null)
                    throw new InvalidOperationException("Buff HUD icon references or Ids are invalid.");
                icon.Root.SetActive(false);
                icon.Shown = null;
                icon.Second = -1;
            }
            foreach (var notice in floating)
                if (notice.Text == null || notice.Fade == null)
                    throw new InvalidOperationException("Buff floating text references are incomplete.");
            canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null) throw new InvalidOperationException("Buff HUD requires a Canvas.");
            ClearFeedback();
            gameObject.SetActive(false);
        }

        public void Present(IReadOnlyList<Buff> active)
        {
            foreach (var buff in active)
            {
                bool mapped = false;
                foreach (var icon in icons) if (icon.Id == buff.Config.Id) { mapped = true; break; }
                if (!mapped) throw new InvalidOperationException("Configure a Buff HUD icon for Id: " + buff.Config.Id);
            }
            foreach (var icon in icons)
            {
                Buff current = null;
                foreach (var buff in active) if (buff.Config.Id == icon.Id) { current = buff; break; }
                bool visible = current != null;
                if (icon.Root.activeSelf != visible) icon.Root.SetActive(visible);
                if (!visible) { icon.Shown = null; icon.Second = -1; continue; }
                icon.Ring.fillAmount = Mathf.Clamp01(current.Remaining / current.Config.Duration);
                int seconds = Mathf.CeilToInt(current.Remaining);
                if (icon.Shown != current || icon.Second != seconds)
                {
                    icon.Time.text = seconds + "s";
                    icon.Shown = current;
                    icon.Second = seconds;
                }
            }
            bool show = active.Count > 0;
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
        }

        public void Notify(BuffConfig config, bool refreshed)
        {
            // 同帧多件物品各自保留一行；全部占用时替换最早的一条，不在运行时创建标签。
            FloatingView target = null;
            foreach (var notice in floating)
            {
                if (!notice.Text.gameObject.activeSelf) { target = notice; break; }
                if (target == null || notice.Age > target.Age) target = notice;
            }
            target.Age = 0;
            target.Text.text = EmberBuffPresentation.Notice(config, refreshed);
            ColorUtility.TryParseHtmlString(EmberBuffPresentation.ColorHex(config), out var color);
            target.Text.color = color;
            target.Fade.alpha = 1;
            target.Text.gameObject.SetActive(true);
            floatingRoot.gameObject.SetActive(true);
            PositionLines();
        }

        public void PositionFeedback(Camera worldCamera, Vector3 playerPosition)
        {
            if (worldCamera == null) return;
            Vector3 screen = worldCamera.WorldToScreenPoint(playerPosition);
            if (screen.z <= 0) { floatingRoot.gameObject.SetActive(false); return; }
            var parent = (RectTransform)floatingRoot.parent;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out var point)) return;
            point.y += NoticeOffset;
            float halfWidth = floatingRoot.rect.width * .5f;
            point.x = Mathf.Clamp(point.x, parent.rect.xMin + halfWidth, parent.rect.xMax - halfWidth);
            floatingRoot.anchoredPosition = point;
            floatingRoot.gameObject.SetActive(HasFeedback());
        }

        /// <summary>由 Playing 帧传入真实时间；和 Buff 的游戏内倒计时完全分开。</summary>
        public void TickFeedback(float unscaledDeltaTime)
        {
            foreach (var notice in floating)
            {
                if (!notice.Text.gameObject.activeSelf) continue;
                notice.Age += unscaledDeltaTime;
                if (notice.Age >= NoticeSeconds) { notice.Text.gameObject.SetActive(false); continue; }
                notice.Fade.alpha = 1 - Mathf.InverseLerp(NoticeSeconds * NoticeFadeStart, NoticeSeconds, notice.Age);
            }
            PositionLines();
            floatingRoot.gameObject.SetActive(HasFeedback());
        }

        void PositionLines()
        {
            int row = 0;
            foreach (var notice in floating)
            {
                if (!notice.Text.gameObject.activeSelf) continue;
                notice.Text.rectTransform.anchoredPosition = new Vector2(0, row++ * NoticeStackStep + notice.Age / NoticeSeconds * NoticeRise);
            }
        }

        bool HasFeedback()
        {
            foreach (var notice in floating) if (notice.Text.gameObject.activeSelf) return true;
            return false;
        }

        public void ClearFeedback()
        {
            foreach (var notice in floating) { notice.Age = 0; notice.Text.gameObject.SetActive(false); }
            floatingRoot.gameObject.SetActive(false);
        }
    }
}
