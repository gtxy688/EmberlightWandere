# M3 战斗与波次 验收文档

> 对应架构：`03-combat-waves.md`
> 对应需求：`../requirements.md` §2、§3、§5
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。

## 自动化测试计划与证据

分层依据：波次推进、清波判定、伤害结算与死亡顺序都依赖**帧推进与 MonoBehaviour 生命周期** → **PlayMode**。
纯决策的边界（如波次判定互斥、伤害倍率应用顺序）若不触碰场景，可下沉为 EditMode。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | PlayMode（待建） | `CombatWaveTests::WaveAdvance_RequiresQuotaSpawnedAndFieldCleared` | 仅当配额刷完**且**场上为 0 才清波 | 少任一条件即提前进选卡 | 未执行（无测试程序集） |
| A2 | PlayMode（待建） | `CombatWaveTests::BossWave_ReplacesEliteChallenge` | 第 10/20 波不生成精英挑战小队，只出 Boss | — | 未执行（无测试程序集） |
| A3 | PlayMode（待建） | `CombatWaveTests::EliteWave_SpawnsCappedAffixedSquad` | 每 5 波（非 Boss 波）过半配额时强制最多 6 只带词缀精英 | — | 未执行（无测试程序集） |
| A4 | PlayMode（待建） | `CombatWaveTests::Encounter_TriggersAtMostOncePerWave` | 遭遇事件每波至多 1 次，且事件怪计入配额 | 事件不计配额会导致清波判定卡死 | 未执行（无测试程序集） |
| A5 | PlayMode（待建） | `CombatWaveTests::EnemyCount_NeverExceedsLimit` | 同屏敌人不超过 150 | — | 未执行（无测试程序集） |
| A6 | PlayMode（待建） | `CombatWaveTests::FinalWave_NoCardOffer` | 25 波局第 25 波清完直接结算，不发牌 | 多一次发牌会卡在 Upgrade 态 | 未执行（无测试程序集） |
| A7 | PlayMode（待建） | `CombatWaveTests::BossWaveAfterPick_ExtraPickOffered` | 阶段 Boss 波清完后连续发牌 2 次 | — | 未执行（无测试程序集） |
| A8 | PlayMode（待建） | `CombatWaveTests::NewWeaponPick_OffersCompensationWithoutAdvancingWave` | 选中新武器后补发一次牌，且波次不变 | 补偿选卡推进波次会丢波 | 未执行（无测试程序集） |
| A9 | EditMode（待建） | `DamageTests::HurtPlayer_AppliesDifficultyMultiplierOnce` | 困难难度受伤为原始值 ×1.3，只应用一次 | 双重乘算会让高难度暴毙 | 未执行（无测试程序集） |
| A10 | EditMode（待建） | `DamageTests::HurtPlayer_ShieldAbsorbsBeforeHealth` | 护盾充足时不掉血；溢出部分扣生命 | 顺序反了护盾形同虚设 | 未执行（无测试程序集） |
| A11 | EditMode（待建） | `DamageTests::HurtReady_BlocksDamageDuringInvulnerability` | 无敌帧内（0.65s）再次受伤无效 | — | 未执行（无测试程序集） |
| A12 | PlayMode（待建） | `DeathTests::BroodSplitsThreeChildrenOnlyOnFinalDeath` | 烬胎分裂恰好 3 只；重生读条中不分裂；子体不消耗配额 | — | 未执行（无测试程序集） |
| A13 | PlayMode（待建） | `DeathTests::RebirthRevivesOnceAtFortyPercent` | 重生词缀只复活一次，且以 40% 生命复活 | 无限重生会导致该波无法清空 | 未执行（无测试程序集） |
| A14 | PlayMode（待建） | `DeathTests::BomberExplodesOnlyAfterFuse` | 引线结束才结算爆炸；引线期间不接触伤害、不被锁定 | — | 未执行（无测试程序集） |
| A15 | PlayMode（待建） | `DeathTests::NoPickOfferWhileRebirthOrFusePending` | 重生/引线未结束时不允许进入选卡 | — | 未执行（无测试程序集） |
| A16 | PlayMode（待建） | `DamageTests::OffscreenEnemiesTakeNoDamageAndAreNotTargeted` | 屏外敌人不被自动选敌、不结算伤害；火球离开视口后销毁 | 屏外击杀会让玩家"看不见地过关" | 未执行（无测试程序集） |
| A17 | PlayMode（待建） | `DamageTests::DamageFrom_UsesSourcePositionForShieldFacing` | 从背后击中盾卫造成全额伤害，正面减伤 70% | — | 未执行（无测试程序集） |
| A18 | PlayMode（待建） | `BossTests::BossAdvancesOnlyWhilePlaying` | 升级与暂停时冻结 Boss 预警与招式推进 | 暂停仍推进会让预警失效 | 未执行（无测试程序集） |
| A19 | PlayMode（待建） | `BossTests::DarkBoltVolleyStartsAtWaveTwenty` | 第 20 波起 Boss 释放带 1 秒预警的暗弹齐射 | — | 未执行（无测试程序集） |
| A20 | PlayMode（待建） | `BossTests::DarkBoltsCanBeIntercepted` | 玩家火球/穿透箭可击毁暗弹；穿透箭不消耗穿透次数 | — | 未执行（无测试程序集） |
| A21 | PlayMode（待建） | `BossTests::FrenzyTriggersBelowHalfHealth` | 半血后节奏加快且爆发区域增加 | — | 未执行（无测试程序集） |
| A22 | PlayMode（待建） | `BossTests::KillingFinalBossEndsRunAsWin` | 最终 Boss 死亡 → `Mode.Won` | — | 未执行（无测试程序集） |
| A23 | PlayMode（待建） | `PoolTests::RentedEnemiesRestoreBaselineAppearance` | 池归还后再借出还原基准姿态与颜色，标识不残留 | 复用躯体会把上一次的标识带到新怪 | 未执行（无测试程序集） |
| A24 | PlayMode（待建） | `LifecycleTests::TearDownClearsBoltsAndPool` | 清波/新局/退出时清理暗弹（上限 48）与池 | 残留暗弹会跨局伤人 | 未执行（无测试程序集） |
| A25 | PlayMode（待建） | `LifecycleTests::CombatTick_DoesNotThrowWhenProgressIsNull` | 回营地后 `Progress == null` 时战斗 Tick 不抛空引用 | — | 未执行（无测试程序集） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 尚无可执行的测试程序集 | — | **未验证** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`；25 波 + 标准难度
- 依赖模块状态：M2 选卡可用、M6 开局流程可用
- 目标设备与画质档位：Editor 优先；真机项另列

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | 标准难度 25 波完整打一局 | 波次推进正常；第 10/20/25 波为 Boss；第 25 波击杀即结算通关 | Editor | — | 待人工验收 | 主闭环 |
| H2 | 观察第 5/15 波 | 精英挑战波：高血/盾卫为主，带词缀精英不超过 6 只 | Editor | — | 待人工验收 | — |
| H3 | 战斗中站到地图边缘 | 围墙与灯柱/石块清晰标示 ±22 边界；敌人也被限制在场内 | Editor | — | 待人工验收 | 可读性项 |
| H4 | 用穿透火矢打第 20 波 Boss 的暗弹 | 暗弹可被击落，且穿透次数不被消耗 | Editor | — | 待人工验收 | 对应 A20 |
| H5 | 战斗中按暂停 | 暂停面具出现；Boss 预警冻结；恢复后节奏不跳变 | Editor | — | 待人工验收 | 对应 A18 |
| H6 | 被怪围住直到生命归零 | 进入失败结算，显示本局难度/长度/波数/时间/击杀/Score | Editor | — | 待人工验收 | — |
| H7 | 一局内多次清波选卡 | 每次选卡 HUD 与 Boss 血条正确隐藏/恢复，无残留 | Editor | — | 待人工验收 | — |
| H8 | 通关后回营地再开一局 | 上一局的敌人/暗弹/燃地/粒子全部清理 | Editor | — | 待人工验收 | 对应 A24 |
| H9 | Android 真机打完一局 | 触控摇杆、返回键暂停、切后台恢复、长时间不掉帧 | Android APK | — | 待人工验收 | 依赖 M9 构建 |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 配额刷完但场上仍有重生读条中的敌人 | 不进入选卡，等待重生结算 | PlayMode 待建 | 未验证 | 对应 A15 |
| E2 | 玩家在选卡面板打开时被残留伤害击杀 | 不应发生；选卡时战斗已停 | PlayMode 待建 | 未验证 | — |
| E3 | 回营地后战斗残留对象仍 Tick | 不抛空引用（`Progress == null`） | PlayMode 待建 | 未验证 | 对应 A25 |
| E4 | 暗弹达到上限 48 | 不再生成，不无限增长 | PlayMode 待建 | 未验证 | 对应 A24 |
| E5 | 敌人被池复用 | 标识与预警不附着到新躯体 | PlayMode 待建 | 未验证 | 对应 A23 |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M4 武器系统 | 全部武器命中与伤害 | `EmberWeaponContext` 由本模块赋值 | 待人工验收 | 改 context 必打一局 |
| M5 敌人系统 | 行为、暗弹、掉落 | 宿主在本模块 | 待人工验收 | — |
| M6 界面 | HUD 数值、结算屏、暂停 | 只读本模块状态 | 待人工验收 | — |
| M7 表现 | 粒子、池姿态还原 | 服务由本模块持有 | 待人工验收 | 对应 A23 |
| M8 音频 | 开火/命中/受击触发 | 触发点在武器与 `HurtPlayer` | 待人工验收 | — |

## 交付结论

- **已验证**：无。流程与顺序已**逐项对照代码**确认，但尚无任何可执行检查通过。
- **不适用**：无（本模块几乎全部行为都需运行）。
- **待手动验收**：H1–H9（主闭环、精英波、边界可读性、暗弹拦截、暂停、失败结算、选卡切换、跨局清理、真机）。
- **未验证**：A1–A25 全部（阻塞原因：项目无 asmdef 与测试程序集；PlayMode 测试还需可脚本化装配场景）。
- **未通过**：无。

## 历史记录（已归档内容摘要）

本文件由已归档的 `../archive/`（原 `DemoExpansion.md`）合并整理而来，其中下列内容**已过期**，仅作历史留存：

- 「长夜守卫：第 480 秒出现」「Boss 自然出场仍为 `Elapsed >= 480`」——**Boss 已改为波次驱动**，见 `../requirements.md` §3.2。
- 「战斗顶部保留经验条」——**XP / 经验条已删除**，见 `../requirements.md` §6。
- 「词条颜色对应升级后等级：1 青铜、2 白银、3/4 黄金、5 钻石」——**该口径已废弃**，现行按幸运权重 roll 品质。
- 文中引用的 `EmberExpansionChecks` 与「试玩 Boss（运行时）」菜单**均已删除**。
