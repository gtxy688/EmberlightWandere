# EmberlightWandere 架构总览

> 本文件是**模块边界、依赖关系和任务路由的唯一索引**。
> 模块清单与任务索引只在本文件维护一份，其他文档不得复制。
> 最后核对：2026-09-14

## 技术与目标环境

| 项 | 值 |
|----|----|
| Unity | 2022.3.62f3 |
| 目标平台 | Android（竖屏 540×960）+ Editor 试玩 |
| 渲染管线 | Built-in |
| 输入 | 旧版 Input Manager + 自绘虚拟摇杆 |
| UI | uGUI + TextMeshProUGUI |
| 资源与配置 | `Resources/` + 代码内静态配置类（`LevelConfig`） |
| 程序集 | **当前无 `.asmdef`** → 全部进 `Assembly-CSharp` / `Assembly-CSharp-Editor` |
| 自动化环境 | **无测试程序集**；当前验证手段为编辑器编译 + 手动试玩（见 §测试现状） |

## 模块清单

第 2 列为文档编号，模块文档位于 `Docs/architecture/`。

| 编号 | 模块 | 核心职责 | 明确不负责 | 架构文档 | 验收文档 |
|------|------|----------|------------|----------|----------|
| M1 | 配置与调参 | 难度倍率、波次配额/间隔、Boss 与敌人基础数值、游戏速度 | 不含战斗逻辑与状态推进 | `01-config-tuning.md` | `01-config-tuning-test.md` |
| M2 | 单局进度与构筑 | 槽位/拥有武器/通用属性、选卡池生成、品质 roll、刷新、结算分 | 不含战斗、不含 UI 呈现 | `02-run-progression.md` | `02-run-progression-test.md` |
| M3 | 战斗与波次 | 刷怪、遭遇事件、伤害结算、死亡/分裂/重生、清波判定、Boss 招式 | 不含选卡内容、不含落卡 UI | `03-combat-waves.md` | `03-combat-waves-test.md` |
| M4 | 武器系统 | 6 把武器的 Tick 行为与专属/质变形态 | 不反向依赖 `EmberGame`，不做敌人生成 | `04-weapon-system.md` | — |
| M5 | 敌人系统 | 行为策略、暗弹弹幕、剪影与动画表现、治疗掉落 | 不做伤害应用（由 M3 结算） | `05-enemy-system.md` | `05-enemy-system-test.md` |
| M6 | 界面与流程 | 主菜单、开局四步、HUD、选卡面板、设置与暂停 | 不拥有游戏状态，只读参数 | `06-ui-flow.md` | `06-ui-flow-test.md` |
| M7 | 美术与表现 | 程序化精灵、图集取图、粒子、中文 TMP 字体 | 不改玩法数值与碰撞 | `07-art-presentation.md` | `07-art-presentation-test.md` |
| M8 | 音频 | BGM/SFX 总线、音量持久化、播放节流 | 不决定玩法，缺失资源静默降级 | `08-audio.md` | `08-audio-test.md` |
| M9 | 构建与平台 | Android 打包、PlayerSettings、资源导入器 | 不改运行时行为 | `09-build-platform.md` | `09-build-platform-test.md` |

## 依赖图

箭头指向被依赖方：`A -> B` 表示 A 依赖 B。

```text
M9(构建) ──────────────────────────────► (仅编辑器期)

M6(界面) ──► M2(进度)          M6 以参数接收 RunProgress，不持有 EmberGame
   │
   └──────► (M2 的 id 常量 / 文案表)

M3(战斗) ──► M4(武器) ──► M2(进度)
   │           │
   │           └──► M7(表现)  M4 经 ctx.Effects / ctx.Pool
   │
   ├──► M5(敌人) ──► M1(配置)
   ├──► M3.(Boss)
   └──► M1(配置)

M7(表现) ──► M2/M3 只读状态门控   ⚠️ EmberKeeperAnimation / EmberEffects 读 EmberGame.State
M8(音频) ──► 无玩法依赖（Resources 自加载）
```

稳定的关键契约：

- `EmberWeapon` / `EmberWeaponContext`（M4 与 M3 之间）——**M4 不反向依赖 `EmberGame`**，这是仓库里最干净的边界。
- `RunProgress`（M2）——**零 `UnityEngine` 引用**，纯 C#，是唯一可脱离 Unity 单测的对象。
- `EmberGame.State` + `EmberCombat.Tick`（M3 与 M6 之间）。

