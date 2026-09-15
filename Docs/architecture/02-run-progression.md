# M2 单局进度与构筑

> 相关需求：`../requirements.md` §4（构筑与卡池）、§5（结算与计分）
> 验收文档：`02-run-progression-test.md`
> 代码：`Assets/Scripts/Core/RunProgress.cs`、`Assets/Scripts/Core/EmberRarity.cs`

## 职责边界

- **本模块负责**：单局内的一切构筑状态与规则——槽位与已拥有武器、五个通用属性、单武器攻击力/攻速、护盾、幸运、刷新次数；选卡池的生成（`Choices`）与结算（`Choose`）；品质 roll；专属/质变的效果落位；Score 累计与最终分。
- **本模块不负责**：
  - 武器实际行为与数值 → M4（本模块只提供查询接口）
  - 发牌 UI、卡面排版、刷新按钮 → M6
  - 波次推进与发牌**时机** → M3 / `EmberGame`（本模块不知道波次节奏，只接收 `waveCleared`）

**关键定位**：`RunProgress` 是**纯 C# 类**，`using` 只有 `System` 与 `System.Collections.Generic`，**零 `UnityEngine` 引用**（数值钳制用 `System.Math`）。这是全项目唯一可以脱离 Unity 直接单元测试的对象，也是 M4 与 M6 的共同只读数据源。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| 无 | — | — | — |

`RunProgress` 不依赖任何其他模块；`EmberRarity` 是纯查表 + 枚举。被依赖方向全部向内。

## 设计与数据流

```text
EmberGame: 选槽位 N + 单选 1 把起始武器
   └─ RunProgress.ConfigureRun(int[] slotWeapons, int slots)
         ├─ slots 钳制到 [1,5]
         ├─ 去重授予名册武器，超出 slots 的截断
         ├─ 首个被授予者 → CoreWeaponId
         └─ 空集兜底 WeaponBasic(13)

清波 → EmberGame.OnWaveCleared(wave)
   └─ PicksAfterWave(wave) 次：
         Progress.Choices(rng, waveCleared) → EmberOffer[3 + EmptySlots]
         → EmberGame.OfferUpgrade → M6 面板显示
         → 玩家点选 → EmberGame.SelectUpgrade(offer)
               ├─ 防伪校验：比对 Id + Rarity + WeaponId 三者
               └─ Progress.Choose(offer, out grantedNewWeapon)
                     ├─ 生效加成（ApplyExclusive / ApplyMetamorph / 通用累加）
                     ├─ Pending--
                     └─ grantedNewWeapon == true → 立刻再 OfferChoices 一次（补偿，不推进波次）
```

**所有写入仅由 `EmberGame` 发起**。武器与 UI 只调用只读查询，这使构筑状态的变化点收敛在一处。

## 关键机制

### 开局授予（`ConfigureRun`）

- 触发条件：玩家在开局四步流程中确认了槽位与起始武器。
- 处理顺序：钳制 `slots` → 遍历传入武器 → 去重 → 超过槽位则截断 → 记录 `CoreWeaponId`。
- 成功结果：`OwnedWeaponCount == 1`、`EmptySlots == slots - 1`，其余属性归零。
- 失败与边界：传入非名册 id 时回落 `WeaponBasic`；空集同样兜底。**开局武器数恒为 1，禁止一次填满 N 槽**。

### 选卡池生成（`Choices`）

候选总数 = `3 + EmptySlots`：

1. **基础 3 张**：在「通用 / 专属 / 质变」中按唯一键 `(Id, WeaponId)` 去重抽取。
   - 通用：幸运(2) / 全体增幅(3) / 护盾(4) 各至多 1 张（`WeaponId = -1`）；攻击力(0)与攻速(1) **对每把已有武器各可出一张**（`WeaponId = 该武器 id`）。
   - 专属（30–35）与质变（20–25）：**仅当已拥有对应武器**时进池。质变额外有约 0.5% 的每次发牌门控（防连钻）；**无专属前置条件**。
   - 幸运越高，专属/质变进池权重越高（`specialW = 1 + Luck/40`，上限 5；质变权重 `max(1, specialW - 1)`）。
