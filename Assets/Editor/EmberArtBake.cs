using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>
    /// Bakes the code-native sprites (EmberArt / EmberVisuals.Disc) into PNG assets so the
    /// shapes are pre-prepared instead of rasterised at runtime.
    ///
    /// The pixel maths below is a verbatim copy of what EmberArt.Create / EmberVisuals.Disc
    /// used to run per session. Keeping it here means the baked PNGs are byte-identical to
    /// the old runtime output; the bake tool is the single source of truth for the shapes.
    /// </summary>
    public static class EmberArtBake
    {
        const string OutDir = "Assets/Resources/Art/Baked";
        const string OutDirWithSlash = OutDir + "/";

        struct Entry
        {
            public string File;
            public int Size;
            public int Kind;      // Disc = -1, otherwise EmberArt kind
            public float Ppu;
            public Vector4 Border;
            public Entry(string file, int size, int kind, float ppu, Vector4 border)
            { File = file; Size = size; Kind = kind; Ppu = ppu; Border = border; }
        }

        static readonly Entry[] Entries =
        {
            new Entry("art-flame.png", 128, 0, 128f, Vector4.zero),
            new Entry("art-ring.png",  128, 1, 128f, Vector4.zero),
            new Entry("art-panel.png", 128, 2, 128f, new Vector4(24, 24, 24, 24)),
            new Entry("art-glow.png",  128, 3, 128f, Vector4.zero),
            new Entry("art-disc.png",   64, -1, 64f, Vector4.zero),
        };

        /// <summary>
        /// Headless entry point for a single -executeMethod run: measures the old runtime cost
        /// then bakes the art, so the measurement is recorded next to the bake replacing it.
        ///
        /// There is deliberately no font step. Pre-baking the Chinese SDF atlas was tried twice
        /// and both assets threw on the first label drawn, so EmberFonts always builds the
        /// dynamic atlas; see the comment there.
        /// </summary>
        public static void BakeAll()
        {
            UnityEngine.Debug.Log("[EmberArtBake] ===== measure runtime cost (pre-bake) =====");
            Measure();
            UnityEngine.Debug.Log("[EmberArtBake] ===== bake art + integrity check =====");
            Bake();
            UnityEngine.Debug.Log("[EmberArtBake] ===== done =====");
        }

        [MenuItem("Emberlight/Bake runtime art", false, 210)]
        public static void Bake()
        {
            Directory.CreateDirectory(OutDir);
            var report = new System.Text.StringBuilder();
            var total = Stopwatch.StartNew();
            var written = new byte[Entries.Length][];

            for (int i = 0; i < Entries.Length; i++)
            {
                var e = Entries[i];
                var sw = Stopwatch.StartNew();
                var tex = Rasterise(e);
                sw.Stop();
                var rasterMs = sw.Elapsed.TotalMilliseconds;

                sw.Restart();
                var png = tex.EncodeToPNG();
                sw.Stop();
                var encodeMs = sw.Elapsed.TotalMilliseconds;

                File.WriteAllBytes(OutDirWithSlash + e.File, png);
                written[i] = png;
                UnityEngine.Object.DestroyImmediate(tex);

                report.AppendLine(string.Format("  {0,-16} {1,3}x{1,-3} raster {2,7:F3} ms   encode {3,7:F3} ms   {4,6} KB",
                    e.File, e.Size, rasterMs, encodeMs, png.Length / 1024));
            }

            AssetDatabase.Refresh();
            foreach (var e in Entries) ConfigureImporter(OutDirWithSlash + e.File);
            AssetDatabase.Refresh();
            total.Stop();

            var verify = Verify(written);
            UnityEngine.Debug.Log("[EmberArtBake] baked " + Entries.Length + " sprites in "
                + total.Elapsed.TotalMilliseconds.ToString("F1") + " ms total\n" + report + verify);
        }

        /// <summary>
        /// The pixel maths now lives in two places: here and the runtime fallback in
        /// EmberArt/EmberVisuals. Decode the PNG we just wrote and compare it against a fresh
        /// rasterisation, so editing one without the other is caught here instead of turning
        /// into visible art drift. Decoding from the captured bytes avoids needing a readable
        /// importer (isReadable stays false for shipping).
        /// </summary>
        static string Verify(byte[][] pngs)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("  --- integrity check (baked PNG vs rasteriser) ---");
            for (int i = 0; i < Entries.Length; i++)
            {
                var e = Entries[i];
                var baked = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!baked.LoadImage(pngs[i]))
                {
                    sb.AppendLine("  " + e.File + ": could not decode");
                    UnityEngine.Object.DestroyImmediate(baked);
                    continue;
                }
                Compare(sb, e, baked);
                UnityEngine.Object.DestroyImmediate(baked);
            }
            return sb.ToString();
        }

        static void Compare(System.Text.StringBuilder sb, Entry e, Texture2D baked)
        {
            var live = Rasterise(e);
            int w = Mathf.Min(baked.width, live.width), h = Mathf.Min(baked.height, live.height);
            var a = baked.GetPixels32();
            var b = live.GetPixels32();
            long worst = 0, diffCount = 0;
            for (int i = 0; i < w * h; i++)
            {
                long da = System.Math.Abs(a[i].a - b[i].a);
                long dr = System.Math.Abs(a[i].r - b[i].r);
                long dg = System.Math.Abs(a[i].g - b[i].g);
                long db = System.Math.Abs(a[i].b - b[i].b);
                long d = System.Math.Max(System.Math.Max(da, dr), System.Math.Max(dg, db));
                if (d != 0) diffCount++;
                if (d > worst) worst = d;
            }
            sb.AppendLine(string.Format("  {0,-16} {1}x{2}  size {3}  maxDelta {4}  differing px {5}",
                e.File, baked.width, baked.height,
                (baked.width == e.Size && baked.height == e.Size) ? "ok" : "MISMATCH",
                worst, diffCount));
            UnityEngine.Object.DestroyImmediate(live);
        }

        [MenuItem("Emberlight/Measure runtime art cost", false, 211)]
        public static void Measure()
        {
            var report = new System.Text.StringBuilder();
            double rasterTotal = 0;

            foreach (var e in Entries)
            {
                // Rasterise() includes Apply(); time it as one unit and report that, rather
                // than splitting out an Apply that the raster timer already counted.
                var sw = Stopwatch.StartNew();
                var tex = Rasterise(e);
                sw.Stop();
                var rasterMs = sw.Elapsed.TotalMilliseconds;
                rasterTotal += rasterMs;

                report.AppendLine(string.Format("  {0,-16} {1,3}x{1,-3} raster+Apply {2,7:F3} ms",
                    e.File, e.Size, rasterMs));
                UnityEngine.Object.DestroyImmediate(tex);
            }

            report.AppendLine(string.Format("  TOTAL one-off cost removed from each session: {0:F2} ms", rasterTotal));
            UnityEngine.Debug.Log("[EmberArtBake] MEASURE (the work each shape did once per session at runtime)\n" + report);
        }

        /// <summary>Verbatim port of the old runtime rasterisers.</summary>
        static Texture2D Rasterise(Entry e)
        {
            int n = e.Size;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;

            if (e.Kind < 0)
            {
                // EmberVisuals.Disc
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                        t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31.5f - d)));
                    }
            }
            else
            {
                int kind = e.Kind;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = (x + .5f) / n * 2 - 1, v = (y + .5f) / n * 2 - 1, a;
                        if (kind == 0)
                        {
                            float h = (v + 1) * .5f;
                            float width = Mathf.Sin(Mathf.Pow(h, .65f) * Mathf.PI) * .68f;
                            float bend = .22f * h * h;
                            a = Mathf.Clamp01((width - Mathf.Abs(u - bend)) * n * .5f);
                        }
                        else if (kind == 3) a = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(u * u + v * v)), 2);
                        else if (kind == 1) a = Mathf.Clamp01((.045f - Mathf.Abs(Mathf.Sqrt(u * u + v * v) - .86f)) * n);
                        else
                        {
                            Vector2 q = new Vector2(Mathf.Abs(u) - .75f, Mathf.Abs(v) - .75f);
                            float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude
                                      + Mathf.Min(Mathf.Max(q.x, q.y), 0) - .23f;
                            a = Mathf.Clamp01(-d * n * .5f);
                        }
                        t.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
            }

            t.Apply();
            return t;
        }

        static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                UnityEngine.Debug.LogError("[EmberArtBake] no TextureImporter for " + path);
                return;
            }
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
