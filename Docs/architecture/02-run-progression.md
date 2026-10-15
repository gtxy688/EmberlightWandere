# M2 单局进度与构筑

> 相关需求：`../requirements.md` §4（构筑与卡池）、§5（结算与计分）
> 代码：`Assets/Scripts/Core/` 下的 `RunProgress`、`RunFlowController`、`EmberChoicePool`、`EmberWeaponCatalog`、`EmberRarity`

## 职责边界

- **本模块负责**：单局内的一切构筑状态与规则——槽位与已拥有武器、五个通用属性、单武器攻击力/攻速、护盾、幸运、刷新次数；选卡池的生成（`Choices`）与结算（`Choose`）；品质 roll；专属/质变的效果落位；Score 累计与最终分；清波后的选卡序列、补偿和刷新推进。
- **本模块不负责**：
  - 武器实际行为与数值 → M4（本模块只提供查询接口）
  - 发牌 UI、卡面排版、刷新按钮 → M6
  - 清波判定与下一波推进 → M3；组合根只转交清波事件与展示回调，本模块通过宿主查询选卡次数。

**关键定位**：`RunProgress` 是**纯 C# 类**，`using` 只有 `System` 与 `System.Collections.Generic`，**零 `UnityEngine` 引用**（数值钳制用 `System.Math`）。它与 Catalog、ChoicePool、品质和流程等纯逻辑单元一起支持独立测试，也是 M4 与 M6 的共同只读数据源。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M4 `EmberWeaponTuning` | 专属升级增量 | `RunProgress` 应用升级时读共享参数 | 常量随工程编译 |
| 宿主契约 `RunFlowController.Host` | 构筑、波次奖励、阶段、候选和呈现回调 | 选卡序列与场景解耦 | 宿主保证有效构筑与候选；无局内进度时清波入口返回 |

本模块各单元不引用 UnityEngine；这表示不依赖引擎类型，并不表示没有项目内依赖。`RunProgress` 使用本模块 Catalog/ChoicePool/品质表及 M4 的共享参数；流程控制器只通过 Host 访问宿主。

## 设计与数据流

```text
EmberGame: 选槽位 N + 单选 1 把起始武器
   └─ new RunProgress(int[] slotWeapons, int slots)
         └─ ConfigureRun：统一初始化入口
         ├─ slots 钳制到 [1,5]
         ├─ 去重授予名册武器，超出 slots 的截断
         ├─ 首个被授予者 → CoreWeaponId
         └─ 空集兜底 WeaponBasic(13)

清波 → RunFlowController.OnWaveCleared(wave)
   └─ 记录本波选卡次数 → OfferUpgrade()
         ├─ Host.RequestChoices → RunProgress.Choices
         └─ Host.OnUpgradeOffered → 面板展示候选
               └─ 玩家点选 → RunFlowController.Choose(offer)
                     ├─ 校验 Id + Rarity + WeaponId
                     ├─ RunProgress.Choose → 应用加成并消费 Pending
                     ├─ 新武器 → 补偿一次，不连锁、不推进波次
                     └─ 选卡结束 → Host.OnUpgradeSequenceFinished
```

选卡状态由 `RunFlowController` 推进，构筑变更由 `RunProgress` 应用；武器和 UI 读取构筑数据，界面通过回调提交选择意图。

## 关键机制

### 开局授予（`ConfigureRun`）

- 触发条件：玩家在开局四步流程中确认了槽位与起始武器。
- 处理顺序：钳制 `slots` → 遍历传入武器 → 去重 → 超过槽位则截断 → 记录 `CoreWeaponId`。
- 成功结果：数组重载可授予多个互不重复的有效武器，数量不超过槽位；正式开局 UI 只提交一把，因此产品流程中 `OwnedWeaponCount == 1`。护盾、幸运等本局累计值清空，武器基线与免费刷新次数按配置初始化。
- 失败与边界：数组中的非法 id 与重复项被跳过；没有任何有效武器时兜底火球。单武器重载将非法 id 直接归一化为火球。产品的单选开局约束由菜单流程保证，不是数组 API 的容量保证。

### 选卡池生成（`Choices`）

候选总数 = `3 + EmptySlots`：

1. **基础 3 张**：在「通用 / 专属 / 质变」中按唯一键 `(Id, WeaponId)` 去重抽取。
   - 通用：幸运(2) / 全体增幅(3) / 护盾(4) 各至多 1 张（`WeaponId = -1`）；攻击力(0)与攻速(1) **对每把已有武器各可出一张**（`WeaponId = 该武器 id`）。
   - 专属（30–35）与质变（20–25）：**仅当已拥有对应武器**时进池。质变另有整次发牌门控；已取得的专属/质变不再进池，**无专属前置条件**。
   - 幸运提升专属候选权重，通用和质变权重及质变门控参数见需求 §4.6；通过门控不保证抽出质变卡。
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
- 攻速与攻击力的品质档位分别查需求 §4.4，不共享一套增量表。