2. **额外张**：槽未满时追加**未拥有武器**，数量 = `EmptySlots`。
3. **兜底**：基础张数不足 3 时用尚未出现的通用补齐，**保证总能选**。

- 通用词条的品质由 `RollByLuck(rng, Luck)` 决定，权重表见 `../requirements.md` §4.5。
- 专属固定黄金、质变固定钻石，**不参与 roll**。

### 同屏唯一

唯一键是 `(词条Id, WeaponId)` 而非笼统"种类"：

- ✅ `火球·攻击力` + `环火·攻击力` + `全体伤害增幅` 可同屏
- ❌ 两张 `火球·攻击力`；三张 `全体伤害增幅`

### 伤害乘区

```text
武器最终伤害 = 武器基伤 × (1 + 该武器的 WeaponMagnitude) × (1 + DamageAmp)
```

- 攻击力(0) / 攻速(1) 写入 **per-weapon** 字典，只影响对应武器。
- 全体伤害增幅(3) 是唯一的全局乘区（`DamageMul = 1 + DamageAmp`）。
- 全局 `DamageBonus` 已停用（保留字段但不再作为通用出卡）。
- 攻速档位比攻击力高：黄金 +30% / 钻石 +50%。

### 补偿选卡

选中**新武器**时 `Choose(..., out grantedNewWeapon)` 返回 true，由 `EmberGame` 立刻再发一次牌且**不推进波次**。补选若再得新武器**不连锁**。

### 刷新

整局 `FreeRefreshes = 2`。`TryRefresh` 在次数耗尽或 `Pending <= 0` 时返回 false。**禁止**出现「+刷新次数」类词条。

### 幸运

上限 `MaxLuck = 120`，只影响两件事：通用词条品质权重、专属/质变进池概率。**不影响伤害**。

### Score

`AddScore(int)` 拒绝负数并抛 `ArgumentOutOfRangeException`。`FinalScore()` = 原始 Score，`ScoreMult` 硬编码 1.0。

## 对外契约

| 名称 | 类型 | 输入 | 输出或事件 | 错误/生命周期保证 |
|------|------|------|------------|-------------------|
| `ConfigureRun(int[], int)` | 方法 | 起始武器数组、槽位上限 | 重置全部状态 | 钳制槽位到 1–5；空集兜底火球 |
| `ConfigureRun(int, int)` | 方法 | 核心武器 id、槽位 | 同上 | 非名册 id 降级为火球（兼容重载） |
| `OwnsWeapon(int)` | 查询 | 武器 id | `bool` | 武器 Tick 的第一道门 |
| `HasMetamorph(int)` | 查询 | 武器 id | `bool` | 武器形态切换门 |
| `CanMetamorph(int)` | 查询 | 武器 id | `bool` | 只要求拥有该武器 |
| `CanExclusive(int)` | 查询 | 武器 id | `bool` | — |
| `WeaponMagnitude` / `WeaponAttackSpeed` | 查询 | 武器 id | `float` | 该武器的攻击力/攻速加成 |
| `WeaponCount(int)` | 查询 | 武器 id | `int` | 当前仅穿透火矢使用（扇形箭数） |
| `DamageMul` | 属性 | — | `float` | `1 + DamageAmp`，6 把武器全部相乘 |
| `Luck` / `Shield` / `ShieldCapacity` / `Orbits` / `TrailPower` / `BonusMaxHealth` / `WavePower` | 属性 | — | 各类型 | 全局属性；`WavePower` 恒 0 |
| `WeaponSlots` / `CoreWeaponId` / `OwnedWeaponCount` / `EmptySlots` / `RefreshesRemaining` | 属性 | — | `int` | HUD 与选卡 UI 的数据源 |
| `Choices(Random, int)` | 方法 | 随机源、刚清完的波次 | `EmberOffer[]` | 长度 `3 + EmptySlots`；保证非空 |
| `Choose(EmberOffer, out bool)` | 方法 | 候选牌 | `bool` + 是否授予新武器 | 成功才 `Pending--`；失败不改状态 |
| `TryRefresh(Random, int, out EmberOffer[])` | 方法 | 随机源、波次 | `bool` + 新候选 | 次数耗尽或非选卡态返回 false |
| `OfferChoices()` / `AddScore(int)` / `FinalScore()` | 方法 | — | — | `AddScore` 负数抛异常 |
| `AbsorbDamage(float)` | 方法 | 已乘难度倍率的伤害 | 穿透到生命的伤害 | 先扣护盾，溢出扣血 |
| `EmberOffer` | struct | `id, rarity, weaponId = -1` | — | `WeaponId`：-1 = 全局；0/1 时为目标武器 id |
| `EmberRarityUtil.RollByLuck` / `WeightsForLuck` | 静态 | `Random`, `float luck` | 稀有度 / 权重数组 | 幸运钳制到 0–120 |
| `EmberRarityUtil.GenericAtkAs` / `GenericAttackSpeed` / `GenericLuck` / `GenericAmp` / `ShieldAmount` | 静态 | `EmberRarity` | 数值 | 见 `../requirements.md` §4.4 |
| `EmberRarityUtil.AllNames` | 静态字段 | — | 四个稀有度中文名 | **唯一文案源**，被 `EmberFonts` 预热 glyph |
| `EmberRarityUtil.KindOf(int)` | 静态 | 词条 id | `EmberOfferKind` | 0–9 通用 / 10–19 武器 / 20–29 质变 / 30–39 专属 |
| `EmberRarityUtil.IsGenericId(int)` | 静态 | id | `bool` | 当前为 0–4 |