## 任务索引

一条任务类型映射一个主要模块。模块文档再链接到需求章节与其验收文档。

| 常用关键词 | 主要模块 | 读这个 |
|------------|----------|--------|
| 难度倍率 / 怪血 / 刷怪配额 / 刷怪间隔 / Boss 血量公式 | M1 | `01-config-tuning.md` |
| 词缀概率 / 敌人基础 HP / 地图边界 / 敌人上限 | M1 | `01-config-tuning.md` |
| 游戏速度 / 1x–5x / 倍速 | M1 | `01-config-tuning.md` |
| 选卡 / 出卡 / 卡池 / 补偿选卡 / 刷新 / 槽位 N / 侥幸权重 | M2 | `02-run-progression.md` |
| 幸运 / 稀有度 / 铜银金钻 / 品质 roll | M2 | `02-run-progression.md` |
| 专属 / 质变 / 进化 / 连发 / 燎原 / 日冕 | M2 + M4 | `02-run-progression.md` → `04-weapon-system.md` |
| 结算 / Score / 余烬 / 击杀计分 | M2 | `02-run-progression.md` |
| 护盾 / 回血道具 / 掉落率 | M2 + M5 | `02-run-progression.md` |
| 波次 / 清波 / 精英挑战 / 遭遇事件 / 阶段 Boss / Profiler 标记 / 刷怪尖峰 | M3 | `03-combat-waves.md` |
| 暗弹 / 齐射 / 半血狂怒 / Boss 招式 | M3 | `03-combat-waves.md` |
| 受伤 / 无敌帧 / 护盾吸收顺序 | M3 | `03-combat-waves.md` |
| 火球 / 环火 / 燃地 / 穿透火矢 / 回旋烬蝶 / 天降火雨 | M4 | `04-weapon-system.md` |
| 武器数值 / DPS 档位 / 超模 / 平衡 | M4 | `04-weapon-system.md` |
| 穿透静止开火 / 狙击手感 | M4 | `04-weapon-system.md` |
| 敌人行为 / 射手 / 烬胎分裂 / 铁灯卫 / 引线虫 | M5 | `05-enemy-system.md` |
| 灼热 / 疾风 / 重生词缀 | M5 | `05-enemy-system.md` |
| 敌人动画 / 剪影 / 角色标记 / 预警圈 | M5 + M7 | `05-enemy-system.md` |
| 主菜单 / 开局流程 / 选难度 / 选槽位 / 选武器 | M6 | `06-ui-flow.md` |
| HUD / 血条 / 波次横幅 / 暂停 / 设置页 / 音量滑条 / UI 复用 | M6 | `06-ui-flow.md` |
| 卡面排版 / 徽章 / 图标映射 / 滚动选卡 | M6 + M7 | `06-ui-flow.md` |
| 简笔画素材 / 图集 / 程序化精灵 / 粒子 | M7 | `07-art-presentation.md` |
| 中文字体 / TMP 图集 / 字体预热 / 加载页 | M7 | `07-art-presentation.md` |
| 音效 / BGM / 音量 / 播放节流 / 音效素材 | M8 | `08-audio.md` |
| 音效素材授权 / CC0 来源 | 参考资料 | `../reference/audio-credits.md` |
| Android 打包 / APK / keystore / PlayerSettings / 包依赖 / 本地工具清理 | M9 | `09-build-platform.md` |
| 性能 / 对象池 / 粒子上限 / 帧率 | M9 + M7 | `09-build-platform.md` |
| 项目总览 / 模块依赖 / 任务归属 | 总览 | `00-overview.md` |
| 数值口径 / 产品行为 / 不做什么 | 需求 | `../requirements.md` |

映射不唯一时不要猜测：列出候选模块和影响，请用户裁定后再改。

## 跨模块契约