### 补偿选卡

选中**新武器**时 `Choose(..., out grantedNewWeapon)` 返回 true，由 `RunFlowController.Choose` 请求宿主再次发牌且**不推进波次**。补选若再得新武器**不连锁**。

### 刷新

整局免费次数归 `FreeRefreshes`，当前值见需求 §4.3。`TryRefresh` 在次数耗尽或 `Pending <= 0` 时返回 false，不出现增加刷新次数的词条。

### 幸运

幸运上限见需求 §4.4；影响通用品质与专属权重，不改变质变门控概率，也不直接影响伤害。

### Score

`AddScore(int)` 拒绝负数并抛 `ArgumentOutOfRangeException`。`FinalScore()` = 原始 Score，`ScoreMult` 硬编码 1.0。

## 对外契约

`EmberWeaponCatalog.Find(id)` 返回不可变武器定义；名册、专属与质变归属读同一张表。`Roster` / `RosterCopy` 返回数组副本，外部修改不影响内部数据。`EmberWeaponTuning` 的升级增量同时供构筑应用与卡牌预览读取。修改共享参数时需同时核对构筑落地和卡面预览。


| 名称 | 类型 | 输入 | 输出或事件 | 错误/生命周期保证 |
|------|------|------|------------|-------------------|
| `RunProgress(int[], int)` | 构造函数 | 起始武器、槽位上限 | 已初始化的构筑 | 构造时只初始化一次；无参构造保留默认开局 |
| `ConfigureRun(int[], int)` | 方法 | 起始武器数组、槽位上限 | 重置全部状态 | 钳制槽位到 1–5；空集兜底火球 |
| `ConfigureRun(int, int)` | 方法 | 核心武器 id、槽位 | 同上 | 非名册 id 降级为火球（兼容重载） |
| `OwnsWeapon(int)` | 查询 | 武器 id | `bool` | 武器 Tick 的第一道门 |
| `HasMetamorph(int)` | 查询 | 武器 id | `bool` | 武器形态切换门 |
| `CanMetamorph(int)` | 查询 | 质变 id | `bool` | 拥有对应武器且本局尚未取得该质变；无专属前置 |
| `CanExclusive(int)` | 查询 | 专属 id | `bool` | 拥有对应武器且本局尚未取得该专属 |
| `WeaponMagnitude` / `WeaponAttackSpeed` | 查询 | 武器 id | `float` | 该武器的攻击力/攻速加成 |
| `WeaponCount(int)` | 查询 | 武器 id | `int` | 当前仅穿透火矢使用（扇形箭数） |
| `DamageMul` | 属性 | — | `float` | `1 + DamageAmp`，6 把武器全部相乘 |
| `Luck` / `Shield` / `ShieldCapacity` / `Orbits` / `TrailPower` | 属性 | — | 各类型 | 当前生效的构筑属性 |
| `WeaponSlots` / `CoreWeaponId` / `OwnedWeaponCount` / `EmptySlots` / `RefreshesRemaining` | 属性 | — | `int` | HUD 与选卡 UI 的数据源 |
| `Choices(Random)` | 方法 | 随机源 | `EmberOffer[]` | 生成后调用 `OfferChoices`，将 Pending 设为一次待选；候选长度见需求 §4.3 |
| `Choose(EmberOffer, out bool)` | 方法 | 候选牌 | `bool` + 是否授予新武器 | 成功才 `Pending--`；失败不改状态 |
| `TryRefresh(Random, out EmberOffer[])` | 方法 | 随机源 | `bool` + 新候选 | 次数耗尽或非选卡态返回 false |
| `OfferChoices()` / `AddScore(int)` / `FinalScore()` | 方法 | — | — | `AddScore` 负数抛异常 |
| `AbsorbDamage(float)` | 方法 | 已乘难度倍率的伤害 | 穿透到生命的伤害 | 先扣护盾，溢出扣血 |
| `EmberOffer` | struct | `id, rarity, weaponId = -1` | — | `WeaponId`：Id 为攻击力/攻速词条时指定已有武器，其余默认 -1 |
| `EmberRarityUtil.RollByLuck` / `WeightsForLuck` | 静态 | `Random`, `float luck` | 稀有度 / 权重数组 | 幸运钳制到 0–120 |
| `EmberRarityUtil.GenericAtkAs` / `GenericAttackSpeed` / `GenericLuck` / `GenericAmp` / `ShieldAmount` | 静态 | `EmberRarity` | 数值 | 见 `../requirements.md` §4.4 |
| `RunFlowController.OnWaveCleared` / `Choose` / `TryRefresh` | 方法 | 清波或用户选择 | 展示 / 收尾回调 | 阶段与候选校验、补偿不连锁；刷新替换候选且不累加 Pending |
| `EmberRarityUtil.AllNames` | 静态字段 | — | 四个稀有度中文名 | **唯一文案源**，被 `EmberFonts` 预热 glyph |
| `EmberRarityUtil.KindOf(int)` | 静态 | 词条 id | `EmberOfferKind` | 0–9 通用 / 10–19 武器 / 20–29 质变 / 30–39 专属 |
| `EmberRarityUtil.IsGenericId(int)` | 静态 | id | `bool` | 当前为 0–4 |

