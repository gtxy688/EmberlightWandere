# EmberlightWandere 架构总览

> 本文件是**模块边界、依赖关系和任务路由的唯一索引**。
> 模块清单与任务索引只在本文件维护一份，其他文档不得复制。
> 最后核对：2026-10-14

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
| 自动化环境 | 已建立 EditMode 与 PlayMode 套件；具体覆盖与最新结果见 `../实现说明.md`；其余验证手段为编辑器编译 + 手动试玩（见 §测试现状） |

## 模块清单

第 2 列为文档编号，模块文档位于 `Docs/architecture/`。

| 编号 | 模块 | 核心职责 | 明确不负责 | 架构文档 | 验收文档 |
|------|------|----------|------------|----------|----------|
| M0 | 组合根 | 持有阶段与本局状态、装配各单元、驱动每帧顺序、接回调 | **不含任何业务规则**（规则分别归 M1–M8） | `00-composition-root.md` | `00-composition-root-test.md` |
| M1 | 配置与调参 | 难度倍率、波次配额/间隔、Boss 与敌人基础数值、游戏速度 | 不含战斗逻辑与状态推进 | `01-config-tuning.md` | `01-config-tuning-test.md` |
| M2 | 单局进度与构筑 | Id 空间（`EmberWeaponCatalog`）、候选卡生成（`EmberChoicePool`）、构筑状态推进与品质 roll（`RunProgress`）、刷新、结算分 | 不含战斗、不含 UI 呈现 | `02-run-progression.md` | `02-run-progression-test.md` |
| M3 | 战斗与波次 | 刷怪、遭遇事件、伤害结算、死亡/分裂/重生、清波判定、Boss 招式 | 不含选卡内容、不含落卡 UI | `03-combat-waves.md` | `03-combat-waves-test.md` |
| M4 | 武器系统 | 6 把武器的 Tick 行为与专属/质变形态 | 不反向依赖 `EmberGame`，不做敌人生成 | `04-weapon-system.md` | `04-weapon-system-test.md` |
| M5 | 敌人系统 | 行为策略、暗弹弹幕、剪影与动画表现、治疗掉落 | 不做伤害应用（由 M3 结算） | `05-enemy-system.md` | `05-enemy-system-test.md` |
| M6 | 界面与流程 | 主菜单、开局四步、HUD、选卡面板、设置与暂停 | 不拥有游戏状态，只读参数 | `06-ui-flow.md` | `06-ui-flow-test.md` |
| M7 | 美术与表现 | 程序化精灵、图集取图、粒子、中文 TMP 字体 | 不改玩法数值与碰撞 | `07-art-presentation.md` | `07-art-presentation-test.md` |
| M8 | 音频 | BGM/SFX 总线、音量持久化、播放节流 | 不决定玩法，缺失资源静默降级 | `08-audio.md` | `08-audio-test.md` |
| M9 | 构建与平台 | Android 打包、PlayerSettings、资源导入器 | 不改运行时行为 | `09-build-platform.md` | `09-build-platform-test.md` |

## 依赖图

箭头指向被依赖方：`A -> B` 表示 A 依赖 B。

```text
M9(构建) ──────────────────────────────► (仅编辑器期)

M0(组合根 EmberGame) ──► 全部模块（只做装配与每帧顺序，不实现规则）
   │
M6(界面) ──► M2(进度)          M6 以参数接收 RunProgress，不持有 EmberGame
   │
   └──────► EmberRunPhase      M6 经 IEmberMenuHost.EnterMode 请求切阶段

M3(战斗) ──► IEmberCombatHost ──► M0  由组合根实现，M3 不认识 EmberGame
   │
   ├──► M4(武器) ──► M2(进度)
   │       │
   │       └──► M7(表现)  M4 经 ctx.Effects / ctx.Pool
   │
   ├──► M5(敌人) ──► M1(配置)
   ├──► M3.(Boss)
   └──► M1(配置)

M7(表现) ──► IEmberRunPhaseSource  只读阶段门控，不再反查游戏实例
M8(音频) ──► 无玩法依赖（Resources 自加载）

M2(进度) ──► 无（零 UnityEngine 引用）
RunFlowController ──► Host 契约 ──► M0  选卡推进规则不持有 EmberGame
```

