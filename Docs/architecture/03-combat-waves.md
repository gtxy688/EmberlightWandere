# M3 战斗与波次

临时 Buff 通过宿主的 `Buffs` 与 `TryAddBuff` 接线：伤害 context 读取玩家倍率，掉落和迟缓弹请求施加。效果计时与属性合成归 M10；掉落模块为 `EmberDrops`，支持治疗和 Buff 两种入口。迟缓弹按命中前无敌帧判定附加许可，死亡后的请求被宿主拒绝，拦截与回池清理见 M5。

> 相关需求：`../requirements.md` §2（主循环）、§3.1–3.2、§3.5、§5
> 代码：`Assets/Scripts/Combat/` 下的 `EmberCombat.cs`（战斗根）、`EmberWaveDirector.cs`、
> `EmberEnemyRoster.cs`、`EmberCombatDamage.cs`、`EmberEnemy.cs`、`EmberBoss.cs`、
> `EmberWorld.cs`、`EmberPool.cs`、`IEmberCombatHost.cs`（本模块对宿主的契约）

## 职责边界

- **本模块负责**：波次推进与清波判定、刷怪与遭遇事件、敌人实例的血量与死亡处理（分裂 / 重生 / 引爆）、伤害结算与命中判定、Boss 召唤与招式状态机、暗弹弹幕的宿主、对象池、竞技场边界。
- **本模块不负责**：
  - 波次数值与倍率 → M1（本模块只查询）
  - 选卡内容与发牌规则 → M2（本模块只触发时机）
  - 敌人行为策略与表现 → M5
  - 武器行为 → M4（本模块提供 `EmberWeaponContext` 的全部委托）
  - **局长阶段与玩家生命结算 → M0**（本模块只经 `IEmberCombatHost` 请求）

**关键定位**：`EmberCombat` 是战斗仿真的单点入口，持有武器工厂返回的实例并在每个 `Tick` 中统一驱动。
它对宿主只依赖 `IEmberCombatHost`，不持有 `EmberGame`。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M1 | `LevelConfig.QuotaForWave / SpawnInterval / RollKind / TrashHp / AffixChance / EnemySpeed / IsBossWave / IsEliteWave / BossHpForWave / BossTier / BossSupportTargetForWave / RollBossSupportKind / WaveName / PicksAfterWave` | 全部波次与数值决策 | `Begin` 有 `LevelConfig` 重载；无配置时用 `Default` |
| M0 | `IEmberCombatHost`：`Phase` / `Elapsed` / `Progress` / `HurtReady` / `NeedsHealing` / `HurtPlayer` / `PickHealingDrop` / `PlayBossBurst` / `RegisterKill` / `OnWaveCleared` / `EndRun` | 阶段门控、玩家伤害、计分与收局 | 宿主由组合根提供；**不需要是 `EmberGame`** |
| M2 | `IEmberCombatHost.Progress`（`RunProgress`） | 注入武器 context 与结算伤害加成 | `Progress` 为 null 时须容忍（回营地后） |
| M4 | `EmberWeapon.Tick(dt, ctx)`、`Reset()` | 每帧驱动全部武器 | 武器 `OwnsWeapon` 自我门控，未拥有则不动作 |
| M5 | `IEnemyBehavior.Tick`、`EmberEnemyProjectiles`、`EmberEnemyVisual` | 敌人行为与表现 | 行为为空则退化为直线追击 |
| M7 | `EmberEffects` | 打击反馈 | 粒子容量满时跳过发射；对象池由 M3 自身管理 |
| M8 | `EmberAudio` 各 `Play*` | 音效触发 | clip 缺失静默跳过 |

**对外**：M3 经 `IEmberCombatHost` 反向请求阶段、伤害与收局；M6/M7 只读 `EmberCombat.Wave / WaveQuota / WaveRemaining / Boss / BossAlive / EncounterText` 与 `IEmberRunPhaseSource`。
武器只经 `EmberWeaponContext` 与本模块交互，**不直接持有 `EmberGame`**。

## 设计与数据流

