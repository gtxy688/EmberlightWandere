# M1 配置与调参

> 相关需求：`../requirements.md` §3（局内规则）、§7（技术约束）
> 代码：`Assets/Scripts/Core/LevelConfig.cs`、`Assets/Scripts/Core/GameSpeed.cs`

## 职责边界

- **本模块负责**：关卡与敌人参数的集中定义与查询——难度倍率、波次配额与刷怪间隔、Boss 血量与波次判定、精英波判定、选卡次数、敌人基础血量与速度、词缀概率、遭遇事件参数、地图边界、游戏倍速。
- **本模块不负责**：
  - 战斗推进与刷怪执行 → M3
  - 选卡内容与卡池 → M2
  - 任何 UI 呈现与文案（`DifficultyName` / `WaveName` 只提供字符串，不含排版）→ M6

**存在意义**：把原先散落在 `EmberCombat` / `EmberWorld` 的魔法数收敛到一处，使关卡/难度调参不需要改战斗逻辑。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M3 `EmberWorld` | 可变静态字段 `Limit` | `ApplyWorld` 将配置的地图边界写入世界服务 | 调用时依赖 M3；不是独立叶节点 |

`LevelConfig` 的计算使用 `UnityEngine.Mathf`，`ApplyWorld` 另依赖 M3 的 `EmberWorld`；`GameSpeed` 使用 `UnityEngine.Time` 与 `PlayerPrefs`。查询计算与场景副作用分开，调用 `ApplyWorld` 时须考虑可变静态边界。

`LevelConfig` 的对外副作用是 `ApplyWorld()` 写入 `EmberWorld.Limit`（M3 的静态字段），由组合根在建边框前调用一次；`GameSpeed` 另写入时间缩放与偏好设置。

## 设计与数据流

```text
EmberGame.StartRun(weapons, slots)
   └─ LevelConfig.Create(difficulty, waves)   ← 唯一工厂
         ├─ BossWave        ← 征程长度归一化
         ├─ HealthMultiplier / DamageMultiplier / CountMultiplier
         │  IntervalMultiplier / AffixMultiplier   ← 按难度分支
         └─ BossHp          ← 对应征程基础血量 × HealthMultiplier
   └─ config.ApplyWorld()  → EmberWorld.Limit = MapLimit

EmberCombat 每帧 / 每波查询同一 config 实例：
   QuotaForWave(wave) → SpawnInterval(wave) → RollKind(wave, rng)
   → TrashHp(wave, kind) → AffixChance(wave) → EnemySpeed(kind)
   IsBossWave / IsEliteWave / BossHpForWave / BossTier / WaveName
   BossSupportTargetForWave / RollBossSupportKind → M3 执行 Boss 伴随兵补位
   PicksAfterWave(wave) → 交给 M2 决定发牌
```

**单一实例、只读语义**：`Create` 之后倍率属性为 `get; private set;`，不允许运行中改写。波次相关的公开数组字段（`WaveQuotas` / `SpawnIntervals`）虽为 `public`，但约定只做配置读取。

`EmberWorld.Limit` 是**可变静态**（由 `const` 改来），使地图边界能由配置注入而不再编译期写死。

## 关键机制

### 难度构造

- 触发条件：`EmberGame.StartRun` 收到菜单流程提交的武器与槽位，并读取所选难度和征程长度。
- 处理顺序：先定 `BossWave`，再按难度写五个倍率，最后用血量倍率算 `BossHp`。
- 成功结果：返回一个倍率齐备的 `LevelConfig`。
- 失败与边界：`difficulty` 不在枚举内时回落 `Standard`；`waves` 非 50 一律按 25 处理。倍率值见需求 §3.3，标准难度仅显式覆盖血量倍率，其余使用默认值。

### Boss 与精英波判定

- `IsBossWave(wave)` = `wave == BossWave || wave % 10 == 0`（且在 1..BossWave 内）。
- `IsEliteWave(wave)` = 每 5 波、**且不是 Boss 波**、且不是最终波。
- `BossTier(wave)` = `clamp(wave / 10 + (wave == BossWave ? 1 : 0), 1, 5)`，用于 Boss 招式强度分段。

> 边界：25 波局中第 20 波同时满足 `% 10 == 0`，判为 Boss 波而非精英波；第 25 波既是 `%5` 又是最终波，优先判为最终 Boss。

### 选卡次数

`PicksAfterWave(wave)` = 基础次数 + Boss 额外次数：

- `wave < 1 || wave >= BossWave` → 0（最终波直接结算）
- 否则 `(wave <= 10 || wave % 2 == 0 ? 1 : 0) + (IsBossWave ? 1 : 0)`

### Boss 伴随兵

`BossSupportTargetForWave` 仅在 Boss 波返回目标数，区分阶段与最终配置，应用 `CountMultiplier` 并为 Boss 预留全局容量；非 Boss 波返回零。`RollBossSupportKind` 只抽基础追击兵种，独立于普通刷怪表。初次伴随兵和后续补位由 M3 执行，间隔与批量均查配置，数值见需求 §3.2。

### 刷怪权重表