稳定的关键契约：

- `EmberWeapon` / `EmberWeaponContext`（M4 与 M3 之间）——**M4 不反向依赖 `EmberGame`**。
- `RunProgress`（M2）——**零 `UnityEngine` 引用**，纯 C#，唯一可脱离 Unity 单测的对象。
- `IEmberCombatHost`（M3 与 M0 之间）——战斗只需要阶段/计时/伤害入口，不再持有整个游戏类。
- `IEmberRunPhaseSource`（M7 与 M0 之间）——表现层只问"该不该播"。
- `RunFlowController.Host`（选卡流程与 M0 之间）——选卡次数与补偿规则可脱离场景验证。
- `EmberRunPhase` + `EmberCombat.Tick`（M3 与 M6 之间）。

## 任务索引

一条任务类型映射一个主要模块。模块文档再链接到需求章节与其验收文档。

| 常用关键词 | 主要模块 | 读这个 |
|------------|----------|--------|
| 入口脚本 / 组合根 / EmberGame / 拆类 / 阶段状态机 / 装配顺序 | M0 | `00-composition-root.md` |
| 战斗宿主契约 / IEmberCombatHost / 解除 EmberGame 依赖 | M0 + M3 | `00-composition-root.md` → `03-combat-waves.md` |
| 难度倍率 / 怪血 / 刷怪配额 / 刷怪间隔 / Boss 血量公式 | M1 | `01-config-tuning.md` |
| 词缀概率 / 敌人基础 HP / 地图边界 / 敌人上限 | M1 | `01-config-tuning.md` |
| 游戏速度 / 1x–5x / 倍速 | M1 | `01-config-tuning.md` |
| 选卡 / 出卡 / 卡池 / 补偿选卡 / 刷新 / 槽位 N / 侥幸权重 | M2 | `02-run-progression.md` |
| 候选怎么拼 / 同屏唯一键 / 出卡张数 / EmberChoicePool | M2 | `02-run-progression.md` |
| 词条 Id 分区 / 武器名册 / EmberWeaponCatalog / Definition / 武器文案 / 专属与质变映射 | M2 | `02-run-progression.md` |
| 幸运 / 稀有度 / 铜银金钻 / 品质 roll | M2 | `02-run-progression.md` |
| 专属 / 质变 / 进化 / 连发 / 燎原 / 日冕 | M2 + M4 | `02-run-progression.md` → `04-weapon-system.md` |
| 结算 / Score / 余烬 / 击杀计分 | M2 | `02-run-progression.md` |
| 护盾 / 回血道具 / 掉落率 | M2 + M5 | `02-run-progression.md` |
| 波次 / 清波 / 精英挑战 / 遭遇事件 / 阶段 Boss / Profiler 标记 / 刷怪尖峰 / 屏外刷怪 / EmberSpawnPlacement | M3 | `03-combat-waves.md` |
| 暗弹 / 齐射 / 半血狂怒 / Boss 招式 | M3 | `03-combat-waves.md` |
| 受伤 / 无敌帧 / 护盾吸收顺序 / 生命纯逻辑 | M0 | `00-composition-root.md` |
| 火球 / 环火 / 燃地 / 穿透火矢 / 回旋烬蝶 / 天降火雨 | M4 | `04-weapon-system.md` |
| 武器数值 / EmberWeaponTuning / EmberWeaponFactory / 注册 / DPS 档位 / 超模 / 平衡 | M4 | `04-weapon-system.md` |
| 共享燃地 / EmberBurnGround | M4 | `04-weapon-system.md` |
| 穿透静止开火 / 狙击手感 | M4 | `04-weapon-system.md` |
| 敌人行为 / 射手 / 烬胎分裂 / 铁灯卫 / 引线虫 | M5 | `05-enemy-system.md` |
| 灼热 / 疾风 / 重生词缀 | M5 | `05-enemy-system.md` |
| 敌人动画 / 剪影 / 角色标记 / 预警圈 | M5 + M7 | `05-enemy-system.md` |
| 主菜单 / 开局流程 / 选难度 / 选槽位 / 选武器 | M6 | `06-ui-flow.md` |
| HUD / 文字刷新缓存 / 血条 / 波次横幅 / 暂停 / 设置页 / 音量滑条 / UI 复用 / 旧 UI 创建清理 / UIRoot 运行时引用 / Connect UIRoot Runtime | M6 + M0 | `06-ui-flow.md` → `00-composition-root.md` |
| HUD 推送点 / 安全区适配 / 摇杆输入 / 相机跟随 | M6 + M0 | `00-composition-root.md` → `06-ui-flow.md` |
| 卡面排版 / EmberOfferPresentation / EmberUiFactory / 徽章 / 图标映射 / 滚动选卡 | M6 + M7 | `06-ui-flow.md` |
| 简笔画素材 / 图集 / 程序化精灵 / 粒子 | M7 | `07-art-presentation.md` |
| UIRoot / Prefab 图片丢失 / Editor 图片绑定 / Repair UIRoot Images | M7 + M6 | `07-art-presentation.md` → `06-ui-flow.md` |
| 加载页 / IntroRoot / 加载页预制体 | M6 + M7 | `06-ui-flow.md` → `07-art-presentation.md` |
| 中文字体 / TMP 图集 / 字体预热 | M7 | `07-art-presentation.md` |
| 音效 / BGM / 音量 / 播放节流 / 音效素材 | M8 | `08-audio.md` |
| 音效素材授权 / CC0 来源 | 参考资料 | `../reference/audio-credits.md` |
| Android 打包 / APK / keystore / PlayerSettings / 包依赖 / 本地工具清理 | M9 | `09-build-platform.md` |
| 性能 / 对象池 / 粒子上限 / 帧率 | M9 + M7 | `09-build-platform.md` |
| 项目总览 / 模块依赖 / 任务归属 | 总览 | `00-overview.md` |
| 数值口径 / 产品行为 / 不做什么 | 需求 | `../requirements.md` |
| 工程源码交付 / 打包工程 / 导入验证 | M9 | `09-build-platform.md` → `../../README.md` |
| 测试运行 / run-tests / PlayMode 冒烟 | 验证 | `../实现说明.md` |
| 离线编译校验 / 不启动 Unity 验证改动 | 工具 | `../../Tools/compile-check.ps1` |
| 笔试题要求对照 / 符合性核查 / 提交前改进项 | 交付 | `../笔试题2符合性核查.md` |
| 设计取舍说明 / 阅卷入口 | 交付 | `../技术分析.md` → `../../README.md` |
| 历史决策 / 踩坑记录 / 重构过程 | 归档（只读） | `../archive/` |

