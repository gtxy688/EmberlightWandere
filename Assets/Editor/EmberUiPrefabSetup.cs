using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    /// <summary>补齐 UIRoot 的运行时引用与缺失控件；日常布局直接编辑 Prefab。</summary>
    public static class EmberUiPrefabSetup
    {
        const string PrefabPath = "Assets/UI/UIRoot.prefab";
        const string ScenePath = "Assets/Scenes/Emberlight.unity";

        [MenuItem("Emberlight/UI/Connect UIRoot Runtime")]
        public static void Connect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            ConfigurePrefab();
            EmberIntroPrefabSetup.ConfigurePrefab();
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.Log("UIRoot prefab configured. Open Emberlight scene and run this menu to assign its runtime reference.");
                return;
            }
            ConnectScene(scene);
            Debug.Log("UIRoot runtime connected. Save the scene to retain its assigned reference.");
        }

        // 批处理入口，仅作用于显式打开的工程；资产写入一律走 Editor API。
        public static void ConnectProject()
        {
            ConfigurePrefab();
            EmberIntroPrefabSetup.ConfigurePrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            ConnectScene(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the connected scene.");
        }

        public static void ConfigurePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<EmberUiRoot>() ?? root.AddComponent<EmberUiRoot>();
                view.Safe = At<RectTransform>(root.transform, "Safe area");
                view.Overlay = At<Transform>(view.Safe, "Overlay").gameObject;
                view.Brand = At<Transform>(view.Overlay.transform, "Brand").gameObject;
                view.Heading = At<TextMeshProUGUI>(view.Overlay.transform, "Title");
                view.Summary = At<TextMeshProUGUI>(view.Overlay.transform, "Description");
                view.StickBase = At<RectTransform>(view.Safe, "Joystick");
                view.StickKnob = At<RectTransform>(view.StickBase, "Knob");
                view.Upgrade = At<EmberUpgradePanel>(root.transform, "Safe area");
                view.Settings = At<EmberSettingsPanel>(root.transform, "Safe area");
                view.GodsSelect = At<EmberGodsSelectPanel>(root.transform, "Safe area");
                var font = EmberFonts.CreateChinese(); // 与启动一致，走 ValidateBaked 闸门。
                ConfigureMenu(view, font);
                ConfigureHud(view);
                ConfigureUpgrade(view);
                ConfigureSettings(view);
                ConfigureGods(view, font);
                foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true)) label.font = font;
                EmberUiPrefabRepair.RepairHierarchy(root, EmberUiPrefabRepair.SpriteDirectory);
                view.ValidateReferences();
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save configured UIRoot.");
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ConnectScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var views = roots.SelectMany(r => r.GetComponentsInChildren<EmberUiRoot>(true)).ToArray();
            if (views.Length > 1) throw new InvalidOperationException("Keep exactly one UIRoot in the scene.");
            var view = views.Length == 1 ? views[0] :
                ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene)).GetComponent<EmberUiRoot>();
            var game = roots.SelectMany(r => r.GetComponentsInChildren<EmberGame>(true)).Single();
            var intros = roots.SelectMany(r => r.GetComponentsInChildren<EmberIntroView>(true)).ToArray();
            if (intros.Length > 1) throw new InvalidOperationException("Keep exactly one IntroRoot in the scene.");
            var intro = intros.Length == 1 ? intros[0] :
                ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EmberIntroPrefabSetup.PrefabPath), scene)).GetComponent<EmberIntroView>();
            intro.gameObject.SetActive(false); // 主场景编辑时默认预览菜单，加载页在 Prefab Mode 预览。
            var serialized = new SerializedObject(game);
            serialized.FindProperty("uiRoot").objectReferenceValue = view;
            serialized.FindProperty("introRoot").objectReferenceValue = intro;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void ConfigureMenu(EmberUiRoot view, TMP_FontAsset font)
        {
            var parent = At<Transform>(view.Overlay.transform, "Choices");
            var buttons = parent.GetComponentsInChildren<Button>(true).ToList();
            while (buttons.Count < 3)
            {
                int i = buttons.Count;
                var image = EmberUiFactory.Box("Button choice " + i, parent,
                    new Vector2(.09f, .43f - i * .135f), new Vector2(.91f, .54f - i * .135f), Color.white);
                image.sprite = EmberUiArt.Get(i == 0 ? EmberUiArt.Piece.Primary : EmberUiArt.Piece.Secondary);
                image.pixelsPerUnitMultiplier = 2;
                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                var label = EmberUiFactory.Text("Label", image.transform, font, "", 23,
                    new Vector2(.14f, .10f), new Vector2(.94f, .90f), new Color(.98f, .88f, .69f), true);
                label.enableAutoSizing = true;
                label.fontSizeMin = 16;
                label.fontSizeMax = 23;
                buttons.Add(button);
            }
            view.MenuButtons = buttons.ToArray();
            var hint = view.Overlay.transform.Find("Hint");
            if (hint == null)
            {
                var face = EmberUiFactory.Box("Hint", view.Overlay.transform, new Vector2(.07f, .045f),
                    new Vector2(.93f, .15f), new Color(.13f, .19f, .25f, .96f));
                face.sprite = EmberUiArt.Get(EmberUiArt.Piece.Panel);
                face.pixelsPerUnitMultiplier = 3;
                face.gameObject.AddComponent<Button>();
                EmberUiFactory.Text("Hint text", face.transform, font, EmberCopy.MenuHint, 16,
                    new Vector2(.06f, .16f), new Vector2(.94f, .84f), new Color(1, .89f, .66f), true);
                hint = face.transform;
            }
            view.Hint = hint.gameObject;
            view.HintText = At<TextMeshProUGUI>(hint, "Hint text");
            view.Heading.text = EmberCopy.MenuTitle;
            view.Summary.text = EmberCopy.MenuTagline;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (i < EmberCopy.MenuChoices.Length)
                {
                    buttons[i].name = "Button " + EmberCopy.MenuChoices[i];
                    buttons[i].GetComponentInChildren<TextMeshProUGUI>(true).text = EmberCopy.MenuChoices[i];
                }
                buttons[i].gameObject.SetActive(i < EmberCopy.MenuChoices.Length);
            }
            view.Brand.SetActive(true);
            view.Overlay.SetActive(true);
            view.Hint.SetActive(false);
            view.StickBase.gameObject.SetActive(false);
        }

        static void ConfigureHud(EmberUiRoot view)
        {
            var hud = At<Transform>(view.Safe, "Battle HUD");
            var boss = At<Transform>(view.Safe, "Boss HUD");
            var encounter = At<Transform>(hud, "Encounter banner");
            var labels = DirectTexts(hud);
            view.Hud = new EmberUiRoot.HudReferences
            {
                Root = hud.gameObject, BossRoot = boss.gameObject, EncounterRoot = encounter.gameObject,
                Health = At<Image>(hud, "Life track/Life fill"), Shield = At<Image>(hud, "Shield track/Shield fill"),
                WaveFill = At<Image>(hud, "Wave track/Wave fill"), BossFill = At<Image>(boss, "Boss life track/Boss life fill"),
                HealthText = DirectTexts(At<Transform>(hud, "Life track"))[0], Clock = labels[0], ShieldText = labels[1], WaveText = labels[2],
                BossText = DirectTexts(boss)[0], EncounterLabel = DirectTexts(encounter)[0], Pause = At<Button>(hud, "Pause")
            };
            hud.gameObject.SetActive(false);
            boss.gameObject.SetActive(false);
            encounter.gameObject.SetActive(false);
        }

        static void ConfigureUpgrade(EmberUiRoot view)
        {
            var panel = At<Transform>(view.Safe, "Blessing veil");
            var text = DirectTexts(panel);
            var scroll = At<ScrollRect>(panel, "Card viewport");
            var refresh = At<Image>(panel, "Refresh");
            var data = new SerializedObject(view.Upgrade);
            Set(data, "root", panel.gameObject);
            Set(data, "subtitle", text[1]);
            Set(data, "scrollHint", text[2]);
            Set(data, "scroll", scroll);
            Set(data, "refreshFace", refresh);
            Set(data, "refreshButton", refresh.GetComponent<Button>());
            Set(data, "refreshLabel", DirectTexts(refresh.transform)[0]);
            var cards = panel.GetComponentsInChildren<Image>(true).Where(i => i.name.StartsWith("Blessing card ", StringComparison.Ordinal)).ToArray();
            var entries = data.FindProperty("cards");
            entries.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var inner = At<Image>(cards[i].transform, "Card face");
                var shade = At<Image>(inner.transform, "Route tint");
                var copy = DirectTexts(inner.transform);
                Set(entry, "Face", cards[i]); Set(entry, "Inner", inner); Set(entry, "Shade", shade);
                Set(entry, "Icon", At<Image>(shade.transform, "Doodle icon"));
                Set(entry, "Title", copy[0]); Set(entry, "Description", copy[1]); Set(entry, "Preview", copy[2]);
                Set(entry, "Rarity", DirectTexts(shade.transform)[0]);
                Set(entry, "Button", cards[i].GetComponent<Button>()); Set(entry, "Motion", cards[i].GetComponent<EmberCardMotion>());
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
        }

        static void ConfigureSettings(EmberUiRoot view)
        {
            var panel = At<Transform>(view.Safe, "Settings");
            var music = At<Transform>(panel, "背景音乐");
            var sfx = At<Transform>(panel, "游戏音效");
            var speed = At<Transform>(panel, "Game speed");
            var data = new SerializedObject(view.Settings);
            Set(data, "root", panel.gameObject);
            var buttons = panel.GetComponentsInChildren<Button>(true).Where(b => b.transform.parent == panel).ToArray();
            Set(data, "closeLabel", DirectTexts(buttons[0].transform)[0]); Set(data, "campLabel", DirectTexts(buttons[1].transform)[0]);
            Set(data, "musicSlider", music.GetComponentInChildren<Slider>(true)); Set(data, "sfxSlider", sfx.GetComponentInChildren<Slider>(true));
            Set(data, "musicPercent", DirectTexts(music)[1]); Set(data, "sfxPercent", DirectTexts(sfx)[1]); Set(data, "speedValue", DirectTexts(speed)[2]);
            var faces = data.FindProperty("speedFaces"); var labels = data.FindProperty("speedLabels");
            faces.arraySize = labels.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                var face = At<Image>(speed, "Speed " + (i + 1));
                faces.GetArrayElementAtIndex(i).objectReferenceValue = face;
                labels.GetArrayElementAtIndex(i).objectReferenceValue = DirectTexts(face.transform)[0];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
        }

        static void ConfigureGods(EmberUiRoot view, TMP_FontAsset font)
        {
            EmberGodsViewBuilder.EnsurePanels(view.Safe, font);
            var slots = At<Transform>(view.Safe, "Gods slots select");
            var roster = At<Transform>(view.Safe, "Gods roster select");
            var data = new SerializedObject(view.GodsSelect);
            Set(data, "slotsRoot", slots.gameObject);
            Set(data, "rosterRoot", roster.gameObject);
            Set(data, "slotConfirm", At<Button>(slots, "Confirm"));
            Set(data, "rosterConfirm", At<Button>(roster, "Confirm starter"));
            Set(data, "rosterConfirmLabel", DirectTexts(At<Transform>(roster, "Confirm starter"))[0]);
            var slotCards = data.FindProperty("slotCards");
            slotCards.arraySize = 5;
            for (int i = 0; i < 5; i++) slotCards.GetArrayElementAtIndex(i).objectReferenceValue = At<Image>(slots, "Slot " + (i + 1));
            var ids = EmberWeaponCatalog.RosterCopy();
            var cards = data.FindProperty("rosterCards");
            var descriptions = data.FindProperty("rosterDescriptions");
            cards.arraySize = descriptions.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var card = At<Image>(roster, "Roster viewport/Roster/Roster " + ids[i]);
                cards.GetArrayElementAtIndex(i).objectReferenceValue = card;
                descriptions.GetArrayElementAtIndex(i).objectReferenceValue = DirectTexts(card.transform)[1];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            slots.gameObject.SetActive(false);
            roster.gameObject.SetActive(false);
        }

        static T At<T>(Transform parent, string path) where T : Component
        {
            var node = parent.Find(path);
            var value = node != null ? node.GetComponent<T>() : null;
            if (value == null) throw new InvalidOperationException("UIRoot missing " + parent.name + "/" + path + " (" + typeof(T).Name + ")");
            return value;
        }

        static TextMeshProUGUI[] DirectTexts(Transform parent) => parent.Cast<Transform>()
            .Select(t => t.GetComponent<TextMeshProUGUI>()).Where(t => t != null).ToArray();

        static void Set(SerializedObject data, string field, UnityEngine.Object value) => data.FindProperty(field).objectReferenceValue = value;
        static void Set(SerializedProperty data, string field, UnityEngine.Object value) => data.FindPropertyRelative(field).objectReferenceValue = value;
    }
}
