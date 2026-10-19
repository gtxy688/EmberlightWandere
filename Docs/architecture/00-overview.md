# EmberlightWandere 架构总览

> 本文件是**模块边界、依赖关系和任务路由的唯一索引**。
> 模块清单与任务索引只在本文件维护一份，其他文档不得复制。
> 最后核对：2026-10-19

## 技术与目标环境

| 项 | 值 |
|----|----|
| Unity | 2022.3.62f3 |
| 目标平台 | Android（竖屏 540×960）+ Editor 试玩 |
| 渲染管线 | Built-in |
| 输入 | 旧版 Input Manager + 自绘虚拟摇杆 |
| UI | uGUI + TextMeshProUGUI |
| 资源与配置 | `Resources/` + 代码内静态配置类（`LevelConfig`） |
| 程序集 | Runtime / Editor / EditMode.Tests / PlayMode.Tests 四个自建程序集 |
| 开发检查 | EditMode / PlayMode 测试源码位于 `Assets/Tests`；检查命令见根目录 README |

## 模块清单

第 1 列为模块编号，模块文档位于 `Docs/architecture/`。

| 编号 | 模块 | 核心职责 | 明确不负责 | 架构文档 |
|------|------|----------|------------|----------|
| M0 | 组合根 | 持有阶段与本局状态、装配各单元、驱动每帧顺序、结果转译与回调接线 | **不放数值规则或状态推进规则**（分别归各职责单元） | `00-composition-root.md` |
| M1 | 配置与调参 | 难度倍率、波次配额/间隔、Boss 与敌人基础数值、游戏速度 | 不含战斗逻辑与状态推进 | `01-config-tuning.md` |
| M2 | 单局进度与构筑 | Id 空间（`EmberWeaponCatalog`）、候选卡生成（`EmberChoicePool`）、构筑状态推进与品质 roll（`RunProgress`）、选卡序列（`RunFlowController`）、刷新、结算分 | 不含战斗、不含 UI 呈现 | `02-run-progression.md` |
| M3 | 战斗与波次 | 刷怪、遭遇事件、伤害结算、死亡/分裂/重生、清波判定、Boss 招式 | 不含选卡内容、不含落卡 UI | `03-combat-waves.md` |
| M4 | 武器系统 | 6 把武器的 Tick 行为与专属/质变形态 | 不反向依赖 `EmberGame`，不做敌人生成 | `04-weapon-system.md` |
| M5 | 敌人系统 | 行为策略、暗弹弹幕、剪影与动画表现、治疗掉落 | 不做伤害应用（由 M3 结算） | `05-enemy-system.md` |
| M6 | 界面与流程 | 主菜单、开局四步、HUD、选卡面板、设置与暂停 | 不拥有游戏状态，只读参数 | `06-ui-flow.md` |
| M7 | 美术与表现 | 程序化精灵、图集取图、粒子、中文 TMP 字体 | 不改玩法数值与碰撞 | `07-art-presentation.md` |
| M8 | 音频 | BGM/SFX 总线、音量持久化、播放节流 | 不决定玩法，缺失资源静默降级 | `08-audio.md` |
| M9 | 构建与平台 | Android 打包、PlayerSettings、资源导入器 | 不改运行时行为 | `09-build-platform.md` |
| M10 | Buff 系统 | Buff 配置、对象、计时与属性修正 | 不做掉落、技能命中与 UI 创建 | `10-buff-system.md` |

## 依赖图

箭头指向被依赖方：`A -> B` 表示 A 依赖 B。