映射不唯一时不要猜测：列出候选模块和影响，请用户裁定后再改。

## 跨模块契约

| 使用方 | 被依赖方 | 契约 | 失败或降级行为 |
|--------|----------|------|----------------|
| M3 | M4 | `EmberWeapon.Tick(dt, EmberWeaponContext)` | 武器无 Tick 即不生效；`OwnsWeapon` 为假时武器自我门控 |
| M4 | M2 | `RunProgress.OwnsWeapon/HasMetamorph/WeaponMagnitude/WeaponAttackSpeed/DamageMul` | 只读；进度为空时武器不运行 |
| M4 | M3 | `ctx.Damage / DamageFrom / Intercept / FindNearestVisibleEnemy` | 委托未赋值时空引用风险由 M3 保证全部赋值 |
| M4 | M7 | `ctx.Effects`（粒子）、`ctx.Pool`（弹体池） | 池空时 `EmberEffects` 直接丢弃粒子，不报错 |
| M3 | M5 | `IEnemyBehavior.Tick` / `EmberEnemyProjectiles` | 行为为空则退化为直线追击 |
| M3 | M2 | `IEmberCombatHost.OnWaveCleared` → `RunFlowController` → `Progress.Choices` | 无进度时不发牌 |
| M6 | M2 | `RunProgress` 作为**方法参数**传入，UI 不持有 `EmberGame` | 参数为空则不绘制该块 |
| M6 | M3 | 只读 `EnemyCount / Wave / WaveRemaining / Boss / BossAlive` | 无战斗实例时 HUD 隐藏战斗块 |
| M0 | M3 | `IEmberCombatHost` | 由组合根实现；**实现方不必是 `EmberGame`** |
| M3 | M0 | `IEmberCombatHost` 反向请求阶段、伤害入口、计分与收局 | 宿主未提供时战斗不推进 |
| M4 | M8 | `ctx.PlaySound(EmberWeaponSound)`，由 M3 装配音频适配 | 无回调时只跳过音效 |
| M4 | M0 | **无依赖**（经 `EmberWeaponContext`） | — |
| M7 | M0 | 阶段委托；构造时注入，不用服务定位器 | 委托为空时动画照常播放 |
| M8 | M7/Resources | `Resources.Load("Audio/...")` | **clip 缺失静默跳过**，只警告一次，不崩 |
| M9 | 全部 | 仅在编辑器期改写 PlayerSettings 与资源导入设置 | 缺环境变量即停止打包并列缺失项 |

