# M5 敌人系统

> 相关需求：`../requirements.md` §3.1、§3.4、§3.5、§4.4（回血掉落）
> 验收文档：`05-enemy-system-test.md`
> 代码：`Assets/Scripts/Combat/Enemies/`（4 个文件）+ `Assets/Scripts/Combat/EmberHealingDrops.cs`

## 职责边界

- **本模块负责**：敌人种类定义与行为策略（追击 / 射手 / 分裂 / 盾卫 / 引线虫）、暗弹弹幕、敌人剪影与程序化动画、战斗提示层（角色标记、攻击预警、词缀光环、风之尾迹）、治疗掉落的生成与拾取。
- **本模块不负责**：
  - 伤害结算与死亡处理 → M3（本模块只提供行为与表现）
  - 刷怪配额与权重 → M1
  - 血量数值 → M1（`LevelConfig.TrashHp`）

**关键定位**：本模块与 M4 采用**同构的策略抽象**（`IEnemyBehavior.Tick(dt, ctx)` 对应 `EmberWeapon.Tick(dt, ctx)`），是项目里第二处清晰的契约边界。

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M1 | `EnemySpeed(kind)`、`TrashHp(wave, kind)`、射击/护盾/爆炸/词缀参数、`EnemyLimit` | 全部行为数值 | — |
| M3 | `EmberCombat.Enemy` 实例、`game.State`、`game.NeedsHealing`、`game.PickHealingDrop`、`game.HurtPlayer` | 宿主与伤害入口 | — |
| M7 | `EmberEnemySilhouette` / `EmberEnemyVisual` 使用的共享精灵与粒子 | 外观 | 精灵缺失时用程序化占位 |

## 设计与数据流

```text
EmberCombat 刷怪：RollKind(wave, rng) → kind
   └─ 从 EmberPool 借出敌人 GameObject
        ├─ EmberEnemySilhouette.Apply(kind)   恢复基准姿态 → 应用类型装饰
        ├─ EmberEnemyVisual.Attach(...)       挂角色标记 / 预警 / 词缀光环
        └─ 按 kind 选择 IEnemyBehavior：
             0 普通 → ChaseBehavior
             1 快速 → ChaseBehavior（更高速度）
             2 高血 → ChaseBehavior
             4 暗烛射手 → ShooterBehavior
             5 烬胎(母) → SplitBehavior
             6 烬虫(子) → ChaseBehavior（不自掉落）
             7 铁灯卫 → ShieldBehavior
             8 引线虫 → BomberBehavior

每帧：behavior.Tick(dt, ctx)
   └─ 移动 / 蓄力 / 发射暗弹 / 引爆 / 护盾朝向
        └─ 伤害经 M3 的 HurtPlayer / ContactDamage 路径
```

敌人 `kind` 编号**与刷怪权重表的列顺序不同**：权重表列为 `{0,1,2,4,5,7,8}`，因为 **kind 3 = Boss**（由 `EmberBoss` 独立负责）与 **kind 6 = 分裂子体**（不参与随机抽取）。

## 关键机制

### 敌人名册

| kind | 名称 | 出场 | 行为要点 |
|------|------|------|----------|
| 0 | 普通 | W1+ | 直线追击，基准速度 1.0 |
| 1 | 快速 | W2+ | 追击，速度 1.9 |
| 2 | 高血 | W2+ | 追击，血量为 tough 档 |
| 3 | Boss | 每 10 波 | **由 `EmberBoss` 独立负责**，不走 `IEnemyBehavior` |
| 4 | 暗烛射手 | W3+ | 与玩家保持 5.5–7 距离带；每 2.6s 发射暗弹（速度 6、伤害 8），发射前 0.4s 蓄力闪光 |
| 5 | 烬胎（母） | W4+ | 追击；**最终死亡时分裂 3 只 kind 6** |
| 6 | 烬虫（子） | 分裂产生 | 高速追击（2.2），血量 ×0.25，**不掉落、不消耗配额** |
| 7 | 铁灯卫 | W4+ | 正面 ±60° 扇形**减伤 70%**；转身限速 65°/秒，鼓励绕后 |
| 8 | 引线虫 | W5+ | 接近玩家 2u 或被击杀时点燃 **0.8s 引线**，结束爆炸（半径 1.8、伤害 20） |

- 射手距离带在竖屏下**按相机可见宽度收缩**；只能在屏内开始/完成蓄力。
- 引线期间**不再接触伤害、不再被自动锁定**。
- 盾卫面朝护盾圆弧方向旋转，各武器传入实际攻击来源位置以判定正/背面。

### 精英词缀

| 词缀 | 效果 | 视觉识别 |
|------|------|----------|
| 灼热 `Burning` | 2u 内每秒 2 点伤害，**多圈不叠加** | 橙红外焰粒子 |
| 疾风 `Wind` | 每 3s 爆发速度 ×1.8 持续 1s | 青色残影 |
| 重生 `Rebirth` | 死亡后读条 2s，以 40% 生命复活**一次** | 灰白核心 + 读条圈 |

