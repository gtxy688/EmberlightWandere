# M4 武器系统

> 相关需求：`../requirements.md` §4.2、§4.4、§4.6
> 验收文档：本文档 §验收（无独立 `-test.md`；武器行为需 PlayMode 且与 M3 强耦合）
> 代码：`Assets/Scripts/Combat/Weapons/`（7 个文件）

## 职责边界

- **本模块负责**：6 把武器的 `Tick` 行为、弹体与轨迹、命中判定与伤害投递、专属/质变引起的形态替换。
- **本模块不负责**：
  - 属性加成数值与卡池 → M2（本模块只查询 `Progress`）
  - 敌人生成与死亡结算 → M3（本模块只投递伤害）
  - 卡面文案与图标 → M6 / M7

**最重要的边界约束**：`EmberWeapon.cs` 的文件头注释明文规定 *"Weapons must not reverse-depend on EmberGame."*，且**经全文件验证成立**——6 把武器（含 Pulse）零处 `EmberGame` 引用，只通过 `EmberWeaponContext` 与 `RunProgress` 工作。**这是本仓库最干净的模块边界，改武器时不要引入 `EmberGame`。**

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M2 | `Progress.OwnsWeapon` / `HasMetamorph` / `WeaponMagnitude` / `WeaponAttackSpeed` / `WeaponCount` / `DamageMul` / `Orbits` / `TrailPower` / `PierceLimit` / `MeteorExtra` / `BoomExtraTrips` / `BoomExtraLegHit` / `TrailExtend` / `TrailRing` | 全部数值与形态门控 | `Progress` 为 null 时不得崩溃 |
| M3 | `EmberWeaponContext` 的委托与方法 | 选敌、伤害、拦截、燃地、可见性 | 委托由 M3 保证全部赋值 |
| M7 | `ctx.Effects` / `ctx.Pool` | 粒子与弹体复用 | 池空时丢弃，不报错 |
| M8 | `EmberAudio` 的 `PlayWeapon*` | 开火/落地音效 | clip 缺失静默跳过 |

## 设计与数据流

```text
EmberCombat.Tick(dt)
   └─ foreach weapon in weapons: weapon.Tick(dt, ctx)
         ├─ if (!Progress.OwnsWeapon(id)) return;      ← 第 1 道门
         ├─ 冷却计时 → 到点后选敌 / 落点
         ├─ ctx.FindNearestVisibleEnemy() / FindRandomVisibleEnemy()
         ├─ 结算伤害：基伤 × (1 + WeaponMagnitude) × DamageMul
         │     └─ ctx.Damage(id, amount) / DamageFrom(...)   ← 投递给 M3
         └─ ctx.Effects / ctx.Pool 产出表现
```

每把武器是 `EmberWeapon` 的 `sealed` 子类，由 `EmberCombat` 在 `Begin` 时注册并持有 `List<EmberWeapon>`。

### 抽象基类

```csharp
public abstract class EmberWeapon
{
    public abstract void Tick(float dt, EmberWeaponContext ctx);
    public virtual void Reset() { }   // 新局清理
}
```

`EmberWeaponContext` 是**传送门式委托包**，而非对 `EmberGame` 的引用——这是本模块能保持零反向依赖的原因。详见 `03-combat-waves.md` 的 context 契约表。

## 关键机制

### 武器名册与 Id

| Id | 类 | 武器 | 占槽 |
|----|----|------|------|
| 13 | `EmberBasicShotWeapon` | 火球 | ✅ |
| 14 | `EmberOrbitWeapon` | 环火 | ✅ |
| 15 | `EmberTrailWeapon` | 燃地 | ✅ |
| 10 | `EmberPierceWeapon` | 穿透火矢 | ✅ |
| 11 | `EmberBoomerangWeapon` | 回旋烬蝶 | ✅ |
| 12 | `EmberMeteorWeapon` | 天降火雨 | ✅ |
| — | `EmberPulseWeapon` | 冲击波 | ❌ **死代码** |

### 逐武器数值接线

