using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Emberlight.Editor
{
    /// <summary>将代码原生的二维轮廓和衣褶烘为共享图集；运行时不逐像素绘制。</summary>
    public static class EmberCharacterArtBake
    {
        public const string AtlasPath = "Assets/Resources/Art/Characters/classic-character-parts.png";
        const int Cell = 256;
        static readonly string[] Names = { "classic-cloak", "classic-hood", "classic-hem", "classic-lantern" };

        [MenuItem("Emberlight/Art/Bake Classic Character Parts")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before baking character parts.");
            const string folder = "Assets/Resources/Art/Characters";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources/Art", "Characters");
            var outlines = Outlines();
            var pixels = new Color32[Cell * 2 * Cell * 2];
            for (int part = 0; part < outlines.Length; part++)
            {
                int left = (part % 2) * Cell, bottom = (1 - part / 2) * Cell;
                for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                {
                    var point = new Vector2((x + .5f) / Cell, (y + .5f) / Cell);
                    float distance = SignedDistance(point, outlines[part]);
                    float alpha = Mathf.Clamp01(.5f + distance * Cell);
                    if (alpha <= 0) continue;
                    byte grey = (byte)Mathf.RoundToInt(Shade(part, point, distance) * 255);
                    pixels[(bottom + y) * Cell * 2 + left + x] = new Color32(grey, grey, grey, (byte)Mathf.RoundToInt(alpha * 255));
                }
            }
            var texture = new Texture2D(Cell * 2, Cell * 2, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(AtlasPath, texture.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(texture); }
            Import();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            if (sprites.Length != Names.Length || Names.Any(name => !sprites.Any(sprite => sprite.name == name)))
                throw new InvalidOperationException("The character atlas is missing persistent sprite slices.");
            AssetDatabase.SaveAssets();
            Debug.Log("CLASSIC_CHARACTER_ART_READY: Four persistent parts; original animation roots preserved.");
        }

        static void Import()
        {
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = Cell;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = Cell * 2;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = Cell * 2;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Sprite Editor data provider is unavailable.");
            provider.InitSpriteEditorDataProvider();
            var identities = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (identities == null) throw new InvalidOperationException("Persistent sprite identities are unavailable.");
            var existing = provider.GetSpriteRects().ToDictionary(rect => rect.name);
            var rects = new SpriteRect[Names.Length];
            for (int i = 0; i < rects.Length; i++)
                rects[i] = new SpriteRect
                {
                    name = Names[i], spriteID = existing.TryGetValue(Names[i], out var old) ? old.spriteID : GUID.Generate(),
                    rect = new Rect((i % 2) * Cell, (1 - i / 2) * Cell, Cell, Cell),
                    alignment = SpriteAlignment.Center, pivot = Vector2.one * .5f
                };
            provider.SetSpriteRects(rects);
            identities.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        // 四种轮廓均保持单位格与居中轴，替换图元不改变调用方的缩放与碰撞。
        static Vector2[][] Outlines()
        {
            var cloak = new Contour(.30f, .93f)
                .Curve(.35f, .99f, .67f, .99f, .72f, .92f)
                .Curve(.80f, .80f, .75f, .62f, .82f, .47f)
                .Curve(.89f, .31f, .90f, .17f, .98f, .07f)
                .Curve(.85f, .12f, .82f, .20f, .77f, .21f)
                .Curve(.73f, .11f, .69f, .03f, .63f, .04f)
                .Curve(.58f, .04f, .54f, .12f, .50f, .15f)
                .Curve(.45f, .13f, .39f, .04f, .31f, .07f)
                .Curve(.23f, .10f, .24f, .20f, .18f, .18f).Line(.02f, .09f)
                .Curve(.08f, .28f, .17f, .46f, .18f, .63f)
                .Curve(.20f, .78f, .22f, .86f, .30f, .93f);
            var hood = new Contour(.10f, .24f)
                .Curve(.02f, .40f, .04f, .71f, .19f, .84f)
                .Curve(.29f, .94f, .42f, .98f, .56f, .94f)
                .Curve(.73f, .96f, .91f, .85f, .96f, .66f)
                .Curve(1f, .45f, .94f, .27f, .86f, .18f)
                .Curve(.73f, .10f, .60f, .11f, .50f, .14f)
                .Curve(.37f, .11f, .18f, .13f, .10f, .24f);
            var hem = new Contour(.13f, .98f)
                .Curve(.24f, .71f, .31f, .39f, .04f, .10f)
                .Curve(.28f, .19f, .48f, .14f, .65f, .02f)
                .Curve(.61f, .32f, .95f, .66f, .84f, .98f).Line(.13f, .98f);
            var lantern = new Contour(.38f, .98f).Line(.62f, .98f).Line(.66f, .86f)
                .Line(.89f, .75f).Line(.83f, .16f).Line(.69f, .08f).Line(.66f, .02f)
                .Line(.34f, .02f).Line(.31f, .08f).Line(.17f, .16f).Line(.11f, .75f)
                .Line(.34f, .86f).Line(.38f, .98f);
            return new[] { cloak.Points, hood.Points, hem.Points, lantern.Points };
        }

        static float Shade(int part, Vector2 p, float edge)
        {
            float fill = .99f - p.x * .16f - (1 - p.y) * .07f;
            if (part == 0 || part == 2)
            {
                float leftFold = (p.x - (.30f + p.y * .11f)) / .035f;
                float rightFold = (p.x - (.65f - p.y * .06f)) / .045f;
                fill -= (Mathf.Exp(-leftFold * leftFold) * .24f + Mathf.Exp(-rightFold * rightFold) * .16f) * (1 - p.y * .6f);
            }
            else if (part == 1)
            {
                fill -= p.x * .14f;
                float fold = (p.x - (.69f - p.y * .14f)) / .05f;
                fill -= Mathf.Exp(-fold * fold) * p.y * .12f;
            }
            else fill -= Mathf.Abs(p.x - .5f) * .25f;
            // 深色细边与左上方受光均烘进部件，共享材质即可显示层次。
            float rim = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.008f, .026f, edge));
            return Mathf.Clamp01(Mathf.Lerp(.27f, fill, rim));
        }

        static float SignedDistance(Vector2 p, Vector2[] outline)
        {
            bool inside = false;
            float nearest = float.MaxValue;
            for (int i = 0, j = outline.Length - 1; i < outline.Length; j = i++)
            {
                var a = outline[j];
                var b = outline[i];
                var segment = b - a;
                float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude) : 0;
                nearest = Mathf.Min(nearest, (p - a - segment * t).sqrMagnitude);
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return Mathf.Sqrt(nearest) * (inside ? 1 : -1);
        }

        sealed class Contour
        {
            readonly List<Vector2> points = new List<Vector2>();
            public Vector2[] Points => points.ToArray();
            public Contour(float x, float y) { points.Add(new Vector2(x, y)); }
            public Contour Line(float x, float y) { points.Add(new Vector2(x, y)); return this; }
            public Contour Curve(float ax, float ay, float bx, float by, float x, float y)
            {
                var start = points[points.Count - 1];
                var a = new Vector2(ax, ay); var b = new Vector2(bx, by); var end = new Vector2(x, y);
                for (int i = 1; i <= 12; i++)
                {
                    float t = i / 12f, s = 1 - t;
                    points.Add(s * s * s * start + 3 * s * s * t * a + 3 * s * t * t * b + t * t * t * end);
                }
                return this;
            }
        }
    }
}
