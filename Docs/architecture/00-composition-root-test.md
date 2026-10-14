# M0 组合根 验收文档

> 对应架构：`00-composition-root.md`
> 对应需求：`../requirements.md` §2、§7
> ⚠️ 工程已有 EditMode / PlayMode 基础设施；本表未实现的条目仍标待建，见 `00-overview.md` §测试现状。

## 自动化测试计划与证据

分层依据：组合根本身是 `MonoBehaviour`，行为依赖帧推进与场景装配 → **PlayMode**；
但抽出的纯逻辑单元（`EmberKeeperVitals`、`RunFlowController`）不依赖帧推进 → **EditMode**。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| C1 | EditMode | `EmberKeeperVitalsTests.Hurt_AppliesDifficultyOnceThenConsumesShield` | 伤害经难度倍率后只乘一次 | 重复乘会双倍扣血 | 已验证：见 `../实现说明.md` / EditMode XML |
| C2 | EditMode | `EmberKeeperVitalsTests.Hurt_AppliesDifficultyOnceThenConsumesShield` | 护盾先扣、溢出部分才进生命 | 顺序错会让护盾形同虚设 | 已验证：见 `../实现说明.md` / EditMode XML |
| C3 | EditMode | `EmberKeeperVitalsTests.Hurt_ShieldOnlyStillStartsInvulnerability` | 无敌帧内再次受伤返回 `Rejected` 且生命不变 | 历史缺陷：连击秒杀 | 已验证：见 `../实现说明.md` / EditMode XML |
| C4 | EditMode | `EmberKeeperVitalsTests.OutsideCombat_RejectsDamageAndDrops` | 非 Playing 阶段伤害不生效 | 结算后仍会掉血 | 已验证：见 `../实现说明.md` / EditMode XML |
| C5 | EditMode | `EmberKeeperVitalsTests.Heal_ClampsAndResetClearsDamage` | 治疗不超上限，溢出丢弃 | — | 已验证：见 `../实现说明.md` / EditMode XML |
| C6 | EditMode | `EmberKeeperVitalsTests.FullHealth_DropIsConsumedButOrdinaryHealIsRejected` | 满血拾取返回 `NoChange` 但仍算拾取 | 满血时应仍播提示音并消耗掉落 | 已验证：见 `../实现说明.md` / EditMode XML |
| C7 | EditMode | `RunFlowControllerTests.BossWave_RequiresTwoPicksBeforeReturningToCombat` | 选卡次数取自 `LevelConfig.PicksAfterWave` | — | 已验证：见 `../实现说明.md` / EditMode XML |
| C8 | EditMode | `RunFlowControllerTests.Compensation_DoesNotConsumeScheduledPickOrChain` | 新武器补偿一次且**不连锁** | 连锁会导致无限选卡 | 已验证：见 `../实现说明.md` / EditMode XML |
| C9 | EditMode | `RunFlowControllerTests.ForgedOrRepeatedChoice_DoesNotChangeBuild` | 候选外的卡不能被选中 | 防伪造/重复提交 | 已验证：见 `../实现说明.md` / EditMode XML |
| C10 | EditMode | `RunFlowControllerTests.Refresh_IsLimitedAndDoesNotSpendScheduledPicks` | 免费刷新用尽后返回 false | — | 已验证：见 `../实现说明.md` / EditMode XML |
| C11 | PlayMode（待建） | `CompositionRootTests::PhaseIsOnlyWrittenByRoot` | `Phase` 无公开 setter，外部只能经契约请求 | 防止阶段被任意改写 | 待建（本条独立用例未实现） |
| C12 | PlayMode（待建） | `CompositionRootTests::UpdateOrder_TicksVitalsBeforeCombat` | 无敌帧在接触伤害判定前已推进 | 顺序错会让无敌帧晚一帧失效 | 待建（本条独立用例未实现） |
| C13 | PlayMode（待建） | `CompositionRootTests::ReturnToCamp_ClearsRunState` | 回营地后 `Run.IsActive == false` 且世界对象已销毁 | 残留引用会拖住下一局 | 待建（本条独立用例未实现） |
| C14 | 源码扫描（待建） | `BoundaryTests::CombatAndVisualsDoNotReferenceEmberGame` | `Combat/`（含 Weapons）与 `Visuals/` 下不出现 `EmberGame` 类型引用 | 防边界腐化；这是本次重构的主要成果 | 待建（本条独立用例未实现） |
| C15 | N/A | 战斗手感、UI 观感 | — | **无法合理自动化**：属审美与手感判断 | N/A（见手动验收 H1） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| 2026-10-14 | 2022.3.62f3，`-batchmode -nographics -runTests -testPlatform editmode` | EditMode 全量（含本模块的 `EmberKeeperVitalsTests` 8 条、`RunFlowControllerTests` 6 条、`RunProgressChoiceEntryTests` 2 条） | `Library/TestRun/editmode.xml` | **通过**：`total=150 passed=150 failed=0 skipped=0`，Unity 退出码 0 |
| 2026-10-14 | 2022.3.62f3，`-batchmode -runTests -testPlatform PlayMode`（图形） | PlayMode 图形冒烟（主场景开局→战斗→选卡→暂停→失败回营地） | `Library/TestRun/playmode.xml` | **通过**：`total=4 passed=4 failed=0 skipped=0`，Unity 退出码 0 |

