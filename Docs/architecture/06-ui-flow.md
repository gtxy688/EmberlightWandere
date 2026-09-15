# M6 界面与流程

> 相关需求：`../requirements.md` §2（主循环）、§4.1（开局四步）
> 验收文档：`06-ui-flow-test.md`
> 代码：`Assets/Scripts/UI/`（7 个文件）+ `Assets/Scripts/Core/EmberGame.cs`（11 态状态机）

## 职责边界

- **本模块负责**：主菜单、开局四步流程 UI、加载/标题页、战斗 HUD、升级选卡面板与卡面排版、设置与暂停浮层、开局武器库存说明文案。
- **本模块不负责**：
  - 游戏状态与规则 → M2 / M3（本模块**只读**）
  - 词条数值与卡池 → M2
  - 卡框/图标美术资源 → M7（本模块负责排版与映射）

**最重要的边界约束**：UI 类**不持有 `EmberGame`**。`EmberHud`、`EmberUpgradePanel`、`EmberGodsSelectPanel`、`EmberSettingsPanel`、`EmberIntro`、`EmberMenuUi`、`EmberCardMotion` 中零处 `EmberGame` 引用（仅一处注释提及）。UI 通过**方法参数**接收 `RunProgress` 等只读数据。**改 UI 时不要引入 `EmberGame` 引用。**

## 依赖

| 被依赖模块 | 只使用的公开契约 | 用途 | 缺失时的行为 |
|------------|------------------|------|--------------|
| M2 | `RunProgress`（作为方法参数）：`Luck` / `CoreWeaponId` / `RefreshesRemaining` / `EmptySlots` / `WeaponMagnitude` / `WeaponAttackSpeed` / `DamageAmp` / `OwnsWeapon`；id 常量 | 卡面数值预览、HUD 幸运与槽位 | 参数为空则该块不绘制 |
| M3 | 只读 `EnemyCount` / `Wave` / `WaveQuota` / `WaveSpawned` / `WaveRemaining` / `Boss` / `BossAlive` / `Elapsed` / `Kills` / `Health` / `Shield` | HUD 与结算 | 无战斗实例时隐藏战斗块 |
| M7 | `EmberArt` / `EmberUiArt` / `EmberCardFrames` / `EmberCardIcons` / `EmberWorldArt` | 全部精灵 | 缺图时回退程序化占位 |
| M8 | `EmberAudio.PlayUiClick` / `PlayUiConfirm` / `PlayCardOpen` / `PlayCardPick` | 交互音 | clip 缺失静默跳过 |
| M1 | `DifficultyName` / `WaveName` | 文案 | — |

**回调方向**：UI 通过**委托/回调**把玩家意图交回 `EmberGame`（如 `SelectUpgrade(offer)`、`TryRefreshOffer()`、`Pause()`、`BeginRun()`），而不是自己改状态。

## 设计与数据流

```text
EmberMenuUi（UI 根容器，持有 Canvas 540×960 / EventSystem / 安全区 / 摇杆 / 遮罩）
   ├─ EmberIntro          加载页 → 标题 → 任意键继续
   ├─ 主菜单 + 开局四步
   │     SelectLength → SelectDifficulty → SelectSlots → SelectRoster
   │        （EmberGodsSelectPanel 负责后两步：先选 N，再整库翻页单选 1 把）
   ├─ EmberHud            战斗 HUD（血量 / 护盾 / 波次 + 暂停键 + Boss 条 + 遭遇横幅）
   ├─ EmberUpgradePanel   升级选卡（≤4 张直排；>4 张走 ScrollRect）
   └─ EmberSettingsPanel  设置浮层（音乐/音效滑条 + 1x–5x 速度 + 继续/返回营地）
```

`EmberGame.UpdateHud()` 是 UI 的**唯一推送点**：每帧把只读数值喂给 HUD，UI 不主动拉取。

### 开局四步（Gods-Select-v3）

1. **选征程长度**：25 / 50 波。
2. **选难度**：休闲 / 标准 / 困难。
3. **选槽位 N**：1–5，默认 2。
4. **单选 1 把起始武器**：整库翻页，武器卡显示白话说明；确认按钮显示所选武器名，未选择时禁用；确认后隐藏面板并**只提交一次**。跨页选择保留。

- **开局不可跳过**：已删除 Skip / `SkipGodsSelect` 入口。
- 武器说明（`RosterDescs`）使用白话，**禁止**出现「基线 0.20」「往返结算」「预警 AOE」「DPS」等术语：

