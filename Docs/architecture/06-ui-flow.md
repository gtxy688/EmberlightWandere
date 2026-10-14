# M6 界面与流程

> 相关需求：`../requirements.md` §2（主循环）、§4.1（开局四步）、§7（UI 编辑与复用）
> 验收文档：`06-ui-flow-test.md`
> 代码：`Assets/Scripts/UI/`（面板、流程、展示模型与共享节点工厂）+ `Assets/Scripts/Core/EmberGame.cs`（组合根，持有阶段）
>
> 2026-09-23 变更：11 态状态机从 `EmberGame` 提为顶层 `EmberRunPhase`；HUD 推送点从 `EmberGame.UpdateHud()` 搬到 `EmberHudPresenter`；
> 安全区轮询改为 `EmberSafeAreaWatcher` 事件。**UI 侧仍然零 `EmberGame` 引用**（硬约束 6），新增的 `EmberHudPresenter` 也只接收入参。

## 职责边界

- **本模块负责**：主菜单、开局四步流程 UI、加载/标题页、战斗 HUD、升级选卡面板与卡面排版、设置与暂停浮层、开局武器库存说明文案。
- **本模块不负责**：
  - 游戏状态与规则 → M2 / M3（本模块**只读**）
  - 词条数值与卡池 → M2
  - 卡框/图标美术资源 → M7（本模块负责排版与映射）

**最重要的边界约束**：UI 类**不持有 `EmberGame`**。`EmberHud`、`EmberHudPresenter`、`EmberUpgradePanel`、`EmberGodsSelectPanel`、`EmberSettingsPanel`、`EmberIntro`、`EmberMenuUi`、`EmberMenuFlow`、`EmberCardMotion`、`EmberSafeAreaWatcher` 中零处 `EmberGame` 类型引用（仅注释提及）。UI 通过**方法参数**接收 `RunProgress` 等只读数据。**改 UI 时不要引入 `EmberGame` 引用。**

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M2 | `RunProgress`（作为方法参数）：`Luck` / `CoreWeaponId` / `RefreshesRemaining` / `EmptySlots` / `WeaponMagnitude` / `WeaponAttackSpeed` / `DamageAmp` / `OwnsWeapon`；id 常量 | 卡面数值预览、HUD 幸运与槽位 | 参数为空则该块不绘制 |
| M3 | 只读 `EnemyCount` / `Wave` / `WaveQuota` / `WaveSpawned` / `WaveRemaining` / `Boss` / `BossAlive` / `Elapsed` / `Kills` / `Health` / `Shield` | HUD 与结算 | 无战斗实例时隐藏战斗块 |
| M7 | `EmberArt` / `EmberUiArt` / `EmberCardFrames` / `EmberCardIcons` / `EmberWorldArt` | 全部精灵 | 缺图时回退程序化占位 |
| M8 | `EmberAudio.PlayUiClick` / `PlayUiConfirm` / `PlayCardOpen` / `PlayCardPick` | 交互音 | clip 缺失静默跳过 |
| M1 | `DifficultyName` / `WaveName` | 文案 | — |
| M0 | 阶段委托（`System.Func<EmberRunPhase>`）作为 `EmberHudPresenter` 的构造入参 | Boss 条是否显示 | 委托为空时 Boss 条隐藏 |

**回调方向**：UI 通过**委托/回调**把玩家意图交回组合根（如 `SelectUpgrade(offer)`、`TryRefreshOffer()`、`Pause()`、`BeginRun()`），而不是自己改阶段。
`EmberMenuFlow` 需要切阶段时走 `IEmberMenuHost.EnterMode(EmberRunPhase)`——**请求**，不是直接写。

## 设计与数据流

```text
EmberIntro + EmberIntroView（IntroRoot Prefab）→ 加载/标题 → 任意键继续
EmberUiRoot（场景 Prefab 引用，Canvas 540×960 / 安全区 / 摇杆 / 遮罩）
   ├─ EmberMenuUi         绑定视图、菜单按钮和安全区
   ├─ 主菜单 + 开局四步
   │     SelectLength → SelectDifficulty → SelectSlots → SelectRoster
   │        （EmberGodsSelectPanel 负责后两步：先选 N，再整库滚动单选 1 把）
   ├─ EmberHud            战斗 HUD（血量 / 护盾 / 波次 + 暂停键 + Boss 条 + 遭遇横幅）
   ├─ EmberUpgradePanel   升级选卡（≤4 张直排；>4 张走 ScrollRect）
   └─ EmberSettingsPanel  设置浮层（音乐/音效滑条 + 1x–5x 速度 + 继续/返回营地）
```

