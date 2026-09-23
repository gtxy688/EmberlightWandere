# M9 构建与平台 验收文档

> 对应架构：`09-build-platform.md`
> 对应需求：`../requirements.md` §7
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。
> 已执行本地工具清理后的包解析与编辑器编译；尚未实际构建过 APK，也未做真机检查（见 `09-build-platform.md`）。

## 自动化测试计划与证据

分层依据：PlayerSettings 与资源导入设置可在 **EditMode** 断言（读 `ProjectSettings` 与 `.meta`）；APK 构建与真机表现属**手动/CI**。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | EditMode（待建） | `PlayerSettingsTests::OrientationIsPortraitOnly` | `Default Orientation = Portrait`，自动旋转仅竖屏 | 横屏会让按 540×960 硬编码的 UI 错乱 | 未执行（无测试程序集） |
| A2 | EditMode（待建） | `PlayerSettingsTests::ScriptingBackendIsIl2CppArm64` | IL2CPP + ARM64 且含 ARMv7 | ARM64 是 Google Play 强制项 | 未执行（无测试程序集） |
| A3 | EditMode（待建） | `PlayerSettingsTests::ApiLevelsArePinned` | Min 24、Target 35（不跟随本机最高 SDK） | — | 未执行（无测试程序集） |
| A4 | EditMode（待建） | `PlayerSettingsTests::PackageNameAndVersionAreSet` | `com.Emberlight.Wanderer`、`0.1.0`、versionCode 1 | — | 未执行（无测试程序集） |
| A5 | EditMode（待建） | `PlayerSettingsTests::MainSceneIsEnabledAndUnique` | `Emberlight.unity` 在 Scenes In Build 且已勾选 | — | 未执行（无测试程序集） |
| A6 | EditMode（待建） | `PlayerSettingsTests::ManagedStrippingIsLow` | Managed Stripping = Low | 提高剥离级别可能剥掉反射用类型 | 未执行（无测试程序集） |
| A7 | EditMode（待建） | `TextureOverrideTests::DoodleAtlasesAreUncompressedOnAndroid` | 三张图集 `.meta` 的 Android 覆写为 Uncompressed | 回退 ETC2 会让描边起块状伪影 | 未执行（无测试程序集） |
| A8 | EditMode（待建） | `BuildConfigTests::DevelopmentBuildDisabledForRelease` | Release 配置下 Development Build / Script Debugging / Profiler 连接 / 深度分析全部关闭 | 误带调试开关会拖慢并对玩家暴露信息 | 未执行（无测试程序集） |
| A9 | EditMode（待建） | `BuildConfigTests::SignedBuildStopsWhenEnvVarsMissing` | 缺任一 keystore 环境变量时构建停止并列出缺失项 | — | 未执行（无测试程序集） |
| A10 | EditMode（待建） | `BuildConfigTests::PasswordsAreNeverPersisted` | 口令不写入 `ProjectSettings.asset`，构建后签名设置被还原 | 明文口令进 git 是安全事故 | 未执行（无测试程序集） |
| A11 | EditMode（待建） | `RepoHygieneTests::KeystoreFilesAreGitIgnored` | `*.keystore` / `*.jks` 命中 `.gitignore` | — | 未执行（无测试程序集） |
| A12 | EditMode（待建） | `RepoHygieneTests::DoesNotReferenceDeletedValidationEntryPoints` | 仓库中不存在对已删除 `EmberChecks` / `Validate*` / `EmberUIRenderCheck` 的活引用 | 防止文档与代码继续引用不存在的验收入口 | 未执行（无测试程序集） |
| A13 | 手动/CI | 实际 APK 构建 | 构建成功并产出 APK | **尚未执行过** | 未验证 |
| A14 | N/A | PlayerSettings 之外的平台服务（支付、推送、云存档） | — | 项目明确不做联网与付费 | N/A |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 尚无可执行的测试程序集 | — | **未验证** |
| — | Unity 2022.3.62f3 | `-executeMethod Emberlight.Editor.EmberAndroidBuild.BuildLocalApk` | `Builds/android-build.log`（预期） | **未执行** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity` 为主场景且已启用
- 依赖模块状态：M1–M8 全部可运行；Preferences → External Tools 指向 Unity 自带 SDK/NDK/JDK
- 目标设备与画质档位：Editor + Android 中端真机

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | 点「准备 APK 配置」 | 预检通过（SDK/NDK/JDK/主场景/字体），无缺项报告 | Editor | — | 待人工验收 | — |
| H2 | 点「构建本地安装 APK」 | 构建成功，产出 `Builds/Android/EmberlightWandere-0.1.0-1-local.apk`；BuildReport 无失败项 | Editor | — | 待人工验收 | **尚未构建过** |
| H3 | 安装 APK，启动 | 应用名显示「烬灯行者」；竖屏锁定；不出现横屏 | Android APK | — | 待人工验收 | 对应 A1 |
| H4 | 真机跑完整一局 | 选长度 → 选难度 → 选槽位 → 选武器 → 全程波次 → Boss → 结算 | Android APK | — | 待人工验收 | 主闭环 |
| H5 | 真机多点触控虚拟摇杆 | 摇杆响应正确，区域为屏幕下方 70% | Android APK | — | 待人工验收 | — |
| H6 | 真机点选卡、刷新按钮、设置滑条 | 触控命中准确，滚动列表可滑 | Android APK | — | 待人工验收 | — |
| H7 | 真机按 Android Back 键 | 映射为暂停（`KeyCode.Escape`） | Android APK | — | 待人工验收 | — |
| H8 | 真机切后台再回前台 | `OnApplicationPause` 暂停正常；`Time.timeScale` 无残留 | Android APK | — | 待人工验收 | — |
| H9 | 真机中端机连续 30 分钟 | 帧率稳定 60、发热可接受、内存不持续增长、无 GC 尖峰 | Android APK | — | 待人工验收 | 依赖 M7 粒子上限 |
| H10 | 真机验证音量设置跨进程保留 | 杀进程重进后音量不变 | Android APK | — | 待人工验收 | 需先有签名/可安装包 |
| H11 | 检查包体大小 | 记录 APK 体积，确认字体占约 16.4 MB | Android APK | — | 待人工验收 | 见 M7 优化项 |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 输出目录已有同名 APK | 构建停止并提示先移走旧包或递增版本号 | EditMode 待建 | 未验证 | 对应 `BuildLocalApk` 行为 |
| E2 | 缺 keystore 环境变量 | 停止构建并列出缺失变量，不产出未签名包 | EditMode 待建 | 未验证 | 对应 A9 |
| E3 | 删除本地 AI 开发工具目录后打开项目 | 包解析与编辑器编译成功 | Unity 批处理 + 手动 | 包解析与编译已验证；主菜单待人工验收 | `Logs/local-tools-cleanup-compile.log`，退出码 0；包清单及锁文件无失效的本地工具路径 |
| E4 | `-nographics` 下跑渲染检查 | 进不了 Play 模式；须带图形 + 看门狗 | 手动 | 未验证 | 历史限制，重建检查时仍适用 |
| E5 | 有人把字体改回预烘 | ⚠️ **无自动防线**；历史上两次均导致 UI 全崩且字段校验全绿 | 手动 | 未验证 | 见 M7 E6 |

## 回归范围

- 本地开发工具清理：复验包解析、编辑器编译及主菜单启动；音效源文件保留。

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M1–M8 全部 | 真机完整一局 | 构建配置影响运行时表现 | 待人工验收 | H4 |
| M7 美术与表现 | 图集压缩、字体、粒子预算 | 直接决定包体与帧率 | 待人工验收 | H9/H11 |
| M8 音频 | 真机扬声器/耳机与前后台 | 移动端音频设备差异大 | 待人工验收 | H8 |
| M6 界面 | 竖屏锁定与触控命中 | UI 锚点按 540×960 硬编码 | 待人工验收 | H3/H5/H6 |

## 交付结论

- **已验证**：2026-09-17 本地工具清理后的包解析与编辑器编译。命令：`Unity.exe -batchmode -nographics -quit -projectPath <项目> -logFile Logs/local-tools-cleanup-compile.log`；日志确认退出码 0，无 C# 编译错误。包清单与锁文件 JSON 解析通过，`git diff --check` 通过。尚未构建 APK 或执行真机检查。
- **不适用**：A14 平台服务（支付/推送/云存档）——项目明确不做。
- **待手动验收**：H1–H11（预检、构建、启动与竖屏、真机主闭环、摇杆、触控、返回键、前后台、30 分钟性能、音量持久化、包体）。
- **未验证**：A1–A12 全部（阻塞原因：项目无 asmdef 与测试程序集）；A13 因**尚未执行构建**而未验证。
- **未通过**：无。
- **下一步建议顺序**：打调试包 → 真机手感与性能 → 定 keystore 与图标 → 修崩溃 → 出 AAB。
