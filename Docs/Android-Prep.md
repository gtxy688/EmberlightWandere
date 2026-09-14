# Android 打包清单

> 更新：2026-09-14。工程侧已收口，可打真机调试包。还剩 keystore 签名需要人工决定。

## 环境（已就绪）

- 编辑器：**Unity 2022.3.62f3**（`E:\Unity\Unity\2022.3.62f3`），与 `ProjectSettings/ProjectVersion.txt` 一致
- Android 模块：`PlaybackEngines/AndroidPlayer` 下 SDK / NDK / OpenJDK 齐全
- 无需额外安装，可直接 Build

## 已落地的工程设置

| 项 | 值 | 说明 |
|---|---|---|
| Default Orientation | Portrait | UI 按 540×960 竖屏锚点硬编码，横屏会错乱 |
| Allowed Autorotate | 仅 Portrait | 竖屏倒置与左右横屏全部关闭；`useOSAutorotation: 0` |
| Company Name | Emberlight | 原为 `DefaultCompany` |
| Package Name | `com.Emberlight.Wanderer` | Standalone 与 Android 同值；原 Android 未设 |
| Scripting Backend | IL2CPP | 原 Mono，ARM64 必需 IL2CPP |
| Target Architectures | ARM64 + ARMv7 | `AndroidTargetArchitectures: 3`；Google Play 强制 ARM64 |
| Min API Level | 24 | 原 22 |
| Target API Level | Auto (highest installed) | 上架前需确认 ≥ Play 当前要求 |
| Bundle Version | 0.1.0 | `AndroidBundleVersionCode: 1` |
| 帧率 | `targetFrameRate = 60`，`vSyncCount = 0` | 在 `EmberGame.Start()`；移动端默认 30 |
| 图集压缩 | Android 平台覆写 Uncompressed | 三张 doodle 图集；默认 ETC2 会让描边起块状伪影 |

图集覆写写在各自 `.meta` 的 Android 平台块（`overridden: 1` + `textureCompression: 0`）：
`Assets/Resources/Art/UI/doodle-ui.png`、`doodle-card-frames.png`、`Assets/Resources/Art/Cards/doodle-icons.png`。

## 打调试包（现在就能做）

1. File → Build Settings → 切到 **Android**
2. 勾 **Development Build** + **Script Debugging**
3. 确认 `Assets/Scenes/Emberlight.unity` 在 Scenes In Build 且已勾选（当前是唯一启用的场景）
4. Build → 得到 APK → 装真机

调试包用 Unity 自带 debug keystore，**不需要**配置签名。

## 出正式包前还需要（人工决定）

- [ ] **Keystore 签名**：生成正式 keystore 并填 `AndroidKeystoreName` / alias。
      口令**不要**写进 `ProjectSettings.asset`（明文会进 git），建议构建脚本从环境变量读。
- [ ] **Target API Level**：按 Google Play 当年要求锁定具体值
- [ ] **应用图标 + 启动图**：当前未配置（`androidSplashScreen: {fileID: 0}`）
- [ ] **Unity 启动 Logo**：`m_ShowUnitySplashScreen: 1`，有 Plus/Pro 可关
- [ ] **上架**：AAB、商店文案、截图、隐私政策

## 真机回归清单

- [ ] 一局完整流程：选波数 → 选难度 → 选槽位 → 选起始武器 → 5 波选卡 → Boss → 结算
- [ ] 多点触控虚拟摇杆；摇杆区域为屏幕下方 70%
- [ ] 选卡点按、刷新按钮、设置滑条
- [ ] Android Back 键 → 暂停（`Input.GetKeyDown(KeyCode.Escape)`，Android 上映射 Back）
- [ ] 切后台再回前台：`OnApplicationPause` 暂停是否正常，`Time.timeScale` 有无残留
- [ ] 中端机 30 分钟：帧率、发热、内存、GC 尖峰
- [ ] 存档：**当前没有存档系统**（局外成长已废弃），只需验证音量设置（PlayerPrefs）跨进程保留

## 已知取舍

- **中文字体**：`EmberFonts.CreateChinese()` 运行时从 `Fonts/NotoSansCJKsc-Regular.otf` 烘 2048×2048 动态 SDF 图集。
  包体因此带完整 Noto CJK SC（约 16.4 MB）。改预烘静态图集可省十几 MB，属优化非阻塞。
- **粒子 GC**：`EmberEffects` 池空时仍 `new Particle`，长跑需盯 GC（见 `Perf-Android-v0.md`）。
- **本地包依赖**：`Packages/manifest.json` 引用 `.codely.packages` 与 `.codely-cli` 的 `file:` 依赖，这两个目录已 gitignore。
  **新机器克隆后需先装 Codely CLI**，否则 Unity 解析不到包会报错。
- **`Assets/Audio/Sfx/Candidates/`** 已删除（零引用）；`TJGenerators/History/` 保留为音效源头。

## 建议顺序

打调试包 → 真机手感与性能 → 定 keystore 与图标 → 修崩溃 → 出 AAB
