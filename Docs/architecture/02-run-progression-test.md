# M2 单局进度与构筑 验收文档

> 对应架构：`02-run-progression.md`
> 对应需求：`../requirements.md` §4、§5
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。
> ✅ **本模块是补齐自动化收益最高的目标**：`RunProgress` 与 `EmberRarityUtil` 零 `UnityEngine` 依赖，可直接用 EditMode 测试覆盖全部规则。

## 自动化测试计划与证据

`Choices` / `Choose` / `RollByLuck` 等是**纯规则**，不依赖帧推进、MonoBehaviour 或场景装配 → 全部归 **EditMode**。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | EditMode（待建） | `RunProgressTests::ConfigureRun_GrantsExactlyOneStarterWeapon` | 开局后 `OwnedWeaponCount == 1`，`EmptySlots == N-1` | 「开局一次填满 N 槽」回归时红灯——这是 v3 的核心纠正 | 未执行（无测试程序集） |
| A2 | EditMode（待建） | `RunProgressTests::ConfigureRun_ClampsSlotsToRange` | N 传 0/6/负数均被钳制到 [1,5] | — | 未执行（无测试程序集） |
| A3 | EditMode（待建） | `RunProgressTests::ConfigureRun_FallsBackToFireballOnInvalidRoster` | 传入非名册 id 或空集 → `OwnedWeapons` = {火球 13} | 兜底缺失会导致开局无武器可 Tick | 未执行（无测试程序集） |
| A4 | EditMode（待建） | `RunProgressTests::ConfigureRun_ResetsAllStateBetweenRuns` | 第二局开局后幸运/护盾/加成/刷新次数全部归零 | 状态残留会让新局带上一局构筑 | 未执行（无测试程序集） |
| A5 | EditMode（待建） | `Choices_ReturnsThreePlusEmptySlotsOffers` | 候选长度恒为 `3 + EmptySlots` | 与需求 §4.3 不符则红灯 | 未执行（无测试程序集） |
| A6 | EditMode（待建） | `Choices_NeverRepeatsSameIdWeaponIdPair` | 同一候选内无重复 `(Id, WeaponId)` | 出现两张 `火球·攻击力` 时红灯 | 未执行（无测试程序集） |
| A7 | EditMode（待建） | `Choices_AllowsSameStatOnDifferentWeapons` | 双武器时 `火球·攻击力` 与 `环火·攻击力` 可同屏 | 若按笼统"种类"去重会误判红灯 | 未执行（无测试程序集） |
| A8 | EditMode（待建） | `Choices_ExcludesNewWeaponsWhenSlotsFull` | 满槽（含 N=1）时候选中不含任何 10–19 武器卡 | 满槽仍出新武器时红灯 | 未执行（无测试程序集） |
| A9 | EditMode（待建） | `Choices_AddsOneNewWeaponPerEmptySlot` | 未满槽时额外张数 = `EmptySlots` | — | 未执行（无测试程序集） |
| A10 | EditMode（待建） | `Choices_ExcludesExclusiveAndMetamorphWithoutOwnedWeapon` | 未拥有对应武器时，其专属(30–35)/质变(20–25)不进池 | — | 未执行（无测试程序集） |
| A11 | EditMode（待建） | `Choices_NeverEmitsLegacyGenerics` | 候选中不出现 Id 5–9 的旧通用 | 旧通用回流时红灯 | 未执行（无测试程序集） |
| A12 | EditMode（待建） | `Choices_PadsWithGenericsToGuaranteeThree` | 候选池枯竭时仍返回可选候选，不空手 | 空候选会让玩家卡在选卡态 | 未执行（无测试程序集） |
| A13 | EditMode（待建） | `WeightsForLuck_MatchesNerfedTable` | 幸运 0/60/120 的权重为 72-22-5-1 / 58-28-11-3 / 45-32-18-5 | ⚠️ 回退到旧表 60-28-10-2 时红灯 | 未执行（无测试程序集） |
| A14 | EditMode（待建） | `WeightsForLuck_ClampsLuckAndInterpolates` | 幸运 <0 按 0、>120 按 120；中间值线性插值且权重和恒为 100 | — | 未执行（无测试程序集） |
| A15 | EditMode（待建） | `GenericMagnitudes_MatchRequirementTable` | 攻击力 10/20/30/40%、攻速 10/20/30/50%、幸运 25/50/75/125、增幅 5/10/15/25%、护盾 250/500/750/1250 | 攻速被误并入攻击力档位时红灯 | 未执行（无测试程序集） |
| A16 | EditMode（待建） | `Choose_AttackAndSpeedApplyOnlyToTargetWeapon` | `火球·攻击力` 只抬火球，环火伤害不变 | 全局化回归时红灯 | 未执行（无测试程序集） |
| A17 | EditMode（待建） | `Choose_DamageAmpAppliesToAllWeapons` | 全体增幅抬升全部 6 把武器 | — | 未执行（无测试程序集） |
| A18 | EditMode（待建） | `DamageMul_EqualsOnePlusAmpOnly` | `DamageMul == 1 + DamageAmp`，不受已停用的 `DamageBonus` 影响 | — | 未执行（无测试程序集） |
| A19 | EditMode（待建） | `Choose_NewWeaponReportsGrantedFlag` | 选新武器时 `grantedNewWeapon == true`，升级已有武器时为 false | 补偿选卡依赖此标志 | 未执行（无测试程序集） |
| A20 | EditMode（待建） | `Choose_MetamorphHasNoExclusivePrerequisite` | 未拿专属也可选质变 | 加前置条件时红灯 | 未执行（无测试程序集） |
| A21 | EditMode（待建） | `TryRefresh_ConsumesExactlyTwoPerRun` | 整局只能成功刷新 2 次，第 3 次返回 false | — | 未执行（无测试程序集） |
| A22 | EditMode（待建） | `AbsorbDamage_ConsumesShieldBeforeHealth` | 护盾足够时生命不变；溢出的部分扣生命并返回该值 | 顺序反了会让护盾形同虚设 | 未执行（无测试程序集） |
| A23 | EditMode（待建） | `AddScore_RejectsNegativeAndDoesNotChangeScoreMult` | 负数抛 `ArgumentOutOfRangeException`；`FinalScore() == Score` | ScoreMult 被改成随 N 变化时红灯 | 未执行（无测试程序集） |
| A24 | EditMode（待建） | `KindOf_MapsIdRanges` | 0–9 通用、10–19 武器、20–29 质变、30–39 专属 | — | 未执行（无测试程序集） |
| A25 | EditMode（待建） | `RunProgress_HasNoUnityEngineDependency` | 反射断言 `RunProgress` 的字段/方法签名不出现 `UnityEngine` 类型 | 防边界腐化；若引入 `Mathf`/`Vector2` 则红灯 | 未执行（无测试程序集） |
| A26 | EditMode（待建） | `OfferAntiForgery_RejectsTamperedOffer` | 伪造或已被消耗的 `EmberOffer` 不被接受 | 防重复领卡 | 未执行（无测试程序集） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 尚无可执行的测试程序集 | — | **未验证** |

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
| E4 | 伪造 `EmberOffer` / 重复提交同一张卡 | 防伪校验拒绝，状态不变 | EditMode 待建 | 未验证 | 对应 A26 |
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

