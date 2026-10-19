# M6 界面与流程

战斗 HUD 的 `HudReferences.Buffs` 绑定 UIRoot 内的 `EmberBuffHud`。它管理序列化图标槽位和有限浮字标签，Presenter 传入只读 Buff 列表、相机和玩家位置；`EmberBuffPresentation` 从配置生成简短效果文案，不重复保存效果数值。环逐帧读取剩余比例，秒数变化时才更新文字，不修改 Buff。Editor 的 `Update Buff HUD` 入口负责字体与控件迁移，运行时不创建替代控件。

> 相关需求：`../requirements.md` §2（主循环）、§4.1（开局四步）、§7（UI 编辑与复用）
> 代码：`Assets/Scripts/UI/`（面板、流程与展示模型）+ `Assets/Editor/`（控件工厂与 Prefab 工具）+ `Assets/Scripts/Core/EmberGame.cs`（组合根，持有阶段）

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
| M2 | `RunProgress`（作为方法参数）：`Luck` / `CoreWeaponId` / `RefreshesRemaining` / `EmptySlots` / `WeaponMagnitude` / `WeaponAttackSpeed` / `DamageAmp` / `OwnsWeapon` / `Shield` / `ShieldCapacity`；id 常量 | 卡面数值预览、HUD 幸运、槽位与护盾 | 参数为空时省略构筑统计、护盾归零 |
| M3 | 战斗的波次、剩余敌人、Boss 与遭遇文本 | HUD 战斗块 | 无战斗时跳过 Boss 更新 |
| M10 | `BuffManager.Active`、`Buff.Config` / `Remaining`、施加的新增 / 刷新结果 | 效果说明、剩余时间与短提示 | 列表为空时隐藏效果区 |
| M0 状态容器 | `EmberRunState` 的 `Health` / `MaxHealth` / `Elapsed` / `Kills`，护盾从 `Progress` 查询 | HUD 基础状态 | 缺少状态或构筑时跳过推送 |
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

`EmberHudPresenter.Push()` 是战斗 HUD 的**每帧推送入口**：每帧把只读数值喂给 `EmberHud`，UI 不主动拉取。
它由组合根在 `EmberMenuUi.Bind` 之后创建，只接收入参（`EmberHud` / `EmberRunState` / `EmberCombat` / 阶段委托），**不持有 `EmberGame`**。

HUD 缓存上次显示的生命、护盾、秒数、击杀、武器/槽位/幸运、波次与 Boss 阶段；条形值持续跟随状态，文字只在对应数据变化时格式化。`Stage` 是波次文字唯一写入点，Presenter 仅在波次、征程或配置变化时查询波名。暂停和选卡后恢复须反映最新状态，缓存实现不等于已达到某个 FPS/GC 指标。


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
- HUD 保持常驻：顶部统计和右上暂停，中部生命与独立护盾条，下部波次文字及细金色进度条。
- 回归范围：主菜单设置、战斗暂停、连续升级、刷新；字体闸门和切片配置不变。

### 升级选卡面板