## 已知边界问题（技术债，非本轮修复范围）

| 问题 | 位置 | 影响 |
|------|------|------|
| `LevelConfig.ApplyWorld()` 被重复调用 | `EmberGame` 与 `EmberCombat` 各调一次 | 幂等，暂无实际危害，但属重复副作用 |
| 未接线的武器实现 | `EmberPulseWeapon`（`RunProgress.WavePower` 恒 0，本版无出卡入口；类本身零调用点） | 增加误读成本；去留待裁定，见 `04-weapon-system.md` |
| `LevelConfig.Default` 每次访问新建对象 | `LevelConfig.cs` | 属性而非缓存，高频访问会有额外分配 |
| `EmberHud` 每帧拼字符串 | `EmberHud.Set` / `Stage` | 热路径分配；且 `Stage` 写 `waveText` 与 `Set` 重复（当前行为：`Stage` 覆盖 `Set`） |
| Runtime 内模块尚未按目录拆 asmdef | `Assets/Scripts/Emberlight.Runtime.asmdef` | Runtime 与 Editor/Tests 已隔离，但 Runtime 内依赖仍靠契约与审查约束 |

> **2026-10-14 注释精简中删除的死成员**（`RunProgress`）：`AddShield`（把护盾硬钳到 250 的旧口径陷阱）、`HasEvolved` / `CanEvolve` / `OwnedWeaponsView` / `HealAmount`，以及 `EmberRarityUtil.Magnitude` / `CountBonus`——均经全仓零调用点确认。`WavePower` / `ScoreMult` / `FullRoster` / `OfferCountExpected` 仍被武器或测试读取，**保留**。

> **2026-09-23 已修复的历史技术债**（勿再按旧描述理解代码）：
>
> | 旧问题 | 现状 |
> |--------|------|
> | `EmberGame` 为上帝类（361 行 / 5 种职责） | 已拆为组合根（约 300 行，纯装配与顺序）+ 9 个职责单元，见 `00-composition-root.md` |
> | 表现层用 `FindObjectOfType<EmberGame>()` 反查 | `EmberKeeperAnimation` / `EmberEffects` 改为注入 `IEmberRunPhaseSource` 式阶段委托 |
> | M3 直接持有 `EmberGame` | 改为 `IEmberCombatHost`；`EmberCombat` / `EmberBoss` / `EmberHealingDrops` 零 `EmberGame` 引用 |
> | 安全区变化每帧轮询 | 改为 `EmberSafeAreaWatcher` 事件订阅 |
> | `RunProgress` 死代码清单（`HasExclusive` 等） | 已按 2026-09-23 全仓扫描重新核对，清单见 `02-run-progression.md` |