```text
EmberGame.Update()                       ← 组合根只负责顺序（见 00-composition-root.md）
   └─ combat.Tick(dt)                    ← EmberCombat 只是战斗根：装配 + 每帧顺序
        ├─ waves.Tick(dt)                            EmberWaveDirector
        │     ├─ 刷怪：waveSpawned < QuotaForWave(wave) && EnemyCount < EnemyLimit
        │     │     └─ host.TrySpawnRolledTrash() → roster.Spawn(...)，成功才消耗配额
        │     │           └─ RollKind(wave, rng) → TrashHp(wave, kind) → AffixChance(wave)
        │     ├─ Boss 预警倒计时结束 → host.SpawnBoss() + TrySpawnBossSupport() 初始伴随兵
        │     │     └─ Boss 在场时按配置定时补缺，不消耗普通配额，受目标与全局容量双重限制
        │     └─ 遭遇事件：waveSpawned 达本波 50% 配额时触发一次 → host.TrySpawnTrashAt(...)
        ├─ healingDrops.Tick(dt, player)
        ├─ enemyProjectiles.Move(dt)
        ├─ TickEnemies(dt)：闪白 / 重生读条 / e.ai.Tick（Boss）/ behavior.Tick / 接触伤害 / 灼热检测
        │     └─ ⚠️ Boss 也在名册里，由这一段的 ai 分支推进，**不要另外再 Tick 一次**
        ├─ 灼热光环脉冲（多圈不叠加）
        ├─ TickWeapons(dt)：weapon.Tick(dt, ctx)   ← ctx 的委托指向 EmberCombatDamage
        │     └─ ctx.Damage / DamageFrom / ContactDamageFrom → EmberCombatDamage.Apply
        ├─ enemyProjectiles.HitPlayer(...)
        └─ ResolveDeaths()
              └─ EmberCombatDamage.ResolveDeaths(ctx) → Outcome
                    ├─ 重生接管 → 行为 OnDeath → 掉落 → roster.Release → 记分
                    ├─ outcome.Kills  → waves.NoteKill() × N
                    ├─ outcome.BossDefeated → 取消待分裂请求 / 回收余兵 → waves.NoteBossDefeated()
                    └─ 否则 waves.CheckWaveCleared()
                          └─ host.OnWaveCleared(wave)          ← IEmberCombatHost
                                └─ RunFlowController.OnWaveCleared   （M2，规则见 02-run-progression.md）
                                      └─ 本波不需选卡 ? 恢复下一波 : 发牌（M2 + M6）
                                            └─ SelectUpgrade → combat.AdvanceAfterUpgrade()
```

### 模块内的职责划分

战斗按装配、波次、敌人名册与伤害结算划分职责：

| 单元 | 文件 | 职责 |
|------|------|------|
| `EmberCombat` | `EmberCombat.cs` | **战斗根**：持有武器实例、装配单元、每帧顺序、接回调 |
| `EmberWaveDirector` | `EmberWaveDirector.cs` | 波次推进：配额、刷怪间隔、Boss 预警、遭遇事件、清波判定、HUD 只读量 |
| `EmberEnemyRoster` | `EmberEnemyRoster.cs` | 敌人实例化与回收：种类/落点/颜色/体型/血量/词缀/部件、对象池归还 |
| `EmberCombatDamage` | `EmberCombatDamage.cs` | 伤害应用与死亡结算：正面减伤、闪白击退、重生词缀、记分、掉落触发 |
| `EmberEnemy` | `EmberEnemy.cs` | 单个敌人的运行时数据 |

**两个易踩的坑**（都写进代码注释了）：

1. **Boss 在名册里，由 `TickEnemies` 的 `ai` 分支推进**。战斗根不要再单独 `bossEnemy.ai.Tick(...)`——
   重构过程中曾这样多推一次，效果是 Boss 招式速度翻倍。
2. **`bossSpawned` 的语义是"这一波是 Boss 波"**，不是"Boss 已经出来了"。
   若在预警期间不置位，空场会被判成本波已清完，波次会在 Boss 出场前前进并连锁跳过后面每一波（历史事故）。

### 状态机（`EmberRunPhase`，11 态）

阶段使用顶层枚举 `EmberRunPhase`；所有权与写入规则见 `00-composition-root.md` 的「阶段所有权」。

```text
Loading → Menu → SelectLength → SelectDifficulty → SelectSlots → SelectRoster
                                                                     ↓
   Playing ⇄ Upgrade ──────────────────────────────────────────────┘
      ↓  ↑
   Paused ──┘
      ↓
   Lost / Won → Menu
```

- `Upgrade` 与 `Paused` 期间**战斗不推进**；`Time.timeScale` 由 M1 的 `GameSpeed` 控制，因此要求"选卡时仍动"的动画必须用 `unscaledDeltaTime`。
- 普通波清空后由 `RunFlowController` 查询奖励次数；零次时通过收尾回调继续下一波，并不代表胜利。最终 Boss 死亡由 `EmberWaveDirector.NoteBossDefeated` 走 `OnFinalBossDefeated` → `EndRun(true)`，不进入普通选卡分支。

