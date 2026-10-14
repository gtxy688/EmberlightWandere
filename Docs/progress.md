# 进度追踪

> 更新日期：2026-10-14
> 本文件**只记录当前状态和阻塞，不定义需求或架构**。需求见 [`requirements.md`](requirements.md)，模块边界见 [`architecture/00-overview.md`](architecture/00-overview.md)。
> 历史轮次的详细记录（重构过程、每轮实测输出、踩坑经过）见 [`archive/development-history.md`](archive/development-history.md)。

## 版本状态

- 当前版本 **0.1.0**，最后提交 `b60d8b1 release: 完成 0.1.0 版本，完善 UI 与 Android 打包配置`。
- 主分支 `main`；本轮重构与提交前优化从 `refactor/decouple-embergame` 合入。
- **尚未实际构建过 APK，也未做任何真机检查。**
- `Delivery/` 下已有的工程包**早于** 2026-10-13 之后的改动，提交前必须用 `Tools/export-project.ps1` 重新导出。

## 自动化现状

| 项 | 状态 |
|----|------|
| 程序集 | `Emberlight.Runtime` / `Emberlight.Editor` / `Emberlight.EditMode.Tests` / `Emberlight.PlayMode.Tests` |
| EditMode | **150 个用例，全绿**（2026-10-14 注释精简后复跑：`total=150 passed=150 failed=0 skipped=0`，Unity 退出 0）。分项见 `architecture/02-run-progression-test.md` 与 `实现说明.md` |
| PlayMode | **4 个图形冒烟，全绿**（主场景开局 → 战斗 → 选卡 → 暂停 → 失败回营地；另有面板复用与加载页绑定），Unity 退出 0 |
| 字形覆盖 | `MISSING (0)`，TMP 特殊字符齐全（`atlas holds 529`） |
| 未覆盖 | M1 配置、M3 波次/Boss 边界、M4 六武器行为、M5 敌人行为、M8 音频接线、M9 平台——这些模块的验收文档里各自的 A 类项仍标「待建」 |

跑测试与检查的命令见根目录 `README.md` 的「验证」一节。

## 模块进度

| 模块 | 阶段 | 最近验证 | 待人工验收 | 下一步 |
|------|------|----------|------------|--------|
| M0 组合根 | 已重构 | EditMode 覆盖 `EmberKeeperVitals` 8 条 + `RunFlowController` 6 条；源码扫描 `Combat`/`Visuals`/`UI` 零 `EmberGame` 引用 | 完整胜利路径、0.65 秒无敌手感 | 补 C11/C12 的 PlayMode 断言 |
| M1 配置与调参 | 已实现 | 数值已逐项对照代码；**无自动化** | 波次节奏体感 | 补 EditMode（纯函数，收益最高） |
| M2 单局进度与构筑 | 已实现 | **EditMode 105 条覆盖本模块，全绿** | 出卡体感、六线平衡 | 裁定权重和漂移与 `ConfigureRun` 守卫 |
| M3 战斗与波次 | 已实现 | 宿主契约已解耦；图形冒烟覆盖开局到回营地 | 波次推进、Boss 出场、分裂数量 | 补波次/Boss 边界用例 |
| M4 武器系统 | 已实现 | **无自动化** | 六线平衡、狙击手感、质变可辨识 | 补逐武器行为用例 |
| M5 敌人系统 | 已实现 | **无自动化** | 敌人可读性与辨识度 | 补行为与回池用例 |
| M6 界面与流程 | 已实现 | 图形冒烟覆盖四步开局、HUD、暂停、面板复用 | 布局观感、点击手感、宽高比适配 | 补窄屏与触控用例 |
| M7 美术与表现 | 已实现 | 字形覆盖 `MISSING (0)`；批处理烘制与重载校验通过 | 72 px 采样点清晰度、切图取图效果 | 破坏路径用例（坏资产回落） |
| M8 音频 | 已实现 | **无自动化** | 听感与混音平衡 | 补接线与节流用例 |
| M9 构建与平台 | 配置就绪，**未构建** | 包解析与编辑器编译通过 | 真机全流程 | 打调试包 → 真机走查 |

