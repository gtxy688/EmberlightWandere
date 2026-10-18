# M4 武器系统

> 相关需求：`../requirements.md` §4.2、§4.4、§4.6
> 代码：`Assets/Scripts/Combat/Weapons/`（武器基类、实现与注册工厂）

## 职责边界

- **本模块负责**：6 把武器的 `Tick` 行为、弹体与轨迹、命中判定与伤害投递、专属/质变引起的形态替换。
- **本模块不负责**：
  - 属性加成数值与卡池 → M2（本模块只查询 `Progress`）
  - 敌人生成与死亡结算 → M3（本模块只投递伤害）
  - 卡面文案与图标 → M6 / M7

**最重要的边界约束**：武器通过 `EmberWeaponContext` 与 `RunProgress` 工作，六把可获取武器均不持有 `EmberGame`。改武器时保持这条依赖边界。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M2 | `Progress.OwnsWeapon` / `HasMetamorph` / `WeaponMagnitude` / `WeaponAttackSpeed` / `WeaponCount` / `DamageMul` / `Orbits` / `TrailPower` / `PierceLimit` / `MeteorExtra` / `BoomExtraTrips` / `BoomExtraLegHit` / `TrailExtend` / `TrailRing` | 全部数值与形态门控 | `Progress` 为 null 时不得崩溃 |
| M3 | `EmberWeaponContext` 的委托与方法 | 选敌、伤害、拦截、燃地、可见性 | 委托由 M3 保证全部赋值 |
| M7 / M3 | `ctx.Effects` / `ctx.Pool` | M7 粒子与 M3 对象池 | 粒子满时跳过发射；对象池未命中则创建视图 |
| M8（经 M3 装配） | `ctx.PlaySound(EmberWeaponSound)` | 开火/接触/落地音效 | 回调缺失只跳过反馈，不创建音频单例 |

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

每把武器是 `EmberWeapon` 的 `sealed` 子类，由 `EmberWeaponFactory.CreateAll` 在战斗构造时注册并持有 `List<EmberWeapon>`。

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

### 参数与计算归属

基础参数和升级增量统一读 `Core/EmberWeaponTuning.cs`，当前值见需求 §4.6、§4.9。`RunProgress` 与卡牌预览共用该表；注册顺序统一由 `EmberWeaponFactory` 维护。

伤害使用对应武器的攻击力与全体伤害乘区。弹体武器的冷却和燃地铺火间隔受对应武器攻速影响；环火通过转速响应攻速。共享燃地伤害按帧积分，不能把铺火频率等同于单块火区 DPS。

临时 Buff 伤害倍率由 M3 的 `EmberCombatDamage.Apply` 在实际命中时统一应用一次，不写入 RunProgress，也不由各武器重复计算；飞行中的弹体和共享燃地都按命中当时的倍率结算。生命周期契约见 M10。

### 共享燃地

`Combat/EmberBurnGround` 持有火区列表、生命周期、容量和伤害。所有产火武器经 `ctx.SpawnBurnPatch` 投递；战斗在 Trail 时隙后紧接推进共享效果，因此未持有燃地武器也可结算其他武器产生的火区。收局与新局重置火区，世界节点由组合根回收。

### 逐武器行为要点

**火球（13）**
- 自动锁定最近可见敌人，基础单发；`ExtraShots` 由连发专属增加，决定弹数与扇形。
- **燎原**（质变 20）：扩大体积与伤害，命中后在路径留下较小燃地；倍率见需求 §4.6。
- **连发**（专属 30）：增加发射弹数，增量见需求 §4.6。
- 弹体离开可战斗视口后销毁，防止屏外击杀。

**环火（14）**
- 定半径旋转火种，接触式持续伤害。
- **同一敌人每帧只结算最近火种的一次接触伤害**，不按重叠球数叠加 DPS。
- **添薪**（专属 31）：增加环绕火种，增量见需求 §4.6。
- **日冕**（质变 21）：半径 / 转速 / DPS 上调 + 灼烧拖尾。