### Boss（`EmberBoss`）

显式 6 态 `AttackState`，仅在 `Playing` 推进（升级与暂停冻结预警）：

```text
追击 → 冲锋预警 → 冲锋 → 爆发预警 → 齐射预警 → 硬直 →（循环）
```

- **暗弹齐射**：开启波次和预警参数见需求 §3.2；随 `BossTier` 增加弹数与暗焰落点。
- **半血狂怒**：低于半血后加快节奏并增加爆发区域。
- **迟缓命中**：所有档位的冲锋与暗焰爆发经同一方法结算，先记录命中前 `HurtReady`，再调用宿主伤害；仍在 Playing 且原本可受伤时请求共享 Slow。预警、躲避、无敌帧和致死后均不施加，护盾吸收不豁免。普通接触与齐射沿用原伤害路径。
- 暗弹**可被玩家火球 / 穿透箭拦截击毁**（`ctx.Intercept`）。
- 击败最终 Boss = 通关。
- Boss 出场时生成伴随兵，之后按 `BossSupportInterval` / `BossSupportBatch` 分批补足 `BossSupportTargetForWave`，构成来自 `RollBossSupportKind`，数值见需求 §3.2。`Host.TrySpawnBossSupport` 经名册走普通屏外落点，不使用屏内生成特例；落点失败留待下一批，重生中的敌人仍占名额。
- Boss 阵亡后先取消 `pendingBroods` 并回收余兵，再通知波次结束；退场不走死亡奖励入口，不额外加击杀、分数或掉落。阶段选卡与最终胜利沿用原流程，不等待余兵清空。
- 同一死亡分支从玩家 BuffManager 移除共享 Slow，保留正面 Buff；计时与叠加规则继续由 M10 负责，Boss 不直接修改玩家速度。

### 伤害与受伤

| 路径 | 处理 |
|------|------|
| 武器 → 敌人 | 经 `ctx.Damage(id, amount)`；先判 `Enemy.Targetable` |
| 玩家受伤 | `EmberGame.HurtPlayer(damage)`：**先乘难度 `DamageMultiplier`，再过 `Progress.AbsorbDamage` 吃护盾，溢出扣生命** |
| 无敌帧 | `HurtReady` 门，近战接触前必查；时长见需求 §3.7 |
| 可见性 | 自动选敌只针对摄像机内目标，并排除顶部状态栏区域；屏外不结算伤害 |

### 死亡处理顺序

**先结算全部武器伤害，再统一处理死亡与分裂，最后检查清波**。最终死亡的 `Outcome.Kills` 由战斗根同时回报 `waves.NoteKill` 与宿主 `RegisterKill`，同步波次和整局击杀；重生接管不算最终死亡。这保证了：
- 母体只在**最终**死亡时分裂（重生读条中不提前分裂）；
- 重生 / 引线未结束时**不允许**提前进入选卡；
- 分裂子体补入 HUD 总量但**不消耗**常规刷怪配额。

Boss 波例外：Boss 死亡优先结束本波，未展开的分裂请求作废；普通波仍按上述顺序先补子体再判清波。

### 对象池（`EmberPool`）

按字符串 key 复用：`fire`（玩家弹体）/ `burn`（燃地）/ `warn`（预警）/ `shade` / `boss` / `dark-bolt`（暗弹）/ `healing-drop`。归还时必须还原姿态与角色标识，避免附着到复用的躯体上。

敌人模板包含 Boss 皮肤时，`boss` 池也从同一 Prefab 建立视图，池键仍独立；没有 Boss 皮肤则沿用程序化 Boss。整体皮肤的 Boss 不再叠加旧冠饰与提灯，但保留原招式状态机。

## 对外契约