> 上表是本模块的**运行记录**；用例级别的红灯理由与最近结果见「自动化测试计划与证据」的 C1–C14。
> 下面的编译与字形检查是**工程可导入/可编译**的证据，不是行为测试证据。

### 离线编译校验（已跑，非测试程序集）

`Tools/compile-check.ps1` 用 Unity 自带 Roslyn 编译 `Assets/Scripts`，**不启动编辑器**，
因此 Unity 开着也能跑；同时用 `Tools/atlas_glyph_check.py` 复查字形覆盖。
`Tools/unity-batch-verify.ps1` 则是真正的 Unity 批处理导入 + 编译（**必须先关 Unity**，
且需在普通终端运行——沙箱会阻断授权客户端的命名管道）。

| 日期 | 命令 | 结果 |
|------|------|------|
| 2026-10-14 | `powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1` | **通过**：67 个 Runtime 源文件、0 错误、退出码 0 |
| 2026-10-14 | `python Tools/atlas_glyph_check.py Library/RefactorCheck/Emberlight.Runtime.dll` | **通过**：`MISSING (0)`，TMP 特殊字符齐全，退出码 0 |
| 2026-09-23 | `powershell -ExecutionPolicy Bypass -File Tools/unity-batch-verify.ps1`（Unity 2022.3.62f3，`-batchmode -nographics -quit`） | **通过（历史记录）**：退出码 0、`AssetDatabase: script compilation time: 1.519s`、日志内 0 条 `error CS`。**当时程序集还叫 `Assembly-CSharp.dll`**，2026-10-03 起已迁入自建 `Emberlight.Runtime` |