`EmberHudPresenter.Push()` 是 UI 的**唯一推送点**：每帧把只读数值喂给 `EmberHud`，UI 不主动拉取。
它由组合根在 `EmberMenuUi.Bind` 之后创建，只接收入参（`EmberHud` / `EmberRunState` / `EmberCombat` / 阶段委托），**不持有 `EmberGame`**。

> 重构前这段逻辑是 `EmberGame.UpdateHud()`，且**每帧都会被调用**（含暂停与结算态）。现在只在 `Playing` 阶段推送。
> 显示结果等价：暂停与结算时战斗栏本就处于隐藏态，且这些阶段的进入点各自会重绘界面。

### 开局四步（Gods-Select-v3）

1. **选征程长度**：25 / 50 波。
2. **选难度**：休闲 / 标准 / 困难。
3. **选槽位 N**：1–5，默认 2。
4. **单选 1 把起始武器**：整库滚动，武器卡显示白话说明；确认按钮显示所选武器名，未选择时禁用；确认后隐藏面板并**只提交一次**。滚动选择保留。

- **开局不可跳过**：已删除 Skip / `SkipGodsSelect` 入口。
- 武器说明（`EmberWeaponCatalog.Definition.Description`）使用白话，**禁止**出现「基线 0.20」「往返结算」「预警 AOE」「DPS」等术语：

开局与升级武器卡共用 Catalog 的基础说明，避免同一武器读到不同文案。

### UI 实例复用

- 主流程的菜单、开局选择、设置与升级控件来自 UIRoot Prefab；Initialize 注册监听，Hide 仅停用并清理业务回调，不销毁层级。
- Prefab 保存 8 个升级缓存槽位（不影响候选规则），覆盖现行候选上限；超出容量报告配置错误，运行时不扩容。显示时更新数据、重置滚动与动画，按钮监听只注册一次。
- 菜单复用按钮并替换当前动作；槽位与武器确认读取最新业务回调，隐藏后不能重复提交。
- 设置以 SetValueWithoutNotify 同步音量，重置二次确认，并替换业务回调。
- 隐藏取消选中动画，避免旧选项提交。
- HUD 保持常驻：顶部统计和右上暂停，中部生命与细护盾条，下部波次文字及细金色进度条。
- 回归范围：主菜单设置、战斗暂停、连续升级、刷新；字体闸门和切片配置不变。

### 升级选卡面板

- 候选张数 = `3 + 空位数`。
- **≤4 张直接排布；>4 张使用纵向滚动区域 + 固定 132 单位卡高**，保留底部刷新按钮（最多 7 张不溢出屏幕）。
- 卡面分区：标题 / 说明 / 数值预览。说明预留两行并自适应字号（12–15）；**移除省略号截断**。
- 护盾说明按语义固定两行，保留叠加、受伤优先消耗、仅本局有效的信息。
- 徽章：通用显示稀有度名；专属「专属」；质变「质变」；新武器「新武器」。
- `EmberCardMotion`：入场淡入、按压缩放、选中闪光，**全部使用 `unscaledDeltaTime`**（因 `GameSpeed` 走 `Time.timeScale`）。
- **全局选择锁**防止动画期间重复领取。

### HUD

- 复用简笔画面板；生命 / 护盾 / 波次保留条形显示。
- 显示 `武器 已有/N` 与幸运值。
- 顶栏删除与击杀信息重复的余烬展示；文字自适应避免越界。
- 暂停与 Boss 框同步描边。
- 遭遇事件触发时顶部横幅 2 秒（「遭遇 · 精英小队」/「遭遇 · 围杀圈」），暂停时不推进。
- 战斗状态栏在暂停、结算、升级时隐藏。

### 设置与暂停

