using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Diagnostic helper. Batch mode does not enter play mode reliably (neither with nor
    /// without -nographics), so this is driven from the editor instead.
    ///
    /// In play mode: press F8 to jump to the wave before the Boss wave, F9 to dump the
    /// combat state. The state dump is what tells apart "the Boss never spawned" from
    /// "the Boss spawned and was cleared".
    /// </summary>
    public static class EmberBossHelpers
    {
        /// <summary>Jump to the wave right before the configured Boss wave.</summary>
        [MenuItem("Emberlight/Probe: jump to pre-boss wave", false, 240)]
        public static void JumpToPreBoss()
        {
            var game = Object.FindObjectOfType<EmberGame>();
            var combat = Field(game, "combat");
            if (combat == null) { Debug.LogError("[Probe] not in a run"); return; }
            int bossWave = BossWave(combat);
            Invoke(combat, "StartWave", bossWave - 1);
            Debug.Log("[Probe] jumped to wave " + (bossWave - 1) + " (boss wave is " + bossWave + ")\n" + Dump(combat));
        }

        /// <summary>Dump the combat state so the Boss-wave behaviour can be read off a log.</summary>
        [MenuItem("Emberlight/Probe: dump combat state", false, 241)]
        public static void DumpState()
        {
            var game = Object.FindObjectOfType<EmberGame>();
            var combat = Field(game, "combat");
            if (combat == null) { Debug.LogError("[Probe] not in a run"); return; }
            Debug.Log("[Probe] state=" + (game != null ? game.State.ToString() : "?") + " :: " + Dump(combat));
        }

        /// <summary>Editor-only hotkeys so a log can be captured without leaving play mode.</summary>
        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.update += () =>
            {
                if (!EditorApplication.isPlaying) return;
                if (Input.GetKeyDown(KeyCode.F8)) JumpToPreBoss();
                if (Input.GetKeyDown(KeyCode.F9)) DumpState();
            };
        }

        static int BossWave(object combat)
        {
            var cfg = Field(combat, "config");
            if (cfg == null) return 25;
            var f = cfg.GetType().GetField("BossWave");
            return f != null ? (int)f.GetValue(cfg) : 25;
        }

        static string Dump(object combat)
        {
            if (combat == null) return "<no combat>";
            var m = combat.GetType().GetMethod("DebugState");
            return m != null ? (string)m.Invoke(combat, null) : "<no DebugState>";
        }

        static object Field(object target, string name)
        {
            if (target == null) return null;
            var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return f != null ? f.GetValue(target) : null;
        }

        static object Invoke(object target, string name, params object[] args)
        {
            var t = target.GetType();
            var m = t.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (m == null) { Debug.LogError("[Probe] method not found: " + name); return null; }
            return m.Invoke(target, args);
        }
    }
}

