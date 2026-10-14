# M4 武器系统 验收文档

> 对应架构：`04-weapon-system.md`
> 对应需求：`../requirements.md` §4.2、§4.4、§4.6
> 层级依据：武器行为的本质是"每帧被 M3 驱动一次 `Tick`"，依赖帧推进、对象池与场景装配 → **PlayMode**；数值乘区与门控公式可用 EditMode 覆盖，但当前用例尚未实现。

## 自动化测试计划与证据

| 编号 | 层级 | 拟测试路径与名称 | 行为 | 红灯原因或 N/A 理由 | 最近结果 |
|------|------|------------------|------|---------------------|----------|
| W1 | PlayMode（待建） | `WeaponTests::EveryWeaponDealsDamageInOneRun` | 每把武器单独一局，`Tick` 能产出伤害且不抛异常 | 武器未接线时静默无输出 | 未验证（用例未实现） |
| W2 | PlayMode（待建） | `WeaponTests::UnownedWeaponStaysSilent` | 未拥有武器不产生任何弹体与伤害 | `OwnsWeapon` 门控失效会让未选武器也开火 | 未验证（用例未实现） |
| W3 | PlayMode（待建） | `WeaponTests::PerWeaponAttackPowerOnlyAffectsThatWeapon` | `火球·攻击力` 只抬火球，其他武器伤害不变 | 退回全局作用域会早期伤害膨胀（硬约束 2） | 未验证（用例未实现） |
| W4 | PlayMode（待建） | `WeaponTests::PierceFiresOnlyWhileStanding` | 穿透火矢仅静止时开火，移动中不产生新箭；已飞出的箭继续结算 | 静止门控失效会让狙击手感消失 | 未验证（用例未实现） |
| W5 | PlayMode（待建） | `WeaponTests::BoomerangHitsOnBothLegs` | 烬蝶去程/回程各结算一次 | 只结算一段会让伤害腰斩 | 未验证（用例未实现） |
| W6 | PlayMode（待建） | `WeaponTests::MeteorDamagesAfterWarning` | 火雨射程内预警结束后才结算伤害 | 预警与结算顺序错会变成"无提示瞬伤" | 未验证（用例未实现） |
| W7 | PlayMode（待建） | `WeaponTests::OrbitOnlyNearestOrbDealsFullDamage` | 环火多球命中同一敌人时只有最近球满伤 | 多球叠伤曾是超模主因 | 未验证（用例未实现） |
| W8 | EditMode（待建） | `WeaponTuningTests::DamageFormulaUsesPerWeaponMagnitudeAndAmp` | `基伤 × (1 + WeaponMagnitude) × DamageMul` 三段乘区 | 把攻击力改成全局或漏掉增幅即红灯 | 未验证（用例未实现） |
| W9 | EditMode（待建） | `WeaponFactoryTests::CreateAllRegistersEveryRosterWeapon` | 工厂产出的实例集合与 `EmberWeaponCatalog` 名册一一对应 | 名册与注册漏同步会让某把武器永远不生效 | 未验证（用例未实现） |
| W10 | N/A | 六条线的成型强度是否超模、狙击手感、质变视觉差异 | — | **无法合理自动化**：属手感与平衡判断 | N/A（见手动验收 H1–H3） |

> **不是缺基础设施**：项目已有 `Emberlight.EditMode.Tests` / `Emberlight.PlayMode.Tests` 两个测试程序集（当前 EditMode 150 个、PlayMode 4 个图形冒烟），只是都还不覆盖武器行为。上表的 W1–W9 可直接照着写。

## 自动化运行记录

| 日期 | Unity 版本与环境 | 命令或 Test Runner 过滤器 | 结果文件 | 结论 |
|------|------------------|---------------------------|----------|------|
| — | 2022.3.62f3 | 本模块用例尚未实现；全仓套件结果见 `../实现说明.md` | — | **未验证** |

## 手动验收前置条件

