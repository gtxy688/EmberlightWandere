using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Boots the real game scene in play mode and fails on any logged error or exception.
    ///
    /// This exists because the previous round of checks passed a font asset that crashed the
    /// game. EmberBakeVerify only inspected the asset's fields, and a font whose
    /// m_AtlasTextures array was never serialised still reports non-zero glyph tables, so the
    /// fault only appears when something actually draws text. The loading screen builds TMP
    /// labels immediately, so a few seconds of play mode is enough to catch it.
    ///
    /// Run via: -executeMethod Emberlight.Editor.EmberUIRenderCheck.Run
    /// </summary>
    public static class EmberUIRenderCheck
    {
        const string ScenePath = "Assets/Scenes/Emberlight.unity";

        static int errors;
        static readonly System.Text.StringBuilder log = new System.Text.StringBuilder();

        public static void Run()
        {
            errors = 0;
            Application.logMessageReceived += OnLog;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
            EditorApplication.update += Tick;
        }

        static double started;

        static void Tick()
        {
            if (started == 0) { started = EditorApplication.timeSinceStartup; return; }

            // Loading screen builds its TextMeshProUGUI labels on the first frames, which is
            // exactly where the broken atlas threw.
            if (EditorApplication.timeSinceStartup - started < 8.0) return;

            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
            Application.logMessageReceived -= OnLog;

            if (errors > 0)
            {
                Debug.LogError("[EmberUIRenderCheck] RESULT: FAIL - " + errors
                    + " error(s)/exception(s) while booting the game\n" + log);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[EmberUIRenderCheck] RESULT: PASS - the game booted and drew its UI with no errors");
            EditorApplication.Exit(0);
        }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errors++;
            if (errors <= 8) log.AppendLine("  [" + type + "] " + message);
        }
    }
}
