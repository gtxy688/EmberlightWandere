# M3 战斗与波次

> 相关需求：`../requirements.md` §2（主循环）、§3.1–3.2、§3.5、§5
> 验收文档：`03-combat-waves-test.md`
> 代码：`Assets/Scripts/Combat/EmberCombat.cs`、`EmberBoss.cs`、`EmberWorld.cs`、`EmberPool.cs`、
> `Assets/Scripts/Core/EmberGame.cs`（状态机与伤害入口）

## 职责边界

- **本模块负责**：波次推进与清波判定、刷怪与遭遇事件、敌人实例的血量与死亡处理（分裂 / 重生 / 引爆）、伤害结算与命中判定、Boss 召唤与招式状态机、暗弹弹幕的宿主、对象池、竞技场边界、玩家受伤与结算入口、全局 11 态状态机。
- **本模块不负责**：
  - 波次数值与倍率 → M1（本模块只查询）
  - 选卡内容与发牌规则 → M2（本模块只触发时机）
  - 敌人行为策略与表现 → M5
  - 武器行为 → M4（本模块提供 `EmberWeaponContext` 的全部委托）

**关键定位**：`EmberCombat` 是战斗仿真的单点入口，**拥有全部 7 个武器实例**并在每个 `Tick` 中统一驱动。`EmberGame` 是全局状态机与玩家属性（生命、无敌帧、Score 入口）的持有者。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M1 | `LevelConfig.QuotaForWave / SpawnInterval / RollKind / TrashHp / AffixChance / EnemySpeed / IsBossWave / IsEliteWave / BossHpForWave / BossTier / WaveName / PicksAfterWave` | 全部波次与数值决策 | `Begin` 有 `LevelConfig` 重载；无配置时用 `Default` |
| M2 | `EmberGame.Progress`（`RunProgress`） | 注入武器 context 与结算伤害加成 | `Progress` 为 null 时须容忍（回营地后） |
| M4 | `EmberWeapon.Tick(dt, ctx)`、`Reset()` | 每帧驱动全部武器 | 武器 `OwnsWeapon` 自我门控，未拥有则不动作 |
| M5 | `IEnemyBehavior.Tick`、`EmberEnemyProjectiles`、`EmberEnemyVisual` | 敌人行为与表现 | 行为为空则退化为直线追击 |
| M7 | `EmberEffects`（粒子）、`EmberPool`（池） | 打击反馈与对象复用 | 池空时丢弃，不报错 |
| M8 | `EmberAudio` 各 `Play*` | 音效触发 | clip 缺失静默跳过 |

**对外**：M3 通过 `EmberGame.State`、`OnWaveCleared`、`HurtPlayer`、`EnemyCount` 等被 M6 与 M7 读取；武器只经 `EmberWeaponContext` 与本模块交互，**不直接持有 `EmberGame`**。

## 设计与数据流

```text
EmberGame.Update()
   └─ combat.Tick(dt)
        ├─ 刷怪：waveSpawned < QuotaForWave(wave) && enemies.Count < EnemyLimit
        │     └─ RollKind(wave, rng) → TrashHp(wave, kind) → AffixChance(wave)
        ├─ 遭遇事件：waveSpawned 达本波 50% 配额时触发一次
        ├─ 每个武器：weapon.Tick(dt, ctx)   ← ctx 的委托全部指向本模块
        │     └─ ctx.Damage / DamageFrom / Intercept → 扣血、死亡、分裂、重生
        ├─ Boss 波：BossAlive ? boss.Tick(dt) : 生成 Boss
        └─ 清波：waveSpawned >= quota && enemies.Count == 0
              └─ game.OnWaveCleared(wave)
                    └─ PicksAfterWave == 0 ? 直接推进 : 发牌（M2 + M6）
                          └─ game.SelectUpgrade → combat.AdvanceAfterUpgrade()
```

### 状态机（`EmberGame.Mode`，11 态）

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
- 清波后若 `PicksAfterWave == 0`（最终波），直接结算。

### Boss（`EmberBoss`）

显式 6 态 `AttackState`，仅在 `Playing` 推进（升级与暂停冻结预警）：

```text
追击 → 冲锋预警 → 冲锋 → 爆发预警 → 齐射预警 → 硬直 →（循环）
```