| 武器 | 伤害 | 攻速 |
|------|------|------|
| 火球 | `基伤 × (1+mag) × DamageMul` | CD `0.8 / (1+as)` |
| 穿透火矢 | 同上 | CD `1.1 / (1+as)` |
| 回旋烬蝶 | 同上（分段结算） | CD `2.0 / (1+as)` |
| 天降火雨 | 同上 | CD `3.0 / (1+as)` |
| 环火 | `DPS × (1+mag) × DamageMul` | 转速 `spin × (1+as)` |
| 燃地 | `DoT × (1+mag) × DamageMul` | tick 间隔 `/(1+as)` |

`mag` = `Progress.WeaponMagnitude(id)`（**单武器**攻击力），`as` = `Progress.WeaponAttackSpeed(id)`（**单武器**攻速），`DamageMul` = `1 + DamageAmp`（**全局**全体伤害增幅）。

> ⚠️ 攻击力与攻速**不是全局**——`{武器名}·攻击力` 只抬该武器。加全局攻击力会让早期伤害膨胀，已明确禁止。

### 逐武器行为要点

**火球（13）**
- 自动锁定最近**可见**敌人，基础单发；`WeaponCount` 与专属影响弹数与扇形。
- **燎原**（质变 20）：体积 ×1.5、伤害 ×1.25、命中点在路径留半功率燃地。
- **连发**（专属 30）：额外火球 **+1**（已从 +2 收档，防弹数雪球）。
- 弹体离开可战斗视口后销毁，防止屏外击杀。

**环火（14）**
- 定半径旋转火种，接触式持续伤害。
- **同怪仅最近的球造成满伤**（多球叠伤曾是超模主因）；日冕单球 DPS 已从 90 收到 **75**。
- **添薪**（专属 31）：环绕 **+1**（已从 +2 收档）。
- **日冕**（质变 21）：半径 / 转速 / DPS 上调 + 灼烧拖尾。

**燃地（15）**
- 沿玩家路径留火，踩踏掉血；`TrailPower` 决定强度（开局基线 0.20）。
- **兼任全局共享烧地服务**：`SpawnPatch` 被火球（燎原）与 `ctx.SpawnBurnPatch` 调用。
- **延烧**（专属 32）：拖尾更长更烫；**火海**（质变 22）：身周持续火圈。
- 同屏燃地上限 40（性能约束，见 M9）。

**穿透火矢（10）**
- **仅当玩家基本静止时才开火**（`ctx.PlayerStanding`，速度阈值 0.15）；移动中不进入射击，已飞出的箭继续飞。冷却照常推进。
- 朝最近可见敌直线穿透，逐层消耗穿透次数。
- **贯穿**（专属 33）：穿透上限 +1（已从 +2 收档）。
- **穿杨**（质变 23）：命中分裂一小支，分裂箭**伤害 ×0.45** 且**静音**（避免一次射击叠两声）。

**回旋烬蝶（11）**
- 飞出后返回玩家，**去程与回程各自独立结算**（两套命中集合，同一敌人可各吃一次）。
- CD 已从 2.4 收到 **2.0**。
- **回马**（专属 34）：往返各多一段结算；**折返无尽**（质变 24）：多一个来回。

**天降火雨（12）**
- 随机选可见敌，**0.6 秒预警圈**后落范围伤害（基础半径 1.6）。
- **多落点**（专属 35）：+1 落点；**天火**（质变 25）：圈更大、伤更高（约 +10%）。
- 预警圈**静音**，仅在落地瞬间发声。

### 相对强度口径（平衡目标）

评估口径：**单目标持续**（盯一只怪输出）与**清杂**（一技能打多只）；"成型" = 该武器 + 专属(金) + 质变(钻)，**未**叠通用增幅。

| 武器 | 基线 | +专属 | +专属+质变 | 已收档措施 |
|------|------|-------|------------|------------|
| 火球 | 中 | 高 | 高 → 压到中高 | 连发 +2→**+1**；燎原 ×1.5→**×1.25** |
| 环火 | 中高 | 过高 | 超模 | 添薪 +2→**+1**；日冕 DPS 90→**75**；同怪仅**最近球**满伤 |
| 燃地 | 低 | 中低 | 中 | 踩踏系数 **40→46**（+15%）；火海维持中档 |
| 穿透火矢 | 中 / 清杂高 | 中高 | 高 | 分裂 **0.45** 维持；贯穿 +2→**+1**；改为**静止才开火** |
| 回旋烬蝶 | 低中 | 中 | 中高 | CD 2.4→**2.0**；段伤 18 维持 |
| 天降火雨 | 低中 / AOE 中 | 中 | 中高 | 天火半径 **2.53**、伤 **×1.54**（约 +10%） |