- 标题「暂息灯塔」/「设置」；音乐与音效各一条滑条（细轨道 + 奶油色圆形滑块 + 橙色中心，外层透明 Image 保留整行触控区），实时显示四舍五入百分比，零值显示静音。
- 另含 **1x–5x 游戏速度**分段选择。
- 主菜单打开设置时底部按钮为「返回」。
- 战斗暂停后打开设置，底部提供「继续战斗」与「返回营地」。
- 「返回营地」**首次点击显示「再次点击确认放弃本局」**，再次点击才结束单局。

### 加载页

- `Assets/UI/IntroRoot.prefab` 保存标题、图标、提示、进度条、百分比和就绪标语；`EmberIntroView` 持有序列化引用，EmberIntro 只负责加载与继续输入。
- 进度条分段推进（约 0.15 → 0.55 → 1.0），最短展示约 5 秒。
- **中文字体就绪前隐藏中文文案**（避免渲染缺字方框）。
- 就绪后显示「烬灯行者 / EmberlightWandere / 提灯入夜，以火破晓。」并启用继续输入。
- 加载页按原排序覆盖主 UI；继续时隐藏实例，不销毁视图。布局、字号与静态图片不由运行时代码重新创建或覆盖。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberUiRoot` | MonoBehaviour | 序列化控件引用 | 菜单、HUD、面板视图 | 只持有视图；ValidateReferences 检查必需引用 |
| `EmberMenuUi.Bind` | 方法 | EmberUiRoot、已校验字体、暂停回调 | UI 根与面板 | 使用现有控件、注册交互；不重建布局；必要时补 EventSystem |
| `EmberHud.Set(...)` | 方法 | 生命、护盾、波次、`RunProgress` 等 | — | `RunProgress` 为空时不绘制该块 |
| `EmberUiFactory`（Editor） | 静态工厂 | 尺寸与父节点 | Prefab 控件 | 只在编辑器准备资产；Runtime 不引用 |
| `EmberUpgradePanel.Show` / `Preview` | 方法 | 候选数组、`RunProgress` | — | 候选为空不显示；>4 张自动滚动 |
| `EmberGodsSelectPanel.ShowSlots` / `ShowRosterSelect` | 方法 | 默认值/回调 | — | 单选 1 把；未选择时确认禁用 |
| `EmberGodsSelectPanel.RosterNames` / `RosterDescs` | 静态字段 | — | 名称与白话说明 | **被 `EmberFonts` 用于字形预热** |
| `EmberSettingsPanel` | 类 | 音量/速度回调 | — | 滑条实时生效并持久化 |
| `EmberIntroView` | MonoBehaviour | 序列化加载页控件 | 加载页视图 | 必需引用校验；不持有游戏宿主 |
| `EmberIntro.Begin` / `Continue` | 方法 | IntroRoot 视图、字体准备与进入回调 | 就绪状态 | 校验字体后激活；就绪前不能继续；继续仅提交一次并隐藏视图 |
| `EmberCardMotion` | `MonoBehaviour` | — | 卡动画 | 使用 `unscaledDeltaTime` |

### 文案的字体预热耦合

以下三处文案会被 `EmberFonts` 预取字形，**改文案必须同步确认预热范围**，否则首次显示会触发运行时字形烘焙卡顿：

- `EmberCopy.UpgradeNames` / `UpgradeDetails`（36 项）
- `EmberGodsSelectPanel.RosterNames` / `RosterDescs`
- `EmberRarityUtil.AllNames`（青铜/白银/黄金/钻石）

## Unity 装配

- 主场景持有 `Assets/UI/UIRoot.prefab` 实例，`EmberGame.uiRoot` 引用其 `EmberUiRoot`。主菜单默认可见，HUD/设置/选卡按需在 Prefab Mode 临时开启预览；运行时由面板统一切换状态。
- 主场景另持有 `Assets/UI/IntroRoot.prefab`，`EmberGame.introRoot` 引用 EmberIntroView。场景编辑时默认隐藏加载页，打开其 Prefab Mode 可预览；Play 时校验字体后显示。
- `Emberlight/UI/Connect UIRoot Runtime` 使用 Editor API 配置两套 Prefab 的引用与持久图片，首次生成加载页，关联当前主场景并标记待保存。已有 IntroRoot 不重新创建控件、覆盖锚点或字号；日常改布局无需重跑菜单。
- 布局与静态图片归 Prefab；运行时更新文字、交互状态、按候选数量排卡与安全区。旧 EmberMenuUi.Build、HUD 构建构造函数及面板创建回退已移除。EmberUiFactory 与开局选择页补建归 Editor，Runtime 只绑定序列化控件；缺失引用明确报错。
- 当前布局以竖屏为目标，PlayerSettings 锁定 Portrait（见 M9）；横屏适配未实现。
- 面板的生命周期由 `EmberMenuUi` 统一持有；暂停与设置是浮层而非独立场景。
- `EmberUiRoot`、`EmberGodsSelectPanel`、`EmberSettingsPanel` 与 `EmberUpgradePanel` 为 `MonoBehaviour`；EmberMenuUi 与 EmberHud 为普通类。
- **安全区变化**由 `EmberSafeAreaWatcher` 观察（`RuntimeInitializeOnLoadMethod` 安装常驻监听对象，`LateUpdate` 采样）。
  `EmberMenuUi.Bind` 后调用 `WatchSafeArea()` 订阅，`EmberGame.OnDestroy` 调用 `UnwatchSafeArea()` 退订。
  静态事件在 `SubsystemRegistration` 阶段重置，避免关闭 Domain Reload 时残留。

## 影响与回归范围

- **直接影响模块**：M2（卡面读取构筑数据）、M3（HUD 读取战斗状态）、M7（精灵供给）、M8（交互音）。
- **必须复验的契约**：开局四步的顺序与单选约束、候选卡数量与滚动阈值、`unscaledDeltaTime` 使用、返回营地的二次确认、字体预热覆盖。
- UI 创建边界需复验缺失引用报错、Editor 配置工具与场景 Prefab 复用；HUD 缓存需复验秒/数值变化及时显示及波次文案不重复写入。
- **对应验收项**：`06-ui-flow-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| UI 迁移到 Prefab | 已按 jack 授权接入 UIRoot；主流程使用序列化视图 | 主场景须保留一个有效的 EmberUiRoot 实例 | 已实施（2026-10-14） |
| 是否补小屏适配（<540×960） | 引入 CanvasScaler 匹配 / 维持硬编码锚点 | 现有锚点按 540×960 硬编码，窄屏可能越界 | xiaoUI |
| 返回营地二次确认是否有超时复位 | 加 2s 超时 / 维持需再次点击 | 误触后可能长期停留在确认态 | xiaoUI |