```text
M9(构建) ──────────────────────────────► (仅编辑器期)

M0(组合根 EmberGame) ──► 全部模块（装配、阶段所有权、每帧顺序与结果转译）
   │
M6(界面) ──► M2(进度)          M6 以参数接收 RunProgress，不持有 EmberGame
   │
   └──────► EmberRunPhase      M6 经 IEmberMenuHost.EnterMode 请求切阶段

M3(战斗) ──► IEmberCombatHost ──► M0  由组合根实现，M3 不认识 EmberGame
   │
   ├──► M4(武器) ──► M2(进度)
   │       │
   │       └──► M7(表现)  M4 经 ctx.Effects；ctx.Pool 归 M3
   │
   ├──► M5(敌人) ──► M1(配置)
   ├──► M3.(Boss)
   └──► M1(配置)

M7(表现) ──► Func<EmberRunPhase>  通过 Initialize 注入，只读阶段门控
M8(音频) ──► 无玩法依赖（Resources 自加载）
M0 ──► M10(Buff) ◄── M3(伤害读取) / M5(施加请求) / M6(只读呈现)

M1(配置) ──► M3.EmberWorld.Limit  ApplyWorld 的反向静态写入
M2(进度) ──► M4.EmberWeaponTuning  共享升级常量，零 UnityEngine 引用
M2.RunFlowController ──► Host 契约 ──► M0  选卡规则不持有 EmberGame
```

稳定的关键契约：

- `EmberWeapon` / `EmberWeaponContext`（M4 与 M3 之间）——**M4 不反向依赖 `EmberGame`**。
- `RunProgress`（M2）——**零 `UnityEngine` 引用**，纯 C#；与卡池、品质和流程等纯逻辑单元一起支持独立测试。
- `IEmberCombatHost`（M3 与 M0 之间）——战斗只需要阶段/计时/伤害入口，不再持有整个游戏类。
- 阶段委托 `Func<EmberRunPhase>`（M7 与组合根之间）——表现组件经 `Initialize` 接收；`IEmberRunPhaseSource` 是战斗宿主继承的只读阶段接口，不是这两个组件的入参。
- `RunFlowController.Host`（选卡流程与 M0 之间）——选卡次数与补偿规则可脱离场景验证。
- `EmberRunPhase` + `EmberCombat.Tick`（M3 与 M6 之间）。

## 任务索引

一条任务类型映射一个主要模块。模块文档再链接到相关需求章节。

