# M0 组合根（Composition Root）

> 相关需求：`../requirements.md` §2（单局主循环）、§7（技术约束）
> 验收文档：`00-composition-root-test.md`
> 代码：`Assets/Scripts/Core/EmberGame.cs`（唯一入口）+ `Assets/Scripts/Core/` 下的职责单元
> 本模块为 2026-09-23 重构引入；重构前这些职责全部挤在 `EmberGame`（361 行，见 `progress.md`）。

## 职责边界

- **本模块负责**：
  - 持有**局长阶段**（`EmberRunPhase`）与**本局状态**（`EmberRunState`）；
  - 在 `Start` / `Awake` 中**装配**各单元，在 `Update` 中确定**每帧调用顺序**；
  - 把各单元的回调接到一起（`IEmberMenuHost` / `RunFlowController.Host` / `IEmberCombatHost` / `EmberInput.Presenter`）；
  - 场景级副作用：帧率设置、相机创建与配置、世界节点与灯芯的创建/销毁。
- **本模块不负责**：
  - 伤害与护盾结算 → `EmberKeeperVitals`
  - 选卡次数与补偿推进 → `RunFlowController`
  - 刷怪、波次、Boss 招式 → `EmberCombat` / `EmberBoss`（M3）
  - 构筑、卡池、品质 roll → `RunProgress`（M2）
  - 开局四步界面流程 → `EmberMenuFlow`（M6）
  - HUD 字段构成 → `EmberHudPresenter`（M6）
  - 数值 → `LevelConfig`（M1）

**最重要的边界约束**：`EmberGame` 只做装配与顺序，**不要再往里加业务规则**。判断标准：新增逻辑如果需要"在某个条件下改变数值或推进状态"，它就不属于本模块。

## 依赖

| 被依赖模块/单元 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|-----------------|------------------|------|--------------|
| M1 `LevelConfig` | `Create` / `ApplyWorld` / `DifficultyName` / `BossWave` / `PicksAfterWave` | 建一局的关卡配置 | 未开局时 HUD 与结算不绘制 |
| M2 `RunProgress` | `ConfigureRun` / `Choices` / `Choose` / `TryRefresh` / `AddScore` | 本局构筑 | 为 null 时选卡入口直接返回 |
| M3 `EmberCombat` | `Begin` / `Tick` / `TearDown` / `AdvanceAfterUpgrade` / `Boss` / `PlayBossBurst` | 战斗推进 | 未 `Begin` 时 `Tick` 无效果 |
| M4 `EmberEffects` | `Initialize(阶段委托)` / `Nova` / `Impact` | 世界特效宿主 | 委托为空时特效不推进 |
| M6 `EmberUiRoot` / `EmberMenuUi` / `EmberMenuFlow` / `EmberHudPresenter` | `Bind` / `Show` / `Set` / `Stage` / `Boss` | 绑定场景界面并推送数据 | 缺少 UIRoot 或必需引用时报配置错误；绑定前不推送 |
| M7 `EmberKeeperAnimation` | `Initialize(阶段委托)` | 灯芯动画 | 委托为空时动画照常播放 |
| M8 `EmberAudio` | `Ensure().Play*` | 音乐与音效 | clip 缺失静默跳过 |

## 设计与数据流

`Start` 取得序列化的 `uiRoot` 与 `introRoot`（旧场景未赋值时只查找已有实例），先隐藏主 UI。`EmberIntro.Begin(introRoot, prepared, enter)` 在字体闸门校验后显示已有加载页，准备回调调用 `EmberMenuUi.Bind`，随后创建 HUD presenter、接菜单流程与安全区监听；就绪后的继续输入隐藏加载页并进入菜单。组合根不创建主 UI 或加载页视图；缺少视图时不静默回落到代码构建。

```text
EmberGame (MonoBehaviour, 组合根)
├─ EmberRunPhase            enum：11 个局长阶段
├─ EmberRunState            本局数据 + 世界/灯芯/相机/特效句柄
│    └─ EmberKeeperVitals   生命 / 护盾吸收 / 无敌帧 / 回血 / 掉落拾取
├─ RunFlowController        波次选卡次数、新武器补偿、刷新
├─ EmberInput               触屏/鼠标 → 摇杆向量（经 Presenter 回调）
├─ EmberKeeperMotion        位移 + 边界钳制 + 相机跟随（静态）
├─ EmberHudPresenter        每帧把只读状态推给 EmberHud
├─ EmberMenuUi              界面根（M6）
├─ EmberMenuFlow            开局四步（M6）
└─ EmberCombat              战斗与波次（M3）
```

### 每帧顺序（`Update`，仅 Playing 阶段）

```text
Elapsed += dt  →  Vitals.Tick(dt)  →  input.Tick()
                                        ↓ 若阶段已改变则中止本帧
                  EmberKeeperMotion.Step()
                                        ↓
                  combat.Tick(dt)
                                        ↓ 若阶段已改变则中止本帧
                  hud.Push()
```

