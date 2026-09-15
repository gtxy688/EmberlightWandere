# M5 敌人系统 验收文档

> 对应架构：`05-enemy-system.md`
> 对应需求：`../requirements.md` §3.4、§3.5、§4.4
> ⚠️ 项目当前**无测试基础设施**，见 `00-overview.md` §测试现状。

## 自动化测试计划与证据

分层依据：行为策略依赖帧推进与敌人池复用 → **PlayMode**；表现与动画只依赖 `Transform` 层级，部分可 PlayMode 断言，**观感本身不可自动化**。

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| A1 | PlayMode（待建） | `EnemyShooterTests::KeepsDistanceBandAndOnlyFiresAfterCharge` | 射手维持 5.5–7 距离带；仅蓄力 0.4s 后才出弹 | 直接出弹会让玩家无反应窗口 | 未执行（无测试程序集） |
| A2 | PlayMode（待建） | `EnemyShooterTests::ChargeStartsAndCompletesOnlyOnScreen` | 屏外不开始/不完成蓄力 | — | 未执行（无测试程序集） |
| A3 | PlayMode（待建） | `EnemySplitTests::BroodSpawnsExactlyThreeChildren` | 烬胎最终死亡分裂恰好 3 只 kind 6 | — | 未执行（无测试程序集） |
| A4 | PlayMode（待建） | `EnemySplitTests::ChildrenDoNotDropOrConsumeQuota` | 子体不产生掉落、不消耗刷怪配额、Reward 为 0 | 子体计入配额会导致清波永远差几只 | 未执行（无测试程序集） |
| A5 | PlayMode（待建） | `EnemyShieldTests::FrontFacingReducesDamageBySeventyPercent` | 正面 ±60° 伤害约为背面的 0.3 倍 | 若用敌人朝向而非攻击来源判定，绕后无效 | 未执行（无测试程序集） |
| A6 | PlayMode（待建） | `EnemyShieldTests::TurnSpeedIsLimited` | 转身不超过 65°/秒 | 无限转身速度会让绕后战术失效 | 未执行（无测试程序集） |
| A7 | PlayMode（待建） | `EnemyBomberTests::ExplodesOnlyAfterFuseEnds` | 0.8s 引线结束后才结算爆炸 | — | 未执行（无测试程序集） |
| A8 | PlayMode（待建） | `EnemyBomberTests::UntargetableAndNonContactDuringFuse` | 引线期间不被自动锁定、不造成接触伤害 | — | 未执行（无测试程序集） |
| A9 | PlayMode（待建） | `EnemyAffixTests::NoAffixBeforeWaveThree` | 第 1–2 波不产出任何词缀 | — | 未执行（无测试程序集） |
| A10 | PlayMode（待建） | `EnemyAffixTests::BurningDoesNotStackAcrossOverlaps` | 多圈灼热重叠仍为每秒 2 点 | 叠加会让多词缀怪瞬间秒杀 | 未执行（无测试程序集） |
| A11 | PlayMode（待建） | `EnemyAffixTests::RebirthRevivesExactlyOnce` | 重生只生效一次（40% 生命），二次死亡不再复活 | 无限重生导致波次无法清空 | 未执行（无测试程序集） |
| A12 | PlayMode（待建） | `EnemyAffixTests::WindBurstMultipliesSpeedByOnePointEight` | 疾风每 3s 内速度 ×1.8，持续 1s | — | 未执行（无测试程序集） |
| A13 | PlayMode（待建） | `EnemyAffixTests::AffixedEnemiesScoreTriple` | 词缀怪记 3 分，普通 1 分，子体 0 分 | — | 未执行（无测试程序集） |
| A14 | PlayMode（待建） | `EnemyAffixTests::EliteChallengeSquadSkipsAffixRoll` | 精英挑战强制小队按配额刷出，不受 `AffixMultiplier` 影响 | — | 未执行（无测试程序集） |
| A15 | PlayMode（待建） | `DarkBoltTests::CapIsFortyEight` | 暗弹同屏不超过 48 | — | 未执行（无测试程序集） |
| A16 | PlayMode（待建） | `DarkBoltTests::SweptCollisionCatchesFastCrossings` | 高速交叉不穿透漏判 | 用离散点检测会漏判 | 未执行（无测试程序集） |
| A17 | PlayMode（待建） | `DarkBoltTests::InterceptDestroysBoltWithoutPierceCost` | 穿透箭击毁暗弹且不消耗穿透次数；火球与暗弹互相消失 | — | 未执行（无测试程序集） |
| A18 | PlayMode（待建） | `DarkBoltTests::ClearedOnWaveEndAndNewRun` | 清波 / 新局 / 退出时暗弹全部清理 | 残留暗弹跨局伤人 | 未执行（无测试程序集） |
| A19 | PlayMode（待建） | `HealingDropTests::DropChanceMatchesTable` | 普通最终死亡 8% / 词缀 20% / Boss 100%；子体 0% | — | 未执行（无测试程序集） |
| A20 | PlayMode（待建） | `HealingDropTests::RebirthPendingDoesNotCountAsFinalDeath` | 重生读条前不判定为最终死亡，不重复掉落 | — | 未执行（无测试程序集） |
| A21 | PlayMode（待建） | `HealingDropTests::RestoresTwentyAndNeverExceedsMax` | 每个回复 20 HP 且不超上限；满血不消耗 | — | 未执行（无测试程序集） |
| A22 | PlayMode（待建） | `HealingDropTests::MagnetRadiusDependsOnNeedsHealing` | `NeedsHealing` 时磁吸半径 2.2，否则 1.5 | — | 未执行（无测试程序集） |
| A23 | PlayMode（待建） | `HealingDropTests::LifetimeFreezesDuringPauseAndPick` | 60 秒存在时长在暂停与选卡时冻结，普通换波保留 | — | 未执行（无测试程序集） |
| A24 | PlayMode（待建） | `HealingDropTests::CapIsTwentyFour` | 同屏治疗掉落不超过 24 | — | 未执行（无测试程序集） |
| A25 | PlayMode（待建） | `EnemyVisualTests::ReturnToPoolRestoresBaselineShape` | 池归还后再借出恢复基准剪影、颜色与姿态 | 复用躯体会把上一次装饰带到新怪 | 未执行（无测试程序集） |
| A26 | PlayMode（待建） | `EnemyVisualTests::MarkersAndWarningsDoNotStickToRecycledBodies` | 角色标识与预警随敌人销毁，不残留 | 历史缺陷回归点 | 未执行（无测试程序集） |
| A27 | PlayMode（待建） | `EnemyVisualTests::AnimationMovesOnlyVisualChildren` | 动画只改外观子节点，根节点、血条、碰撞与预警范围不变 | 动到根节点会破坏碰撞与瞄准 | 未执行（无测试程序集） |
| A28 | PlayMode（待建） | `EnemyVisualTests::PhasesDifferBetweenEnemies` | 同屏敌人动画相位不同 | — | 未执行（无测试程序集） |
| A29 | PlayMode（待建） | `EnemyVisualTests::SpecialEnemiesDisableBaselineSilhouetteDecor` | 特殊敌人不叠加基础轮廓装饰 | — | 未执行（无测试程序集） |

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | — | 尚无可执行的测试程序集 | — | **未验证** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`；25 波 + 标准难度
- 依赖模块状态：M3 战斗可运行、M7 图集已导入
- 目标设备与画质档位：Editor 优先；真机项另列

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | 打到第 3 波起，观察暗烛射手 | 有清晰蓄力闪光与瞄准线；横移可以躲开；距离带随竖屏宽度收缩 | Editor | — | 待人工验收 | 可读性项 |
| H2 | 让烬胎死在怪群中 | 分裂 3 只小虫，视觉上能区分母体与子体 | Editor | — | 待人工验收 | — |
| H3 | 正面与背后分别打铁灯卫 | 正面明显"打不动"，绕后伤害全额 | Editor | — | 待人工验收 | 对应 A5 |
| H4 | 贴近引线虫直至引爆 | 红圈预警清晰，爆炸范围与伤害符合直觉 | Editor | — | 待人工验收 | — |
| H5 | 遇到带词缀精英 | 灼热/疾风/重生三种在视觉上可区分；重生读条清晰 | Editor | — | 待人工验收 | — |
| H6 | 观察敌人动画 | 各类型姿态有辨识度（浮动/急促/起伏/挤压/呼吸/倾斜/压缩回弹/鼓动/收缩），相位不整齐划一 | Editor | — | 待人工验收 | 对应 A28 |
| H7 | 拾取回血包 | 掉落与磁吸手感自然；满血时不被消耗 | Editor | — | 待人工验收 | — |
| H8 | 长时间战斗观察 | 不出现"躯干带着上一次标识"或动画错乱 | Editor | — | 待人工验收 | 对应 A26 |
| H9 | Android 真机大波次 | 同屏 150 敌人 + 暗弹 + 掉落时帧率可接受 | Android APK | — | 待人工验收 | 依赖 M9 |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | 敌人池复用 | 基准形态与颜色先还原再应用类型装饰 | PlayMode 待建 | 未验证 | 对应 A25 |
| E2 | 暗弹达上限 48 | 停止生成，不无限增长 | PlayMode 待建 | 未验证 | 对应 A15 |
| E3 | 治疗掉落达上限 24 | 停止生成，不无限增长 | PlayMode 待建 | 未验证 | 对应 A24 |
| E4 | 子体补入后本波被清空 | 子体不消耗配额，清波判定不受影响 | PlayMode 待建 | 未验证 | 对应 A4 |
| E5 | 引线期间玩家离开爆炸范围 | 引线继续，爆炸只结算一次 | PlayMode 待建 | 未验证 | 对应 A7 |
| E6 | 重生读条中本波其它敌人已清空 | 不提前进入选卡 | PlayMode 待建 | 未验证 | 见 M3 E1 |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M3 战斗与波次 | 清波判定、死亡顺序、配额 | 行为由本模块产生，结算在 M3 | 待人工验收 | — |
| M1 配置 | 敌人血量、速度、词缀概率 | 全部读 M1 | N/A | 仅数值耦合，改 M1 时复验 |
| M4 武器系统 | 对全部敌人种类的伤害与拦截 | 新敌人会暴露武器判定缺陷 | 待人工验收 | 对应 A17 |
| M7 表现 | 共享精灵、粒子、剪影服务 | 本模块消费 | 待人工验收 | — |
| M2 进度 | 掉落计入 Score、回血不改护盾 | — | 待人工验收 | — |

## 交付结论

- **已验证**：无。行为与数值已**逐项对照代码**确认，但尚无任何可执行检查通过。
- **不适用**：动画"好看与否"的审美判断——属于手动验收 H6，不做自动化。
- **待手动验收**：H1–H9（射手可读性、分裂、盾卫绕后、引线预警、词缀辨识、动画辨识度、回血手感、长时稳定性、真机大波次）。
- **未验证**：A1–A29 全部（阻塞原因：项目无 asmdef 与测试程序集）。
- **未通过**：无。