> 「已实现」指代码存在于工作区且能编译，**不等于已验收**。按 `AGENTS.md` 的口径，只有适用自动化检查通过**且**适用人工验收完成后才可标记「已验收」；**当前无模块达到「已验收」**。

## 当前阻塞

| 阻塞项 | 影响 | 需要谁 |
|--------|------|--------|
| **本次改动尚未真进过一局** | 注释精简面覆盖全部 Runtime 与 Editor 文件；虽已证明"代码行逐字节一致"，仍需按流程在编辑器里确认中文无方框、Console 干净 | jack |
| **PlayMode 覆盖薄** | 依赖帧推进的行为（波次节奏、Boss 招式、摇杆、卡动画、状态效果）只能手测 | jack 裁定优先级 |
| **未构建过 APK** | M9 全部真机验收无法开始 | jack |
| **keystore 签名身份未创建** | 阻塞正式发布（不阻塞调试包） | jack |
| 应用图标 / 启动图未配置 | 阻塞上架 | jack + xiaoUI |
| **字体改为静态图集后，每次改文案都要重跑烘制** | 漏字会静默显示为空心方框，不报错 | 流程已固化在 `AGENTS.md` 硬约束 1 |
| `Assets/Prefabs/` 三个 UI prefab 是早期失败路线的残留，未被任何场景引用 | 易被误当作可用资产 | jack 决定删除或保留 |

## 待裁定事项（汇总）

各模块文档的「待裁定事项」小节是权威列表，这里只做导航：

| 事项 | 位置 |
|------|------|
| 中间幸运值的品质权重和漂到 101–102 | `architecture/02-run-progression-test.md` |
| `ConfigureRun` 是否加"只取第一把"的守卫 | `architecture/02-run-progression-test.md` |
| `EmberPulseWeapon` / `WavePower` 去留（当前零调用点） | `architecture/04-weapon-system.md`、`requirements.md` §9 |
| 满血拾取治疗掉落的语义（历史验收条目与实际实现不一致） | `architecture/05-enemy-system-test.md`、`实现说明.md` |
| Runtime 内是否按目录拆 asmdef | `architecture/00-composition-root.md` |
| `EmberRunState` 是否拆为"纯数据 + 场景句柄" | `requirements.md` §9 |
| 是否进一步扩充行为测试 | `requirements.md` §9 |
| 是否补一个可观察的 debuff（笔试题进阶项唯一缺口） | `笔试题2符合性核查.md` |

## 建议的下一步顺序

1. **在编辑器里真进一局**，确认中文无方框、Console 干净、四步开局到结算的闭环正常——本轮改动了全部 Runtime 源文件（仅注释），属流程必修项。
2. **补 debuff 示例**：这是笔试题 2 进阶项唯一的实质缺口，扩展点现成（`IEnemyBehavior.Tick(dt, ctx)`）。
3. **确认敌人主题**：当前默认追击怪是暗影披风造型，题面写的是"丧尸"。
4. **补 M1 与 M3 的用例**：纯函数与边界规则，收益最高、风险最低。
5. **打调试 APK**，开始 M9 真机走查。
6. **重新导出工程包**，再走一遍"无 Library 新目录首次导入"。

## 文档健康度

- [x] 模块文档引用的 `requirements.md` 章节仍然存在
- [x] 「对外契约」与实现一致（2026-10-14 复核并修正 `EmberHealingDrops`、`EmberFontBake`、字体字形数、asmdef/测试口径等 20 余处过期断言）
- [x] 任务索引唯一维护在 `architecture/00-overview.md`
- [x] `progress.md` 只记状态；历史轮次已移入 `archive/development-history.md`
- [x] `Docs/archive/` 已建立并附归档说明；历史文档的下落可追溯
- [ ] 数值仍散落在多篇模块文档（多数带"不在此复制"声明）。**冲突时一律以 `requirements.md` 为准**
- [ ] 各模块验收文档中仍有大量「待建」的自动化项
- [ ] 无模块达到「已验收」
