# Android APK 构建准备

更新：2026-09-14。当前只完成清理与构建准备，不代表已生成 APK 或通过真机验收。

## 清理

- 删除波次调用栈日志、F8/F9 调试热键、状态转储、Boss 跳波与预览入口。
- 删除 9 个临时编辑器脚本及其 meta：ArtBake、BakeVerify、BossProbe、Checks、EnemyChecks、ExpansionChecks、ExpeditionChecks、SurvivalChecks、UIRenderCheck（均为 Ember 前缀）。
- 保留已烘焙图片及 4 个贴图 Importer，保证重新导入资源时仍有正确设置。
- 保留字体、音频资源缺失的故障日志。保留现有第三方开发插件。

## 配置

Unity 2022.3.62f3；中文应用名“烬灯行者”；包名 `com.Emberlight.Wanderer`；版本 `0.1.0` / versionCode `1`。
主场景 `Assets/Scenes/Emberlight.unity`，竖屏，IL2CPP，ARM64 + ARMv7，最低 API 24，目标 API 35，Managed Stripping 为 Low。
目标 API 锁定已安装版本，不跟随本机最高 SDK 36。中文字体继续使用动态 TMP 图集。

## 操作

1. 等待 Unity 编译完成，停止 Play 模式。
2. 点击 **Emberlight → Android → 准备 APK 配置**。检查内置 SDK/NDK/JDK、主场景和字体，应用 Release 编译配置，关闭 Development Build、Script Debugging、Profiler 连接和深度分析，选择 APK 输出。
3. Preferences → External Tools 使用当前 Unity 附带的 SDK、NDK、JDK，预检检查的就是这些目录。
4. 点击 **构建本地安装 APK**。使用默认 debug keystore 签名，但不启用开发调试功能，适合安装试玩。
5. 输出为 `Builds/Android/EmberlightWandere-0.1.0-1-local.apk`。已有同名文件时停止，先移走旧包或递增版本号。

关闭当前项目后也可以命令行构建：

```powershell
& 'E:/Unity/Unity/2022.3.62f3/Editor/Unity.exe' -batchmode -quit -projectPath 'E:/Unity/Projects/DemoProjects/EmberlightWandere' -buildTarget Android -executeMethod Emberlight.Editor.EmberAndroidBuild.BuildLocalApk -logFile 'Builds/android-build.log'
```

本机已有 SDK API 35、Build Tools 34.0.0、NDK r23b 和 OpenJDK 11。首次 Gradle 构建可能需要联网下载依赖；是否完整成功以 BuildReport 为准。

## 自有签名

发布前使用长期保管的自有 keystore，本次没有创建签名身份。启动 Unity 前，在其父进程环境中设置：

- `EMBER_KEYSTORE_PATH`：绝对路径。
- `EMBER_KEY_ALIAS`：签名别名。
- `EMBER_KEYSTORE_PASSWORD` 与 `EMBER_KEY_PASSWORD`：对应口令。

然后点击 **构建自有签名 APK**。缺少变量即停止；口令不写入脚本，构建后恢复原签名设置。Git 已忽略 `.keystore` 和 `.jks`。

## 待完成

- 实际 APK 构建、安装；真机检查触控、返回键、后台切换、声音、设置保存和长时间性能。
- 桌面应用图标尚未配置，现有游戏图标在图集中，不能直接把整张图集用作 Android 图标。
- 对外发布前确定正式签名与版本号，并按渠道确认 API、图标和隐私要求；本配置面向 APK 安装测试。
