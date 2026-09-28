using UnityEngine;

namespace Emberlight
{
    /// <summary>
    /// 安全区与分辨率变化的观察点。
    ///
    /// 原先这段判断写在 `EmberGame.Update()` 顶部，每帧都要为一个"只在分辨率/朝向变化时才发生"
    /// 的事件做一次比较，而且把安全区这件事塞进了局长入口脚本。现在改成由本组件在 Play 期间
    /// 维护，`EmberMenuUi` 只订阅一个回调，`EmberGame` 不再参与。
    /// </summary>
    public static class EmberSafeAreaWatcher
    {
        static int lastWidth, lastHeight;
        static Rect lastSafeArea;
        static bool hasSample;

        /// <summary>安全区或分辨率变化时触发。</summary>
        public static event System.Action Changed;

        /// <summary>进入 Play 时清空采样，避免沿用上一次运行的分辨率。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            lastWidth = lastHeight = 0;
            lastSafeArea = new Rect();
            hasSample = false;
            Changed = null;
        }

        /// <summary>采样当前值；返回 true 表示与上次采样不同（首次调用视为变化）。</summary>
        public static bool Poll()
        {
            if (hasSample && Screen.width == lastWidth && Screen.height == lastHeight && Screen.safeArea == lastSafeArea)
                return false;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
            hasSample = true;
            return true;
        }

        static void Dispatch()
        {
            if (Poll() && Changed != null) Changed();
        }

        /// <summary>启动观察；按配置的 Player Loop 时机每帧检查一次。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            ResetStatics();
            var hook = new GameObject("[EmberSafeAreaWatcher]");
            Object.DontDestroyOnLoad(hook);
            hook.hideFlags = HideFlags.HideAndDontSave;
            hook.AddComponent<Watcher>();
            Dispatch();
        }

        sealed class Watcher : MonoBehaviour
        {
            void LateUpdate() { Dispatch(); }
        }
    }
}