三组互斥的权重表，列序固定为 `SpawnKinds = { 0普通, 1快速, 2高血, 4射手, 5母体, 7盾卫, 8引线虫 }`：

| 表 | 适用 | 说明 |
|----|------|------|
| `weights[5]` | 第 1–5 波 | 前 5 波固定教学顺序 |
| `lateWeights[4]` | 第 6 波起循环 | 每 4 波一个主题：烬虫潮涌 / 暗烛交火 / 铁灯阵线 / 引线围猎 |
| `eliteWeights` | 精英挑战波（>5） | 高血与盾卫为主 |

`RollKind` 以 100 为总权重掷点，**返回 kind 值本身而非列下标**——列顺序与 kind 编号不同（无 kind 3/6，因为 Boss 与分裂子体不参与随机）。

### 游戏倍速

`GameSpeed` 为静态类，通过 `Time.timeScale` 提供 1x–5x，并持久化到 `PlayerPrefs`。

> ⚠️ 因为走 `Time.timeScale`，所有要求"暂停/选卡时仍动"的动画必须使用 `unscaledDeltaTime`（`EmberCardMotion` 已如此）。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `LevelConfig.Create` | 静态工厂 | `EmberDifficulty`, `int waves` | `LevelConfig` | 非法输入回落标准/25 波；返回对象倍率齐备 |
| `LevelConfig.Default` | 静态属性 | — | `LevelConfig` | ⚠️ **每次访问新建对象**，非缓存 |
| `DifficultyName` | 静态方法 | `EmberDifficulty` | `"休闲"/"标准"/"困难"` | 非枚举值回落"标准" |
| `IsBossWave` / `IsEliteWave` | 实例方法 | `int wave` | `bool` | 越界波返回 false |
| `PicksAfterWave` | 实例方法 | `int wave` | `int` | 最终波及越界返回 0 |
| `QuotaForWave` / `SpawnInterval` | 实例方法 | `int wave` | `int` / `float` | Boss 波配额返回 0；间隔有下限钳制 |
| `TrashHp` | 实例方法 | `int wave, int kind` | `float` | 未知 kind 回落普通怪公式 |
| `RollKind` | 实例方法 | `int wave, Random` | `int kind` | 权重和不足时回落 kind 0 |
| `AffixChance` | 实例方法 | `int wave` | `float` | 第 3 波前为 0；增长段与最终结果分别钳制，公式见需求 §3.5 |
| `BossHpForWave` / `BossTier` | 实例方法 | `int wave` | `float` / `int` | 最终波用 `BossHp`；Tier 钳制 1–5 |
| `BossSupportTargetForWave` / `RollBossSupportKind` | 实例方法 | 波次 / 随机源 | 伴随兵目标 / 基础兵种 kind | 目标含难度数量倍率与全局容量钳制，不含 Boss；非 Boss 波为零 |
| `WaveName` | 实例方法 | `int wave` | `string` | 覆盖最终/Boss/精英/前 5 波/四种后期主题 |
| `ApplyWorld` | 实例方法 | — | — | 唯一副作用：写 `EmberWorld.Limit` |
| `GameSpeed` 倍速 API | 静态 | 档位 | 设置 `Time.timeScale` + 写 Prefs | 需在场景切换后保持 |

玩法参数见 `../requirements.md` §3，游戏速度范围见 §7；本模块只说明查询规则。

## Unity 装配

- 武器参数归 `Core/EmberWeaponTuning.cs`（M4），构筑应用和卡面预览共用；本模块只持有关卡与敌人参数。
- **无场景对象、无 Prefab、无 ScriptableObject**：配置由代码构造，不进资源管线。
- `LevelConfig` 是普通 C# 类（非 `MonoBehaviour`），生命周期由 `EmberGame` 持有的引用决定。
- `GameSpeed` 为静态类，状态存活于 `Time.timeScale` 与 `PlayerPrefs`；**不参与场景切换清理**，设计上跨局保持。

> 取舍说明：当前选择"代码内配置类"而非 ScriptableObject，好处是零资产依赖、可被纯逻辑测试直接构造；代价是无法在 Inspector 里调参、策划改数值需要改代码。

## 影响与回归范围

- `SpawnScreenPadding` / `SpawnBandWidth` / `SpawnMapInset` 控制普通与遭遇杂兵的屏外外沿带和地图内缩，完整权威值见 requirements §3.1；只影响落点，不改变配额、间隔、Boss 与分裂规则。回归覆盖地图边缘、不同视口比例、无合法区域和配额重试。
- **直接影响模块**：M3（消费全部数值）、M2（消费 `PicksAfterWave`）、M6（显示 `DifficultyName` / `WaveName`）、M5（消费敌人血量/速度/词缀概率）。
- **必须复验的契约**：难度倍率表、`QuotaForWave` 与 `SpawnInterval` 的钳制、`PicksAfterWave` 的三种分支、`IsBossWave` / `IsEliteWave` 的互斥性。
- Boss 伴随兵调参需复验难度缩放、为 Boss 预留容量、预警期不刷兵、补位不消耗普通配额与 Boss 死亡后停止补位，检查入口为 `Assets/Tests/EditMode/EmberBossSupportTests.cs`。
