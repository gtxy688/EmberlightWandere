# M2 单局进度与构筑 验收文档

> 对应架构：`02-run-progression.md`
> 对应需求：`../requirements.md` §4、§5
> ⚠️ 工程已有 EditMode / PlayMode 基础设施；本表未实现的条目仍标待建，见 `00-overview.md` §测试现状。
> ✅ **本模块是补齐自动化收益最高的目标**：`RunProgress`、`EmberChoicePool`、`EmberWeaponCatalog`、
> `EmberRarityUtil` 现已**全部零 `UnityEngine` 依赖**，可直接用 EditMode 测试覆盖全部规则。
>
> 2026-09-23 订正：此前这里写"`RunProgress` 与 `EmberRarityUtil` 零 `UnityEngine` 依赖"，
> 但当时 `EmberRarityUtil` 用了 `Mathf` 并返回 `Color`，该断言**不成立**。
> 现已把 `WeightsForLuck` 改用 `System.Math`、配色改为 `float[3]` + UI 侧 `EmberRarityTint.ToColor()`，
> A25 那类断言才真正可测。

## 自动化测试计划与证据

`Choices` / `Choose` / `RollByLuck` 等是**纯规则**，不依赖帧推进、MonoBehaviour 或场景装配 → 全部归 **EditMode**。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | EditMode ✅ | `RunProgressTests::ConfigureRun_WithASingleStarterWeapon_LeavesTheRestOfTheSlotsEmpty` | 开局后 `OwnedWeaponCount == 1`，`EmptySlots == N-1` | 「开局一次填满 N 槽」回归时红灯——这是 v3 的核心纠正 | **通过**（2026-10-03） |
| A2 | EditMode ✅ | `RunProgressTests::ConfigureRun_ClampsSlotsToRange` | N 传 0 / -3 / 6 / 99 均被钳制到 [1,5] | — | **通过**（2026-10-03，4 个用例） |
| A3 | EditMode ✅ | `RunProgressTests::ConfigureRun_NullOrEmptyRoster_FallsBackToFireball`、`ConfigureRun_NonRosterId_FallsBackToFireball` | 传 null / 空集 / 非名册 id → 兜底为火球 13 | 兜底缺失会导致开局无武器可 Tick | **通过**（2026-10-03） |
| A4 | EditMode ✅ | `RunProgressTests::ConfigureRun_ResetsAllStateBetweenRuns` | 第二局开局后幸运 / 护盾 / 增幅 / 分数 / 刷新次数全部归零、武器不继承 | 状态残留会让新局带上一局构筑 | **通过**（2026-10-03） |
| A5 | EditMode ✅ | `EmberChoicePoolTests::Build_OfferCountAlwaysMatchesThreePlusEmptySlots`、`ExpectedCount_IsThreePlusEmptySlots`、`Build_ReturnsThreeOffersWhenSlotsAreFull` | 候选长度恒为 `3 + EmptySlots`（N=1..5 全测） | 与需求 §4.3 不符则红灯 | **通过**（2026-10-03，7 个用例） |
| A6 | EditMode ✅ | `EmberChoicePoolTests::Build_NeverRepeatsTheSameIdWeaponIdPair`、`OfferKey_EncodesBothIdAndWeaponId` | 60 个种子下同一候选内无重复 `(Id, WeaponId)` | 出现两张 `火球·攻击力` 时红灯 | **通过**（2026-10-03） |
| A7 | EditMode ✅ | `EmberChoicePoolTests::Build_AllowsTheSameStatOnDifferentWeapons` | 双武器时同词条可绑定不同武器（键不同即允许同屏） | 若按笼统"种类"去重会误判红灯 | **通过**（2026-10-03）。注：测试为条件式断言（仅当同屏出现多张攻击力卡时校验其武器互异） |
| A8 | EditMode ✅ | `EmberChoicePoolTests::Build_ExcludesNewWeaponCardsWhenSlotsAreFull` | 满槽（N=1）时 40 个种子下候选均不含 10–19 武器卡 | 满槽仍出新武器时红灯 | **通过**（2026-10-03） |
| A9 | EditMode ✅ | `EmberChoicePoolTests::Build_AddsOneExtraOfferPerEmptySlot`、`Build_WithEmptySlots_OffersUnownedWeapons`、`Build_NewWeaponCardsAreGoldRarity` | 未满槽时额外张数 = `EmptySlots`，且额外卡是未拥有武器、固定黄金 | — | **通过**（2026-10-03） |
| A10 | EditMode ✅ | `EmberChoicePoolTests::Build_ExcludesExclusivesAndMetamorphsForUnownedWeapons`、`Build_ExclusivesAreGoldAndMetamorphsAreDiamond` | 未拥有对应武器时其专属(30–35)/质变(20–25)不进池；专属固定黄金、质变固定钻石 | — | **通过**（2026-10-03） |
| A11 | EditMode ✅ | `EmberChoicePoolTests::Build_NeverEmitsRetiredGenericIds` | 60 个种子下候选中不出现 Id 5–9 的旧通用 | 旧通用回流时红灯 | **通过**（2026-10-03） |
| A12 | EditMode ✅ | `EmberChoicePoolTests::Build_AlwaysReturnsAPickableHandEvenAtMaximumSlots`、`Build_EveryOfferIsAnAcceptedIdSpaceEntry` | 池子枯竭时仍返回 ≥3 张合法候选，不空手 | 空候选会让玩家卡在选卡态 | **通过**（2026-10-03） |
| A13 | EditMode ✅ | `EmberRarityUtilTests::WeightsForLuck_AtZero_MatchesAuthoritativeTable`、`_AtSixty_`、`_AtOneTwenty_` | 幸运 0/60/120 的权重为 72-22-5-1 / 58-28-11-3 / 45-32-18-5 | ⚠️ 回退到旧表 60-28-10-2 时红灯 | **通过**（2026-10-03） |
| A14 | EditMode ⚠️ 部分 | `WeightsForLuck_ClampsBelowZeroAndAboveMax`、`WeightsForLuck_InterpolatesLinearlyBetweenAnchorPoints`、`WeightsForLuck_SumToExactlyHundredAtTheDocumentedAnchorPoints`、`WeightsForLuck_IntermediateValuesDriftFromHundredByAtMostTwo` | 幸运 <0 按 0、>120 按 120；中间值线性插值 | — | **通过**（2026-10-03），但**"权重和恒为 100"在中间幸运值不成立**：15/45 → 102，75/90 → 101。已改为断言漂移 ≤ 2 并记录为待裁定问题（见下） |
| A15 | EditMode ✅ | `EmberRarityUtilTests::GenericAtkAs_`、`GenericAttackSpeed_UsesItsOwnDiamondStep`、`GenericLuck_`、`GenericAmp_`、`ShieldAmount_`（5 个 `_MatchesRequirementTable`） | 攻击力 10/20/30/40%、攻速 10/20/30/50%、幸运 25/50/75/125、增幅 5/10/15/25%、护盾 250/500/750/1250 | 攻速被误并入攻击力档位时红灯 | **通过**（2026-10-03）。攻速钻石档另有一条显式断言 `Is.Not.EqualTo` 攻击力钻石档 |
| A16 | EditMode ✅ | `RunProgressTests::Choose_AttackAppliesOnlyToTheTargetWeapon`、`Choose_AttackSpeedAppliesOnlyToTheTargetWeapon` | `火球·攻击力` 只抬火球，环火数值不变 | 全局化回归时红灯（硬约束 2） | **通过**（2026-10-03） |
| A17 | EditMode ✅ | `RunProgressTests::Choose_AmpIsGlobalAndFeedsDamageMul` | 全体增幅写入 `DamageAmp` 并通过 `DamageMul` 作用于全部武器 | — | **通过**（2026-10-03）。注：断言的是全局字段而非逐武器伤害，因为 6 把武器的 Tick 需 PlayMode 才能测 |
| A18 | EditMode ✅ | `RunProgressTests::DamageMul_IgnoresTheRetiredDamageBonusField` | `DamageMul == 1 + DamageAmp`，不受已停用的 `DamageBonus` 影响 | — | **通过**（2026-10-03） |
| A19 | EditMode ✅ | `RunProgressTests::Choose_NewWeaponReportsGrantedFlag`、`Choose_WhenSlotsAreFull_RejectsNewWeapon` | 选新武器时 `grantedNewWeapon == true`；满槽时拒绝且标志为 false | 补偿选卡依赖此标志 | **通过**（2026-10-03）。注：**"升级已有武器时为 false"这一半未单独断言** |
| A20 | EditMode ✅ | `RunProgressTests::CanMetamorph_RequiresOwningTheWeaponButNoExclusive` | 未拿专属也可选质变；未拥有对应武器时不可选 | 加前置条件时红灯 | **通过**（2026-10-03） |
| A21 | EditMode ✅ | `RunProgressTests::TryRefresh_ConsumesExactlyTwoPerRun`、`TryRefresh_WithoutPendingOffer_DoesNotConsumeABudget` | 整局只能成功刷新 2 次，第 3 次返回 false；失败不扣次数 | — | **通过**（2026-10-03） |
| A22 | EditMode ✅ | `RunProgressTests::AbsorbDamage_ConsumesShieldBeforeHealthAndReturnsTheOverflow`、`AbsorbDamage_WithNoShield_ReturnsFullDamage`、`AbsorbDamage_TreatsNegativeDamageAsZero` | 护盾足够时溢出为 0；不足时返回溢出值且护盾不为负 | 顺序反了会让护盾形同虚设 | **通过**（2026-10-03） |
| A23 | EditMode ✅ | `RunProgressTests::AddScore_RejectsNegativeAmounts`、`AddScore_AccumulatesAndFinalScoreHasNoMultiplier` | 负数抛 `ArgumentOutOfRangeException`；`FinalScore() == Score`；`ScoreMult == 1.0` | ScoreMult 被改成随 N 变化时红灯 | **通过**（2026-10-03） |
| A24 | EditMode ✅ | `EmberRarityUtilTests::KindOf_MapsEveryIdRange`、`IsGenericId_TrueOnlyForZeroThroughFour` | 0–9 通用、10–19 武器、20–29 质变、30–39 专属 | — | **通过**（2026-10-03） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| 2026-10-14 | Unity 2022.3.62f3，`-batchmode -nographics`，EditMode | `Unity.exe -batchmode -nographics -projectPath <项目> -runTests -testPlatform editmode -testResults Library/TestRun/editmode.xml -logFile Library/TestRun/tests.log` | `Library/TestRun/editmode.xml` | **通过**：`total=150 passed=150 failed=0 skipped=0`，Unity 退出码 **0** |
| 2026-10-03 | 同上（本模块基线首跑） | 同上 | `Library/TestRun/editmode.xml` | **通过（历史）**：`total=103 passed=103 failed=0`。当时全仓只有本模块的 4 个测试类 |

