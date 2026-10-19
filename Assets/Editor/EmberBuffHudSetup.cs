using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    /// <summary>首次装配 Buff HUD；重复执行保留人工布局。</summary>
    public static class EmberBuffHudSetup
    {
        public const string SlowArtPath = "Assets/Resources/Art/Buffs/buff-slow.png";

        public static EmberBuffHud Configure(Transform hud, TMP_FontAsset font)
        {
            var existing = hud.Find("Buff icons");
            if (existing != null)
            {
                var bound = existing.GetComponent<EmberBuffHud>();
                if (bound == null) throw new InvalidOperationException("Buff icons requires EmberBuffHud.");
                MigrateBannerSpacing(hud, new SerializedObject(bound));
                return bound;
            }
            var slow = ImportSlow();
            var pickups = AssetDatabase.LoadAllAssetsAtPath(EmberPickupArtSetup.AtlasPath).OfType<Sprite>().ToArray();
            var sprites = new[] { pickups.Single(s => s.name == "pickup-power"), pickups.Single(s => s.name == "pickup-haste"), slow };
            var configs = new[] { EmberBuffCatalog.Power, EmberBuffCatalog.Haste, EmberBuffCatalog.Slow };
            var ring = EmberUiPrefabRepair.PersistSprite(EmberArt.Ring, "buff-ring", EmberUiPrefabRepair.SpriteDirectory);
            var disc = AssetDatabase.LoadAssetAtPath<Sprite>(EmberUiPrefabRepair.SpriteDirectory + "/shape-disc.asset");
            if (disc == null) throw new InvalidOperationException("Persistent UI disc sprite is missing.");
            foreach (string legacy in new[] { "Buff effects", "Buff notice", "Buffs" })
            {
                var old = hud.Find(legacy);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var strip = Node("Buff icons", hud, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 72));
            strip.pivot = new Vector2(.5f, 1);
            strip.anchoredPosition = new Vector2(0, -8);
            var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 0, 0, 0);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var view = strip.gameObject.AddComponent<EmberBuffHud>();
            var serialized = new SerializedObject(view);
            var entries = serialized.FindProperty("icons");
            entries.arraySize = configs.Length;
            for (int i = 0; i < configs.Length; i++)
            {
                var slot = Node(configs[i].Id, strip, Vector2.zero, Vector2.zero, new Vector2(60, 72));
                slot.pivot = new Vector2(.5f, 1);
                Picture("Disc", slot, disc, new Vector2(6, -24), new Vector2(48, 48), new Color(.035f, .065f, .085f));
                Picture("Ring track", slot, ring, new Vector2(6, -24), new Vector2(52, 52), new Color(.18f, .24f, .28f));
                ColorUtility.TryParseHtmlString(EmberBuffPresentation.ColorHex(configs[i]), out var tint);
                var fill = Picture("Ring", slot, ring, new Vector2(6, -24), new Vector2(52, 52), tint);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillOrigin = (int)Image.Origin360.Top;
                fill.fillClockwise = true;
                var icon = Picture("Icon", slot, sprites[i], new Vector2(6, -24), new Vector2(42, 42), Color.white);
                var time = EmberUiFactory.Text("Time", slot, font, "8s", 16, Vector2.zero, Vector2.zero,
                    new Color(.96f, .85f, .66f), false);
                time.rectTransform.anchorMin = time.rectTransform.anchorMax = new Vector2(.5f, 1);
                time.rectTransform.sizeDelta = new Vector2(60, 24);
                time.rectTransform.anchoredPosition = new Vector2(0, -60);
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Id").stringValue = configs[i].Id;
                entry.FindPropertyRelative("Root").objectReferenceValue = slot.gameObject;
                entry.FindPropertyRelative("Icon").objectReferenceValue = icon;
                entry.FindPropertyRelative("Ring").objectReferenceValue = fill;
                entry.FindPropertyRelative("Time").objectReferenceValue = time;
                slot.gameObject.SetActive(false);
            }

            var floats = Node("Buff floating", hud.parent, Vector2.one * .5f, Vector2.one * .5f, new Vector2(360, 32));
            serialized.FindProperty("floatingRoot").objectReferenceValue = floats;
            var notices = serialized.FindProperty("floating");
            notices.arraySize = 3;
            for (int i = 0; i < notices.arraySize; i++)
            {
                var label = EmberUiFactory.Text("Notice " + i, floats, font, "", 20, Vector2.one * .5f, Vector2.one * .5f, Color.white, false);
                label.rectTransform.sizeDelta = new Vector2(360, 32);
                var fade = label.gameObject.AddComponent<CanvasGroup>();
                fade.blocksRaycasts = fade.interactable = false;
                var entry = notices.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Text").objectReferenceValue = label;
                entry.FindPropertyRelative("Fade").objectReferenceValue = fade;
                label.gameObject.SetActive(false);
            }
            floats.gameObject.SetActive(false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            strip.gameObject.SetActive(false);
            MigrateBannerSpacing(hud, serialized);
            return view;
        }

        static void MigrateBannerSpacing(Transform hud, SerializedObject view)
        {
            var version = view.FindProperty("layoutVersion");
            if (version.intValue >= 1) return;
            // 固定 Canvas 单位间隔，避免矮窗口中相对锚点把横幅挤进图标区；迁移后不再覆盖手动布局。
            var encounter = (RectTransform)hud.Find("Encounter banner");
            encounter.anchorMin = new Vector2(.06f, 0);
            encounter.anchorMax = new Vector2(.94f, 0);
            encounter.pivot = new Vector2(.5f, 1);
            encounter.sizeDelta = new Vector2(0, 48);
            encounter.anchoredPosition = new Vector2(0, -88);
            var boss = (RectTransform)hud.parent.Find("Boss HUD");
            float hudBottom = ((RectTransform)hud).anchorMin.y;
            boss.anchorMin = new Vector2(.1f, hudBottom);
            boss.anchorMax = new Vector2(.9f, hudBottom);
            boss.pivot = new Vector2(.5f, 1);
            boss.sizeDelta = new Vector2(0, 58);
            boss.anchoredPosition = new Vector2(0, -146);
            version.intValue = 1;
            view.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite ImportSlow()
        {
            AssetDatabase.ImportAsset(SlowArtPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(SlowArtPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Slow icon texture is missing.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = Vector2.one * .5f;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var platform = importer.GetPlatformTextureSettings("Android");
            platform.overridden = true;
            platform.maxTextureSize = 256;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SlowArtPath);
        }

        static RectTransform Node(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)node.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.sizeDelta = size;
            return rect;
        }

        static Image Picture(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var rect = Node(name, parent, new Vector2(0, 1), new Vector2(0, 1), size);
            rect.anchoredPosition = position + new Vector2(24, 0);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
