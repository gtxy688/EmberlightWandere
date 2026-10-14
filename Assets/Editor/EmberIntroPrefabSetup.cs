using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Emberlight.Editor
{
    /// <summary>首次迁移加载页布局，之后保留 Prefab 的人工编辑。</summary>
    public static class EmberIntroPrefabSetup
    {
        public const string PrefabPath = "Assets/UI/IntroRoot.prefab";

        public static void ConfigurePrefab()
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(PrefabPath) : CreateView();
            try
            {
                var view = root.GetComponent<EmberIntroView>();
                var font = EmberFonts.CreateChinese();
                foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true)) label.font = font;
                EmberUiPrefabRepair.RepairHierarchy(root, EmberUiPrefabRepair.SpriteDirectory);
                view.ValidateReferences();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Could not save IntroRoot prefab.");
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static GameObject CreateView()
        {
            var root = new GameObject("IntroRoot", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(EmberIntroView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = .5f;
            var view = root.GetComponent<EmberIntroView>();
            var font = EmberFonts.CreateChinese();
            var background = EmberUiFactory.Box("Night sky", root.transform, Vector2.zero, Vector2.one, new Color(.025f, .045f, .07f));
            background.raycastTarget = true;
            var mark = EmberUiFactory.Box("Wanderer emblem", background.transform, new Vector2(.37f, .74f), new Vector2(.63f, .89f), Color.white);
            mark.sprite = EmberWorldArt.Get(8);
            mark.type = Image.Type.Simple;
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            view.EnglishTitle = Text(background.transform, font, "English title", EmberIntro.EnglishTitle, 26, new Vector2(.04f, .48f), new Vector2(.96f, .57f));
            view.ChineseTitle = Text(background.transform, font, "Chinese title", EmberIntro.ChineseTitle, 40, new Vector2(.03f, .58f), new Vector2(.97f, .70f));
            view.Prompt = Text(background.transform, font, "Prompt", EmberIntro.LoadingPrompt, 20, new Vector2(.1f, .30f), new Vector2(.9f, .38f));
            view.ProgressFill = EmberUiFactory.Bar(background.transform, "Loading", new Vector2(.16f, .20f), new Vector2(.72f, .228f), new Color(.95f, .67f, .27f));
            view.ProgressFill.fillAmount = 0;
            view.ProgressRoot = view.ProgressFill.transform.parent.gameObject;
            view.ProgressRoot.GetComponent<Image>().raycastTarget = false;
            view.Percent = Text(background.transform, font, "Percent", "0%", 18, new Vector2(.74f, .19f), new Vector2(.92f, .24f));
            view.Percent.alignment = TextAlignmentOptions.Left;
            view.Tagline = Text(background.transform, font, "Tagline", EmberIntro.Tagline, 21, new Vector2(.08f, .38f), new Vector2(.92f, .44f));
            view.Tagline.gameObject.SetActive(false);
            return root;
        }

        static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string name, string value, float size, Vector2 min, Vector2 max)
        {
            var label = EmberUiFactory.Text(name, parent, font, value, size, min, max, new Color(1, .92f, .76f), false);
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }
    }
}