覆盖本模块的测试类与用例数（2026-10-14）：

| 测试类 | 用例数 | 覆盖内容 |
|--------|--------|----------|
| `EmberWeaponCatalogTests` | 17 | Id 分区、名册顺序与副本隔离、`WeaponForMeta` / `WeaponForExcl` 逐项映射、越界返回 0 |
| `EmberRarityUtilTests` | 24 | 权重表三个锚点、插值、边界钳制、`RollByLuck` 可复现性与分布、通用词条五张数值表、`KindOf` / `IsGenericId` / `AllNames` / `Tint` |
| `EmberChoicePoolTests` | 25 | `3 + 空位` 张数、`(Id, WeaponId)` 同屏唯一、满槽不出新武器、专属/质变归属、旧通用不回流、兜底不空手、发牌可复现 |
| `RunProgressTests` | 37 | 开局构筑与槽位钳制、跨局状态清零、攻击力/攻速单武器作用域、扩幅全局、护盾叠加不被钳、刷新 2 次、护盾先于生命、计分 |
| `RunProgressChoiceEntryTests` | 2 | 真实选卡入口：`Choices` 置待选状态后才可提交；刷新替换候选而不累加选择次数 |

### 本轮通过测试发现的既有问题（未擅自改产品行为）

| # | 现象 | 证据 | 影响 | 处置 |
|---|------|------|------|------|
| 1 | **中间幸运值的品质权重和不是 100** | 幸运 0/60/120 恰好为 100，但 15 → `69/24/7/2 = 102`、45 → 102、75 → 101、90 → 101（四个通道各自四舍五入造成） | `RollWeighted` 用 `weights.Sum()` 当分母，因此这些幸运档下各品质的实际出率有约 1–2% 的相对偏移（例：钻石 4/101 而非 4/100） | 已用 `WeightsForLuck_IntermediateValuesDriftFromHundredByAtMostTwo` **钉住 ±2 上限**；是否归一化到 100 属**待裁定**，见 `../requirements.md` §9 |
| 2 | **`ConfigureRun` 不是"开局只给 1 把"的守卫** | 传 2 把 + `slots=3` → 实际拥有 2 把（循环上限是 `granted < slots`，不是 `granted < 1`） | 当前 UI（`EmberMenuFlow.OnRosterChosen`）只传 1 把，所以**线上行为不受影响**；但任何从别处传多把进来的调用都会绕过 v3 硬约束 4 | 已用 `ConfigureRun_WithSeveralInputWeapons_GrantsMoreThanOne` 记录真实行为并写明"若将来加守卫，此测试应红灯"；是否加守卫属**待裁定** |
| 3 | `RunProgress.AddShield` 是死代码且与现行需求冲突 | 全仓仅 1 处匹配（其自身定义）；它把护盾硬钳到 **250**，即"护盾 /2"之前的旧口径 | 无运行时影响（无人调用），但**是个陷阱**：后来者若用它发护盾，钻石护盾会被砍成青铜数值 | 已加 XML 注释显式警告并指向真正的发放点（`Choose` 的 `StatShield` 分支）；删除与否属**待裁定** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`
- 依赖模块状态：M6 开局四步 UI 与选卡面板可用
- 目标设备与画质档位：Editor 优先

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | 开局流程：选 N=2 → 整库翻页任选 1 把 → 开战 | 先选 N 再单选 1 把；开战 HUD 显示 `1/2`；无"跳过"入口 | Editor | — | 待人工验收 | 开局不可跳过为 v3 硬口径 |
| H2 | N=1 打到中期 | 整局几乎只在起始武器上长，清波很难刷出第二把武器 | Editor | — | 待人工验收 | 槽位口径的手感验证 |
| H3 | N=5 打到中期 | 明显能塞多种武器，也更容易"什么都沾一点" | Editor | — | 待人工验收 | 同上 |
| H4 | 双武器时反复选卡 | 可同屏看到两张不同武器的攻击力 + 全体伤害增幅；看不到两张同名卡 | Editor | — | 待人工验收 | 对应 A7 |
| H5 | 选中一张新武器卡 | 立即再弹一次选卡，且**波次没有推进** | Editor | — | 待人工验收 | 补偿选卡 |
| H6 | 用掉 2 次刷新后再点刷新 | 刷新按钮不可用或点击无效 | Editor | — | 待人工验收 | 对应 A21 |
| H7 | 逐局对照幸运值与卡面品质 | 幸运越高金银钻越常见；幸运 120 时钻石仍明显稀少 | Editor | — | 待人工验收 | 权重表体感；对应 A13 |
| H8 | 六把武器各开一局，只靠该武器 + 专属 + 质变打到 Boss | 六条线成型后强度接近，无一条明显超模 | Editor | — | 待人工验收 | 平衡口径见 `04-weapon-system.md` |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 槽位传 0 / 6 / 负数 | 钳制到 [1,5]，不产出非法 `EmptySlots` | EditMode 待建 | 未验证 | 对应 A2 |
| E2 | 名册全非法或为空 | 兜底授予火球，仍能开局 | EditMode 待建 | 未验证 | 对应 A3 |
| E3 | 候选池枯竭（全武器已拥有 + 满槽） | 用通用补齐，绝不返回空候选 | EditMode 待建 | 未验证 | 对应 A12 |
| E4 | 伪造 `EmberOffer` / 重复提交同一张卡 | 防伪校验拒绝，状态不变 | EditMode 待建 | 未验证 | 对应 `RunFlowControllerTests.ForgedOrRepeatedChoice_DoesNotChangeBuild` |
| E5 | 连续开局两局 | 第二局不携带第一局的构筑与 Score | EditMode 待建 | 未验证 | 对应 A4 |
| E6 | `Progress` 为 null 时（已回营地）战斗 Tick | 不抛空引用 | PlayMode 待建 | 未验证 | 依赖 M3 的 null 容忍 |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M4 武器系统 | 6 把武器伤害随攻击力/攻速/增幅的变化 | 全部读 M2 的加成接口 | 待人工验收 | 对应 A16/A17 |
| M6 界面 | 候选卡数量、卡面文案、幸运与刷新显示 | 由 M2 数据驱动 | 待人工验收 | — |
| M3 战斗 | 清波 → 发牌 → 选后推进 | 补偿选卡会暂停波次推进 | 待人工验收 | 对应 H5 |
| M7 表现 | 卡面图标映射 | 按词条 id 取图 | N/A | 仅 id 常量耦合，改 id 才需复验 |

## 交付结论

- **已验证**（2026-10-14 实跑，命令与结果见上表）：
  - EditMode 测试套件 **150 个用例全部通过**（`total=150 passed=150 failed=0`），Unity 退出码 **0**；
  - 其中覆盖本模块的用例：`EmberWeaponCatalogTests` 17、`EmberRarityUtilTests` 24、`EmberChoicePoolTests` 25、`RunProgressTests` 37、`RunProgressChoiceEntryTests` 2，共 **105** 条；
  - 源码扫描确认 M2 四个文件零 `UnityEngine` 引用。
- **不适用**：场景装配与 Prefab 检查——`RunProgress` 无场景对象。
- **待手动验收**：H1–H8（开局流程、槽位手感、同屏唯一、补偿选卡、刷新、幸运体感、六线平衡）。
- **部分覆盖**：A14（权重和断言已放宽为漂移 ≤2）、A17（断言全局字段而非逐武器伤害）、A19（仅覆盖 `true` 分支）、A25（源码扫描代替反射断言）。
- **未通过**：无。
- **本轮测试发现的既有问题（待裁定，未擅自改产品行为）**：
  1. 中间幸运值的品质权重和漂到 101–102（`RollWeighted` 以权重和为分母 → 出率约 1–2% 相对偏移）；
  2. `ConfigureRun` 并非"开局只给 1 把"的守卫（当前 UI 只传 1 把，故线上无影响）。

  | 问题 | 候选方案 | 影响 | 需要谁裁定 |
  |------|----------|------|------------|
  | 权重和漂移 | 归一化到 100 / 维持现状并把 ±2 写进需求 | 影响各幸运档的实际出率口径；改则需重跑 A13 与 jack 的体感验收 | jack + xiaoPlanner |
  | `ConfigureRun` 多把输入 | 加"只取第一把"的守卫 / 维持现状（视为越界调用） | 加守卫可编译期外防住硬约束 4 被绕过 | jack + xiaoCoder |

> 2026-10-14 注释精简时已删除 `RunProgress` 的死成员：`AddShield`（把护盾硬钳到 250 的旧口径陷阱）、`HasEvolved` / `CanEvolve` / `OwnedWeaponsView` / `HealAmount` / `EmberRarityUtil.Magnitude` / `CountBonus`（全仓零调用点）。`WavePower` / `ScoreMult` 等仍被测试或武器读取，保留。因此原「`AddShield` 死代码去留」待裁定项已关闭。

## ⚠️ 已修正的历史记录

本文件由已归档的 `Rarity-and-TMP.md` 与 `Survival-v1.md` 合并整理而来，其中下列内容**与现行代码不符，已在 `../requirements.md` 中订正**，此处仅留警示，勿再引用：

| 历史表述 | 正确值 |
|----------|--------|
| 幸运权重 `0 → 60/28/10/2`、`60 → 45/30/18/7`、`120 → 30/28/28/14` | **`0 → 72/22/5/1`、`60 → 58/28/11/3`、`120 → 45/32/18/5`** |
| 表头把攻速并入 `G-ATK / G-AS` 同档（+10/20/30/40） | **攻速独立档位：+10/20/30/50%** |
| `ATK/AS 加算全局`、`DamageMul = (1+DamageBonus)*(1+DamageAmp)` | **攻击力/攻速只作用于对应武器；`DamageMul = 1 + DamageAmp`** |
| 护盾"参考数值除以 2"（125/250/375/625 语境） | **250 / 500 / 750 / 1250**（见 `../requirements.md` §4.4） |

## 本轮新增回归（2026-10-13）

`RunProgressChoiceEntryTests` 验证实际 Choices 入口能提交一次、拒绝重复提交，以及刷新不累加待选次数；修复前两条均失败。现有测试仍保留直接 OfferChoices 路径，但不能替代实际发牌入口。运行结果见 `../实现说明.md`。