**具体数值不在此复制**，一律见 `../requirements.md` §4。

### 零调用点成员（死代码，勿当作契约）

`FullRoster()`、`HasEvolved()`（`HasMetamorph` 别名）、`HasExclusive()`、`CanEvolve()`、`OwnedWeaponsView()`、`AddShield()`、`OfferCountExpected()`、`ScoreMult`、`HealAmount()`。评估清理前先确认无外部引用。

## Unity 装配

- **无场景对象、无 Prefab、无 ScriptableObject**。`RunProgress` 由 `EmberGame` 在 `BeginRun` 时 `new` 出来并持有。
- 回营地时 `Progress` 被置为 `null`，因此**所有消费方必须容忍 null**（`EmberCombat.Tick` 注入前先判）。
- `EmberRarity` / `EmberRarityUtil` 为纯静态查表，无状态、无生命周期。
- 词条名称与说明文案集中在 `EmberGame.UpgradeNames` / `UpgradeDetails`（36 项，下标 == 词条 id），并被 `EmberFonts` 用于字形预热——**改文案必须同时确认字体预热覆盖**。

## 影响与回归范围

- **直接影响模块**：M4（读全部属性与查询）、M6（读 `Luck` / `CoreWeaponId` / `RefreshesRemaining` / `EmptySlots` 并渲染候选）、M3（经 `EmberGame` 触发发牌）、M7（`EmberCardIcons` 只读 id 常量）。
- **必须复验的契约**：`Choices` 的长度与唯一性、`Choose` 的防伪与补偿选卡、品质权重表、伤害乘区、护盾吸收顺序。
- **对应验收项**：`02-run-progression-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| 是否清理零调用点成员 | 删除 / 保留作向后兼容 | 死代码增加误读成本，但也可能被外部工具引用 | xiaoCoder |
| 全局 `DamageBonus` 字段是否删除 | 删除 / 保留停用 | 与"不得再做全局攻击力"的口径一致性 | jack |
| 质变 0.5% 门控是否保留 | 保留 / 改为纯权重竞争 | 影响钻石卡出现频率与手感 | jack + xiaoPlanner |
| 冲击波（`WavePower`）去留 | 恢复为不占槽词条 / 删除 | 当前为死代码 | jack + xiaoPlanner |
