using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 触屏 / 鼠标输入：把下半屏的拖拽翻译成一根摇杆向量。
    ///
    /// 原先这段逻辑写在 `EmberGame.ReadInput()` 里，直接读写入口脚本的私有字段
    /// （`pointer` / `origin` / `stick`）。现在输入有自己的状态，且不持有 `EmberGame`——
    /// 摇杆呈现与暂停请求通过 <see cref="Presenter"/> 回调，因此本类可脱离游戏实例验证。
    /// </summary>
    public sealed class EmberInput
    {
        /// <summary>宿主只需要这两件事：摆放摇杆、响应暂停键。</summary>
        public interface Presenter
        {
            /// <summary>把摇杆状态交给界面（active = 是否正在拖动）。</summary>
            void OnStick(bool active, Vector2 screenOrigin, Vector2 stick);

            /// <summary>请求暂停。</summary>
            void OnPauseRequested();
        }

        /// <summary>只有屏幕下方这个比例以内的按下才算移动输入（上方留给按钮）。</summary>
        public const float MoveZoneHeightRatio = .7f;

        /// <summary>摇杆满量程对应的屏幕宽度比例。</summary>
        public const float StickRadiusRatio = .14f;

        readonly Presenter presenter;

        int pointer = -1;
        Vector2 origin;
        Vector2 stick;

        public EmberInput(Presenter owner) { presenter = owner; }

        /// <summary>当前摇杆向量（已钳到单位长度）。</summary>
        public Vector2 Stick { get { return stick; } }

        /// <summary>是否正在拖动摇杆。</summary>
        public bool IsDragging { get { return pointer != -1; } }

        /// <summary>手指/鼠标按下的屏幕坐标，用于摆放摇杆底盘。</summary>
        public Vector2 Origin { get { return origin; } }

        /// <summary>本局开始时清空拖动状态，避免沿用上一局的摇杆。</summary>
        public void Reset()
        {
            pointer = -1;
            stick = Vector2.zero;
        }

        /// <summary>读取这一帧的输入；退出键会请求暂停。</summary>
        public void Tick()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && pointer == -1 && t.position.y < Screen.height * MoveZoneHeightRatio)
                    {
                        pointer = t.fingerId;
                        origin = t.position;
                    }
                    if (t.fingerId == pointer)
                    {
                        stick = Vector2.ClampMagnitude((t.position - origin) / (Screen.width * StickRadiusRatio), 1);
                        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        {
                            pointer = -1;
                            stick = Vector2.zero;
                        }
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && Input.mousePosition.y < Screen.height * MoveZoneHeightRatio)
                {
                    pointer = -2;
                    origin = Input.mousePosition;
                }
                if (pointer == -2) stick = Vector2.ClampMagnitude(((Vector2)Input.mousePosition - origin) / (Screen.width * StickRadiusRatio), 1);
                if (Input.GetMouseButtonUp(0))
                {
                    pointer = -1;
                    stick = Vector2.zero;
                }
            }
            if (presenter != null) presenter.OnStick(pointer != -1, origin, stick);
            if (Input.GetKeyDown(KeyCode.Escape) && presenter != null) presenter.OnPauseRequested();
        }

        /// <summary>键盘方向键/WASD，与摇杆相加后再钳制。</summary>
        public static Vector2 KeyboardAxis()
        {
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
    }
}