**燃地（15）**
- 沿玩家路径留火，踩踏掉血；`TrailPower` 决定强度，开局基线见需求 §4.9。
- 只负责自身产火与火海圈；轨迹、火球和环火质变都经 `ctx.SpawnBurnPatch` 投递到共享 `EmberBurnGround`，由后者管理燃地生命周期与伤害。
- **延烧**（专属 32）：铺火更频繁、燃地伤害提高，单块火区寿命不变；**火海**（质变 22）：身周持续火圈。参数见需求 §4.9，卡片描述与该行为一致。
- 同屏火区达到容量时替换最旧火区，容量归属 `EmberWeaponTuning.BurnGround`，见需求 §4.9。

**穿透火矢（10）**
- **仅当玩家基本静止时才开火**（`ctx.PlayerStanding`，判定参数见 [需求 §4.8](../requirements.md#48-静止判定)）；移动中不进入射击，已飞出的箭继续飞。冷却照常推进。
- 朝最近可见敌直线穿透，逐层消耗穿透次数。
- **贯穿**（专属 33）：增加穿透上限，增量见需求 §4.6。
- **穿杨**（质变 23）：命中后生成弱化分裂箭且保持静音；伤害倍率见需求 §4.6。

**回旋烬蝶（11）**
- 飞出后返回玩家，**去程与回程各自独立结算**（两套命中集合，同一敌人可各吃一次）。
- **回马**（专属 34）：往返各多一段结算；**折返无尽**（质变 24）：多一个来回。

**天降火雨（12）**
- 随机选可见敌，预警结束后结算范围伤害；预警时长与半径见需求 §4.9。
- **多落点**（专属 35）：增加落点；**天火**（质变 25）：扩大范围并提升伤害，参数见需求 §4.6、§4.9。
- 预警圈**静音**，仅在落地瞬间发声。



## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberWeapon.Tick(float, EmberWeaponContext)` | 抽象方法 | dt、context | — | 未拥有该武器时自我门控，不动作 |
| `EmberWeapon.Reset()` | 虚拟方法 | — | — | 新局清理内部弹体与计时 |
| `EmberWeaponContext` | 类 | — | 委托与服务 | 由 M3 全部赋值；**武器不得持有 `EmberGame`** |
| 武器 Id 常量 | `RunProgress` 中的 `const int` | — | — | 10–15；被图标表与卡池共用 |

**具体数值不在此复制**，一律见 `../requirements.md` §4。

## Unity 装配

- **无 Prefab、无 ScriptableObject**：武器是普通 C# 类，`EmberCombat` 初始化时通过 `EmberWeaponFactory.CreateAll` 创建，开局由 `Begin` 重置。
- 弹体与轨迹 GameObject 经 `EmberPool` 按 key 复用（`fire` / `burn` / `warn`）。
- 武器不持有场景引用，场景引用全部经 `ctx`（`Player` / `World` / `Cam`）传入。
- **生命周期**：`EmberCombat` 构造时创建并持有武器实例；`TearDown` 统一重置实例内部状态，`Begin` 先调用它再重新装配，回营地保留实例供下局复用。弹体与世界视图按池和本局世界生命周期回收。

## 影响与回归范围

- **直接影响模块**：M3（伤害投递与 context）、M2（读取加成）、M6（基础武器名与说明读 Catalog，其余卡面文案读 EmberCopy）、M7（弹体外观）、M8（开火音效接线点）。
- 共享效果改动影响其他武器产火、无 Trail 所有权时结算、容量替换、到期回池和跨局清理；参数变更还影响 M2 应用与 M6 预览。
- **必须复验的契约**：伤害乘区公式、`OwnsWeapon` 门控、专属/质变形态替换、穿透静止门控、同怪环火满伤规则。
- **回归触发条件**：任何 `EmberWeaponContext` 字段变更、`RunProgress` 加成接口变更、`EmberPool` key 变更。