| 使用方 | 被依赖方 | 契约 | 失败或降级行为 |
|--------|----------|------|----------------|
| M3 | M4 | `EmberWeapon.Tick(dt, EmberWeaponContext)` | 武器无 Tick 即不生效；`OwnsWeapon` 为假时武器自我门控 |
| M4 | M2 | `RunProgress.OwnsWeapon/HasMetamorph/WeaponMagnitude/WeaponAttackSpeed/DamageMul` | 只读；进度为空时武器不运行 |
| M4 | M3 | `ctx.Damage / DamageFrom / Intercept / FindNearestVisibleEnemy` | 委托未赋值时空引用风险由 M3 保证全部赋值 |
| M4 | M7 | `ctx.Effects`（粒子）、`ctx.Pool`（弹体池） | 池空时 `EmberEffects` 直接丢弃粒子，不报错 |
| M3 | M5 | `IEnemyBehavior.Tick` / `EmberEnemyProjectiles` | 行为为空则退化为直线追击 |
| M3 | M2 | `EmberGame.OnWaveCleared` → `Progress.Choices` | 无进度时不发牌 |
| M6 | M2 | `RunProgress` 作为**方法参数**传入，UI 不持有 `EmberGame` | 参数为空则不绘制该块 |
| M6 | M3 | 只读 `EnemyCount / Wave / WaveRemaining / Boss / BossAlive` | 无战斗实例时 HUD 隐藏战斗块 |
| M7 | M3/M2 | 只读 `EmberGame.State` 作"是否播放"门控 | 找不到游戏实例则动画照常播放 |
| M8 | M7/Resources | `Resources.Load("Audio/...")` | **clip 缺失静默跳过**，只警告一次，不崩 |
| M9 | 全部 | 仅在编辑器期改写 PlayerSettings 与资源导入设置 | 缺环境变量即停止打包并列缺失项 |

## 已知边界问题（技术债，非本轮修复范围）

| 问题 | 位置 | 影响 |
|------|------|------|
| `EmberGame` 为上帝类 | 11 态状态机 + 36 项文案表 + 开局流程 + 血量 + HUD 驱动 | 单文件承担 M6/M2/M3 三种职责，改动易互相波及 |
| 表现层反向依赖游戏层 | `EmberKeeperAnimation` 用 `FindObjectOfType<EmberGame>()` 反查；`EmberEffects` 读 `EmberGame.State` | 服务定位器隐藏依赖，难以单测 |
| `LevelConfig.ApplyWorld()` 被重复调用 | `EmberGame` 与 `EmberCombat` 各调一次 | 幂等，暂无实际危害，但属重复副作用 |
| 死代码 | `EmberPulseWeapon`（`WavePower` 恒 0）、`RunProgress` 中 `FullRoster` / `HasEvolved` / `ScoreMult` / `HealAmount` 等零调用点成员 | 增加误读成本 |
| `LevelConfig.Default` 每次访问新建对象 | `LevelConfig.cs` | 属性而非缓存，高频访问会有额外分配 |
| 无程序集边界 | 全项目无 `.asmdef` | 模块边界只能靠文档与纪律维持，编译期无法阻止违规依赖 |

## 测试现状

**当前项目没有任何自动化测试基础设施**，这是全部验收文档最大的缺口：

| 项 | 状态 |
|----|------|
| `.asmdef` 文件 | **0 个**（Assets 下） |
| EditMode / PlayMode 测试程序集 | **不存在** |
| `[Test]` / `UnityTest` / `Assert` | **不存在** |
| 编辑器校验入口（`EmberChecks` / `Validate*` / `EmberUIRenderCheck`） | **已被删除**（见 `09-build-platform.md` 清理记录） |

> ⚠️ 历史文档中大量「`Emberlight/Validate …` 已通过」「`ValidateGodsSelect` 覆盖验收点」的表述**已失效**——这些入口在 2026-09-14 发布准备清理中连同临时编辑器脚本一并删除。引用它们作为证据是不成立的。

因此各模块验收文档中：

- 可自动化但当前无基础设施的检查 → 标记为**待建**，并写明拟测位置与依赖层级；
- 只能人工判断的项 → 写入手动验收表；
- 不属于本模块的检查 → 标记 `N/A` 并给出理由。

补齐测试基础设施的方案见 `../requirements.md` §9 待裁定事项。建议的落地起点：仅为 M1（`LevelConfig`）与 M2（`RunProgress`）建 `Runtime` + `EditMode.Tests` 两个 asmdef——这两个模块不依赖场景与帧推进，收益最高、风险最低。
