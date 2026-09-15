# M1 配置与调参 验收文档

> 对应架构：`01-config-tuning.md`
> 对应需求：`../requirements.md` §3
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。下表中「待建」项需先补齐 asmdef + 测试程序集才能执行。

## 自动化测试计划与证据

`LevelConfig` 与 `GameSpeed` 的数值逻辑**不依赖帧推进、MonoBehaviour 生命周期或场景装配**，因此全部可归入 **EditMode** 层级——这正是补齐测试基础设施后收益最高的模块。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | EditMode（待建） | `Emberlight.Tests.LevelConfigTests::DifficultyHealthMultiplier_MatchesRequirements` | 三种难度构造出的 `HealthMultiplier` 分别为 0.25 / 0.5 / 0.675 | 实现前无此断言；若回退到旧的 0.75/1/1.35 则红灯 | 未执行（无测试程序集） |
| A2 | EditMode（待建） | `LevelConfigTests::StandardDifficulty_LeavesNonHealthMultipliersAtOne` | 标准难度下伤害/配额/间隔/词缀四个倍率均为 1.0 | 实现前无此断言 | 未执行（无测试程序集） |
| A3 | EditMode（待建） | `LevelConfigTests::BossWave_IsFinalWaveOrMultipleOfTen` | 25 波局 Boss 波 = 10/20/25；50 波局 = 10/20/30/40/50 | — | 未执行（无测试程序集） |
| A4 | EditMode（待建） | `LevelConfigTests::EliteWave_ExcludesBossWaves` | 第 10/20/25 波不判为精英波；第 5/15 波判为精英波 | 若两判定不互斥则红灯 | 未执行（无测试程序集） |
| A5 | EditMode（待建） | `LevelConfigTests::PicksAfterWave_MatchesFixedPickCount` | 25 波累计固定选卡 = 18 次；50 波 = 32 次 | 与需求 §3.6 不符则红灯 | 未执行（无测试程序集） |
| A6 | EditMode（待建） | `LevelConfigTests::QuotaForWave_EarlyWavesUseTableThenFormula` | 第 1–5 波为 12/18/24/30/36；第 6 波起 `min(70, 36+2×(w−5))`；Boss 波为 0 | — | 未执行（无测试程序集） |
| A7 | EditMode（待建） | `LevelConfigTests::QuotaForWave_AppliesCountMultiplierWithFloorOfOne` | 休闲难度配额乘 0.85 取整；极小配额不落到 0 | 下限缺失会产出 0 配额死波 | 未执行（无测试程序集） |
| A8 | EditMode（待建） | `LevelConfigTests::SpawnInterval_RespectsFloor` | 后期间隔不低于 0.22；乘难度后不低于 0.14 | — | 未执行（无测试程序集） |
| A9 | EditMode（待建） | `LevelConfigTests::TrashHp_MatchesPerKindFactors` | 普通 `16+6w`、高血 `40+10w`，射手 ×0.8、母体 ×0.6、子体 ×0.25、盾卫 ×1.2、引线虫 ×0.6，全部再乘血量倍率 | — | 未执行（无测试程序集） |
| A10 | EditMode（待建） | `LevelConfigTests::AffixChance_GatesBeforeWaveThreeAndCapsAtHalf` | 第 1–2 波为 0；第 3 波 12%；第 5 波 18%；任意波不超 50% | 门控或封顶缺失则红灯 | 未执行（无测试程序集） |
| A11 | EditMode（待建） | `LevelConfigTests::RollKind_ReturnsKindNotColumnIndex` | 抽取结果恒属于 `SpawnKinds` 集合，且第 1 波恒为 kind 0 | 若误返回列下标，会产出不存在的 kind 3/6 | 未执行（无测试程序集） |
| A12 | EditMode（待建） | `LevelConfigTests::WeightTables_SumToHundred` | `weights`、`lateWeights`、`eliteWeights` 每行权重和 = 100 | 权重和不足会静默回落 kind 0 | 未执行（无测试程序集） |
| A13 | EditMode（待建） | `LevelConfigTests::BossHpForWave_UsesFinalValueOnlyOnFinalWave` | 非最终 Boss 波用 `(1600+100w)×血量倍率`；最终波用 `BossHp` | — | 未执行（无测试程序集） |
| A14 | EditMode（待建） | `LevelConfigTests::InvalidInput_FallsBackToStandardAnd25Waves` | 非法难度回落标准；`waves != 50` 一律 25 波 | — | 未执行（无测试程序集） |
| A15 | EditMode（待建） | `LevelConfigTests::ApplyWorld_WritesMapLimit` | `ApplyWorld()` 后 `EmberWorld.Limit == MapLimit` | — | 未执行（无测试程序集） |
| A16 | EditMode（待建） | `GameSpeedTests::SetSpeed_WritesTimeScaleAndPrefs` | 设 1x–5x 后 `Time.timeScale` 与 `PlayerPrefs` 同步 | 依赖 `PlayerPrefs`，需在 TearDown 清理 | 未执行（无测试程序集） |