**顺序不可随意调整**，原因：

1. `Vitals.Tick` 必须在 `combat.Tick` 之前——否则接触伤害读到的无敌帧是上一帧的；
2. `input.Tick()` 里可能触发 `Pause()`，所以之后要重新检查阶段；
3. `combat.Tick` 可能因灯芯阵亡或收局改变阶段，`hud.Push()` 前必须再查一次；
4. 无敌帧**只在 Playing 阶段递减**（保持重构前口径）：暂停与选卡期间不会"白送"无敌帧。

### 阶段所有权

`Phase` 只有组合根能写（`private set`）。其他模块通过各自契约**请求**切换：

| 请求方 | 契约 | 目标阶段 |
|--------|------|----------|
| `EmberMenuFlow` | `IEmberMenuHost.EnterMode` | Menu / SelectLength / SelectDifficulty / SelectSlots / SelectRoster |
| `RunFlowController` | `Host.OnUpgradeOffered` | Upgrade |
| `RunFlowController` | `Host.OnUpgradeSequenceFinished` | Playing（经 `SetPlaying`） |
| 生命结算调用方 | `HurtPlayer` → `CommitHealthChange` → `End` | Lost |
| `EmberCombat` | `IEmberCombatHost.EndRun` | Won / Lost |

## 对外契约

`EmberKeeperVitals` 构造时只注入构筑、伤害倍率与阶段提供者；不持有 `EmberRunState`，不引用 UnityEngine 或音频。受伤结果由组合根转换为音效、HUD 与收局动作。`RunFlowController.Host` 只暴露 `Progress`、`FinalWave`、`PicksAfterWave`、阶段、候选与呈现回调；不暴露场景状态，不包含死亡回调。


| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberGame.Phase` | 属性 | — | `EmberRunPhase` | 仅组合根可写；是全局唯一阶段来源 |
| `EmberGame.Run` | 属性 | — | `EmberRunState` | `Awake` 后非空；`ReturnToCamp` 后 `IsActive == false` |
| `EmberGame.Elapsed` / `.Progress` | 属性 | — | 转发 `Run` 上的字段 | 未开局时分别为 0 / null |
| `EmberRunState.Vitals` | 属性 | — | `EmberKeeperVitals` | 每局同一实例，`Clear()` 时 `Reset()` |
| `EmberKeeperVitals.Hurt` | 方法 | 伤害值 | `EmberVitalOutcome` | `Rejected` = 阶段不符或无敌帧；倍率只乘一次；护盾先吃 |
| `EmberKeeperVitals.Heal` / `PickHealingDrop` | 方法 | 治疗量 | `EmberVitalOutcome` | 溢出丢弃；满血拾取返回 `NoChange` 但仍算拾取 |
| `RunFlowController.OnWaveCleared` | 方法 | 已清波次 | — | 非 Playing 或未开局时不动 |
| `RunFlowController.Choose` | 方法 | 候选卡 | `bool` | 卡不在当前候选内返回 false；补偿选卡**不连锁** |
| `IEmberCombatHost` | 接口 | — | 见 `03-combat-waves.md` | **实现方不需要是 `EmberGame`** |
| `EmberInput` | 类 | `Presenter` | `Stick` / `IsDragging` | 不持有 `EmberGame` |

## 影响与回归范围

- **直接影响模块**：M1–M8 全部（组合根是唯一的装配点）。
- **必须复验的契约**：
  - 每帧顺序（尤其无敌帧递减时机与阶段二次检查）；
  - 生命结算口径：难度倍率只乘一次、护盾先于生命、溢出丢弃、满血拾取仍播提示音；
  - 选卡：`3 + 空位` 候选数、同屏唯一、新武器补偿不连锁、免费刷新 2 次；
  - 阶段流转：加载页 → 主菜单 → 四步开局 → 战斗 → 升级 → 暂停 → 结算；
  - 输入：摇杆底盘定位、下半屏触发区、Esc 暂停。
- **对应验收项**：`00-composition-root-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| `EmberInput` / `EmberHudPresenter` 目前仍在 `Core/`、`UI/` 而非独立 asmdef | 建 `Runtime` asmdef 分目录 / 维持单程序集 | 当前已有 Runtime / Editor / Tests asmdef，Runtime 内模块依赖仍靠契约约束 | jack + xiaoCoder |
| `EmberRunState` 引用 `Transform` / `Camera`，不能脱离 Unity 单测 | 拆成"纯数据 + 场景句柄"两层 / 维持现状 | 拆开后可单测的面更大，但引入一层间接 | jack + xiaoCoder |

2026-10-13：结果页返回营地与暂停放弃共用 `ReturnToCamp`，执行战斗回收、世界销毁与 Run.Clear 后显示主菜单；不再只隐藏结束局的世界。回归包含阶段 Menu、Run.IsActive=false、世界/玩家/特效引用清空。