**成型后目标带**：六条线单目标持续都落在**中 ±半档**；允许穿透清杂略高、燃地移杀略低。**禁止**环火/火球成型后明显高于其余"高+"。

> ⚠️ 上表数值为**策划档位意图**，与代码常量可能存在未同步的小差异。改数值前先对照 `Combat/Weapons/*` 实际常量，并回写 `../requirements.md` §4.6。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberWeapon.Tick(float, EmberWeaponContext)` | 抽象方法 | dt、context | — | 未拥有该武器时自我门控，不动作 |
| `EmberWeapon.Reset()` | 虚拟方法 | — | — | 新局清理内部弹体与计时 |
| `EmberWeaponContext` | 类 | — | 委托与服务 | 由 M3 全部赋值；**武器不得持有 `EmberGame`** |
| 武器 Id 常量 | `RunProgress` 中的 `const int` | — | — | 10–15；被图标表与卡池共用 |

**具体数值不在此复制**，一律见 `../requirements.md` §4。

## Unity 装配

- **无 Prefab、无 ScriptableObject**：武器是普通 C# 类，由 `EmberCombat.Begin` 构造。
- 弹体与轨迹 GameObject 经 `EmberPool` 按 key 复用（`fire` / `burn` / `warn`）。
- 武器不持有场景引用，场景引用全部经 `ctx`（`Player` / `World` / `Cam`）传入。
- **创建/销毁所有权**：`EmberCombat` 拥有武器实例；新局调用 `Reset()`，回营地随 `TearDown` 一并丢弃。

## 影响与回归范围

- **直接影响模块**：M3（伤害投递与 context）、M2（读取加成）、M6（卡面名称与说明文案由 `EmberGame.UpgradeNames/Details` 提供）、M7（弹体外观）、M8（开火音效接线点）。
- **必须复验的契约**：伤害乘区公式、`OwnsWeapon` 门控、专属/质变形态替换、穿透静止门控、同怪环火满伤规则。
- **回归触发条件**：任何 `EmberWeaponContext` 字段变更、`RunProgress` 加成接口变更、`EmberPool` key 变更。

## 验收

| 编号 | 层级 | 检查 | 状态 |
|------|------|------|------|
| W1 | PlayMode（待建） | 每把武器单独一局，`Tick` 能产出伤害且不抛异常 | 未验证（无测试程序集） |
| W2 | PlayMode（待建） | 未拥有武器不产生任何弹体与伤害 | 未验证 |
| W3 | PlayMode（待建） | `火球·攻击力` 只抬火球，其他武器伤害不变 | 未验证 |
| W4 | PlayMode（待建） | 穿透火矢仅静止时开火，移动中不产生新箭 | 未验证 |
| W5 | PlayMode（待建） | 烬蝶去程/回程各结算一次 | 未验证 |
| W6 | PlayMode（待建） | 火雨射程内预警结束后才结算伤害 | 未验证 |
| W7 | PlayMode（待建） | 环火多球命中同一敌人时只有最近球满伤 | 未验证 |
| W8 | 手动 | N=1 六条线各打一局，成型强度不超模 | 待人工验收（jack） |
| W9 | 手动 | 穿透"站住才开火"的狙击手感成立 | 待人工验收（jack） |
| W10 | 手动 | 专属/质变的视觉差异可辨认 | 待人工验收（jack） |

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| `EmberPulseWeapon` 死代码去留 | 恢复为不占槽词条 / 删除 | 当前 `WavePower` 恒 0，永不生效 | jack + xiaoPlanner |
| 策划档位表与代码常量的一致性维护点 | 以代码为准并回写需求 / 保持双份 | 当前存在未同步风险 | xiaoCoder + xiaoPlanner |
| 是否给武器补音效 pitch 抖动 | 加随机 pitch / 维持一致音色 | 多次连发听感偏机械 | jack |