| 常用关键词 | 主要模块 | 读这个 |
|------------|----------|--------|
| 入口脚本 / 组合根 / EmberGame / 拆类 / 阶段状态机 / 装配顺序 / 重开一局 / 初始化与清理 | M0 | `00-composition-root.md` |
| 战斗宿主契约 / IEmberCombatHost / 解除 EmberGame 依赖 | M0 + M3 | `00-composition-root.md` → `03-combat-waves.md` |
| 难度倍率 / 怪血 / 刷怪配额 / 刷怪间隔 / Boss 血量公式 | M1 | `01-config-tuning.md` |
| 词缀概率 / 敌人基础 HP / 地图边界 / 敌人上限 | M1 | `01-config-tuning.md` |
| 游戏速度 / 1x–5x / 倍速 | M1 | `01-config-tuning.md` |
| 选卡 / 出卡 / 卡池 / 补偿选卡 / 刷新 / 槽位 N / 候选权重 / RunFlowController | M2 | `02-run-progression.md` |
| 候选怎么拼 / 同屏唯一键 / 出卡张数 / EmberChoicePool | M2 | `02-run-progression.md` |
| 词条 Id 分区 / 武器名册 / EmberWeaponCatalog / Definition / 武器文案 / 专属与质变映射 | M2 | `02-run-progression.md` |
| 幸运 / 稀有度 / 铜银金钻 / 品质 roll | M2 | `02-run-progression.md` |
| 专属 / 质变 / 进化 / 连发 / 燎原 / 日冕 | M2 + M4 | `02-run-progression.md` → `04-weapon-system.md` |
| 结算 / Score / 余烬 / 击杀计分 | M2 | `02-run-progression.md` |
| 护盾 / 回血道具 / 掉落率 | M2 + M5 | `02-run-progression.md` |
| 拾取 / 治疗掉落 / Buff 掉落 / 自动吸附 | M5 | `05-enemy-system.md` |
| Buff / Debuff / BuffConfig / BuffManager / 增伤 / 疾行 / 迟缓 / 减速 / 效果刷新与到期 | M10 | `10-buff-system.md` |
| 波次 / 清波 / 精英挑战 / 遭遇事件 / 阶段 Boss / Profiler 标记 / 刷怪尖峰 / 屏外刷怪 / EmberSpawnPlacement | M3 | `03-combat-waves.md` |
| 暗弹 / 齐射 / 半血狂怒 / Boss 招式 | M3 | `03-combat-waves.md` |
| Boss 负面 Buff / Boss 迟缓 / 冲锋减速 / 爆发减速 | M3 + M10 | `03-combat-waves.md` → `10-buff-system.md` |
| Boss 群战 / Boss 伴随兵 / 小兵补位 / BossSupportTargetForWave / BossSupportInterval | M3 + M1 | `03-combat-waves.md` → `01-config-tuning.md` |
| 受伤 / 无敌帧 / 护盾吸收顺序 / 生命纯逻辑 | M0 | `00-composition-root.md` |
| 自动攻击 / 自动选敌 / 武器 Tick | M4 | `04-weapon-system.md` → `03-combat-waves.md` |
| 火球 / 环火 / 燃地 / 穿透火矢 / 回旋烬蝶 / 天降火雨 | M4 | `04-weapon-system.md` |
| 武器数值 / EmberWeaponTuning / EmberWeaponFactory / 注册 / DPS 档位 / 超模 / 平衡 | M4 | `04-weapon-system.md` |
| 共享燃地 / EmberBurnGround / 火区替换分配 / 燃地压力测试 / EmberCombatPerformanceTests | M4 + M3 | `04-weapon-system.md` → `03-combat-waves.md` |
| 穿透静止开火 / 静止阈值 / 狙击手感 | M4 | `04-weapon-system.md` |
| 敌人行为 / 射手 / 烬胎分裂 / 铁灯卫 / 引线虫 | M5 | `05-enemy-system.md` |
| 灼热 / 疾风 / 重生词缀 | M5 | `05-enemy-system.md` |
| 敌人动画 / 剪影 / 角色标记 / 预警圈 | M5 + M7 | `05-enemy-system.md` |
| 无脚 / 悬浮 / 怪物皮肤 / EnemySkinSetup / Build Enemy Skins / 独立盾图 | M5 + M7 | `05-enemy-system.md` → `07-art-presentation.md` |
| 主菜单 / 开局流程 / 选难度 / 选槽位 / 选武器 | M6 | `06-ui-flow.md` |
| HUD / 文字刷新缓存 / 血条 / 波次横幅 / 暂停 / 设置页 / 音量滑条 / UI 复用 / 旧 UI 创建清理 / UIRoot 运行时引用 / Connect UIRoot Runtime | M6 + M0 | `06-ui-flow.md` → `00-composition-root.md` |
| 护盾 UI / 独立护盾条 / 归零置灰 / Update Shield HUD / EmberShieldHudSetup | M6 + M7 | `06-ui-flow.md` → `07-art-presentation.md` |
| Buff 拾取提示 / 效果说明 / 持续时间提示 / Buff 倒计时 / 图标计时环 / 角色浮字 / EmberBuffHud / EmberBuffPresentation | M6 + M10 | `06-ui-flow.md` → `10-buff-system.md` |
| HUD 推送点 / 安全区适配 / 摇杆输入 / 相机跟随 | M6 + M0 | `00-composition-root.md` → `06-ui-flow.md` |
| 卡面排版 / EmberOfferPresentation / EmberUiFactory / 徽章 / 图标映射 / 滚动选卡 / 卡高 / 行步长 | M6 + M7 | `06-ui-flow.md` |
| 卡牌描述 / 卡片说明 / 选择前后预览 / 攻速说明 / 延烧文案 | M6 + M2 | `06-ui-flow.md` → `02-run-progression.md` |
| 简笔画素材 / 图集 / 程序化精灵 / 粒子 | M7 | `07-art-presentation.md` |
| 原版角色精修 / 衣褶 / 兜帽部件 / EmberCharacterArt | M7 + M5 | `07-art-presentation.md` → `05-enemy-system.md` |
| A 方案 / 清爽卡通 / CleanToonSample | M7 + M5 | `07-art-presentation.md` → `05-enemy-system.md` |
| 玩家两帧 / Idle / Move / IsMoving / flipX / Player_Idle / Player_Move / EmberPlayerSpriteSetup / Build Player Sprite Animations | M7 | `07-art-presentation.md` |
| 手绘样板 / 美术替换 / 恢复原版角色 / EmberArtSet / KeeperPrefab / ShadePrefab / CourtyardPrefab / HandPaintedSample / 遗迹地面 / 场景装饰 | M7 + M5 | `07-art-presentation.md` → `05-enemy-system.md` |
| 玩家走路 / 行走动画 / 分件关节 / 提灯摆动 / 透明序列帧 / KeeperWalk / EmberKeeperRig | M7 | `07-art-presentation.md` |
| 2.5D / 3D 角色 / 低模 / 立体庭院 / 蒙皮 / 膝盖 / IK / SkinnedMeshRenderer / LowPolyCourtyard / EmberLowPolyGait | M7 | `07-art-presentation.md` 的「低模 2.5D 独立小样」 |
| 紧凑 HUD / 手绘边框 / HUD 九宫格 | M6 + M7 | `06-ui-flow.md` → `07-art-presentation.md` |
| 掉落物美术 / 拾取物图标 / 药瓶 / 火种护符 / 风羽 / EmberPickupArt | M7 + M5 | `07-art-presentation.md` → `05-enemy-system.md` |
| UIRoot / Prefab 图片丢失 / Editor 图片绑定 / Repair UIRoot Images | M7 + M6 | `07-art-presentation.md` → `06-ui-flow.md` |
| 加载页 / IntroRoot / 加载页预制体 | M6 + M7 | `06-ui-flow.md` → `07-art-presentation.md` |
| 中文字体 / TMP 图集 / 字体预热 | M7 | `07-art-presentation.md` |
| 音效 / BGM / 音量 / 初始音量 / 播放节流 / 音效素材 | M8 | `08-audio.md` |
| 音效素材授权 / CC0 来源 | 参考资料 | `../reference/audio-credits.md` |
| Android 打包 / APK / keystore / PlayerSettings / 包依赖 / 本地工具清理 | M9 | `09-build-platform.md` |
| 性能 / 对象池 / GC.Alloc / 取池闭包 / 燃地 CPU / 粒子上限 / 帧率 | M3 + M4 + M9 | `03-combat-waves.md` → `04-weapon-system.md` → `09-build-platform.md` |
| 项目总览 / 模块依赖 / 任务归属 | 总览 | `00-overview.md` |
| 源码学习 / 通读 / 精读 / 学习笔记 / 阅读地图 | 学习方法与阅读进度（非架构契约） | [源码阅读地图](../项目复习/00-源码阅读地图.md) → [通用源码学习方案](../项目复习/学习辅助/01-通用源码学习方案.md) |
| 数值口径 / 参数归属 / 表现默认值 / 产品行为 / 单局规则边界 / 不做什么 | 需求 | `../requirements.md` |
| 工程源码交付 / 打包工程 / 导入验证 | M9 | `09-build-platform.md` → `../../README.md` |
| 测试运行 / run-tests / PlayMode 冒烟 | 开发工具 | `../../README.md` |
| 样板录屏 / 美术截图 / EmberArtSampleCapture / EmberBossSwarmSmokeTests / 怪物渲染检查 | 开发工具 | [运行说明](../../README.md)、[截图与录制辅助](../../Assets/Tests/PlayMode/EmberArtSampleCapture.cs)、[渲染检查](../../Assets/Tests/PlayMode/EmberEnemyRenderingTests.cs) |
| UI 控件复用 / 回调替换 / 取消选择 | M6 + 测试 | `06-ui-flow.md` → `../../Assets/Tests/PlayMode/EmberRuntimeSmokeTests.cs` |
| 离线编译校验 / 不启动 Unity 验证改动 | 工具 | `../../Tools/compile-check.ps1` |
| 源码清理 / 冗余代码 / 冗余注释 / 接口归属 / 重复实现 | 维护方法 | [开发约定](../../AGENTS-dev.md)的「改动约定」；具体契约按本索引定位模块 |
| 技术思路 / 设计取舍 / 内部开发说明 | 开发参考 | `../技术分析.md` → `../实现说明.md` |
| 历史决策 / 踩坑记录 / 重构过程 | 归档（只读） | `../archive/` |

