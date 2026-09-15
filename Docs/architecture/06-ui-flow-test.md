# M6 界面与流程 验收文档

> 对应架构：`06-ui-flow.md`
> 对应需求：`../requirements.md` §2、§4.1
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。

## 自动化测试计划与证据

分层依据：UI 为运行时构建，需 Canvas 与帧推进 → **PlayMode**。**纯排版结果与观感不可自动化**，只断言结构与状态。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | PlayMode（待建） | `FlowTests::StartRunRequiresAllFourStepsInOrder` | 必须依次经过 长度 → 难度 → 槽位 → 起始武器 才能开战 | 跳步会拿到未初始化配置 | 未执行（无测试程序集） |
| A2 | PlayMode（待建） | `FlowTests::RosterConfirmDisabledUntilSelection` | 未选中任何武器时确认按钮禁用；选中后显示该武器名 | — | 未执行（无测试程序集） |
| A3 | PlayMode（待建） | `FlowTests::RosterSelectionSurvivesPageChange` | 跨页翻动后选择保留 | — | 未执行（无测试程序集） |
| A4 | PlayMode（待建） | `FlowTests::RosterConfirmSubmitsExactlyOnce` | 反复点击确认只提交一次 | 重复提交会重复开战 | 未执行（无测试程序集） |
| A5 | PlayMode（待建） | `FlowTests::NoSkipGodsSelectEntryExists` | 不存在跳过开局的入口或 API | v3 硬口径：开局不可跳过 | 未执行（无测试程序集） |
| A6 | PlayMode（待建） | `UpgradePanelTests::ShowsThreePlusEmptySlotsCards` | 面板渲染的卡数等于传入候选数（`3 + 空位`） | — | 未执行（无测试程序集） |
| A7 | PlayMode（待建） | `UpgradePanelTests::SwitchesToScrollAboveFourCards` | ≤4 张直排；>4 张启用滚动区域且加入 132 单位卡高 | 7 张时溢出屏幕 | 未执行（无测试程序集） |
| A8 | PlayMode（待建） | `UpgradePanelTests::RefreshButtonDisabledWhenExhausted` | 刷新次数用尽后按钮不可用或点击无效 | — | 未执行（无测试程序集） |
| A9 | PlayMode（待建） | `UpgradePanelTests::SelectionLockPreventsDoublePick` | 动画期间重复点击只接受一次 | 历史缺陷：动画期重复领卡 | 未执行（无测试程序集） |
| A10 | PlayMode（待建） | `CardMotionTests::AnimationsUseUnscaledTime` | 卡动画在 `timeScale == 0` 时仍推进 | 用 `deltaTime` 会在暂停/选卡时冻结 | 未执行（无测试程序集） |
| A11 | PlayMode（待建） | `HudTests::ShowsOwnedOverSlotsAndLuck` | HUD 显示 `已有/N` 与幸运值 | — | 未执行（无测试程序集） |
| A12 | PlayMode（待建） | `HudTests::HidesCombatBarDuringPauseUpgradeAndResult` | 暂停/升级/结算时隐藏战斗状态栏 | — | 未执行（无测试程序集） |
| A13 | PlayMode（待建） | `HudTests::BossBarAppearsOnlyOnBossWaves` | Boss 条仅在 Boss 波出现 | — | 未执行（无测试程序集） |
| A14 | PlayMode（待建） | `HudTests::EncounterBannerShowsTwiceSecondsAndFreezesOnPause` | 遭遇横幅 2 秒，暂停时不推进 | — | 未执行（无测试程序集） |
| A15 | PlayMode（待建） | `SettingsTests::ReturnToCampRequiresSecondClick` | 首次点击显示「再次点击确认放弃本局」，二次才结束单局 | 误触直接丢局 | 未执行（无测试程序集） |
| A16 | PlayMode（待建） | `SettingsTests::VolumeSlidersApplyImmediatelyAndPersist` | 拖动即时生效；杀进程重进仍保持 | — | 未执行（无测试程序集） |
| A17 | PlayMode（待建） | `SettingsTests::BackButtonLabelDependsOnEntry` | 主菜单进入显示「返回」；战斗暂停进入显示「继续战斗」+「返回营地」 | — | 未执行（无测试程序集） |
| A18 | PlayMode（待建） | `IntroTests::ChineseTextHiddenUntilFontReady` | 字体就绪前不渲染中文 | 提前渲染会出现缺字方框 | 未执行（无测试程序集） |
| A19 | PlayMode（待建） | `IntroTests::InputEnabledOnlyAfterReady` | 继续输入在准备完成后才启用 | — | 未执行（无测试程序集） |
| A20 | PlayMode（待建） | `IntroTests::MinimumDisplayDurationRespected` | 加载页最短展示约 5 秒 | — | 未执行（无测试程序集） |
| A21 | PlayMode（待建） | `UiBoundaryTests::UiClassesDoNotReferenceEmberGame` | 反射/源码扫描断言 7 个 UI 类不出现 `EmberGame` 类型引用 | 防边界腐化；引入后 UI 将无法脱离游戏实例测试 | 未执行（无测试程序集） |
| A22 | PlayMode（待建） | `CardLayoutTests::NoTextUsesEllipsisTruncation` | 开场武器说明与升级面板文本不使用省略号截断，改用自适应字号 | — | 未执行（无测试程序集） |
| A23 | N/A | 卡面美观度、按钮间距手感、色彩搭配 | — | **无法合理自动化**：属审美判断 | N/A（列入手动验收 H2/H6） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 尚无可执行的测试程序集 | — | **未验证** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`（UI 为运行时构建，场景无 UI 层级）
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

## 交付结论

- **已验证**：无。布局与交互规则已**逐项对照代码**确认，但尚无任何可执行检查通过。
- **不适用**：A23 卡面美观度与间距手感——属审美判断，不做自动化。
- **待手动验收**：H1–H10（主闭环、武器库可读性、滚动选卡、防重复领卡、返回营地确认、HUD 可读性、音量持久化、倍速、真机触控、窄屏适配）。
- **未验证**：A1–A22 全部（阻塞原因：项目无 asmdef 与测试程序集；PlayMode 测试还需可脚本化装配 Canvas）。
- **未通过**：无。