- **暗弹齐射**：第 20 波起加入，1 秒预警；随 `BossTier` 增加弹数与暗焰落点。
- **半血狂怒**：低于半血后加快节奏并增加爆发区域。
- 暗弹**可被玩家火球 / 穿透箭拦截击毁**（`ctx.Intercept`）。
- 击败最终 Boss = 通关。

### 伤害与受伤

| 路径 | 处理 |
|------|------|
| 武器 → 敌人 | 经 `ctx.Damage(id, amount)`；先判 `Enemy.Targetable` |
| 玩家受伤 | `EmberGame.HurtPlayer(damage)`：**先乘难度 `DamageMultiplier`，再过 `Progress.AbsorbDamage` 吃护盾，溢出扣生命** |
| 无敌帧 | `HurtReady` 门（0.65 秒），近战接触前必查 |
| 可见性 | 自动选敌只针对摄像机内目标，并排除顶部状态栏区域；屏外不结算伤害 |

### 死亡处理顺序

**先结算全部武器伤害，再统一处理死亡与分裂，最后检查清波**。这保证了：
- 母体只在**最终**死亡时分裂（重生读条中不提前分裂）；
- 重生 / 引线未结束时**不允许**提前进入选卡；
- 分裂子体补入 HUD 总量但**不消耗**常规刷怪配额。

### 对象池（`EmberPool`）

按字符串 key 复用：`fire`（玩家弹体）/ `burn`（燃地）/ `warn`（预警）/ `shade` / `boss` / `dark-bolt`（暗弹）/ `healing-drop`。归还时必须还原姿态与角色标识，避免附着到复用的躯体上。

## 对外契约

| 名称 | 类型 | 输入 | 输出或事件 | 错误/生命周期保证 |
|------|------|------|------------|-------------------|
| `EmberGame.Mode` | 枚举（11 态） | — | — | 全项目以 `EmberGame.Mode.X` 引用 |
| `EmberGame.State` | 属性 | — | 当前状态 | **唯一状态真相源**，被 M3/M5/M7 读取 |
| `EmberGame.Progress` | 属性 | — | `RunProgress` 或 **null** | 回营地后为 null，消费方必须容忍 |
| `EmberGame.Elapsed` / `Kills` / `Health` | 属性 | — | `float` / `int` / `float` | 供 HUD 与动画读取 |
| `EmberGame.HurtReady` | 属性 | — | `bool` | 无敌帧门（0.65s） |
| `EmberGame.HurtPlayer(float)` | 方法 | 原始伤害 | — | 内部应用难度倍率与护盾；致死则 `EndRun(false)` |
| `EmberGame.RegisterKill()` | 方法 | — | — | 击杀计数 |
| `EmberGame.OnWaveCleared(int)` | 方法 | 刚清完的波次 | — | **战斗 → 流程的唯一回调** |
| `EmberGame.SelectUpgrade(EmberOffer)` | 方法 | 候选牌 | — | **防伪校验**：比对 `Id` + `Rarity` + `WeaponId` 三者才接受 |
| `EmberGame.TryRefreshOffer()` | 方法 | — | `bool` | 非 `Upgrade` 态返回 false |
| `EmberGame.HealPlayer` / `PickHealingDrop` | 方法 | 回复量 | `bool` | 满血时 `HealPlayer` 返回 false |
| `EmberGame.Pause()` / `ShowMainMenu()` / `BeginRun()` / `EndRun(bool)` | 方法 | — | — | 流程入口 |
| `EmberCombat.Tick(float)` | 方法 | dt | — | 战斗唯一推进点 |
| `EmberCombat.Begin(..., LevelConfig)` / `TearDown()` | 方法 | 场景引用、配置 | — | 成对调用；`TearDown` 清理池与弹幕 |
| `EmberCombat.AdvanceAfterUpgrade()` | 方法 | — | — | 由 `EmberGame` 在选卡完成后调用 |
| `EmberCombat.Wave` / `WaveQuota` / `WaveSpawned` / `WaveKilled` / `WaveRemaining` / `EnemyCount` / `Boss` / `BossAlive` | 属性 | — | `int` / `Enemy` / `bool` | HUD 只读数据源；Boss 波 `WaveRemaining` 返回 0 |
| `EmberCombat.Enemy`（嵌套类） | 类 | — | 敌人实例 | `Targetable` 是所有武器命中判定的统一门 |
| `EmberWeaponContext` | 类 | — | 委托包 | 见下表；由本模块在 `Begin` 时**全部赋值** |