映射不唯一时不要猜测：列出候选模块和影响，请用户裁定后再改。

## 跨模块契约

| 使用方 | 被依赖方 | 契约 | 失败或降级行为 |
|--------|----------|------|----------------|
| M3 | M4 | `EmberWeapon.Tick(dt, EmberWeaponContext)` | 武器无 Tick 即不生效；`OwnsWeapon` 为假时武器自我门控 |
| M4 | M2 | `RunProgress.OwnsWeapon/HasMetamorph/WeaponMagnitude/WeaponAttackSpeed/DamageMul` | 只读；进度为空时武器不运行 |
| M4 | M3 | `ctx.Damage / DamageFrom / Intercept / FindNearestVisibleEnemy` | 委托未赋值时空引用风险由 M3 保证全部赋值 |
| M4 | M7 / M3 | `ctx.Effects`（粒子）/ `ctx.Pool`（弹体池） | 粒子满时跳过发射；对象池无缓存时创建视图 |
| M3 | M5 | `IEnemyBehavior.Tick` / `EmberEnemyProjectiles` | 行为为空则退化为直线追击 |
| M3 | M2（经 M0 接线） | `IEmberCombatHost.OnWaveCleared` → `RunFlowController` → `Progress.Choices` | 无进度时不发牌 |
| M6 | M2 | `RunProgress` 作为**方法参数**传入，UI 不持有 `EmberGame` | 参数为空则不绘制该块 |
| M6 | M0 状态 / M3 战斗 | Presenter 读取 `EmberRunState` 的生命/计时/击杀及战斗的波次/Boss | 无构筑时跳过推送；无战斗时保留基本状态并跳过 Boss 更新 |
| M0 | M3 | `IEmberCombatHost` | 由组合根实现；**实现方不必是 `EmberGame`** |
| M3 | M0 | `IEmberCombatHost` 反向请求阶段、伤害入口、计分与收局 | 宿主未提供时战斗不推进 |
| M4 | M8 | `ctx.PlaySound(EmberWeaponSound)`，由 M3 装配音频适配 | 无回调时只跳过音效 |
| M4 | M0 | **无依赖**（经 `EmberWeaponContext`） | — |
| M7 | M0 | 阶段委托；通过 `Initialize` 注入 | 委托为空时粒子与玩家动画均不推进 |
| M8 | M7/Resources | `Resources.Load("Audio/...")` | **clip 缺失静默跳过**，只警告一次，不崩 |
| M9 | 全部 | 仅在编辑器期改写 PlayerSettings 与资源导入设置 | 缺工具链或资源停止；自有签名另校验环境变量 |

## 已知边界问题（技术债，非本轮修复范围）

| 问题 | 位置 | 影响 |
|------|------|------|
| `LevelConfig.Default` 每次访问新建对象 | `LevelConfig.cs` | 属性而非缓存，高频访问会有额外分配 |
| Runtime 内模块尚未按目录拆 asmdef | `Assets/Scripts/Emberlight.Runtime.asmdef` | Runtime 与 Editor/Tests 已隔离，但 Runtime 内依赖仍靠契约与审查约束 |

## 开发检查

- 自动化测试源码：`Assets/Tests/EditMode` 与 `Assets/Tests/PlayMode`。
- Runtime 离线编译：`Tools/compile-check.ps1`。
- Unity 导入与编译：`Tools/unity-batch-verify.ps1`。
- 字形覆盖：`Tools/atlas_glyph_check.py`。
- 文档源码行号：`Tools/doc-line-check.ps1`。

运行方式见根目录 README；工具输出保存在 Library，不在架构文档中维护执行记录。
