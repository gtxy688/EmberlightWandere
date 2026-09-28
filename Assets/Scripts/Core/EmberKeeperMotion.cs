using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 灯芯的位移与相机跟随。
    ///
    /// 原先这两件事直接写在 `EmberGame.Update()` 中，把移动速度倍数、边界钳制与正交相机
    /// 的摆放细节混在入口脚本里。搬出来后 `EmberGame` 只负责调用顺序。
    /// </summary>
    public static class EmberKeeperMotion
    {
        /// <summary>灯芯基础移动速度（单位/秒），实际速度再乘构筑加成。</summary>
        public const float PlayerSpeed = 3.3f;

        /// <summary>相机在 Z 轴的固定偏移（2D 正交，必须为负才能看到 +Z 方向的精灵）。</summary>
        public const float CameraDepth = -10f;

        /// <summary>
        /// 推进一帧移动：方向取键盘与摇杆之和，钳到单位长度后按帧时间位移，并夹在战场边界内。
        /// 随后把相机贴到灯芯身上。
        /// </summary>
        public static void Step(EmberRunState run, Vector2 stick, float dt)
        {
            if (run == null || run.Keeper == null) return;

            Vector2 move = Vector2.ClampMagnitude(EmberInput.KeyboardAxis() + stick, 1);
            float bonus = run.Progress != null ? run.Progress.MoveSpeedBonus : 0f;
            run.Keeper.position += (Vector3)(move * (PlayerSpeed * (1 + bonus) * dt));
            run.Keeper.position = EmberWorld.Clamp(run.Keeper.position);
            Follow(run);
        }

        /// <summary>把相机放到灯芯正上方。</summary>
        public static void Follow(EmberRunState run)
        {
            if (run == null || run.Cam == null || run.Keeper == null) return;
            run.Cam.transform.position = new Vector3(run.Keeper.position.x, run.Keeper.position.y, CameraDepth);
        }
    }
}
