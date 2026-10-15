# M9 构建与平台

> 相关需求：`../requirements.md` §7（技术约束）
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

字体烘制和资源导入工具随工程保留。字体优先加载经 ValidateBaked 校验的静态图集，失败时动态回落；具体机制见 M7。

### 构建期烘焙（只做美术）

| 菜单项 | 作用 |
|--------|------|
| `Bake runtime art` | 把 5 个程序化图元写成 `Resources/Art/Baked/*.png` |
| `Measure runtime art cost` | 汇报这些图元原本每局一次的光栅化开销 |

烘焙结束会解码 PNG 与现场重新光栅化的结果逐像素比对（`maxDelta` 必须为 0），防止两处像素数学漂移。

预生成图片用于支持直接编辑形状，并减少首次使用时的光栅化工作。

### 字体策略与包体

- 中文字体经 `EmberFonts.CreateChinese()` 优先加载校验通过的静态图集，失败回落动态 SDF；现行口径见 `07-art-presentation.md`。
- 工程保留 Noto CJK 源字体以支持重烘与动态回落；资源裁剪须同时验证这两条路径，不能直接删除源字体。

### 签名配置

正式发布使用由开发者提供的签名身份。构建前需在启动 Unity 的父进程环境中设置：

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
- **开发工具包**：项目包清单不依赖机器本地的 AI 工具安装目录；克隆后无需额外安装该类 CLI。
- `Assets/Audio/_download/` 原始素材包保留，**上架前可删除以减小包体**。

## 对外契约

- 包清单与锁文件不得引用已删除的本地开发工具目录。

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberAndroidBuild.BuildLocalApk` | 静态方法 | — | APK | debug keystore；不启用开发调试功能 |
| `EmberAndroidBuild.BuildSignedApk` | 静态方法 | 环境变量 | APK | 缺变量即停止；不改动持久签名设置 |
| `EmberAndroidBuild.Prepare` | 静态方法 | — | — | 预检 SDK/NDK/JDK/主场景/字体并应用 Release 配置 |
| `EmberFontBake` | 静态方法 | — | 图集资产 + 渲染校验 | 生成 M7 运行时**优先使用**的预烘静态图集；校验不通过时运行时回落动态图集 |
| 4 个 `AssetPostprocessor` | 编辑器钩子 | 导入事件 | — | 见 M7 |

## Unity 装配

- 输出目录 `Builds/Android/`，命名 `EmberlightWandere-0.1.0-1-local.apk`；**已有同名文件时停止**，需先移走旧包或递增版本号。
- 本机已有 SDK API 35、Build Tools 34.0.0、NDK r23b、OpenJDK 11。
- 首次 Gradle 构建可能需要联网下载依赖；**是否完整成功以 BuildReport 为准**。
- Preferences → External Tools 需指向当前 Unity 附带的 SDK / NDK / JDK（预检检查的就是这些目录）。

## 影响与回归范围

- **直接影响模块**：全部（构建配置影响运行时）。
- **必须复验的契约**：竖屏锁定、IL2CPP + ARM64、API 24/35、`targetFrameRate`、图集 Uncompressed 覆写、字体包体。

## 工程与源码交付契约（2026-10-13）

`Tools/export-project.ps1` 导出 Assets（含实际资源、源码与 .meta）、Packages、ProjectSettings、Tools、Docs 和 README，拒绝 LFS 指针与缺失 .meta。生成物在 Delivery，默认不覆盖已有包，重新导出用 -Force。提交包必须重新导入验证，不依赖原 Library。

`Tools/compile-check.ps1` 读取最新 Runtime.rsp，重新扫描源文件，移除 Unity out/refout，写入隔离输出并显示完整编译诊断；它仅覆盖 Runtime。`Tools/run-tests.ps1` 等待 Unity 真实退出，核验 XML 的用例数、失败数与完成状态，拒绝 0 用例。PlayMode 使用图形模式。

回归范围：离线工具不污染引用程序集、Runtime/Editor/Tests 均能导入、测试实际发现并执行、字体不缺字、包内完整资源能在无 Library 工程中导入。运行方式见根目录 README。