**具体数值不在此复制**，一律见 `../requirements.md` §4。

### 查询入口

`FullRoster()` 返回名册副本，`OfferCountExpected()` 转发卡池候选数计算；武器归属和形态分别查询 `OwnsWeapon`、`HasMetamorph`、`HasExclusive`。

## 模块内的职责划分

候选生成、构筑应用与发牌序列各自独立：

| 单元 | 文件 | 职责 | 零 `UnityEngine` |
|------|------|------|------------------|
| `EmberWeaponCatalog` | `Core/EmberWeaponCatalog.cs` | **Id 空间与武器名册的唯一出处**：0–4 通用 / 10–15 武器 / 20–25 质变 / 30–35 专属；`WeaponForMeta` / `WeaponForExcl` / `IsRosterWeapon` | ✅ |
| `EmberChoicePool` | `Core/EmberChoicePool.cs` | **候选卡生成**：基础 3 张 + 空位补新武器、权重抽卡、同屏唯一键、补位与兜底 | ✅ |
| `RunProgress` | `Core/RunProgress.cs` | **只负责构筑状态怎么变**：槽位与归属、通用属性、品质 roll 落地、`Choose` / `TryRefresh` / `AbsorbDamage` / 计分 | ✅ |
| `RunFlowController` | `Core/RunFlowController.cs` | 清波选卡次数、补偿序列、刷新与候选校验；通过 Host 请求呈现和恢复战斗 | ✅ |
| `EmberRarityUtil` | `Core/EmberRarity.cs` | 品质权重与通用词条数值表 | ✅（见下） |

**为什么拆**："候选怎么拼"与"状态怎么变"混在一起时，
想验证"满槽不出新武器""同屏唯一键是 `(Id, WeaponId)`"必须先摆好整套构筑状态。拆开后 `EmberChoicePool.Build(progress, random)`
只读 `RunProgress` 的公开只读事实，可直接单测。

**注意：随机数消耗顺序是行为的一部分。** `EmberChoicePool.Build` 里每次 `random.Next*` 的**顺序与次数必须与拆分前逐字一致**，
否则同一个种子发出来的手牌会变，jack 无法复现问题。改这个文件前先读它的类注释。

### 配色为什么返回 `float[3]` 而不是 `Color`

`EmberRarityUtil.Tint(rarity)` 返回 `float[3]`（RGB 0–1），UI 侧用 `EmberRarityTint.ToColor()`
（`UI/EmberRarityTint.cs`）转成 `UnityEngine.Color`。

配色数据保持为 RGB 数组，Unity 的颜色类型转换由 UI 层负责，从而维持 M2 对 UnityEngine 的隔离。

## Unity 装配

- **无场景对象、无 Prefab、无 ScriptableObject**。`RunProgress` 由组合根（`EmberGame.StartRunWith`）在开局时直接用所选武器和槽位构造，并由 `EmberRunState.Progress` 持有；无参构造仍提供默认火球构筑。
- 回营地时 `Progress` 被置为 `null`，因此**所有消费方必须容忍 null**（`EmberCombat.Tick` 注入前先判）。
- `RunFlowController` 由组合根在 `Awake` 构造，开局和回营地调用 `Reset`；展示与恢复战斗通过 Host 回调请求。
- `EmberRarity` 为枚举，`EmberRarityUtil` 为静态查表，无实例生命周期。
- 武器名称与说明集中在 `EmberWeaponCatalog.Definition`，`EmberCopy.UpgradeNames` / `UpgradeDetails` 组装公共词条文案（36 项，下标 == 词条 id），并被 `EmberFonts` 用于字形预热——**改文案必须同时确认字体预热覆盖**。

## 影响与回归范围

- **直接影响模块**：M4（读全部属性与查询）、M6（读 `Luck` / `CoreWeaponId` / `RefreshesRemaining` / `EmptySlots` 并渲染候选）、M3（经 `EmberGame` 触发发牌）、M7（`EmberCardIcons` 只读 id 常量）。
- **必须复验的契约**：`Choices` 的长度与唯一性、`Choose` 的防伪与补偿选卡、品质权重表、伤害乘区、护盾吸收顺序。
