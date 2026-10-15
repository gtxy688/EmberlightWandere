# M0 组合根（Composition Root）

> 相关需求：`../requirements.md` §2（单局主循环）、§7（技术约束）
> 代码：`Assets/Scripts/Core/EmberGame.cs`（唯一入口）+ `Assets/Scripts/Core/` 下的职责单元

## 职责边界

- **本模块负责**：
  - 持有**单局阶段**（`EmberRunPhase`）与**本局状态**（`EmberRunState`）；
  - 在 `Start` / `Awake` 中**装配**各单元，在 `Update` 中确定**每帧调用顺序**；
  - 把各单元的回调接到一起（`IEmberMenuHost` / `RunFlowController.Host` / `IEmberCombatHost` / `EmberInput.Presenter`）；
  - 场景级副作用：帧率设置、相机创建与配置、世界节点与灯芯的创建/销毁；
  - **把各单元的"结果"翻译成呈现与流程动作**：受伤结果 → 音效 + HUD 刷新 + 收局判定（`HurtPlayer`）、拾取结果 → 音效 + HUD（`PickHealingDrop`）、结算页文案拼装（`End`）、暂停/恢复时的面板开关（`Pause` / `SetPlaying` / `ReturnToCamp`）。
- **本模块不负责**：
  - 伤害与护盾结算 → `EmberKeeperVitals`
  - 选卡次数与补偿推进 → `RunFlowController`
  - 刷怪、波次、Boss 招式 → `EmberCombat` / `EmberBoss`（M3）
  - 构筑、卡池、品质 roll → `RunProgress`（M2）
  - 开局四步界面流程 → `EmberMenuFlow`（M6）
  - HUD 字段构成 → `EmberHudPresenter`（M6）
  - 数值 → `LevelConfig`（M1）

**最重要的边界约束**：`EmberGame` 只做装配、顺序与结果转译，**不要在这里放数值规则或状态推进规则**。

判断标准：新增逻辑如果需要"在某个条件下**算出数值**"或"在某个条件下**推进状态/阶段**"，它就不属于本模块，应落到 M1–M8；如果它只是把别人算好的结果**转成一个 UI 调用、一次音效或一次阶段切换请求**，那它属于本模块，搬走反而会把组合根拆散。

> ⚠️ 不要把这条读成"组合根里不许出现任何字符串或 if"。`End()` 拼结算文案、`HurtPlayer()` 在扣血后决定放什么音效，都是**合法的转译**——它们不产生新的产品规则，只是把已有结果送到 M6/M8。真正要拒绝的是"顺手把倍率、概率、上限也算在这里"。

## 依赖

| 被依赖模块/单元 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|-----------------|------------------|------|--------------|
| M1 `LevelConfig` | `Create` / `ApplyWorld` / `DifficultyName` / `BossWave` / `PicksAfterWave` | 建一局的关卡配置 | 未开局时 HUD 与结算不绘制 |
| M2 `RunProgress` | `ConfigureRun` / `Choices` / `Choose` / `TryRefresh` / `AddScore` | 本局构筑 | 为 null 时选卡入口直接返回 |
| M3 `EmberCombat` | `Begin` / `Tick` / `TearDown` / `AdvanceAfterUpgrade` / `Boss` / `PlayBossBurst` | 战斗推进 | 未 `Begin` 时 `Tick` 无效果 |
| M7 `EmberEffects` | `Initialize(阶段委托)` / `Nova` / `Impact` | 世界特效宿主 | 委托为空时特效不推进 |
| M6 `EmberUiRoot` / `EmberMenuUi` / `EmberMenuFlow` / `EmberHudPresenter` | `Bind` / `Show` / `Set` / `Stage` / `Boss` | 绑定场景界面并推送数据 | 缺少 UIRoot 或必需引用时报配置错误；绑定前不推送 |
| M7 `EmberKeeperAnimation` | `Initialize(阶段委托)` | 灯芯动画 | 委托为空时动画照常播放 |
| M8 `EmberAudio` | `Ensure().Play*` | 音乐与音效 | clip 缺失静默跳过 |

## 设计与数据流

`Awake` 创建本局状态容器、选卡流程与输入单元；`Start` 装配战斗、菜单与场景视图。`RunProgress`、关卡配置与世界对象在 `StartRunWith` 开局时创建。视图绑定顺序与场景引用见下文「Unity 装配」。

```text
EmberGame (MonoBehaviour, 组合根)
├─ EmberRunPhase            enum：11 个单局阶段
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

## 关键机制

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

### 结果转译

生命结算返回 `EmberVitalOutcome`，组合根再把结果接到音效、HUD 与收局入口；选卡流程通过宿主回调请求展示候选或恢复战斗。组合根负责写入阶段和调用呈现，数值结算与选卡推进仍归各自单元。结果页返回营地与暂停放弃共用内部 `ReturnToCamp`：回收战斗、销毁世界并清空本局状态，再显示主菜单。

## Unity 装配

主场景 `Assets/Scenes/Emberlight.unity` 的 `Emberlight Game` 对象挂载 `EmberGame`。两个序列化字段应绑定**场景中的 Prefab 实例组件**：

| 字段 | 组件与资产 | 用途 |
|------|------------|------|
| `uiRoot` | `EmberUiRoot`，`Assets/UI/UIRoot.prefab` | 主菜单、开局选择、HUD、升级与设置面板的引用入口 |
| `introRoot` | `EmberIntroView`，`Assets/UI/IntroRoot.prefab` | 加载页文字、进度条与继续提示的引用入口 |

启动与清理顺序：

1. `Start` 配置相机与音频，进入 Loading；字段未赋值时，查找场景已有实例（含未激活对象）。找不到任一根视图则抛出配置错误，不在运行时生成替代 UI。
2. 隐藏 `uiRoot`，在 `Emberlight Game` 上添加 `EmberIntro` 控制器，并调用 `Begin(introRoot, prepared, enter)`。`Begin` 校验加载页引用；字体经 `EmberFonts.CreateChinese` 内的闸门校验或回落后，才显示加载页。
3. 准备回调调用 `EmberMenuUi.Bind`，校验主 UI 引用、绑定字体和暂停回调，开始安全区监听，再创建 `EmberHudPresenter`。加载就绪后，继续输入隐藏加载页并由 `EmberMenuFlow` 显示主菜单。
4. 返回营地只清理本局世界与战斗，保留场景 UI 实例供复用；`OnDestroy` 解除安全区监听并恢复 `Time.timeScale`。

布局、静态图片与控件引用由 Prefab 维护；组合根负责绑定与生命周期协调，面板负责展示和交互。缺少必需控件引用时，`EmberUiRoot.ValidateReferences` / `EmberIntroView.ValidateReferences` 报错并提示 Editor 的 `Emberlight/UI/Connect UIRoot Runtime` 接线入口。

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
