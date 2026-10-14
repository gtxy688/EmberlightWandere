using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    public static class EmberUiPrefabRepair
    {
        public const string SpriteDirectory = "Assets/UI/Generated/Sprites";
        const string RootPrefab = "Assets/UI/UIRoot.prefab";

        [MenuItem("Emberlight/UI/Repair UIRoot Images")]
        public static void RepairUiRoot()
        {
            RepairPrefab(RootPrefab);
        }

        [MenuItem("Emberlight/UI/Repair Selected Images")]
        public static void RepairSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null) throw new InvalidOperationException("Select a UI object or prefab first.");
            if (EditorUtility.IsPersistent(selected))
            {
                RepairPrefab(AssetDatabase.GetAssetPath(selected));
                return;
            }

            // Scene/Prefab Mode: allow Undo, keep changes pending for the user to save/apply.
            int count = RepairHierarchy(selected, SpriteDirectory, true);
            if (count > 0) EditorSceneManager.MarkSceneDirty(selected.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UI image repair] Selected hierarchy: {count} bindings restored. Save the scene or apply prefab overrides to keep them.", selected);
        }

        [MenuItem("Emberlight/UI/Repair Selected Images", true)]
        static bool CanRepairSelected() => !EditorApplication.isPlayingOrWillChangePlaymode && Selection.activeGameObject != null;

        [MenuItem("Emberlight/UI/Repair UIRoot Images", true)]
        static bool CanRepairUiRoot() => !EditorApplication.isPlayingOrWillChangePlaymode && AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefab) != null;

        public static int RepairPrefab(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before repairing a prefab.");
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new ArgumentException("Expected an existing prefab asset.", nameof(path));

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int count = RepairHierarchy(root, SpriteDirectory);
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                    if (!saved) throw new IOException("Could not save repaired prefab: " + path);
                    AssetDatabase.SaveAssets();
                }
                Debug.Log($"[UI image repair] {path}: {count} bindings restored. Existing asset references and layout preserved.");
                return count;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>Restore known runtime UI images, including inactive children. Leave unrelated and intentionally empty images alone.</summary>
        public static int RepairHierarchy(GameObject root, string spriteDirectory, bool recordUndo = false)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (!spriteDirectory.StartsWith("Assets/", StringComparison.Ordinal) || spriteDirectory.Contains("..") || spriteDirectory.Contains('\\'))
                throw new ArgumentException("Use a project-relative Assets/... directory.", nameof(spriteDirectory));

            int repaired = 0;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                // Never replace a manually assigned, persistent sprite.
                if (image.sprite != null && EditorUtility.IsPersistent(image.sprite)) continue;
                var source = Resolve(image, out string key);
                if (source == null) continue;
                var sprite = PersistSprite(source, key, spriteDirectory);
                if (recordUndo) Undo.RecordObject(image, "Repair UI image");
                image.sprite = sprite;
                EditorUtility.SetDirty(image);
                if (recordUndo && PrefabUtility.IsPartOfPrefabInstance(image))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                repaired++;
            }
            return repaired;
        }

        static Sprite PersistSprite(Sprite source, string key, string directory)
        {
            if (EditorUtility.IsPersistent(source)) return source;
            if (source.texture == null || !EditorUtility.IsPersistent(source.texture))
                throw new InvalidOperationException($"{key}: source texture is temporary. Run Emberlight/Bake runtime art first.");

            EnsureFolder(directory);
            string path = directory + "/" + key + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new IOException("Another asset already occupies: " + path);

            // Clone exactly what the runtime draws: keep crop, pivot, PPU and nine-slice border.
            // The original texture remains an imported PNG; no atlas import settings are changed.
            var saved = UnityEngine.Object.Instantiate(source);
            saved.name = key;
            AssetDatabase.CreateAsset(saved, path);
            return saved;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        static Sprite Resolve(Image image, out string key)
        {
            string name = image.name;
            key = "shape-panel";
            switch (name)
            {
                case "Joystick": case "Knob": case "Handle": case "Orange center":
                    key = "shape-disc"; return EmberVisuals.Disc;
                case "Brand": case "Wanderer emblem":
                    key = "world-brand"; return EmberWorldArt.Get(8);
                case "Night sky": case "Loading track": case "Loading fill":
                    return EmberArt.Panel;
                case "Battle HUD": case "背景音乐": case "游戏音效": case "Game speed": case "Hint":
                    return Ui(EmberUiArt.Piece.Panel, out key);
                case "Pause":
                    return Frame(EmberRarity.Silver, out key);
                case "Boss HUD":
                    return Frame(EmberRarity.Gold, out key);
                case "Emblem":
                    if (image.transform.parent.name == "Blessing veil") return Ui(EmberUiArt.Piece.Lantern, out key);
                    key = "shape-glow"; return EmberArt.Glow;
                case "Confirm": case "Confirm starter":
                    return Ui(EmberUiArt.Piece.Primary, out key);
                case "Doodle icon":
                    var card = FindCard(image.transform);
                    if (card == null || !TryOfferId(card, out int offerId)) return null;
                    key = "offer-" + offerId; return EmberCardIcons.ForOffer(offerId);
                case "Route tint": case "Refresh": case "Encounter banner":
                case "Life track": case "Life fill": case "Shield track": case "Shield fill":
                case "Wave track": case "Wave fill": case "Boss life track": case "Boss life fill":
                    return EmberArt.Panel;
                case "Background": case "Fill":
                    return image.GetComponentInParent<Slider>(true) != null ? EmberArt.Panel : null;
            }
            if (name.StartsWith("Blessing card ", StringComparison.Ordinal)) return Frame(CardRarity(image.transform), out key);
            if (name.StartsWith("Roster ", StringComparison.Ordinal)) return Frame(EmberRarity.Gold, out key);
            if (name.StartsWith("Starter doodle ", StringComparison.Ordinal) && int.TryParse(name.Substring(14), out int starterId))
            {
                key = "offer-" + starterId; return EmberCardIcons.ForOffer(starterId);
            }
            if (name.StartsWith("Speed ", StringComparison.Ordinal) && image.transform.parent.name == "Game speed") return EmberArt.Panel;
            if (name.StartsWith("Button ", StringComparison.Ordinal))
                return Ui(image.transform.parent.name == "Settings" || image.transform.GetSiblingIndex() != 0
                    ? EmberUiArt.Piece.Secondary : EmberUiArt.Piece.Primary, out key);
            if (name.StartsWith("Slot ", StringComparison.Ordinal) && int.TryParse(name.Substring(5), out int slot))
                return Ui(slot == 2 ? EmberUiArt.Piece.Primary : EmberUiArt.Piece.Secondary, out key);
            // Overlay, viewports, card faces and slider hit areas intentionally have no sprite.
            return null;
        }

        static Sprite Ui(EmberUiArt.Piece piece, out string key)
        {
            key = "ui-" + piece;
            return EmberUiArt.Get(piece);
        }

        static Sprite Frame(EmberRarity rarity, out string key)
        {
            key = "frame-" + rarity;
            return EmberCardFrames.Get(rarity);
        }

        static Transform FindCard(Transform child)
        {
            for (var current = child.parent; current != null; current = current.parent)
                if (current.name.StartsWith("Blessing card ", StringComparison.Ordinal)) return current;
            return null;
        }

        static EmberRarity CardRarity(Transform card)
        {
            var badge = card.Find("Card face/Route tint");
            if (badge != null)
                foreach (var label in badge.GetComponentsInChildren<TMP_Text>(true))
                    for (int i = 0; i < 4; i++)
                        if (label.text.Contains(EmberRarityUtil.Name((EmberRarity)i))) return (EmberRarity)i;
            // Initialize warms the captured card cache with gold shield cards.
            return EmberRarity.Gold;
        }

        static bool TryOfferId(Transform card, out int id)
        {
            // Cached card names can be stale after Show reuses them; prefer the current title.
            var face = card.Find("Card face");
            if (face != null)
                foreach (Transform child in face)
                {
                    var title = child.GetComponent<TMP_Text>();
                    if (title == null) continue;
                    for (int i = 0; i < EmberCopy.UpgradeNames.Length; i++)
                        if (!string.IsNullOrEmpty(EmberCopy.UpgradeNames[i]) && title.text == EmberCopy.UpgradeNames[i]) { id = i; return true; }
                    break;
                }
            return int.TryParse(card.name.Substring("Blessing card ".Length), out id);
        }
    }
}