> **层级判定依据**：以上均为纯数值/规则查询，不需要进入运行模式即可判定，故为 EditMode。`GameSpeed` 触碰 `Time.timeScale` 与 `PlayerPrefs`，属于 EditMode 可测范围（需在 TearDown 还原）。

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 尚无可执行的测试程序集 | — | **未验证** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`；无额外配置
- 依赖模块状态：无（M1 是叶节点）
- 目标设备与画质档位：Editor 优先，Android 真机可选

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | 分别以休闲 / 标准 / 困难各开一局 | 三档难度下怪血、敌方伤害、同屏怪量、刷怪速度有可感知差异 | Editor | — | 待人工验收 | 数值验算已由 A1–A3 覆盖；此处只判手感差异 |
| H2 | 25 波局打到第 10 / 20 / 25 波 | 第 10、20 波为阶段 Boss；第 25 波为最终决战；第 20 波 Boss 会释放暗弹齐射 | Editor | — | 待人工验收 | 暗弹预警可读性见 M3 |
| H3 | 全程开启 5x 倍速打完一局 | 倍速生效且退出/重进后保持；选卡动画与暂停不受 `timeScale` 影响 | Editor | — | 待人工验收 | 依赖 M6 的 `unscaledDeltaTime` 使用 |
| H4 | 困难难度第 5 波观察怪群 | 词缀怪明显少于旧版（封顶 50%），精英挑战小队不带词缀 | Editor | — | 待人工验收 | — |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 传入非法 `EmberDifficulty` 值 | 回落标准难度，不抛异常 | EditMode 待建 | 未验证 | 对应 A14 |
| E2 | 传入 `waves = 0` / 负数 / 999 | 一律按 25 波构造，不产出非法 `BossWave` | EditMode 待建 | 未验证 | 对应 A14 |
| E3 | 查询波次 0 或负波次 | 配额与选卡次数返回 0，布尔判定返回 false，不抛异常 | EditMode 待建 | 未验证 | 对应 A6 |
| E4 | 极低配额 × 休闲难度倍率 | 配额下限为 1，不产生"0 只怪却等清波"的死锁 | EditMode 待建 | 未验证 | 对应 A7 |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M3 战斗与波次 | 刷怪节奏、Boss 出场、清波判定 | 全部消费 M1 数值 | 待人工验收 | 改 M1 必须打一局 |
| M2 单局进度 | 选卡次数 | 由 `PicksAfterWave` 决定 | 未验证 | 对应 A5 |
| M5 敌人系统 | 敌人血量与词缀概率 | 由 `TrashHp` / `AffixChance` 决定 | 待人工验收 | — |
| M6 界面 | 难度名与波次主题文案 | 由 `DifficultyName` / `WaveName` 提供 | 待人工验收 | — |

## 交付结论

- **已验证**：无。数值口径已通过**逐项对照代码**确认（见 `../requirements.md` §3 的代码来源标注），但**尚未通过任何可执行检查**。
- **不适用**：场景装配与 Prefab 相关检查——M1 无场景对象。
- **待手动验收**：H1–H4（难度手感差异、Boss 波次体感、倍速与退出保持、词缀密度）。
- **未验证**：A1–A16 全部（阻塞原因：项目无 asmdef 与测试程序集）。
- **未通过**：无。
