# M9 构建与平台

> 相关需求：`../requirements.md` §7（技术约束）
> 验收文档：`09-build-platform-test.md`
> 代码：`Assets/Editor/EmberAndroidBuild.cs`、`Assets/Editor/EmberFontBake.cs`、4 个资源导入器
> 主场景：`Assets/Scenes/Emberlight.unity`（唯一启用场景）

## 职责边界

- **本模块负责**：Android 环境预检、PlayerSettings 强制、APK 构建入口、签名（环境变量驱动）、以及编辑器期的资源导入与烘焙工具。
- **本模块不负责**：任何运行时行为。本模块**只在编辑器期执行**。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| 全部（仅编辑器期） | 项目设置与资源 | 构建与导入 | 缺环境变量即停止打包并列出缺失项 |

## 设计与数据流

```text
Emberlight → Android 菜单
   ├─ 准备 APK 配置   → 预检 SDK/NDK/JDK + 主场景 + 字体，应用 Release 配置
   ├─ 构建本地安装 APK → debug keystore 签名，APK 输出到 Builds/Android/
   └─ 构建自有签名 APK → 从环境变量读 keystore，缺变量即停止

命令行等价：
   Unity.exe -batchmode -quit -projectPath <项目> -buildTarget Android \
     -executeMethod Emberlight.Editor.EmberAndroidBuild.BuildLocalApk \
     -logFile Builds/android-build.log
```

## 关键机制

### 已锁定的工程设置

| 项 | 值 | 说明 |
|----|----|------|
| Unity | 2022.3.62f3 | `E:\Unity\Unity\2022.3.62f3` |
| Default Orientation | Portrait | UI 按 540×960 竖屏锚点硬编码，**横屏会错乱** |
| Allowed Autorotate | 仅 Portrait | 竖屏倒置与左右横屏全关；`useOSAutorotation: 0` |
| Company Name | Emberlight | — |
| 中文应用名 | 烬灯行者 | — |
| Package Name | `com.Emberlight.Wanderer` | Standalone 与 Android 同值 |
| Scripting Backend | IL2CPP | ARM64 必需（原 Mono） |
| Target Architectures | ARM64 + ARMv7 | `AndroidTargetArchitectures: 3` |
| Min API Level | 24 | — |
| Target API Level | **35（锁定）** | 不跟随本机最高 SDK 36 |
| Bundle Version | `0.1.0` / versionCode `1` | — |
| Managed Stripping | Low | — |
| 帧率 | `targetFrameRate = 60`、`vSyncCount = 0` | 在 `EmberGame.Start()`；**移动端默认封顶 30，不改会一直跑 30** |
| 图集压缩 | Android 覆写 **Uncompressed** | 三张 doodle 图集；默认 ETC2 会让描边起块状伪影 |

图集覆写写在各自 `.meta` 的 Android 平台块（`overridden: 1` + `textureCompression: 0`）：
`Assets/Resources/Art/UI/doodle-ui.png`、`doodle-card-frames.png`、`Assets/Resources/Art/Cards/doodle-icons.png`。

### 媒体资源清理记录（2026-09-14）

发布准备时删除了下列**临时调试设施**——它们的删除使大量历史文档的"验收依据"失效：

- 波次调用栈日志、F8/F9 调试热键、状态转储、Boss 跳波与预览入口
- **9 个临时编辑器脚本及其 `.meta`**：`ArtBake`、`BakeVerify`、`BossProbe`、`Checks`、`EnemyChecks`、`ExpansionChecks`、`ExpeditionChecks`、`SurvivalChecks`、`UIRenderCheck`（均为 Ember 前缀）

**保留**：已烘焙图片、4 个贴图导入器（保证重新导入资源时设置正确）、字体与音频缺失的故障日志、第三方开发插件。

> ⚠️ 因此历史文档中的 `Emberlight/Validate …`、`ValidateGodsSelect`、`ValidateBuildDepth`、`EmberChecks`、`EmberExpansionChecks`、`EmberUIRenderCheck`、试玩 Boss 菜单**全部已不存在**。这是 `00-overview.md` §测试现状 的成因。

> ⚠️ `EmberFontBake.cs` **仍然存在**（与上表 `ArtBake` 不同），可做真实渲染校验；但按 M7 的口径，**字体运行时仍走动态图集**，不要把它当作字体来源。

### 构建期烘焙（只做美术）

| 菜单项 | 作用 |
|--------|------|
| `Bake runtime art` | 把 5 个程序化图元写成 `Resources/Art/Baked/*.png` |
| `Measure runtime art cost` | 汇报这些图元原本每局一次的光栅化开销 |

烘焙结束会解码 PNG 与现场重新光栅化的结果逐像素比对（`maxDelta` 必须为 0），防止两处像素数学漂移。

**实测收益很小**（个位数毫秒，一局一次）；保留理由是形状可当 PNG 改、消掉首次使用卡顿，**不是启动时间**。

