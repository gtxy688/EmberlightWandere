using System.Collections;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Boots the real game scene in play mode and fails on any logged error or exception.
    ///
    /// Written after an asset-field check passed a font that crashed the game. A TMP font whose
    /// atlas references do not resolve still reports healthy glyph and character tables, so the
    /// fault only surfaces once something draws text. The loading screen builds TMP labels in
    /// its first frames, so a few seconds of play mode is enough to reproduce it. Every change
    /// to how the UI is built should go through this before it is trusted.
    ///
    /// The watcher is created from a RuntimeInitializeOnLoadMethod hook because neither
    /// AddComponent on a live behaviour nor DontDestroyOnLoad is legal from an edit-mode editor
    /// script, and EditorApplication.update does not tick reliably in -batchmode.
    ///
    /// Run via: -executeMethod Emberlight.Editor.EmberUIRenderCheck.Run
    /// Exit code 0 = the game booted clean, 1 = something was logged as an error.
    /// </summary>
    public static class EmberUIRenderCheck
    {
        const string ScenePath = "Assets/Scenes/Emberlight.unity";
        const float WatchSeconds = 12f;

        static bool armed;
        static int errors;
        static readonly System.Text.StringBuilder captured = new System.Text.StringBuilder();

        public static void Run()
        {
            errors = 0;
            armed = true;
            Application.logMessageReceived += OnLog;

            // If leaving play mode does not unwind the batch process, this ends it anyway, so a
            // regression shows up as a failed run instead of a hung one.
            var watchdog = new Thread(() =>
            {
                Thread.Sleep(120000);
                Debug.LogError("[EmberUIRenderCheck] watchdog fired, the run did not finish");
                System.Environment.Exit(1);
            });
            watchdog.IsBackground = true;
            watchdog.Start();

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!armed) return;
            var go = new GameObject("EmberUIRenderCheck");
            go.AddComponent<Driver>().StartCoroutine(Watch());
        }

        static IEnumerator Watch()
        {
            float deadline = Time.realtimeSinceStartup + WatchSeconds;
            while (Time.realtimeSinceStartup < deadline) yield return null;

            Application.logMessageReceived -= OnLog;
            armed = false;

            if (errors > 0)
            {
                Debug.LogError("[EmberUIRenderCheck] RESULT: FAIL - " + errors
                    + " error(s)/exception(s) while booting the game\n" + captured);
                EditorApplication.ExitPlaymode();
                System.Environment.Exit(1);
                yield break;
            }

            Debug.Log("[EmberUIRenderCheck] RESULT: PASS - the game booted and drew its UI with no errors");
            EditorApplication.ExitPlaymode();
            System.Environment.Exit(0);
        }

        sealed class Driver : MonoBehaviour { }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errors++;
            if (errors <= 6) captured.AppendLine("  [" + type + "] " + message);
        }
    }
}