## 测试现状

2026-10-03 建立 M2 基线；2026-10-13 增加生命、流程、卡面、共享燃地与图形冒烟测试；2026-10-14 补充 Prefab、屏外刷怪和 HUD 更新回归。结果以 `../实现说明.md` 和 XML 为准；自动化仍未覆盖全部玩法。

| 项 | 状态 |
|----|------|
| `Assets` 下自建 `.asmdef` | **4 个**：`Emberlight.Runtime`（`Assets/Scripts`）、`Emberlight.Editor`、`Emberlight.EditMode.Tests`、`Emberlight.PlayMode.Tests`。项目代码已不在 `Assembly-CSharp` |
| EditMode 测试程序集 | **有**：`Assets/Tests/EditMode`，150 个用例 |
| PlayMode 测试程序集 | **有**：`Assets/Tests/PlayMode`，四个图形集成冒烟场景（含 UIRoot/IntroRoot 绑定与面板复用） |
| 已覆盖模块 | **M2**（Id 空间、卡池、品质权重与数值表、构筑推进与护盾吸收）；M0 的 `EmberKeeperVitals` / `RunFlowController`、卡牌说明、共享燃地、屏外落点/配额重试与 HUD 更新；另有主场景与 UI 复用冒烟 |
| 编辑器校验入口（`EmberChecks` / `Validate*` / `EmberUIRenderCheck`） | **已被删除**（见 `09-build-platform.md` 清理记录） |
| 离线编译校验 | **有**：`Tools/compile-check.ps1`（Unity 自带 Roslyn，不启动编辑器；**只覆盖 Runtime**） |
| Unity 批处理编译 | **有**：`Tools/unity-batch-verify.ps1`（真 Unity 导入 + 编译；需先关编辑器，且在普通终端运行） |
| 字形覆盖检查 | **有**：`Tools/atlas_glyph_check.py`（读 `Emberlight.Runtime.dll` 的字符串堆） |

跑测试：

```powershell
Unity.exe -batchmode -nographics -projectPath <项目> -runTests -testPlatform editmode `
  -testResults Library/TestRun/editmode.xml -logFile Library/TestRun/tests.log
```

> 后三项是**检查工具**，它们能证明"工程能导入、代码写得对、文案都在盘上"，
> 但不能证明"运行行为正确"——那部分由 EditMode / PlayMode 套件与人工验收分别承担。
> **EditMode 全绿不等于模块已验收**：手感、观感与依赖帧推进的行为仍须人工验收。

> ⚠️ 历史文档中大量「`Emberlight/Validate …` 已通过」「`ValidateGodsSelect` 覆盖验收点」的表述**已失效**——这些入口在 2026-09-14 发布准备清理中连同临时编辑器脚本一并删除。引用它们作为证据是不成立的。

因此各模块验收文档中：

- 可自动化但当前无基础设施的检查 → 标记为**待建**，并写明拟测位置与依赖层级；
- 只能人工判断的项 → 写入手动验收表；
- 不属于本模块的检查 → 标记 `N/A` 并给出理由。

**下一步建议的测试扩展顺序**（基础设施已就位，属"照着写"而非"从零搭"）：

1. **M1 的 `LevelConfig`**——波次配额、刷怪间隔、Boss 血量公式全是对数表的纯函数，
   对应 `01-config-tuning-test.md`；
2. **M3 的波次与 Boss 边界**——配额失败重试已有桩测试，Boss 波清场、重生与分裂仍需独立用例；
3. **PlayMode 的完整胜利与敌人/武器行为**——主场景和序列化 Canvas 已可装配，继续复用现有冒烟基础。
