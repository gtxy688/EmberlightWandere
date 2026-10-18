using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Emberlight.Editor
{
    /// <summary>把已确认的 A 造型接入现有角色插槽；不重建地图、UI 或玩法。</summary>
    public static class EmberCleanToonSetup
    {
        const string Root = "Assets/Art/CleanToonSample";
        const string Prefabs = Root + "/Prefabs";
        const string Legacy = "Assets/Art/HandPaintedSample/Prefabs";

        [MenuItem("Emberlight/Art/Use Clean Toon Characters")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before changing character assets.");
            var set = AssetDatabase.LoadAssetAtPath<EmberArtSet>(EmberArtSampleSetup.SetPath);
            if (set == null) throw new InvalidOperationException("Build the environment sample first.");
            EnsureFolder(Prefabs);
            var keeper = Import("keeper-v1");
            var shade = Import("shade-v1");
            CreatePrefab("Keeper", root =>
            {
                var body = root.transform.Find("Keeper artwork").GetComponent<SpriteRenderer>();
                body.sprite = keeper;
                body.color = Color.white;
                body.transform.localPosition = new Vector2(0, .05f);
                body.transform.localScale = Vector3.one * (1.4f / keeper.bounds.size.y);
                var glow = root.transform.Find("Ground light").GetComponent<SpriteRenderer>();
                glow.transform.localPosition = new Vector2(.16f, -.42f);
                glow.transform.localScale = Vector3.one * (1.05f / glow.sprite.bounds.size.x);
                glow.color = new Color(1, .51f, .13f, .12f);
            });
            CreatePrefab("Shade", root =>
            {
                var serialized = new SerializedObject(root.GetComponent<EmberEnemySilhouette>());
                serialized.FindProperty("ordinarySprite").objectReferenceValue = shade;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });

            Undo.RecordObject(set, "Use clean cartoon characters");
            var art = new SerializedObject(set);
            art.FindProperty("keeperPrefab").objectReferenceValue = LoadPrefab("Keeper");
            art.FindProperty("shadePrefab").objectReferenceValue = LoadPrefab("Shade");
            art.ApplyModifiedProperties();
            set.ValidateReferences();
            AssetDatabase.SaveAssets();
            Debug.Log("CLEAN_TOON_CHARACTERS_READY: Player and ordinary enemy connected; existing prefab edits preserved.");
        }

        static Sprite Import(string name)
        {
            string path = Root + "/Textures/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing character artwork: " + path);
            // 每张 PNG 只有一个完整角色；保留源像素与透明留白，不在运行时切图。
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = Vector2.one * .5f;
            importer.SetTextureSettings(settings);
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Sprite import failed: " + path);
            return sprite;
        }

        static void CreatePrefab(string name, Action<GameObject> configure)
        {
            if (LoadPrefab(name) != null) return;
            string source = Legacy + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(source) == null)
                throw new InvalidOperationException("Missing character template: " + source);
            var root = PrefabUtility.LoadPrefabContents(source);
            try
            {
                configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab", out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save character prefab: " + name);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static GameObject LoadPrefab(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + name + ".prefab");

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
