using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Emberlight.Editor
{
    /// <summary>Android preparation and APK packaging; never runs automatically on import.</summary>
    public static class EmberAndroidBuild
    {
        const string Scene = "Assets/Scenes/Emberlight.unity";

        [MenuItem("Emberlight/Android/准备 APK 配置")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new BuildFailedException("请先停止运行游戏。");
            ValidateEnvironment();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("无法切换至 Android 平台，请检查模块安装。");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
            AssetDatabase.SaveAssets();
            Debug.Log("APK 配置已就绪：竖屏、IL2CPP、ARM64 + ARMv7、API 24–35，无开发调试选项。");
        }

        static void ValidateEnvironment()
        {
            string android = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer");
            RequireFile(Path.Combine(android, "SDK/platforms/android-35/android.jar"));
            RequireFile(Path.Combine(android, "SDK/build-tools/34.0.0/aapt2.exe"));
            RequireFile(Path.Combine(android, "SDK/platform-tools/adb.exe"));
            RequireFile(Path.Combine(android, "NDK/source.properties"));
            RequireFile(Path.Combine(android, "OpenJDK/bin/java.exe"));
            RequireFile(Scene);
            RequireFile("Assets/Resources/Fonts/NotoSansCJKsc-Regular.otf");
        }

        static void RequireFile(string path)
        {
            if (!File.Exists(path)) throw new BuildFailedException("缺少打包依赖：" + path);
        }

        [MenuItem("Emberlight/Android/构建本地安装 APK")]
        public static void BuildLocalApk() { Build(false); }

        [MenuItem("Emberlight/Android/构建自有签名 APK")]
        public static void BuildSignedApk() { Build(true); }

        static void Build(bool signed)
        {
            Prepare();
            bool previousCustom = PlayerSettings.Android.useCustomKeystore;
            string previousName = PlayerSettings.Android.keystoreName;
            string previousAlias = PlayerSettings.Android.keyaliasName;
            string previousStorePass = PlayerSettings.Android.keystorePass;
            string previousAliasPass = PlayerSettings.Android.keyaliasPass;
            try
            {
                PlayerSettings.Android.useCustomKeystore = signed;
                if (signed)
                {
                    PlayerSettings.Android.keystoreName = RequiredVariable("EMBER_KEYSTORE_PATH");
                    RequireFile(PlayerSettings.Android.keystoreName);
                    PlayerSettings.Android.keyaliasName = RequiredVariable("EMBER_KEY_ALIAS");
                    PlayerSettings.Android.keystorePass = RequiredVariable("EMBER_KEYSTORE_PASSWORD");
                    PlayerSettings.Android.keyaliasPass = RequiredVariable("EMBER_KEY_PASSWORD");
                }
                Directory.CreateDirectory("Builds/Android");
                string path = "Builds/Android/EmberlightWandere-" + PlayerSettings.bundleVersion
                    + "-" + PlayerSettings.Android.bundleVersionCode + (signed ? "-signed.apk" : "-local.apk");
                if (File.Exists(path))
                    throw new BuildFailedException("输出文件已存在，请先移走旧 APK 或递增版本号：" + path);
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { Scene }, locationPathName = path,
                    target = BuildTarget.Android, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException("APK 构建失败，请查看 Console 和 Editor.log。");
                Debug.Log("APK 已生成：" + Path.GetFullPath(path));
            }
            finally
            {
                PlayerSettings.Android.useCustomKeystore = previousCustom;
                PlayerSettings.Android.keystoreName = previousName;
                PlayerSettings.Android.keyaliasName = previousAlias;
                PlayerSettings.Android.keystorePass = previousStorePass;
                PlayerSettings.Android.keyaliasPass = previousAliasPass;
            }
        }

        static string RequiredVariable(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value)) throw new BuildFailedException("缺少签名环境变量：" + name);
            return value;
        }
    }
}