| 武器 | 文案 |
|------|------|
| 火球 | 自己会打火球，好上手 |
| 环火 | 火团围着你转，碰到就烫 |
| 燃地 | 走过的路着火，怪踩了掉血 |
| 穿透火矢 | 站住才开火，一箭穿一串 |
| 回旋烬蝶 | 丢出去再飞回来，来回都能打 |
| 天降火雨 | 先画圈再砸火，打一片 |

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

- 进度条分段推进（约 0.15 → 0.55 → 1.0），最短展示约 5 秒。
- **中文字体就绪前隐藏中文文案**（避免渲染缺字方框）。
- 就绪后显示「烬灯行者 / EmberlightWandere / 提灯入夜，以火破晓。」并启用继续输入。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberMenuUi` | 类 | 回调组 | UI 根与面板 | 建 Canvas / EventSystem / 安全区 |
| `EmberHud.Set(...)` | 方法 | 生命、护盾、波次、`RunProgress` 等 | — | `RunProgress` 为空时不绘制该块 |
| `EmberHud.Box` / `Bar` / `Text` | 静态工厂 | 尺寸与父节点 | uGUI 组件 | 被 `EmberIntro` 复用 |
| `EmberUpgradePanel.Show` / `Preview` | 方法 | 候选数组、`RunProgress` | — | 候选为空不显示；>4 张自动滚动 |
| `EmberGodsSelectPanel.ShowSlots` / `ShowRosterSelect` | 方法 | 默认值/回调 | — | 单选 1 把；未选择时确认禁用 |
| `EmberGodsSelectPanel.RosterNames` / `RosterDescs` | 静态字段 | — | 名称与白话说明 | **被 `EmberFonts` 用于字形预热** |
| `EmberSettingsPanel` | 类 | 音量/速度回调 | — | 滑条实时生效并持久化 |
| `EmberIntro` | 类 | 就绪回调 | — | 字体未就绪不显示中文 |
| `EmberCardMotion` | `MonoBehaviour` | — | 卡动画 | 使用 `unscaledDeltaTime` |

### 文案的字体预热耦合

以下三处文案会被 `EmberFonts` 预取字形，**改文案必须同步确认预热范围**，否则首次显示会触发运行时字形烘焙卡顿：

- `EmberGame.UpgradeNames` / `UpgradeDetails`（36 项）
- `EmberGodsSelectPanel.RosterNames` / `RosterDescs`
- `EmberRarityUtil.AllNames`（青铜/白银/黄金/钻石）

## Unity 装配

- **全部 UI 为运行时构建**：`EmberMenuUi` 以代码创建 Canvas（540×960 参考分辨率）、`EventSystem`、安全区适配与摇杆；**场景中无预制 UI 层级**，因此 UI 改动不需要动场景。
- 锚点按竖屏硬编码 → **横屏会错乱**，故 PlayerSettings 锁定仅 Portrait（见 M9）。
- 面板的生命周期由 `EmberMenuUi` 统一持有；暂停与设置是浮层而非独立场景。
- `EmberSettingsPanel` 与 `EmberUpgradePanel` 为 `MonoBehaviour`；其余为普通类或静态工厂。

## 影响与回归范围

- **直接影响模块**：M2（卡面读取构筑数据）、M3（HUD 读取战斗状态）、M7（精灵供给）、M8（交互音）。
- **必须复验的契约**：开局四步的顺序与单选约束、候选卡数量与滚动阈值、`unscaledDeltaTime` 使用、返回营地的二次确认、字体预热覆盖。
- **对应验收项**：`06-ui-flow-test.md`。

## 待裁定事项

| 问题 | 候选方案 | 影响 | 需要谁裁定 |
|------|----------|------|------------|
| UI 是否迁移到 Prefab / UI Toolkit | 运行时构建 / Prefab / UI Toolkit | 当前代码构建难以在 Inspector 调整，但零场景依赖 | jack + xiaoCoder |
| 是否补小屏适配（<540×960） | 引入 CanvasScaler 匹配 / 维持硬编码锚点 | 现有锚点按 540×960 硬编码，窄屏可能越界 | xiaoUI |
| 返回营地二次确认是否有超时复位 | 加 2s 超时 / 维持需再次点击 | 误触后可能长期停留在确认态 | xiaoUI |