### `EmberWeaponContext` 契约（M4 与 M3 的边界）

| 成员 | 类型 | 语义 |
|------|------|------|
| `Player` / `World` / `Cam` | `Transform` / `Camera` | 场景引用 |
| `Effects` / `Pool` | M7 服务 | 粒子与池 |
| `Progress` | `RunProgress` | 只读构筑状态（M2） |
| `Elapsed` / `Random` / `PlayerSpeed` | `float` / `Random` / `float` | 局内时间、随机源、玩家速度 |
| `EnemyCount` / `GetEnemy(int)` | `Func` | 敌人枚举访问 |
| `Damage(int, float)` / `DamageFrom(int, float, Vector2)` | `Action` | 普通伤害 / 带来源位置的伤害（用于护盾朝向判定） |
| `ContactDamageFrom(int, float, Vector2)` | `Action` | 接触型持续伤害 |
| `Intercept(Vector2, Vector2, float)` | `Func` | 弹体拦截（暗弹可被击毁） |
| `StillPlaying` | `Func<bool>` | 是否仍在战斗态 |
| `SpawnBurnPatch(Vector2, float)` | `Action` | 请求生成燃地（由 M4 的燃地武器提供实现） |
| `DealDamage` / `DealContactDamage` | 方法 | 包装器；后者**返回是否真的扣了血**，用于音效去重 |
| `PlayerPos` / `PlayerStanding` / `Visible(Vector2)` | 属性/方法 | 位置、是否静止（阈值 0.15）、可见性 |
| `FindNearestVisibleEnemy()` / `FindRandomVisibleEnemy()` | 方法 | 自动选敌；复用内部 scratch list 避免每帧分配 |

> ⚠️ `PlayingGate` 字段存在但**从未赋值**，是预留 no-op，勿使用。

**具体数值不在此复制**，一律见 `../requirements.md` §3。

## Unity 装配

- 主场景 `Assets/Scenes/Emberlight.unity`；`EmberCombat` 与 `EmberBoss` 是**普通 C# 类**，由 `EmberGame` 构造并持有，不是场景组件。
- `EmberWorld` 是**静态类**，持有可变静态 `Limit`（由 `LevelConfig.ApplyWorld()` 写入）。跨场景不重置——同一进程内重开一局沿用同一边界值。
- `EmberPool` 是普通类，按 key 缓存 GameObject；`TearDown` 时清理。
- 敌人 GameObject 由池借出，必须经 `EmberEnemySilhouette` 恢复基准外观后再应用类型装饰（见 M5）。
- 创建/销毁所有权：`EmberGame` 拥有 `EmberCombat` 生命周期；`EmberCombat` 拥有武器实例与敌人池。

## 影响与回归范围

- **直接影响模块**：M4（经 context）、M5（行为与表现）、M6（HUD/流程）、M7（粒子与池）、M8（音效触发点）。
- **必须复验的契约**：`OnWaveCleared` 的发牌时机、`SelectUpgrade` 防伪、`HurtPlayer` 的倍率与护盾顺序、死亡→分裂→清波的处理顺序、暗弹拦截。
- **对应验收项**：`03-combat-waves-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| `EmberGame` 是否拆分为状态机 + 玩家 + 流程三个类 | 拆分 / 维持上帝类 | 当前单文件承担 M2/M3/M6 三种职责，改动易互波及 | xiaoCoder + jack |
| 表现层反向依赖（`FindObjectOfType<EmberGame>`）是否改为注入 | 构造注入 / 事件契约 / 维持 | 影响可测性与生命周期正确性 | xiaoCoder |
| `LevelConfig.ApplyWorld()` 重复调用是否收敛为一次 | 仅 `BeginRun` 调用 / 维持 | 幂等，暂无实际危害 | xiaoCoder |
| `PlayingGate` 未赋值字段是否删除 | 删除 / 保留 | 死字段增加误读 | xiaoCoder |