| 名称 | 类型 | 输入 | 输出或事件 | 错误/生命周期保证 |
|------|------|------|------------|-------------------|
| `EmberRunPhase` | 枚举（11 态） | — | — | 顶层类型，全项目以 `EmberRunPhase.Playing` 等枚举值 引用；由 M0 持有并写入 |
| `IEmberCombatHost.Phase` | 属性 | — | 当前阶段 | **M3 的唯一阶段入口**；不依赖 `EmberGame` |
| `IEmberCombatHost.Progress` | 属性 | — | `RunProgress` 或 **null** | 回营地后为 null，消费方必须容忍 |
| `IEmberCombatHost.Elapsed` | 属性 | — | `float` | 敌人动画与武器节奏用 |
| `IEmberCombatHost.HurtReady` | 属性 | — | `bool` | 无敌帧门；时长见需求 §3.7 |
| `IEmberCombatHost.HurtPlayer(float)` | 方法 | 原始伤害 | — | 难度倍率只乘一次、护盾先吸收；致死则收局 |
| `IEmberCombatHost.RegisterKill()` | 方法 | — | — | 击杀计数 |
| `IEmberCombatHost.OnWaveCleared(int)` | 方法 | 刚清完的波次 | — | **战斗 → 流程的唯一回调** |
| `RunFlowController.Choose(EmberOffer)` | 方法 | 候选牌 | `bool` | 校验 Id、Rarity、WeaponId 和当前选卡阶段后提交 |
| `RunFlowController.TryRefresh()` | 方法 | — | `bool` | 非 Upgrade 阶段或刷新次数不足时返回 false |
| `EmberKeeperVitals.Heal` / `PickHealingDrop` | 方法 | 回复量 | `EmberVitalOutcome` | 满血 `Heal` 返回 `Rejected`；满血拾取返回 `NoChange` 但仍算拾取 |
| `IEmberCombatHost.EndRun(bool)` / `EmberMenuFlow.ShowMainMenu()` / `.BeginRun()` | 方法 | — | — | 流程入口 |
| `EmberCombat.Tick(float)` | 方法 | dt | — | 战斗唯一推进点 |
| `EmberCombat.Begin(..., LevelConfig, GameObject shadePrefab = null)` / `TearDown()` | 方法 | 已准备的世界/玩家/相机、配置、可选敌人模板 | — | `Begin` 先清理旧状态再装配，并把模板传入对象池；主场景必传美术集中的模板，独立调用未传时保留原有程序化外观。`TearDown` 清理池、弹幕、武器与共享燃地，逻辑地图边界由调用方预先配置 |
| `EmberCombat.AdvanceAfterUpgrade()` | 方法 | — | — | 由组合根在选卡完成后调用（`ReturnToPlay`） |
| `EmberCombat.Wave` / `WaveQuota` / `WaveSpawned` / `WaveKilled` / `WaveRemaining` / `WaveKillsRemaining` / `EnemyCount` / `Boss` / `BossAlive` | 属性 | — | `int` / `EmberEnemy` / `bool` | HUD 只读数据源；Boss 波 `WaveRemaining` 返回 0。波次量转发自 `EmberWaveDirector`；`EnemyCount` 读名册，`Boss` / `BossAlive` 读战斗根持有的 Boss 引用 |
| `EmberEnemy` | 类 | — | 敌人实例 | 顶层类型；`Targetable` 是所有武器命中判定的统一门 |
| `EmberWaveDirector` | 类 | `Host` 契约 | 波次只读量 | 不造敌人：需要刷怪时回调 `Host`。**不含伤害逻辑** |
| `EmberWaveDirector.Host.TrySpawnBossSupport` | 方法 | — | 是否成功 | 经 M1 抽伴随兵种类、M3 名册执行屏外生成；失败不占目标名额、不消耗普通配额 |
| `EmberEnemyRoster` | 类 | 池、配置、世界、玩家、随机源、显式相机 | `EmberEnemy` 或 null | 杂兵保证屏外且在地图内；无法落点时返回 null，只有成功才借出节点 |
| `EmberSpawnPlacement.TryFind` | 静态方法 | 相机视口/地图区域、随机源、请求落点 | 成功标记与位置 | 从视口外沿带和地图交集取点，无合法区不回落到屏内；数值归 M1 |
| `EmberCombatDamage.Apply` / `.ResolveDeaths` | 静态方法 | `Context` + 目标下标 | `Outcome` | **不改波次、不造新敌人**；击杀数与 Boss 阵亡经 `Outcome` 回报 |
| `EmberWeaponContext` | 类 | — | 委托包 | 见下表；由本模块在 `Begin` 时**全部赋值** |

### `EmberWeaponContext` 契约（M4 与 M3 的边界）

