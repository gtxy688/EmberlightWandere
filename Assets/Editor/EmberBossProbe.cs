using UnityEditor;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Temporary Boss-wave diagnostics. Batch mode never enters play mode (with or without
    /// -nographics), so these are driven from the editor during a run instead.
    ///
    /// The in-game hotkeys live on EmberGame.Update, not here: an EditorApplication.update
    /// hook does not receive key input during play mode, which is why binding F8 there did
    /// nothing. These menu items are the fallback for the same two actions.
    ///
    /// Remove this file, EmberCombat.DebugState and EmberCombat.StartWaveForProbe once the
    /// Boss wave behaves.
    /// </summary>
    public static class EmberBossProbe
    {
        [MenuItem("Emberlight/Probe: jump to pre-boss wave", false, 240)]
        public static void JumpToPreBoss()
        {
            var game = Object.FindObjectOfType<EmberGame>();
            if (game == null) { Debug.LogError("[Probe] no EmberGame in the scene"); return; }
            game.ProbeJumpToPreBoss();
        }

        [MenuItem("Emberlight/Probe: dump combat state", false, 241)]
        public static void DumpState()
        {
            var game = Object.FindObjectOfType<EmberGame>();
            if (game == null) { Debug.LogError("[Probe] no EmberGame in the scene"); return; }
            game.ProbeDumpState();
        }
    }
}