> 批处理通过只证明**工程能导入、代码能编译**，仍**不能替代**进一局。

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`（入口脚本挂在常驻对象上；场景中无 UI 层级）
- 依赖模块状态：M1–M8 均可运行
- 目标设备与画质档位：Editor + Android 真机

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| R1 | 完整走一遍：加载页 → 主菜单 → 四步开局 → 战斗 → 选卡 → 暂停 → 恢复 → 结算 → 回营地 | 无卡死、无 Console 报错；中文全部正常 | Editor | — | 待人工验收 | **本次重构的主回归项**（对应 H1） |
| R2 | 战斗中连续被多个敌人接触 | 掉血节奏与重构前一致：每次受伤后有约 0.65 秒无敌，不被连击秒杀 | Editor | — | 待人工验收 | 对应 C1–C4 |
| R3 | 取得护盾后受伤 | 先扣护盾、溢出才掉生命；护盾条随之下降 | Editor | — | 待人工验收 | 对应 C2 |
| R4 | 拾取治疗掉落 | 残血时吸附并回血；满血时拾取不报错、播放提示音 | Editor | — | 待人工验收 | 对应 C5/C6 |
| R5 | 连续清波选卡，中途选到一把新武器 | 立刻补发**一次**选卡；补选后**不会**再次连锁补发 | Editor | — | 待人工验收 | 对应 C7/C8 |
| R6 | 选卡面板连点同一张卡 / 点候选外的区域 | 只领取一次；面板外点击无效果 | Editor | — | 待人工验收 | 对应 C9 |
| R7 | 用完 2 次免费刷新后再点刷新 | 按钮不可用或无效果，不扣次数 | Editor | — | 待人工验收 | 对应 C10 |
| R8 | 拖动屏幕下半区 / 按 Esc | 摇杆底盘出现在按下位置并跟手；Esc 暂停 | Editor | — | 待人工验收 | 输入抽离后的回归 |
| R9 | 暂停 → 设置 → 返回营地 → 再开一局 | 上一局世界被收起，新局从满血、第 1 波开始，无残留 | Editor | — | 待人工验收 | 对应 C13 |
| R10 | Android 真机走一遍 R1/R8 | 触控摇杆、按钮、滚动列表正常；竖屏不倒置 | Android APK | — | 待人工验收 | 依赖 M9 |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 无敌帧内再次受伤 | 返回 `Rejected`，生命与护盾都不变 | EditMode 待建 | 未验证 | 对应 C3 |
| E2 | 非 Playing 阶段受伤 | 不生效 | EditMode 待建 | 未验证 | 对应 C4 |
| E3 | 未开局时调 `OnWaveCleared` | 直接返回，不抛空引用 | EditMode 待建 | 未验证 | `Run.IsActive` 为假 |
| E4 | 选了新武器但槽位已满 | 构筑拒绝该次选择，`Choose` 返回 false，不消耗选卡次数 | EditMode 待建 | 未验证 | 见 `02-run-progression.md` |
| E5 | 回营地后仍收到战斗回调 | 无异常（世界已销毁，`Progress` 为 null 时各入口自行返回） | PlayMode 待建 | 未验证 | — |
| E6 | 字体资产损坏 | `ValidateBaked` 闸门拦住，回落动态图集，游戏仍可进 | 手动 | 未验证 | 见 `07-art-presentation.md` |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M2 单局进度 | 选卡次数、候选数、补偿与刷新 | 选卡推进改由 `RunFlowController` 承担 | 待人工验收 | 对应 R5–R7 |
| M3 战斗与波次 | 波次推进、Boss 出场、伤害数值 | 战斗宿主改为 `IEmberCombatHost` | 待人工验收 | 对应 R2 |
| M6 界面与流程 | 开局四步、HUD 数值、暂停与设置 | 阶段改为 `EmberRunPhase`，HUD 推送点搬到 `EmberHudPresenter` | 待人工验收 | 对应 R1 |
| M7 美术与表现 | 灯芯动画、粒子特效 | 阶段门控改为注入委托，去掉 `FindObjectOfType` | 待人工验收 | 对应 R1 |
| M8 音频 | 受伤音、拾取音、选卡音 | 播放入口随结算搬迁 | 待人工验收 | 对应 R4 |

## 交付结论

- **已验证**：
  - C1–C10（EditMode，实际通过）：生命结算的倍率只乘一次、护盾先于生命、无敌帧、非 Playing 拒绝、治疗钳制、满血拾取语义、选卡次数、补偿不连锁、候选外/重复提交、刷新次数；
  - 离线编译 **67** 个 Runtime 源文件 / 0 错误（`Tools/compile-check.ps1`，2026-10-14）；
  - 字形覆盖 `MISSING (0)`、TMP 特殊字符齐全（`Tools/atlas_glyph_check.py`）；
  - `Combat/`（含 `Weapons/`）与 `Visuals/`、`UI/` 下零 `EmberGame` 类型引用（源码扫描；**结论已人工确认，但尚无自动化断言守住它**，见 C14）。
- **不适用**：C15 战斗手感与 UI 观感——属审美判断，不做自动化。
- **待人工验收**：R1–R10（尤其完整胜利路径、0.65 秒无敌手感、摇杆跟手与真机触控）。
- **未验证**：C11/C12（PlayMode 层面的阶段所有权与每帧顺序断言尚未写成独立用例）。图形 PlayMode 冒烟会实跑开局到回营地，但不构成对这两条的断言。
- **未通过**：无。

## 字形覆盖

本次重构新增的源码文件与注释**不含新的玩家可见文案**：文案仍集中在 `EmberCopy` 与各面板常量中，
未改动任何字符串字面量。因此理论上不需要重跑烘制——仍以实跑结果为准
（该脚本读编译产物字符串堆，能看见方法体内的内联字面量）。

| 日期 | 命令 | 结果 |
|------|------|------|
| 2026-09-23 | `python Tools/atlas_glyph_check.py Library/RefactorCheck/Emberlight.dll` | **通过**：`MISSING (0)`、TMP 特殊字符齐全、退出码 0 |

## 本轮回归范围（2026-10-13）

新增生命规则不创建音频对象、倍率/护盾/无敌帧、满血掉落、选卡次数、补偿不连锁、候选外与重复提交、刷新与重置测试。图形冒烟走主场景四步开局、移动与跟随、自动攻击与击杀计数、升级、暂停、失败回营地；清波入口显式触发，完整胜利路径仍待人工。详见 `../实现说明.md` 与 `Assets/Tests`。

## UIRoot 装配回归（2026-10-14）

主场景图形测试验证启动绑定现有 UIRoot、不替换实例、不覆盖修改过的 HUD 锚点、不新建 Emberlight UI；随后走四步开局到失败回营地。字体加载日志确认 ValidateBaked 通过后才绑定，UI 源码扫描零 EmberGame 类型引用。EditMode 150/150、PlayMode 4/4，Unity 均退出 0；命令和证据见 `../实现说明.md`。完整胜利、手感与当前编辑器观感仍待人工验收。

加载页也已接入 IntroRoot：序列化引用由连接工具保存，缺失时只查找现有视图，不能回落到运行时创建界面。新增 PlayMode 用例验证场景实例保留与 Continue 守卫；本轮完整 PlayMode 4/4、EditMode 150/150，退出 0。完整验证与人工边界见 `../实现说明.md`。
