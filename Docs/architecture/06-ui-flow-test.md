# M6 界面与流程 验收文档

> 对应架构：`06-ui-flow.md`
> 对应需求：`../requirements.md` §2、§4.1
> ⚠️ 工程已有 EditMode / PlayMode 基础设施；本表未实现的条目仍标待建，见 `00-overview.md` §测试现状。

## 自动化测试计划与证据

分层依据：UI 使用场景 Prefab，运行时绑定数据、回调与动画，需 Canvas 与帧推进 → **PlayMode**。**纯排版结果与观感不可自动化**，只断言结构与状态。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | PlayMode（待建） | `FlowTests::StartRunRequiresAllFourStepsInOrder` | 必须依次经过 长度 → 难度 → 槽位 → 起始武器 才能开战 | 跳步会拿到未初始化配置 | 待建（本条独立用例未实现） |
| A2 | PlayMode（待建） | `FlowTests::RosterConfirmDisabledUntilSelection` | 未选中任何武器时确认按钮禁用；选中后显示该武器名 | — | 待建（本条独立用例未实现） |
| A3 | PlayMode（待建） | `FlowTests::RosterSelectionSurvivesPageChange` | 跨页翻动后选择保留 | — | 待建（本条独立用例未实现） |
| A4 | PlayMode（待建） | `FlowTests::RosterConfirmSubmitsExactlyOnce` | 反复点击确认只提交一次 | 重复提交会重复开战 | 待建（本条独立用例未实现） |
| A5 | PlayMode（待建） | `FlowTests::NoSkipGodsSelectEntryExists` | 不存在跳过开局的入口或 API | v3 硬口径：开局不可跳过 | 待建（本条独立用例未实现） |
| A6 | PlayMode（待建） | `UpgradePanelTests::ShowsThreePlusEmptySlotsCards` | 面板渲染的卡数等于传入候选数（`3 + 空位`） | — | 待建（本条独立用例未实现） |
| A7 | PlayMode（待建） | `UpgradePanelTests::SwitchesToScrollAboveFourCards` | ≤4 张直排；>4 张启用滚动区域且加入 132 单位卡高 | 7 张时溢出屏幕 | 待建（本条独立用例未实现） |
| A8 | PlayMode（待建） | `UpgradePanelTests::RefreshButtonDisabledWhenExhausted` | 刷新次数用尽后按钮不可用或点击无效 | — | 待建（本条独立用例未实现） |
| A9 | PlayMode（待建） | `UpgradePanelTests::SelectionLockPreventsDoublePick` | 动画期间重复点击只接受一次 | 历史缺陷：动画期重复领卡 | 待建（本条独立用例未实现） |
| A10 | PlayMode（待建） | `CardMotionTests::AnimationsUseUnscaledTime` | 卡动画在 `timeScale == 0` 时仍推进 | 用 `deltaTime` 会在暂停/选卡时冻结 | 待建（本条独立用例未实现） |
| A11 | PlayMode（待建） | `HudTests::ShowsOwnedOverSlotsAndLuck` | HUD 显示 `已有/N` 与幸运值 | — | 待建（本条独立用例未实现） |
| A12 | PlayMode（待建） | `HudTests::HidesCombatBarDuringPauseUpgradeAndResult` | 暂停/升级/结算时隐藏战斗状态栏 | — | 待建（本条独立用例未实现） |
| A13 | PlayMode（待建） | `HudTests::BossBarAppearsOnlyOnBossWaves` | Boss 条仅在 Boss 波出现 | — | 待建（本条独立用例未实现） |
| A14 | PlayMode（待建） | `HudTests::EncounterBannerShowsTwiceSecondsAndFreezesOnPause` | 遭遇横幅 2 秒，暂停时不推进 | — | 待建（本条独立用例未实现） |
| A15 | PlayMode（待建） | `SettingsTests::ReturnToCampRequiresSecondClick` | 首次点击显示「再次点击确认放弃本局」，二次才结束单局 | 误触直接丢局 | 待建（本条独立用例未实现） |
| A16 | PlayMode（待建） | `SettingsTests::VolumeSlidersApplyImmediatelyAndPersist` | 拖动即时生效；杀进程重进仍保持 | — | 待建（本条独立用例未实现） |
| A17 | PlayMode（待建） | `SettingsTests::BackButtonLabelDependsOnEntry` | 主菜单进入显示「返回」；战斗暂停进入显示「继续战斗」+「返回营地」 | — | 待建（本条独立用例未实现） |
| A18 | PlayMode（待建） | `IntroTests::ChineseTextHiddenUntilFontReady` | 字体就绪前不渲染中文 | 提前渲染会出现缺字方框 | 待建（本条独立用例未实现） |
| A19 | PlayMode | `EmberRuntimeSmokeTests::IntroPrefab_PreservesViewAndLayoutAndWaitsForReady` | Continue 就绪守卫与一次性提交 | — | 已验证；真实键盘/触屏输入仍待人工 |
| A20 | PlayMode | 同上 | 以加载器使用的 unscaledTime 验证约 5 秒展示 | — | 已验证（2026-10-14） |
| A21 | PlayMode（待建） | `UiBoundaryTests::UiClassesDoNotReferenceEmberGame` | 反射/源码扫描断言 7 个 UI 类不出现 `EmberGame` 类型引用 | 防边界腐化；引入后 UI 将无法脱离游戏实例测试 | 待建（本条独立用例未实现） |
| A22 | PlayMode（待建） | `CardLayoutTests::NoTextUsesEllipsisTruncation` | 开场武器说明与升级面板文本不使用省略号截断，改用自适应字号 | — | 待建（本条独立用例未实现） |
| A23 | N/A | 卡面美观度、按钮间距手感、色彩搭配 | — | **无法合理自动化**：属审美判断 | N/A（列入手动验收 H2/H6） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 历史计划尚未实现；当前运行记录见 `../实现说明.md` | — | **未验证** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity` → `Assets/UI/UIRoot.prefab` / `IntroRoot.prefab` → `EmberUiRoot` / `EmberIntroView` 序列化视图引用
- 依赖模块状态：M2 选卡、M3 战斗、M7 图集、M8 音频均可运行
- 目标设备与画质档位：Editor + Android 真机

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | 完整走一遍：加载页 → 主菜单 → 四步开局 → 战斗 → 选卡 → 结算 → 回营地 | 无卡死、无 Console 报错；中文全部正常显示 | Editor | — | 待人工验收 | 主闭环 |
| H2 | 观察开局武器库 | 六把武器说明为两行白话短句，无省略号截断、无术语；武器名与图标匹配 | Editor | — | 待人工验收 | 审美项 |
| H3 | 卡 N=5 打到中期 | 候选超过 4 张时出现纵向滚动，卡片不溢出屏幕，刷新按钮始终可见可点 | Editor | — | 待人工验收 | 对应 A7 |
| H4 | 快速连点同一张卡 | 只领取一次，无重复加成 | Editor | — | 待人工验收 | 对应 A9 |
| H5 | 战斗中按暂停 → 打开设置 → 点「返回营地」一次 | 出现「再次点击确认放弃本局」；不动则不会丢局 | Editor | — | 待人工验收 | 对应 A15 |
| H6 | 逐项查看 HUD | 生命/护盾/波次条清晰；`已有/N` 与幸运可读；Boss 条与暂停描边同步；文字不越界 | Editor | — | 待人工验收 | 审美项 |
| H7 | 拖动音乐/音效滑条 | 实时生效并显示百分比；零值显示静音；杀进程重进保持 | Editor + Android | — | 待人工验收 | 对应 A16 |
| H8 | 切换 1x–5x 倍速 | 战斗节奏明显变化；选卡动画与暂停不受影响 | Editor | — | 待人工验收 | 对应 A10 |
| H9 | Android 真机全流程 | 触控摇杆、按钮、滚动列表、滑条、返回键暂停均正常；竖屏不倒置 | Android APK | — | 待人工验收 | 依赖 M9 |
| H10 | 窄屏或高长宽比设备 | 锚点不越界，顶部状态栏不被遮挡 | Android 真机 | — | 待人工验收 | 已知风险：锚点按 540×960 硬编码 |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 未选武器就点确认 | 按钮禁用，无法进入战斗 | PlayMode 待建 | 未验证 | 对应 A2 |
| E2 | 反复点击确认 | 只开战一次 | PlayMode 待建 | 未验证 | 对应 A4 |
| E3 | 动画期间重复点击卡片 | 全局选择锁只接受第一次 | PlayMode 待建 | 未验证 | 对应 A9 |
| E4 | 刷新次数耗尽后点击刷新 | 无效果，不扣次数、不重建面板 | PlayMode 待建 | 未验证 | 对应 A8 |
| E5 | 中文字体尚未就绪 | 隐藏中文，不就绪时显示方框 | PlayMode 待建 | 未验证 | 对应 A18 |
| E6 | `RunProgress` 为 null 时刷新 HUD | 不抛空引用，隐藏该块 | PlayMode 待建 | 未验证 | — |
| E7 | `timeScale == 0`（暂停/选卡） | 卡动画与 UI 动效继续推进 | PlayMode 待建 | 未验证 | 对应 A10 |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M2 单局进度 | 候选卡数量、卡面数值预览 | 面板按 M2 数据渲染 | 待人工验收 | — |
| M3 战斗与波次 | HUD 数值、结算屏、暂停冻结 | 只读 M3 状态 | 待人工验收 | — |
| M7 美术与表现 | 全部 UI 精灵与卡框图标 | UI 消费图集 | 待人工验收 | — |
| M8 音频 | 交互音与滑条预览 | 触发点在 UI | 待人工验收 | — |
| M7 字体预热 | 改动文案后的首屏字形 | 三处文案被预热 | 待人工验收 | 改文案必查 |
| Editor / Runtime UI 边界 | 缺失引用报错、面板复用、重新配置 Prefab | 删除动态创建回退并移动工厂 | 已验证 | EditMode 缺失引用用例、图形 PlayMode 复用、Editor 配置入口 |
| M3 → HUD | 波次不重复改写、秒/生命/击杀变化及时显示 | HUD 增加显示值缓存 | 已验证 | `EmberSubmissionOptimizationTests`；真机性能仍未测 |

## 交付结论

- **已验证**：2026-10-14 EditMode 150/150、图形 PlayMode 4/4 通过（Unity 退出 0）；包含实际主场景、Prefab 面板复用、缺失引用与 HUD 更新，具体用例与结果见 `../实现说明.md`。未把这些结果当作全部 UI 规则验收。
- **不适用**：A23 卡面美观度与间距手感——属审美判断，不做自动化。
- **待手动验收**：H1–H10（主闭环、武器库可读性、滚动选卡、防重复领卡、返回营地确认、HUD 可读性、音量持久化、倍速、真机触控、窄屏适配）。
- **未验证**：A1–A22 中尚未覆盖的独立场景；不含已经实际跑过的冒烟路径。仍需补齐独立用例；已覆盖项见本轮回归与 `../实现说明.md`。
- **未通过**：无。

## UI 复用回归（2026-09-15）

- 独立脚本：Tools/UiReuse/RuntimeUiChecks.cs，复制到隔离项目 Assets/Editor，以 -batchmode -executeMethod RuntimeUiChecks.Run 执行。不是项目测试程序集。
- 范围：20 次设置/选卡开关保持对象 ID；静默音量同步；确认重置；最新回调；3/7 张切换与滚动复位；隐藏取消动画；双击只提交一次。
- 待人工验收：主菜单 → 开局四步 → 战斗 → 升级/刷新 → 暂停/返回营地 → 结算；中文、滚动顶部标题、HUD 文字与护盾数值无重叠。
- 真机待验收：重复相同操作比较 CPU 与 GC Alloc 尖峰，不以对象复用检查替代性能实测。
- 字体/图片导入检查：N/A，本次未修改字体资源、切片或导入器；另运行现有字形覆盖检查。

### 本次执行结果

- 已验证：使用 Unity 2022.3.62f3 自带 Roslyn 和现有 Assembly-CSharp.rsp 编译到 Library/UiReuseCompile.dll，退出码 0。
- 已验证：`python Tools/atlas_glyph_check.py Library/UiReuseCompile.dll`，MISSING (0)，TMP 特殊字符齐全。
- 已验证：隔离副本 Library/RuntimeUiChecks，以 `-batchmode -executeMethod RuntimeUiChecks.Run` 运行，上述对象身份与交互检查 PASS；结果见该目录 result.txt。
- 已验证：hud.png 实际渲染截图已查看，生命、护盾、波次文字及暂停按钮未重叠；截图只是布局检查，不是主场景完整试玩。
- 未验证：主项目完整游戏闭环，本次仅在隔离副本运行面板检查，未操作当前打开的主场景。
- 待人工验收：HUD 可读性/点击手感，真实选卡界面各文案组合，以及 Android 真机性能对比。

## 本轮回归范围（2026-10-13）

`EmberOfferPresentationTests` 已验证六个武器 ID 的说明、专属增量和质变预览。`EmberRuntimeSmokeTests` 走实际开局/选卡/暂停/结算，并验证升级面板反复打开不增加节点、隐藏取消旧提交、滚动重置、最新回调只触发一次。原表其他独立场景仍待建。结果见 `../实现说明.md`；布局观感与设备操控仍待人工验收。

## UIRoot 图片修复补充（2026-10-13）

图片引用持久化与编辑预览回归见 `07-art-presentation-test.md` 的「UIRoot 图片恢复」。运行时接入已于 2026-10-14 完成，验证边界见下节。

## UIRoot 运行时接入回归（2026-10-14）

`MainScene_SetupCombatUpgradePauseLossAndReturn` 验证启动保留场景 UIRoot 的实例 ID 和编辑后的 HUD 锚点、不创建 Emberlight UI，并通过实际视图按钮走开局、升级、暂停和失败回营地。`PrefabPanels_ReuseViewsAndReplaceCallbacksWithoutRebuilding` 验证槽位、武器库、设置和升级使用最新回调，确认只提交一次，反复显示不增加节点；原独立升级测试保留滚动复位和隐藏取消动画的覆盖。

三个图形 PlayMode 用例通过；结果 `Library/UiRootIntegration/playmode.xml`，命令与计数见 `../实现说明.md`。主菜单的非 Play 预览、HUD 与卡面截图已检查；当前编辑器的点击手感、观感及真机触控仍待人工验收。

## 加载页预制体回归（2026-10-14）

新增 `IntroPrefab_PreservesViewAndLayoutAndWaitsForReady`：确认加载前场景已有 IntroRoot，绑定保留实例、锚点和控件身份，就绪前 Continue 无效，约 5 秒后显示提示与可见 TMP 网格，重复 Continue 只提交一次且实例仍存在并隐藏。节点检查排除 TMP 为回落字体自动创建的 TMP_SubMeshUI，它属于文字渲染内部对象。

完整图形 PlayMode 4/4、EditMode 137/137，Unity 均退出 0。Editor API 另验证重复连接保留手动字号和锚点、图片/纹理持久引用和场景绑定；非 Play 加载中与就绪预览，以及真实 Play 就绪截图已检查。证据及命令见 `../实现说明.md`。A18 的字体破坏/失败独立路径仍待建，键盘/触屏手感和当前编辑器观感待人工验收。

## UI 创建清理与 HUD 回归（2026-10-14）

`UnboundPanel_ReportsConfigurationErrorInsteadOfBuildingUi` 覆盖升级/设置/开局选择三种面板，缺失绑定时抛配置错误且不新增节点。图形 PlayMode 在真实场景复用升级面板，继续验证滚动复位、隐藏取消旧提交和最新回调；共 4/4 通过。Editor 的 `EmberUiPrefabSetup.ConfigurePrefab` 退出 0，保存后 Prefab 与主工程哈希一致，说明工厂迁移没有改变现有布局。

`UnchangedHudData_DoesNotDirtyWaveText` 验证重复推送不再交替改写波次文字；`Hud_UpdatesSecondsAndChangedValuesAndToleratesMissingProgress` 验证显示秒、生命、击杀和波次变化更新。完整 EditMode 150/150，failed=0，Unity 退出 0；离线编译与字形检查通过，命令和证据见 `../实现说明.md`。

HUD 与卡面真实渲染截图已查看；当前编辑器观感、点击手感和窄屏/真机可读性待人工验收。CPU/GC Alloc 未测，不以文字刷新次数测试代替性能测量。字体重烘 N/A：可见文案未变且字形检查缺口为 0。