- 候选张数 = `3 + 空位数`。
- **≤4 张直接排布；>4 张使用纵向滚动区域与固定卡高**，卡高与行步长见 [需求 §7「UI 与音频默认参数」](../requirements.md#ui-与音频默认参数)。保留底部刷新按钮（最多 7 张不溢出屏幕）。
- 卡面分区：标题 / 说明 / 数值预览。说明预留两行并自适应字号，具体字号由面板与 Prefab 定义；**移除省略号截断**。
- 护盾说明按语义固定两行，保留叠加、受伤优先消耗、仅本局有效的信息。
- 徽章：通用显示稀有度名；专属「专属」；质变「质变」；新武器「新武器」。
- `EmberCardMotion`：入场淡入、按压缩放、选中闪光，**全部使用 `unscaledDeltaTime`**（因 `GameSpeed` 走 `Time.timeScale`）。
- **全局选择锁**防止动画期间重复领取。

### HUD

- 战斗栏使用手绘九宫格面板，统计、生命、护盾、波次各自分行，整体高度不变；生命 / 护盾 / 波次仍为真实填充条，文字和暂停按钮不烘入图片。菜单及其他面板仍沿用既有风格。
- 生命条下方的护盾条使用蓝色填充，条内居中显示数值。`EmberHud.Set` 只读 `Shield / ShieldCapacity` 显示剩余量与本局累计获得量的比例，不改变承伤或叠加规则；归零将文字转灰降透明度，获得护盾恢复构造时缓存的 Prefab 配色。表现默认值见需求 §7。
- 显示 `武器 已有/N` 与幸运值。
- 顶栏删除与击杀信息重复的余烬展示；文字自适应避免越界。
- 顶部手绘面板、横幅和 Boss 条的位置保存在 `Assets/UI/UIRoot.prefab`。布局默认值统一见需求 §7，运行时绑定不覆盖。
- `EmberShieldHudSetup.Configure` 在 Editor 内迁移既有护盾条与数值，移除旧护盾图标及其预留空间；迁移后只重绑引用，保留人工排版。HUD 重绑优先复用序列化文字引用，避免数值移入护盾条后因直接子节点顺序变化而错绑。
- 遭遇事件触发时显示顶部短时横幅（「遭遇 · 精英小队」/「遭遇 · 围杀圈」），暂停时不推进。
- 战斗状态栏在暂停、结算、升级时隐藏。
- Buff 图标横排在顶部 HUD 下方，Boss 与遭遇横幅各有独立位置。每个槽位按配置 Id 绑定图标、径向 Filled 环和秒数；到期隐藏，布局组收起空位。新增 Buff 须给 Prefab 增加同 Id 的槽位与美术，未绑定的 Id 明确报告配置错误。
- 组合根把 `BuffManager.Add` 的新增 / 刷新结果交给 Presenter，在角色上方显示效果名称和修正百分比，刷新有独立前缀。浮字复用预存标签，同帧多次拾取分别显示，满时替换最早一条；`TickFeedback` 使用非缩放时间上移并淡出，参数见需求 §7。隐藏 HUD 清空浮字，恢复时图标仍反映当前 Buff。
- 浮字位置由 Presenter 传入相机与角色位置，经屏幕坐标转换为安全区局部坐标，不从 UI 查询游戏入口；外层浮字容器没有背景 Image 或射线拦截。`EmberBuffHud` 没有自行推进的 Update，计时和位置由现有推送入口驱动。

### 设置与暂停

- 标题「暂息灯塔」/「设置」；音乐与音效各一条滑条（细轨道 + 奶油色圆形滑块 + 橙色中心，外层透明 Image 保留整行触控区），实时显示四舍五入百分比，零值显示静音。
- 另含 **1x–5x 游戏速度**分段选择。
- 主菜单打开设置时底部按钮为「返回」。
- 战斗暂停后打开设置，底部提供「继续战斗」与「返回营地」。
- 「返回营地」**首次点击显示「再次点击确认放弃本局」**，再次点击才结束单局。

### 加载页

- `Assets/UI/IntroRoot.prefab` 保存标题、图标、提示、进度条、百分比和就绪标语；`EmberIntroView` 持有序列化引用，EmberIntro 只负责加载与继续输入。
- 进度条分段推进并满足最短展示时间；表现参数见需求 §7，由 `EmberIntro.Load` 定义。
- **中文字体就绪前隐藏中文文案**（避免渲染缺字方框）。
- 就绪后显示「烬灯行者 / EmberlightWanderer / 提灯入夜，以火破晓。」并启用继续输入。
- 加载页按原排序覆盖主 UI；继续时隐藏实例，不销毁视图。布局、字号与静态图片不由运行时代码重新创建或覆盖。

## 对外契约

| 名称 | 类型 | 输入 | 输出 | 保证 |
|------|------|------|------|------|
| `EmberUiRoot` | MonoBehaviour | 序列化控件引用 | 菜单、HUD、面板视图 | 只持有视图；ValidateReferences 检查必需引用 |
| `EmberMenuUi.Bind` | 方法 | EmberUiRoot、已校验字体、暂停回调 | UI 根与面板 | 使用现有控件、注册交互；不重建布局；必要时补 EventSystem |
| `EmberHud.Set(hp, max, progress, seconds, kills, waveRemaining, waveQuota)` | 方法 | 生命、构筑、计时、击杀与波次计数 | — | 只读构筑；护盾随剩余比例填充、归零置灰，空构筑显示空护盾；波次编号与文字由 `Stage` 单独写入 |
| `EmberHud.Buffs` / `NotifyBuff` / `PositionBuffFeedback` / `TickFeedback` | 方法 | 只读列表 / 配置与刷新标记 / 相机与角色位置 / 非缩放时间 | 转发给图标与浮字组件 | 剩余时间来自 Buff；浮字与效果计时分离，不修改玩法数据 |
| `EmberBuffHud` | MonoBehaviour | 已装配的图标与浮字引用 | 径向环、秒数、角色浮字 | 缺引用或槽位 Id 不合法时报错，不动态补建；秒数缓存、环持续更新；隐藏清空反馈 |
| `EmberUiFactory`（Editor） | 静态工厂 | 尺寸与父节点 | Prefab 控件 | 只在编辑器准备资产；Runtime 不引用 |
| `EmberUpgradePanel.Show` | 方法 | 候选数组、`RunProgress` | — | 候选为空不显示；>4 张自动滚动 |
| `EmberGodsSelectPanel.ShowSlots` / `ShowRoster` | 方法 | 默认值/回调 | — | 单选 1 把；未选择时确认禁用 |
| `EmberSettingsPanel` | 类 | 音量/速度回调 | — | 滑条实时生效并持久化 |
| `EmberIntroView` | MonoBehaviour | 序列化加载页控件 | 加载页视图 | 必需引用校验；不持有游戏宿主 |
| `EmberIntro.Begin` / `Continue` | 方法 | IntroRoot 视图、字体准备与进入回调 | 就绪状态 | 校验字体后激活；就绪前不能继续；继续仅提交一次并隐藏视图 |
| `EmberCardMotion` | `MonoBehaviour` | — | 卡动画 | 使用 `unscaledDeltaTime` |

### 文案与字形来源

武器名称和基础说明读取 `EmberWeaponCatalog`；卡面、菜单与开局说明读取 `EmberCopy`。`EmberFonts.CardGlyphSet()` 收集升级文案、品质名称和卡面补充字符，`MenuGlyphSet()` 收集菜单、开局选择与结算常量。

修改文案后按 M7 的流程检查编译后字形覆盖并重新烘制静态图集。`EmberOfferPresentation.Build` 将候选与只读构筑转成标题、说明、数字预览与徽章；数字读取共享参数，面板只绑定展示结果。

卡面预览与徽章直接调用 `EmberOfferPresentation`；`EmberUpgradePanel` 不再提供同义转发入口。`Show` 的刷新回调为可选参数，保留单一展示入口；设置面板直接刷新音量与倍速文字，不缓存只包装方法调用的委托。

描述明确触发方式、作用对象和强化结果。攻击力/攻速标题带武器名；攻速说明区分环火旋转、燃地铺设与其他武器的攻击间隔。通用卡预览展示当前值与选择后值，幸运按构筑上限钳制；专属和质变说明对应实际行为，预览查询共享调参常量，不改变玩法。延烧的间隔同时计入当前燃地攻速；日冕的伤害标明基础值，避免被理解为叠加构筑后的最终伤害。完整与紧凑卡面均需检查字形和文本溢出。

## Unity 装配

- 主场景持有 `Assets/UI/UIRoot.prefab` 实例，`EmberGame.uiRoot` 引用其 `EmberUiRoot`。主菜单默认可见，HUD/设置/选卡按需在 Prefab Mode 临时开启预览；运行时由面板统一切换状态。
- 主场景另持有 `Assets/UI/IntroRoot.prefab`，`EmberGame.introRoot` 引用 EmberIntroView。场景编辑时默认隐藏加载页，打开其 Prefab Mode 可预览；Play 时校验字体后显示。
- `Emberlight/UI/Connect UIRoot Runtime` 使用 Editor API 配置两套 Prefab 的引用与持久图片，首次生成加载页，关联当前主场景并标记待保存。已有 IntroRoot 不重新创建控件、覆盖锚点或字号；日常改布局无需重跑菜单。
- 布局与静态图片归 Prefab；运行时更新文字、交互状态、按候选数量排卡与安全区。EmberUiFactory 与开局选择页补建归 Editor，Runtime 只绑定序列化控件；缺失引用明确报错。
- `Update Buff HUD` 经 `EmberBuffHudSetup` 首次迁移旧文字面板，生成 `Battle HUD/Buff icons` 与安全区下的 `Buff floating`，并错开 Boss / 遭遇横幅；重复运行绑定原组件，保留已有布局。图标和环引用持久 Sprite，不引用设计示意图。
- `Emberlight/UI/Update Shield HUD` 只更新 UIRoot 的引用和护盾布局，不重烘字体或改主场景。`Connect UIRoot Runtime` 也包含同一迁移。日常在 Prefab 编辑护盾条和数值即可，运行时不补建。
- 当前布局以竖屏为目标，PlayerSettings 锁定 Portrait（见 M9）。
- 面板的生命周期由 `EmberMenuUi` 统一持有；暂停与设置是浮层而非独立场景。
- `EmberUiRoot`、`EmberGodsSelectPanel`、`EmberSettingsPanel` 与 `EmberUpgradePanel` 为 `MonoBehaviour`；EmberMenuUi 与 EmberHud 为普通类。
- **安全区变化**由 `EmberSafeAreaWatcher` 观察（`RuntimeInitializeOnLoadMethod` 安装常驻监听对象，`LateUpdate` 采样）。
  `EmberMenuUi.Bind` 后调用 `WatchSafeArea()` 订阅，`EmberGame.OnDestroy` 调用 `UnwatchSafeArea()` 退订。
  静态事件在 `SubsystemRegistration` 阶段重置，避免关闭 Domain Reload 时残留。

## 影响与回归范围

- **直接影响模块**：M2（卡面读取构筑数据）、M3（HUD 读取战斗状态）、M7（精灵供给）、M8（交互音）。
- **必须复验的契约**：开局四步的顺序与单选约束、候选卡数量与滚动阈值、`unscaledDeltaTime` 使用、返回营地的二次确认、字体预热覆盖。
- UI 创建边界需复验缺失引用报错、Editor 配置工具与场景 Prefab 复用；HUD 缓存需复验秒/数值变化及时显示及波次文案不重复写入。
- 护盾需复验获得、吸收、溢出扣血、耗尽置灰、再次获得恢复，以及暂停恢复与重开清零；调整数值层级后重绑仍保留语义引用与人工布局。相关用例为 `Assets/Tests/EditMode/EmberShieldHudTests.cs` 与 `Assets/Tests/PlayMode/EmberShieldHudSmokeTests.cs`，实际排版需在游戏背景中检查。
