using UnityEditor;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Headless verification that the baked art actually loads and carries usable data.
    /// Checking the loaded object rather than trusting the bake log; sizes and format are
    /// asserted because a texture that imported at the wrong size would silently change how
    /// every shape that uses it is drawn.
    ///
    /// Run via: -executeMethod Emberlight.Editor.EmberBakeVerify.Run
    /// </summary>
    public static class EmberBakeVerify
    {
        static readonly string[] Names = { "art-flame", "art-ring", "art-panel", "art-glow", "art-disc" };
        static readonly int[] Sizes = { 128, 128, 128, 128, 64 };

        public static void Run()
        {
            bool ok = true;

            for (int i = 0; i < Names.Length; i++)
            {
                var t = Resources.Load<Texture2D>("Art/Baked/" + Names[i]);
                if (t == null) { Debug.LogError("[EmberBakeVerify] MISSING Art/Baked/" + Names[i]); ok = false; continue; }

                bool sizeOk = t.width == Sizes[i] && t.height == Sizes[i];
                if (!sizeOk) ok = false;
                Debug.Log(string.Format("[EmberBakeVerify] Art/Baked/{0}: {1}x{2} fmt={3} {4}",
                    Names[i], t.width, t.height, t.format, sizeOk ? "ok" : "SIZE MISMATCH, expected " + Sizes[i]));
            }

            Debug.Log(ok ? "[EmberBakeVerify] RESULT: PASS" : "[EmberBakeVerify] RESULT: FAIL");
            if (!ok) EditorApplication.Exit(1);
        }
    }
}