| 成员 | 类型 | 语义 |
|------|------|------|
| `Player` / `World` / `Cam` | `Transform` / `Camera` | 场景引用 |
| `Effects` / `Pool` | M7 `EmberEffects` / M3 `EmberPool` | 粒子与池 |
| `Progress` | `RunProgress` | 只读构筑状态（M2） |
| `Elapsed` / `Random` / `PlayerSpeed` | `float` / `Random` / `float` | 局内时间、随机源、玩家速度 |
| `EnemyCount` / `GetEnemy(int)` | `Func` | 敌人枚举访问 |
| `Damage(int, float)` / `DamageFrom(int, float, Vector2)` | `Action` | 普通伤害 / 带来源位置的伤害（用于护盾朝向判定） |
| `ContactDamageFrom(int, float, Vector2)` | `Action` | 接触型持续伤害 |
| `Intercept(Vector2, Vector2, float)` | `Func` | 弹体拦截（暗弹可被击毁） |
| `StillPlaying` | `Func<bool>` | 是否仍在战斗态 |
| `PlaySound` | `Action<EmberWeaponSound>` | 战斗根将武器反馈适配为 M8 调用；回调为空时跳过音效 |
| `SpawnBurnPatch(Vector2, float)` | `Action` | 请求生成燃地，由战斗根绑定到共享 `EmberBurnGround.SpawnPatch` |
| `DealDamage` / `DealContactDamage` | 方法 | 包装器；后者**返回是否真的扣了血**，用于音效去重 |
| `PlayerPos` / `PlayerStanding` / `Visible(Vector2)` | 属性/方法 | 位置、是否静止（判定参数见 [需求 §4.8](../requirements.md#48-静止判定)）、可见性 |
| `FindNearestVisibleEnemy()` / `FindRandomVisibleEnemy()` | 方法 | 自动选敌；复用内部 scratch list 避免每帧分配 |


**具体数值不在此复制**，一律见 `../requirements.md` §3。

## Unity 装配

- 主场景 `Assets/Scenes/Emberlight.unity`；`EmberCombat` 与 `EmberBoss` 是**普通 C# 类**，由 `EmberGame` 构造并持有，不是场景组件。
- `EmberWorld` 是**静态类**，持有可变静态 `Limit`（由组合根调用 `LevelConfig.ApplyWorld()` 写入）。跨场景不重置——同一进程内重开一局沿用同一边界值。
- `EmberPool` 是普通类，按 key 缓存 GameObject；`TearDown` 时清理。
- 敌人 GameObject 由池借出，必须经 `EmberEnemySilhouette` 恢复基准外观后再应用类型装饰（见 M5）。
- 创建/销毁所有权：`EmberGame` 拥有 `EmberCombat` 生命周期；`EmberCombat` 拥有武器实例与敌人池。

## 影响与回归范围

- **直接影响模块**：M4（经 context）、M5（行为与表现）、M6（HUD/流程）、M7（粒子与池）、M8（音效触发点）。
- **必须复验的契约**：`OnWaveCleared` 的发牌时机、`SelectUpgrade` 防伪、`HurtPlayer` 的倍率与护盾顺序、死亡→分裂→清波的处理顺序、暗弹拦截。
- 屏外落点改动需复验完整视口/地图交集、遭遇请求点迁移和生成失败后的配额重试；Boss 与分裂特殊落点不纳入普通杂兵落点规则。
- Boss 群战需复验首次伴随兵、定时补缺、容量限制、退场无额外奖励、取消分裂和阶段/最终结算，覆盖入口为 `Assets/Tests/EditMode/EmberBossSupportTests.cs` 与 `Assets/Tests/PlayMode/EmberBossSwarmSmokeTests.cs`。
- Boss 负面 Buff 需复验各档位命中、预警期/躲避/无敌帧/致死不施加、刷新不叠层、护盾命中与 Boss 死亡清理，入口为 `Assets/Tests/PlayMode/EmberBossDebuffTests.cs` 与 `Assets/Tests/PlayMode/EmberBossBuffAndCardSmokeTests.cs`。

## 性能定位标记（2026-09-16）

使用静态 ProfilerMarker 和 Auto 作用域，正常返回及异常路径均自动结束采样。仅增加观测，不调整刷怪、对象池和选卡规则。

| 标记 | 范围 |
|---|---|
| Ember.Combat.SpawnEnemy | 单次刷怪全流程 |
| Ember.Pool.RentEnemy | 敌人取池与状态恢复 |
| Ember.Pool.Create | 池空时执行对象创建工厂（从父节点区分敌人或特效） |
| Ember.Pool.Reuse | 命中缓存后的重挂父节点与激活 |
| Ember.Enemy.Silhouette.Initialize | 首次剪影初始化 |
| Ember.Enemy.Silhouette.BuildRoles | 多种角色装饰构建 |
| Ember.Enemy.Visual.Initialize | 战斗提示与外观辅助对象构建 |
| Ember.UI.Upgrade.Show | 显示/刷新选卡，包括初始化阶段的预建 |

在 Development Build 中采集并在 CPU Hierarchy 搜索 Ember.；父子时间包含关系不应重复相加。对照 Calls、Time ms、GC Alloc 判断批量调用与单次开销。