- 概率由 `LevelConfig.AffixChance(wave)` 决定（第 3 波起 12%、第 5 波起 18%、封顶 50%，乘难度 `AffixMultiplier`）。
- 词缀怪余烬奖励 **×3**（直接记 Score，无掉落物）。
- 精英挑战波的强制小队**不**走随机词缀概率。

### 暗弹弹幕

- 上限 **48**，池化复用（key `dark-bolt`）。
- 直线飞行、碰玩家结算、出视口销毁；**与玩家弹体对称管理**。
- **可被玩家拦截击毁**：火球与暗弹碰撞时同时消失；穿透火矢可击毁暗弹**且不消耗穿透次数**。
- 使用**相对运动线段检测**处理高速交叉，避免穿透漏判。
- 清波 / 新局 / 退出时清理。

### 治疗掉落

| 项 | 值 |
|----|----|
| 回复量 | 每个 20 HP（不超上限） |
| 掉落率 | 普通敌人最终死亡 8% / 带词缀 20% / Boss 100% |
| 不掉落 | 分裂子体 |
| 重生读条前 | **不算**最终死亡，不重复掉落 |
| 磁吸 | 受伤时 1.5 单位内吸附；`NeedsHealing` 时 2.2；0.5 单位内拾取 |
| 存在时长 | 60 秒；暂停与选卡时计时冻结，普通换波保留 |
| 上限 | 同屏 24 个 |
| 护盾 | 回血**不补护盾**（两者独立数值） |

### 表现层

- `EmberEnemySilhouette`（`MonoBehaviour`）：按 kind 复用式重着色/重塑，**池化敌人回池前必须恢复基准形态**。
- `EmberEnemyVisual`：角色标记、攻击预警、词缀光环、风之尾迹，以及程序化躯干动画（普通浮动 / 快怪急促摇动 / 重怪缓慢起伏 / 烬虫快速挤压 / 烬胎呼吸 / 盾卫轻微倾斜 / 射手蓄力压缩与射后回弹 / 引线虫高频鼓动 / 重生收缩）。**每个敌人相位不同**。
- 动画**只动外观子节点**，不变更根节点、血条、预警范围或碰撞。
- 基础三类敌人（0/1/2）无词缀也接入动画；特殊敌人禁用基础轮廓装饰。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `IEnemyBehavior.Tick(float, EmberEnemyContext)` | 接口 | dt、context | — | 行为不直接扣血，经 context 投递 |
| `ChaseBehavior` / `ShooterBehavior` / `SplitBehavior` / `ShieldBehavior` / `BomberBehavior` | 类 | — | — | 由 M3 按 kind 构造 |
| `EnemyAffix` | 枚举 | — | `Burning` / `Wind` / `Rebirth` | 与 `LevelConfig` 概率配合 |
| `EmberEnemyProjectiles` | 类 | — | 暗弹弹幕 | 上限 48；`Intercept` 可击毁 |
| `EmberHealingDrops` | 类 | `EmberGame` 引用 | 掉落生成与拾取 | 依赖注入 `EmberGame`（非反查） |
| `EmberEnemySilhouette` | `MonoBehaviour` | kind | 外观 | 借出时应用、归还前还原 |
| `EmberEnemyVisual` | 类 | 敌人实例 | 提示与动画 | 仅动外观子节点 |

**具体数值不在此复制**，一律见 `../requirements.md` §3.4、§3.5、§4.4。

## Unity 装配

- 敌人 GameObject 由 `EmberPool` 借出（key `shade` / `boss`）；**每次借出都恢复基准姿态与颜色**，再应用类型装饰。
- `EmberEnemySilhouette` 是 `MonoBehaviour`，挂在池化敌人对象上；**必须容忍被复用**，不得在 `Awake` 里做一次性的类型假设。
- 角色标识与预警随敌人销毁，**不附着到复用的躯体上**——这是曾经的缺陷来源。
- `EmberEnemyVisual` 与行为策略为普通 C# 类，由 M3 构造与驱动。
- 创建/销毁所有权：M3 拥有敌人池；本模块只负责单个敌人的行为与外观。

## 影响与回归范围

- **直接影响模块**：M3（行为与弹幕宿主）、M1（数值来源）、M7（共享精灵与粒子）、M2（掉落计入 Score 与回血）。
- **必须复验的契约**：池复用的形态还原、分裂数量与配额关系、盾卫正背面倍率、引线爆炸时机、暗弹拦截、词缀概率门控。
- **对应验收项**：`05-enemy-system-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| 敌人行为是否需要更多种类 | 扩到 6+ 种 / 维持现 5 种 | 现配置已覆盖拉距离/控制击杀位/绕后/躲避四种应对 | jack + xiaoPlanner |
| `EmberEnemyVisual` 是否从数据驱动改配置化 | 抽 ScriptableObject / 维持代码常量 | 动画参数目前散在代码里 | xiaoCoder |
| 治疗掉落上限 24 与粒子上限 120 的真机预算 | 真机 Profiler 后调 / 维持 | 影响长局性能 | xiaoCoder + jack |

性能定位标记见 `03-combat-waves.md` 的「性能定位标记」；本次仅新增采样作用域，公开行为不变。
