using UnityEngine;
using UnityEditor;

namespace Emberlight
{
    public static class EmberExpansionChecks
    {
        [MenuItem("Emberlight/\u9a8c\u8bc1\u5c4f\u5e55\u8303\u56f4\u4e0e\u7a00\u6709\u5ea6")]
        public static void Validate()
        {
            var obj = new GameObject("Visibility check camera");
            try
            {
                var camera = obj.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 8;
                camera.transform.position = new Vector3(0, 0, -10);
                foreach (float aspect in new[] { 9f / 16, 3f / 4, 16f / 9 })
                {
                    camera.aspect = aspect;
                    Require(EmberWorld.Visible(camera, Vector3.zero), "Centre target");
                    Require(!EmberWorld.Visible(camera, new Vector3(30, 0, 0)), "Offscreen horizontal");
                    Require(!EmberWorld.Visible(camera, new Vector3(0, 30, 0)), "Offscreen vertical");
                    Require(!EmberWorld.Visible(camera, new Vector3(0, 0, -20)), "Behind camera");
                    Require(!EmberWorld.Visible(camera, camera.ViewportToWorldPoint(new Vector3(.5f, .95f, 10))), "HUD exclusion");
                }
                // Arena Clamp still 22 (LevelConfig.Default.MapLimit)
                LevelConfig.Default.ApplyWorld();
                Require(EmberWorld.Clamp(new Vector2(100, -100)) == new Vector2(22, -22), "Arena bounds");
                Require(EmberRarityUtil.Name(EmberRarity.Bronze) == "\u9752\u94dc"
                    && EmberRarityUtil.Name(EmberRarity.Silver) == "\u767d\u94f6"
                    && EmberRarityUtil.Name(EmberRarity.Gold) == "\u9ec4\u91d1"
                    && EmberRarityUtil.Name(EmberRarity.Diamond) == "\u94bb\u77f3", "Rarity names");
                Require(EmberRarityUtil.Percent(EmberRarity.Bronze) == 20
                    && EmberRarityUtil.Percent(EmberRarity.Silver) == 40
                    && EmberRarityUtil.Percent(EmberRarity.Gold) == 65
                    && EmberRarityUtil.Percent(EmberRarity.Diamond) == 90, "Rarity percents");
                EmberChecks.Validate();
                Debug.Log("Emberlight: screen bounds, rarity mapping and progression checks passed.");
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [MenuItem("Emberlight/\u8bd5\u73a9 Boss\uff08\u8fd0\u884c\u65f6\uff09")]
        public static void BossPreview()
        {
            var game = Object.FindObjectOfType<EmberGame>();
            if (!Application.isPlaying || game == null || game.State != EmberGame.Mode.Playing)
            {
                Debug.LogWarning("\u8bf7\u5148\u8fd0\u884c\u6e38\u620f\u5e76\u8fdb\u5165\u6218\u6597\uff0c\u518d\u4f7f\u7528 Boss \u8bd5\u73a9\u3002");
                return;
            }
            game.PreviewBoss();
        }

        static void Require(bool passed, string label) { if (!passed) throw new System.Exception(label); }
    }
}