### UI 渲染检查（已删除，需要时自建）

`Emberlight.Editor.EmberUIRenderCheck` 曾能真的进 Play 模式跑 `Emberlight.unity`，头 12 秒内有任何 error / exception / assert 即判 FAIL：

```powershell
Unity.exe -batchmode -projectPath <项目> `
  -executeMethod Emberlight.Editor.EmberUIRenderCheck.Run -logFile <日志>
```

⚠️ **已知限制**（重建时仍适用）：**`-nographics` 下进不了 Play 模式**（`RuntimeInitializeOnLoadMethod` 不触发），必须带图形跑；批处理退出不可靠，脚本里需要 120 秒看门狗。

**任何改动 UI 或字体的提交，都应该先在编辑器里手动进一局确认能进游戏。** 字体两次崩溃都是"字段校验全绿但游戏进不去"——**校验替代不了真跑一遍**。

### 字体策略与包体

- 中文字体走 `EmberFonts.CreateChinese()` 运行时动态 SDF 图集（2048×2048），**固定不回预烘**，理由见 `07-art-presentation.md`。
- 包体因此带完整 Noto CJK SC（约 **16.4 MB**）。改预烘静态图集可省十几 MB，属优化非阻塞。

### 签名（未完成，人工决定）

本次**未创建签名身份**。发布前需在 Unity 父进程环境中设置：

| 环境变量 | 含义 |
|----------|------|
| `EMBER_KEYSTORE_PATH` | keystore 绝对路径 |
| `EMBER_KEY_ALIAS` | 签名别名 |
| `EMBER_KEYSTORE_PASSWORD` | keystore 口令 |
| `EMBER_KEY_PASSWORD` | 密钥口令 |

缺变量即停止；**口令不写入脚本**，构建后恢复原签名设置。Git 已忽略 `.keystore` / `.jks`。

> ⚠️ 口令**不要**写进 `ProjectSettings.asset`（明文会进 git）。

### 已知取舍

- **粒子 GC**：`EmberEffects` 池空时仍 `new Particle`，长跑需盯 GC（上限 120）。
- **本地包依赖**：`Packages/manifest.json` 引用 `.codely.packages` 与 `.codely-cli` 的 `file:` 依赖，这两个目录**已 gitignore**。**新机器克隆后需先装 Codely CLI**，否则 Unity 解析不到包会报错。
- `Assets/Audio/Sfx/Candidates/` 已删除（零引用）；`TJGenerators/History/` 保留为音效源头。
- `Assets/Audio/_download/` 原始素材包保留，**上架前可删除以减小包体**。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberAndroidBuild.BuildLocalApk` | 静态方法 | — | APK | debug keystore；不启用开发调试功能 |
| `EmberAndroidBuild.BuildSignedApk` | 静态方法 | 环境变量 | APK | 缺变量即停止；不改动持久签名设置 |
| `EmberAndroidBuild.Prepare` | 静态方法 | — | — | 预检 SDK/NDK/JDK/主场景/字体并应用 Release 配置 |
| `EmberFontBake` | 静态方法 | — | 图集资产 + 渲染校验 | 见 M7：**不作为运行时字体来源** |
| 4 个 `AssetPostprocessor` | 编辑器钩子 | 导入事件 | — | 见 M7 |

## Unity 装配

- 输出目录 `Builds/Android/`，命名 `EmberlightWandere-0.1.0-1-local.apk`；**已有同名文件时停止**，需先移走旧包或递增版本号。
- 本机已有 SDK API 35、Build Tools 34.0.0、NDK r23b、OpenJDK 11。
- 首次 Gradle 构建可能需要联网下载依赖；**是否完整成功以 BuildReport 为准**。
- Preferences → External Tools 需指向当前 Unity 附带的 SDK / NDK / JDK（预检检查的就是这些目录）。

## 影响与回归范围

- **直接影响模块**：全部（构建配置影响运行时）。
- **必须复验的契约**：竖屏锁定、IL2CPP + ARM64、API 24/35、`targetFrameRate`、图集 Uncompressed 覆写、字体包体。
- **对应验收项**：`09-build-platform-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| Keystore 签名身份 | 生成正式 keystore / 继续用 debug | **阻塞正式发布** | jack |
| 应用图标与启动图 | 设计并配置 / 维持未配置 | 当前 `androidSplashScreen: {fileID: 0}`；游戏图标在图集中，**不能直接整张图集当图标** | jack + xiaoUI |
| Unity 启动 Logo | 关闭（需 Plus/Pro）/ 维持 | `m_ShowUnitySplashScreen: 1` | jack |
| 是否重建 UI 渲染检查 | 重建带看门狗的批处理检查 / 维持手测 | 字体与 UI 事故的防线 | xiaoCoder |
| 上架准备 | AAB、商店文案、截图、隐私政策 | 对外发布前置 | jack |