性能定位标记见 `03-combat-waves.md` 的「性能定位标记」；本次仅新增采样作用域，公开行为不变。

## 2026-10-13 对外契约与回归范围

编辑预览与运行时使用同一 `Assets/UI/UIRoot.prefab` 实例；图片修复菜单与资源持久化契约见 `07-art-presentation.md` 的「UI Prefab 图片持久化」。2026-10-14 已接入 EmberMenuUi.Bind，面板数据与回调由各控制器绑定，UI 不持有游戏组合根。

`EmberOfferPresentation.Build` 将候选与只读构筑转成标题、说明、数字预览与徽章；面板只绑定展示结果、布局与交互。Editor 的 EmberUiFactory 创建共用 Image/TMP 节点；已保存的 Prefab 是布局入口。武器卡按 ID 读 Catalog；数字预览读共享配置，修正旧 ID 错位和专属/质变的过期预览，实际玩法数值不变。

### HUD 更新契约（2026-10-14）

EmberHud 缓存上次显示的生命、护盾、秒数、击杀、武器数/槽位/幸运、波次与 Boss 阶段。条形值跟随状态，文字只在对应数据变化时格式化并赋值；Stage 是波次文字唯一写入点。EmberHudPresenter 只在波次、局长或关卡配置变化时查询波名。暂停/选卡后恢复仍反映最新状态；不以源码缓存推断已达到某个 FPS/GC 指标。

回归包含起始武器与升级卡说明一致、单武器攻击/攻速标题、卡片不同数量重排、隐藏取消动画、最新回调只提交一次、字体覆盖与真实网格渲染。对应 `EmberOfferPresentationTests`、`EmberRuntimeSmokeTests`，运行结果见 `../实现说明.md`；审美与手感仍待 jack 验收。
