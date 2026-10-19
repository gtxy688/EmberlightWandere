# M5 敌人系统

> 相关需求：`../requirements.md` §3.1–3.5、§4.4（回血掉落）、§7（皮肤与表现默认值）
> 代码：`Assets/Scripts/Combat/Enemies/`（4 个文件）+ `Assets/Scripts/Combat/EmberDrops.cs`

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
| M3 | `EmberEnemy` 实例（顶层类型，原 `EmberCombat.Enemy`）与 `EmberEnemyContext` | 行为策略读写敌人数据 | — |
| M0 | `IEmberCombatHost`：`NeedsHealing` / `PickHealingDrop` / `TryAddBuff` / `HurtPlayer` / `Phase` | 掉落吸附、Buff 请求与伤害入口（经 M3 转交，不再直接读 `game.State`） | — |
| M7 | `EmberEnemySilhouette` / `EmberEnemyVisual` 使用的共享精灵与粒子 | 外观 | 精灵缺失时用程序化占位 |

## 设计与数据流

```text
EmberCombat 刷怪：RollKind(wave, rng) → kind
   └─ 从 EmberPool 借出敌人 GameObject
        ├─ EmberEnemySilhouette.Apply(kind)   恢复基准姿态 → 按 kind 选完整皮肤，缺少映射才用旧装饰
        ├─ new EmberEnemyVisual(enemy, world)       挂角色标记 / 预警 / 词缀光环
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
| 0 | 普通 | W1+ | 直线追击 |
| 1 | 快速 | W2+ | 比普通怪更快的追击 |
| 2 | 高血 | W2+ | 追击，血量为 tough 档 |
| 3 | Boss | 阶段与最终 Boss 波 | **由 `EmberBoss` 独立负责**，不走 `IEnemyBehavior` |
| 4 | 暗烛射手 | W3+ | 保持距离，先蓄力再发射暗弹 |
| 5 | 烬胎（母） | W4+ | 追击；最终死亡时生成 kind 6 子体 |
| 6 | 烬虫（子） | 分裂产生 | 高速追击，不掉落、不消耗常规配额 |
| 7 | 铁灯卫 | W4+ | 正面扇形减伤，转身限速，攻击来源决定正背面 |
| 8 | 引线虫 | W5+ | 接近玩家或被击杀时点燃引线，读条结束后爆炸 |

速度、距离、伤害、分裂数量与引线参数见需求 §3.4、§3.7，统一由 LevelConfig 查询。

母体最终死亡的两路分裂登记暂时保留；每批数量读 `SplitCount`，实际总量见需求 §3.7，登记与展开顺序见 M3「死亡处理顺序」。

- 射手距离带在竖屏下**按相机可见宽度收缩**；只能在屏内开始/完成蓄力。
- 引线期间**不再接触伤害、不再被自动锁定**。
- 盾卫面朝护盾圆弧方向旋转，各武器传入实际攻击来源位置以判定正/背面。

### 精英词缀

| 词缀 | 效果 | 视觉识别 |
|------|------|----------|
| 灼热 `Burning` | 周围持续伤害，多圈不叠加 | 橙红外焰粒子 |
| 疾风 `Wind` | 周期性爆发加速 | 青色残影 |
| 重生 `Rebirth` | 死亡后读条，以部分生命复活一次 | 灰白核心 + 读条圈 |

- 效果参数与随机概率见需求 §3.5，由 `LevelConfig.AffixChance` 查询。增长段先封顶，再乘难度倍率，最后钳制最终概率。
- 击杀奖励直接记 Score，治疗物另按掉落规则生成；计分见需求 §5。
- 精英挑战波的强制小队**不**走随机词缀概率。

### 暗弹弹幕

- 暗弹容量见需求 §3.7，按 `dark-bolt` key 池化复用。
- 直线飞行、碰玩家结算、出视口销毁；**与玩家弹体对称管理**。
- **可被玩家拦截击毁**：火球与暗弹碰撞时同时消失；穿透火矢可击毁暗弹**且不消耗穿透次数**。
- 使用**相对运动线段检测**处理高速交叉，避免穿透漏判。
- 清波 / 新局 / 退出时清理。

### 治疗掉落

治疗量与掉落率见需求 §4.4；吸附、寿命与容量见 §3.7。

`EmberDrops.OnDeath` 只在最终死亡时调用，分裂子体不掉落，重生接管不重复掉落。`Tick` 在 Playing 阶段推进；普通换波保留，满血也允许吸附拾取，溢出治疗丢弃且不补护盾。容量满时替换最早掉落物，拾取或过期后回池。

同一模块还独立判定 Buff 掉落，拾取经 `IEmberCombatHost.TryAddBuff` 请求施加增伤或疾行，不替代治疗判定。治疗与 Buff 共用掉落寿命、吸附和总容量；规则见需求 §4.10。

外观由 `EmberPickupArt` 从专用图集按名称取图，治疗、增伤、疾行分别使用药瓶、火种护符、风羽。Sprite 采用居中 pivot，按 bounds 换算缩放；保留既有显示宽度和脉动/临近到期闪烁。彩色素材不额外染色，回池复用时重新赋图。

### 射手迟缓技能

射手按配置的射击频率释放迟缓弹；对应蓄力预警和弹体显示青色。命中先检查原有无敌帧，再扣血并请求施加迟缓。弹体仍可被拦截，回池清除效果引用，普通暗弹不携带 Debuff；Buff 生命周期由 M10 管理。配置归 `LevelConfig.ShooterSlowEvery` 和 `EmberBuffCatalog.Slow`。

### 表现层

- `EmberEnemySilhouette`（`MonoBehaviour`）：借出时先恢复基准位置、缩放、旋转、颜色、Sprite、flipX 和显隐，再按 kind 应用序列化的完整皮肤。当前模板覆盖普通、快速、高血、射手、母体、子体、盾卫、引线虫和 Boss，均为无脚悬浮造型；隐藏旧脸和重复装饰，只保留主体与脚下影子。缺少对应皮肤时沿用旧外观，旧分层装饰只在回落路径首次使用时创建。
- 杂兵共用 `shade` 池，Boss 保留独立 `boss` 池；普通 → 特殊 → 普通必须恢复贴图、翻转和可见部件。恢复发生在 `Apply`，归还时回收附属表现并停用节点。皮肤按 bounds 归一高度后继续使用原有兵种根倍率，不改碰撞。
- 原图默认朝左，整体皮肤按实际水平位移 flipX，上下移动保留朝向。盾卫使用独立盾图随 `enemy.facing` 转动；射手法杖与母体卵囊已画入皮肤，不再重建第二份配饰。攻击预警、引线圈、迟缓弹和词缀效果仍由原逻辑驱动。
- 彩图受击由共享 EnemySkin 材质对采样后的 RGB 混白，避免白色乘色再向白色插值无变化；`EmberEnemyVisual` 缓存 MaterialPropertyBlock，仅在强度变化时更新。到期、重生与回收清零，`Apply` 定向清命中参数并显式绑定当前皮肤纹理，不能清空整个块而丢失 `_MainTex`；旧模板继续原乘色反馈。
- `EmberEnemyVisual`：角色标记、攻击预警、词缀光环、风之尾迹，以及程序化躯干动画（普通浮动 / 快怪急促摇动 / 重怪缓慢起伏 / 烬虫快速挤压 / 烬胎呼吸 / 盾卫轻微倾斜 / 射手蓄力压缩与射后回弹 / 引线虫高频鼓动 / 重生收缩）。**每个敌人相位不同**。
- 动画**只动外观子节点**，不变更根节点、血条、预警范围或碰撞。
- 全部已配置皮肤的敌人（含 Boss）复用躯干动画，不新增走路序列帧或每怪 Animator。无词缀时隐藏角色色块，避免盖住皮肤。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `IEnemyBehavior.Tick(float, EmberEnemyContext)` | 接口 | dt、context | — | 行为不直接扣血，经 context 投递 |
| `ChaseBehavior` / `ShooterBehavior` / `SplitBehavior` / `ShieldBehavior` / `BomberBehavior` | 类 | — | — | 由 M3 按 kind 构造 |
| `EnemyAffix` | 枚举 | — | `Burning` / `Wind` / `Rebirth` | 与 `LevelConfig` 概率配合 |
| `EmberEnemyProjectiles` | 类 | — | 暗弹弹幕 | 容量见需求 §3.7；`Intercept` 可击毁 |
| `EmberDrops` | 类 | `EmberPool` + `IEmberCombatHost` | 掉落生成与拾取 | 构造时注入契约（非反查）：**不持有 `EmberGame`**，只经 `IEmberCombatHost.Phase / NeedsHealing / PickHealingDrop` 工作 |
| `EmberEnemySilhouette` | `MonoBehaviour` | kind、基础色 | `UsesWholeBodySkin` / `WholeBody` / 外观 | `HasSkinFor` 供池选择模板；每次 Apply 恢复模板与翻转后按 kind 选皮肤，缺映射用旧外观；不改变行为、碰撞与血条 |
| `EmberEnemyVisual` | 类 | 敌人实例 | 提示与动画 | 仅动外观子节点 |

**具体数值不在此复制**，一律见 `../requirements.md` §3.4、§3.5、§4.4。

## Unity 装配

- 敌人 GameObject 由 `EmberPool` 借出（key `shade` / `boss`）；**每次借出都恢复基准姿态与颜色**，再应用类型装饰。
- 当前主场景的 `EmberArtSet.ShadePrefab` 绑定 `Assets/Art/CleanToonSample/Prefabs/Shade.prefab`，其 skins 数组保存 kind / Sprite / height，`directionalShieldSprite` 保存独立盾图。资源在同级 Enemies 目录，经 `Emberlight/Art/Build Enemy Skins` 导入切片并补齐映射，重复执行保留已有映射、高度与盾引用。Boss 也从该模板借出，但池键独立；其他旧模板继续可用。
- `EmberEnemySilhouette` 是 `MonoBehaviour`，挂在池化敌人对象上；**必须容忍被复用**，不得在 `Awake` 里做一次性的类型假设。
- 角色标识与预警随敌人销毁，**不附着到复用的躯体上**——这是曾经的缺陷来源。
- `EmberEnemyVisual` 与行为策略为普通 C# 类，由 M3 构造与驱动。
- 创建/销毁所有权：M3 拥有敌人池；本模块只负责单个敌人的行为与外观。

## 影响与回归范围

- **直接影响模块**：M3（行为与弹幕宿主）、M1（数值来源）、M7（共享精灵与粒子）、M2（掉落计入 Score 与回血）。
- **必须复验的契约**：池复用的形态、Sprite、flipX 与显隐还原，皮肤持久引用与旧模板回落，分裂数量与配额关系、盾卫正背面倍率、引线爆炸时机、暗弹拦截、词缀概率门控。检查入口为 `Assets/Tests/EditMode/EmberArtSampleTests.cs`、`Assets/Tests/EditMode/EmberEnemySkinTests.cs`；主场景群战见 `Assets/Tests/PlayMode/EmberBossSwarmSmokeTests.cs`。
