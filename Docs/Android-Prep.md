# Android 打包清单

> 更新：2026-09-14。工程侧已收口，可打真机调试包。还剩 keystore 签名需要人工决定。

## 环境（已就绪）

- 编辑器：**Unity 2022.3.62f3**（`E:\Unity\Unity\2022.3.62f3`），与 `ProjectSettings/ProjectVersion.txt` 一致
- Android 模块：`PlaybackEngines/AndroidPlayer` 下 SDK / NDK / OpenJDK 齐全
- 无需额外安装，可直接 Build

## 构建期烘焙（改了美术或文案后要重跑）

运行时的程序化图元和中文 SDF 图集都已改为构建期预生成，菜单在 `Emberlight` 下：

| 菜单项 | 作用 |
|---|---|
| `Bake runtime art` | 把 5 个程序化图元（火焰/圆环/面板/辉光/圆盘）写成 `Resources/Art/Baked/*.png` |
| `Bake Chinese font atlas` | 用完整字形集烘 2048×2048 Static SDF 到 `Resources/Fonts/` |
| `Measure runtime art cost` | 汇报这些图元原本每局一次的光栅化开销 |

**烘焙器自带的校验**

- 美术烘焙结束会解码刚写出的 PNG，与现场重新光栅化的结果逐像素比对（`maxDelta` 必须为 0）。
  像素数学存在于烘焙器与运行时兜底两处，这条防止改了一处忘了另一处导致美术漂移。
- 字体烘焙会把**从已加载程序集里反射出来的、UI 实际能画出的全部字符**并进字形集，
  而不是只用 `EmberFonts.GlyphSet`。原因见下。

### ⚠️ 字形集曾经漏字（已修）

`EmberFonts.GlyphSet` 是手工维护的。用反射逐个核对后发现 **54 个 UI 字符串里用到的字符不在其中**，例如：

- `燎` `原` `日` `冕` —— 质变词条「燎原」「日冕」
- `站` `住` —— 穿透火矢说明「站住开火」
- `；` 以及 `A B C J K N R _` 等 ASCII

**在动态图集下这只是多烘几个字；换成 Static 图集后就是永久的空白方块。**
所以烘焙改为使用「GlyphSet ∪ 反射发现的全部字符」。当前为 462 字。

注意 `EmberFonts.GlyphSet` 本身仍用于运行时兜底路径，**加新文案后请重跑烘焙**，
烘焙日志会打印实际请求的字数与缺失数。

### 重复烘焙字体

Static 图集不能再加字形，所以字体烘焙器**拒绝覆盖已存在的 Static 图集**并报错，
而不是静默产出空资产（上一个预烘字体就是这样变成空壳的）。
重烘请先删掉 `Assets/Resources/Fonts/NotoSansCJKsc-Regular SDF.asset` 及其 `.meta`。

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