- **已验证**：无。规则已**逐项对照代码**确认（`RunProgress.cs` / `EmberRarity.cs`），但尚无任何可执行检查通过。
- **不适用**：场景装配与 Prefab 检查——`RunProgress` 无场景对象。
- **待手动验收**：H1–H8（开局流程、槽位手感、同屏唯一、补偿选卡、刷新、幸运体感、六线平衡）。
- **未验证**：A1–A26 全部（阻塞原因：项目无 asmdef 与测试程序集）。
- **未通过**：无。

## ⚠️ 已修正的历史记录

本文件由已归档的 `Rarity-and-TMP.md` 与 `Survival-v1.md` 合并整理而来，其中下列内容**与现行代码不符，已在 `../requirements.md` 中订正**，此处仅留警示，勿再引用：

| 历史表述 | 正确值 |
|----------|--------|
| 幸运权重 `0 → 60/28/10/2`、`60 → 45/30/18/7`、`120 → 30/28/28/14` | **`0 → 72/22/5/1`、`60 → 58/28/11/3`、`120 → 45/32/18/5`** |
| 表头把攻速并入 `G-ATK / G-AS` 同档（+10/20/30/40） | **攻速独立档位：+10/20/30/50%** |
| `ATK/AS 加算全局`、`DamageMul = (1+DamageBonus)*(1+DamageAmp)` | **攻击力/攻速只作用于对应武器；`DamageMul = 1 + DamageAmp`** |
| 护盾"参考数值除以 2"（125/250/375/625 语境） | **250 / 500 / 750 / 1250**（见 `../requirements.md` §4.4） |