- 场景、Prefab 与配置：`Assets/Scenes/Emberlight.unity`；`EmberWeaponTuning` 为基础数值来源
- 依赖模块状态：M2 可发卡（否则拿不到专属/质变）、M3 可刷怪（否则没有目标）
- 目标设备与画质档位：Editor + Android 真机

## 手动验收

| 编号 | 操作步骤 | 可观察预期结果 | 环境与构建 | 执行者/日期 | 状态 | 证据或备注 |
|------|----------|----------------|------------|-------------|------|------------|
| H1 | N=1，六条线各打一局 | 每条线都能成型、清得动怪，且没有一条明显碾压其他线 | Editor | — | 待人工验收 | 平衡判断，必须由 jack 做 |
| H2 | 只用穿透火矢，做"站定—开火—走位"循环 | "站住才开火"的狙击手感成立，移动中不出箭符合直觉而非像失灵 | Editor | — | 待人工验收 | 对应 W4 |
| H3 | 依次取得六把武器的专属与质变 | 形态变化在画面上一眼可辨（体积、数量、拖尾、落点） | Editor | — | 待人工验收 | 对应需求 §4.6 |
| H4 | 长时间战斗，反复切波与选卡 | 弹体不残留、不重复计数、不出现"上一局的箭" | Editor | — | 待人工验收 | 对应 `Reset()` 路径 |
| H5 | Android 真机打一局大波次 | 六武器同屏时帧率可接受 | Android APK | — | 待人工验收 | 依赖 M9 |

## 失败路径与边界

| 编号 | 前置状态与操作 | 预期保护或失败行为 | 自动/手动 | 状态 | 证据或备注 |
|------|----------------|--------------------|-----------|------|------------|
| E1 | `Progress` 为 null（回营地后仍有 `Tick`） | 不崩溃、不产弹 | EditMode 待建 | 未验证 | 契约要求消费方容忍 null |
| E2 | 池已耗尽时请求弹体 | 丢弃并不报错 | PlayMode 待建 | 未验证 | 见 `03-combat-waves.md` 池契约 |
| E3 | 无可见敌人时自动选敌 | 不开火、不空转建对象 | PlayMode 待建 | 未验证 | `FindNearestVisibleEnemy` 返回 null |
| E4 | 同一帧内多次请求燃地 | 受同屏燃地上限约束，不无限增长 | PlayMode 待建 | 未验证 | 上限归 M9/`EmberBurnGround` |
| E5 | 未持有燃地武器，但火球（燎原）命中生成燃地 | 共享燃地仍在原时隙推进并结算 | EditMode 待建 | 未验证 | `EmberBurnGroundTests` 已覆盖部分规则 |

## 回归范围

| 受影响模块或契约 | 复验项 | 原因 | 状态 | 证据或备注 |
|------------------|--------|------|------|------------|
| M3 战斗与波次 | 伤害投递、`Intercept` 拦截暗弹、`SpawnBurnPatch` | 武器经 `EmberWeaponContext` 与 M3 交互 | 部分覆盖 | 图形冒烟覆盖火球攻击路径 |
| M2 单局进度 | `WeaponMagnitude` / `WeaponAttackSpeed` 的单武器作用域 | 武器数值全部来自构筑 | 已验证（EditMode） | `RunProgressTests` |
| M7 美术与表现 | 弹体与粒子外观、池复用姿态还原 | 武器经 `ctx.Effects` / `ctx.Pool` 出表现 | 待人工验收 | 池归还必须还原姿态 |
| M8 音频 | 六条武器音效与分裂箭静音 | 触发点在武器内 | 待人工验收 | 见 `08-audio.md` |

## 交付结论

- **已验证**：无本模块专属的自动化证据。间接相关的是 M2 的 EditMode 用例（单武器加成作用域）与图形 PlayMode 冒烟（火球攻击路径）。
- **不适用**：W10 平衡与手感判断——不做自动化。
- **待手动验收**：H1–H5（六线成型强度、狙击手感、质变可辨识度、长时稳定性、真机帧率）。
- **未验证**：W1–W9 全部——**不是缺基础设施，而是本模块用例尚未实现**；E1–E5 的边界路径同样未构造用例。
- **未通过**：无。
