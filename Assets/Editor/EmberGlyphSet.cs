using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Resolves every character the UI can actually draw, and reports any that the font's
    /// glyph set would miss.
    ///
    /// This matters more now that the atlas is baked: a dynamic atlas silently adds a glyph
    /// the first time it is asked for one, but a Static atlas cannot, so a character missing
    /// from EmberFonts.GlyphSet becomes a permanent blank box in the built game. The set was
    /// hand-maintained by reading the source, and had already drifted (U+3000 was used by
    /// EmberGame but never added).
    ///
    /// Strings are read off the loaded assembly rather than the .cs files, so the escapes,
    /// concatenations and array initialisers are already resolved to their runtime values.
    /// </summary>
    public static class EmberGlyphSet
    {
        /// <summary>Every string value reachable from Emberlight types (fields, consts, arrays, properties).</summary>
        public static HashSet<char> CollectGameCharacters()
        {
            var found = new HashSet<char>();
            var visited = new HashSet<object>();

            foreach (var t in typeof(EmberGame).Assembly.GetTypes())
            {
                if (t.Namespace == null || !t.Namespace.StartsWith("Emberlight")) continue;
                if (t.IsAbstract && t.IsSealed && t.Name == "EmberGlyphSet") continue;

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                                         | BindingFlags.DeclaredOnly;

                foreach (var f in t.GetFields(flags))
                    Scavenge(f.GetValue(null), found, visited, 0);

                foreach (var p in t.GetProperties(flags))
                {
                    if (p.GetIndexParameters().Length != 0 || p.GetGetMethod() == null) continue;
                    object v;
                    try { v = p.GetValue(null, null); } catch { continue; }
                    Scavenge(v, found, visited, 0);
                }
            }
            return found;
        }

        static void Scavenge(object v, HashSet<char> found, HashSet<object> visited, int depth)
        {
            if (v == null || depth > 3) return;

            var s = v as string;
            if (s != null)
            {
                foreach (var c in s) found.Add(c);
                return;
            }
            var e = v as System.Collections.IEnumerable;
            if (e != null && !(v is string) && visited.Add(v))
            {
                foreach (var item in e) Scavenge(item, found, visited, depth + 1);
            }
        }

        /// <summary>Diagnostic entry point: report characters the baked atlas would drop.</summary>
        public static void Report()
        {
            var used = CollectGameCharacters();
            var declared = new HashSet<char>(EmberFonts.GlyphSet);
            var missing = new List<char>();
            foreach (var c in used) if (!declared.Contains(c)) missing.Add(c);

            missing.Sort();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[EmberGlyphSet] game text uses " + used.Count + " distinct characters; GlyphSet declares " + declared.Count);
            if (missing.Count == 0)
            {
                sb.AppendLine("[EmberGlyphSet] RESULT: PASS - no character used by the UI is missing from the glyph set");
            }
            else
            {
                sb.AppendLine("[EmberGlyphSet] RESULT: " + missing.Count + " character(s) used by the UI are NOT in GlyphSet:");
                foreach (var c in missing)
                {
                    bool printable = !char.IsControl(c) && !char.IsWhiteSpace(c);
                    sb.AppendLine(string.Format("    U+{0:X4}  {1}", (int)c,
                        printable ? "'" + c + "'" : "(" + (c == '\u3000' ? "ideographic space" : "whitespace/control") + ")"));
                }
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>GlyphSet plus everything the UI is discovered to use, for a complete bake.</summary>
        public static string EffectiveGlyphSet()
        {
            var all = CollectGameCharacters();
            foreach (var c in EmberFonts.GlyphSet) all.Add(c);
            var list = new List<char>(all);
            list.Sort();
            return new string(list.ToArray());
        }
    }
}
