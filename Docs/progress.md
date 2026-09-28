# 进度追踪

> 更新日期：2026-09-23
> 本文件**只记录状态和阻塞，不定义需求或架构**。需求见 `requirements.md`，模块边界见 `architecture/00-overview.md`。

## 版本状态

- 当前版本 **0.1.0**，最后提交 `b60d8b1 release: 完成 0.1.0 版本，完善 UI 与 Android 打包配置`。
- **尚未实际构建过 APK，也未做任何真机检查。**
- 当前分支 `refactor/decouple-embergame`，基线提交 `a216de7 重构前基线`（含 2026-09-15 那批未提交改动）。

## 2026-09-23 本轮改动：拆解上帝类 `EmberGame`

**动因**：全项目只有一个入口脚本 `EmberGame`，361 行里承担 5 种职责（11 态状态机 + 开局装配/销毁 +
摇杆与键盘输入 + 相机跟随 + 血量存亡 + HUD 推送 + 选卡编排）；M3（战斗）与 M7（表现）反向依赖 `EmberGame` 类型，
使这两块无法脱离场景验证。这些都已记在 `00-overview.md` 的「已知边界问题」里。

| 主题 | 内容 | 验证 |
|------|------|------|
| **组合根瘦身** | `EmberGame` 361 → 约 300 行，只保留装配、阶段所有权与每帧顺序 | 离线编译通过 |
| **阶段提为顶层类型** | `EmberGame.Mode` → `EmberRunPhase`（取值与顺序不变） | 离线编译通过 |
| **本局状态抽离** | 新增 `EmberRunState`（构筑/关卡/计时/击杀/世界与相机引用） | 离线编译通过 |
| **生命结算抽离** | 新增 `EmberKeeperVitals`：难度倍率只乘一次、护盾先吸收、0.65s 无敌帧、治疗溢出丢弃 | 离线编译通过 |
| **选卡推进抽离** | 新增 `RunFlowController`：选卡次数、新武器补偿（不连锁）、免费刷新 | 离线编译通过 |
| **战斗解除对 `EmberGame` 的依赖** | 新增 `IEmberCombatHost`；`EmberCombat` / `EmberBoss` / `EmberHealingDrops` 改用它 | 源码扫描：`Combat/` 下零 `EmberGame` 引用 |
| **表现层解除反向依赖** | `EmberKeeperAnimation` 去掉 `FindObjectOfType<EmberGame>()`；与 `EmberEffects` 一起改为注入阶段委托 | 源码扫描：`Visuals/` 下零 `EmberGame` 引用 |
| **输入 / 位移 / 相机抽离** | 新增 `EmberInput`（经 `Presenter` 回调，不持有 `EmberGame`）、`EmberKeeperMotion` | 离线编译通过 |
| **HUD 推送点抽离** | 新增 `EmberHudPresenter`；只在 `Playing` 阶段推送（重构前每帧推，含暂停/结算态） | 离线编译通过；**待人工验收 R1** |
| **安全区轮询改事件** | 新增 `EmberSafeAreaWatcher`，`EmberMenuUi` 订阅、`EmberGame.OnDestroy` 退订 | 离线编译通过 |
| **离线编译校验工具** | 新增 `Tools/compile-check.ps1`：用 Unity 自带 Roslyn 编译，**不启动编辑器** | 基线 42 文件 → 现状 53 文件，均 0 错误 0 警告 |

### 本轮实测结果（2026-09-23）

| 检查 | 命令 | 结果 |
|------|------|------|
| 离线编译 | `powershell -ExecutionPolicy Bypass -File Tools/compile-check.ps1` | **通过**：53 源文件、0 错误、0 警告 |
| 字形覆盖 | `python Tools/atlas_glyph_check.py Library/RefactorCheck/Emberlight.dll` | **通过**：`MISSING (0)`，TMP 特殊字符齐全，退出码 0 |
| 边界（源码扫描） | 见 `00-composition-root-test.md` C14 | **通过**：`Combat/`、`Combat/Weapons/`、`Visuals/` 下零 `EmberGame` 类型引用 |
| Unity 编辑器编译 | **未执行**（编辑器当时开着，批处理会 crash） | **未验证** |
| 真进一局 | — | **待人工验收**（R1） |

> ⚠️ 离线编译**不等于** Unity 编译通过，也**不能替代**进一局。本次重构是行为保持型搬代码，但回归仍必须手测一遍主闭环。

### 未做（有意决定，不是欠债）

- **仍未建 `.asmdef` 与测试程序集**：`Assets` 下依然 0 个 asmdef。模块边界目前靠文档 + 纪律 + 源码扫描维持。
- **未清理既有死代码**：`RunProgress` 的 8 个零调用点成员、`EmberPulseWeapon` 仍留着（清单见 `02-run-progression.md`）。
- **未修 `EmberHud` 每帧拼字符串**：热路径分配仍在；顺带记录 `Stage()` 写入的 `waveText` 会被 `Set()` 覆盖（当前行为，未改）。
- **未改 `LevelConfig.ApplyWorld()` 的重复调用**（`EmberGame` 与 `EmberCombat` 各一次，幂等）。

### 过程教训（工具链）

1. **PowerShell 5.1 会按 ANSI 读 UTF-8 无 BOM 文件**：用 `Get-Content -Raw` + `Set-Content` 批量改含中文注释的文件，
   会把注释写成乱码（本次污染了 `EmberMenuFlow.cs` 与 `EmberCombat.cs`，已从基线提交恢复并改用 UTF-8 安全工具重做）。
   结论：**改含中文的源文件只用编辑工具，或显式 `-Encoding UTF8` + `[System.IO.File]::WriteAllText`**。
2. **`$PSScriptRoot` 在 `powershell -File` 下可能为空**：`compile-check.ps1` 已改为从 `$MyInvocation.MyCommand.Definition` 回退。
3. **执行策略会挡住 `.ps1`**：需 `-ExecutionPolicy Bypass` 运行。

## 2026-09-15 本轮改动（工作区，已并入基线提交 `a216de7`）

| 主题 | 内容 | 验证 |
|------|------|------|
| **中文字体改为预烘静态图集** | 新增 `Assets/Editor/EmberFontBake.cs` 与 `Resources/Fonts/NotoSansCJKsc-Regular SDF.asset`（529 字形、单 2048² 图集、72 px 采样）。`EmberFonts` 预烘优先 + `ValidateBaked` 闸门 + 动态图集回落 | **通过**（A11–A14，见 `architecture/07-art-presentation-test.md`） |
| **加载页文字第 0 帧可见** | `EmberIntro` 原先把 `CreateChinese()` 排在 0.5 s 动画之后，导致中文"慢半拍"；现改为字体优先、动画在后 | 批处理编译通过；**待人工验收 H4** |
| **图集切为真正的 sprite sheet** | 四张图集 `textureType: Sprite` + `spriteMode: Multiple`；四个 `Ember*ArtImporter` 不再强制 `Default`（此前每次重导入都会清掉切片） | 资产结构核验通过；**待人工验收 H1/H2** |
| **取图三级链** | `EmberUiArt` / `EmberCardFrames` / `EmberCardIcons` / `EmberWorldArt` 改为「子 Sprite 优先 → 运行时 `Sprite.Create` 兜底 → 程序化占位」 | 同上 |
| **选武器改滚动** | `EmberGodsSelectPanel` 去掉 3 张一页的翻页，改为 `ScrollRect`，与升级面板同一套交互 | **待人工验收** |
| UI 文案与排版 | 难度页三档各占一行；武器卡说明与开局选武器文案逐字一致；菜单文案提为 `EmberGame` 命名常量 | 文案已提取并比对 |
| 每帧 `UpdateSafeArea` 轮询 | 改为仅在分辨率/安全区变化时更新 | 编译通过 |

> **未做**：纹理压缩（`doodle-*` 仍为 Uncompressed，约 24 MB VRAM）。经确认本轮**不压**，属有意决定而非欠债。
> **未做**：UI prefab 化。前置阻塞（`Image.sprite` 无资产可指）随切图已解除，但仍未开始。

## 模块进度

| 模块 | 阶段 | 最近验证 | 待人工验收 | 阻塞 | 下一步 |
|------|------|----------|------------|------|--------|
| M0 组合根 | **已重构** | **离线编译通过**（53 文件 0 错误 0 警告）+ 源码边界扫描通过；Unity 编译与实跑未做 | **R1–R10**（`00-composition-root-test.md`） | 无测试程序集 | **jack 进一局走查主闭环** |
| M1 配置与调参 | 已实现 | **无自动化**（数值已对照代码） | H1–H4 | 无测试程序集 | 补 EditMode 测试（最高收益） |
| M2 单局进度与构筑 | 已实现 | **无自动化**（规则已对照代码） | H1–H8 | 无测试程序集 | 补 EditMode 测试（最高收益） |
| M3 战斗与波次 | 已实现 | **无自动化**；宿主契约已解耦（源码扫描通过） | H1–H9 | 无测试程序集；PlayMode 需可脚本化装配 | 打一局走查主闭环 |
| M4 武器系统 | 已实现 | **无自动化** | W8–W10 | 无测试程序集 | jack 试玩六线平衡 |
| M5 敌人系统 | 已实现 | **无自动化** | H1–H9 | 无测试程序集 | jack 试玩验收（2026-09-13 起挂起） |
| M6 界面与流程 | 已实现 | **无自动化**；HUD 推送点与安全区已解耦 | H1–H10、**R1/R8** | 无测试程序集 | 真机触控与窄屏适配 |
| M7 美术与表现 | 已实现 | **部分自动化**：A11–A14（字形覆盖 / 预烘加载 / 烘制覆盖 / 重启后渲染）2026-09-15 批处理实跑通过；2026-09-23 字形覆盖复跑通过 | H1–H7、**H4b（72 px 清晰度）** | 其余项目无测试程序集 | jack 试玩验收字体清晰度与切图后的取图 |
| M8 音频 | 已实现 | **无自动化**（v1 时期的 Play Mode 实测已过期） | H1–H7 | 无测试程序集 | jack 听感验收 |
| M9 构建与平台 | **配置就绪，未构建** | **无** | H1–H11 | **keystore 签名未定；未执行过构建** | 打调试包 → 真机走查 |

> 「已实现」指代码存在于工作区且能编译；**不等于已验收**。按 `AGENTS.md` 的口径，只有适用自动化检查通过**且**适用人工验收完成后才可标记「已验收」。

## 当前阻塞

| 阻塞项 | 影响 | 需要谁 |
|--------|------|--------|
| **本次重构尚未在 Unity 里编译过、也未进过一局** | 离线 Roslyn 编译通过不等于 Unity 编译通过；重构面覆盖全部模块，必须先手测主闭环 | jack（或关闭 Unity 后跑批处理编译） |
| **无自动化测试基础设施**（无 asmdef、无测试程序集、校验入口已删） | 除 M7 的 A11–A14 外，全部模块的自动化项均为「未验证」；回归完全依赖手测 | jack 裁定是否补齐 |
| **未构建过 APK** | M9 全部真机验收无法开始 | jack |
| **keystore 签名身份未创建** | 阻塞正式发布（不阻塞调试包） | jack |
| 应用图标 / 启动图未配置 | 阻塞上架 | jack + xiaoUI |
| **字体改为静态图集后，每次改文案都要重跑烘制** | 漏字会静默显示为空心方框，不会报错 | 流程已固化：`python Tools/atlas_glyph_check.py` → 关 Unity → 批处理烘制 → 复查（见 `AGENTS.md` 硬约束 1） |
| `Assets/Prefabs/` 三个 UI prefab 为早期失败路线的残留在盘产物，未被任何场景引用 | 易被误当作可用资产 | jack 决定删除或保留 |

## 建议的下一步顺序

1. **在 Unity 编辑器里走查一遍主闭环（R1）**，确认本次重构没有破坏战斗、选卡与阶段流转——这是当前最高优先项，因为重构面覆盖了全部模块。
2. **裁定是否补齐测试基础设施**（`requirements.md` §9）。建议起点：只为 M1（`LevelConfig`）与 M2（`RunProgress`）建 `Runtime` + `EditMode.Tests` 两个 asmdef——这两个模块不依赖场景与帧推进，收益最高、风险最低。抽出的 `EmberKeeperVitals` 与 `RunFlowController` 现在也属同一档，可一并纳入。
3. 把 M1/M2 的验收文档中「待建」项落成真实测试并记录首次结果。
4. 打调试 APK，开始 M9 真机走查。
5. 逐模块补 jack 的手感与审美验收（M4 W8–W10、M5、M6、M7、M8）。

## 文档健康度

- [x] 已实现模块的公开契约与代码一致（2026-09-14 逐文件核对；2026-09-23 随重构再核一次受影响章节）
- [x] 需求数值集中在 `Docs/requirements.md`，架构文档不再复制数值表
- [x] 任务索引唯一维护在 `architecture/00-overview.md`（已加入 M0 与「拆类 / 解耦 / 编译校验」关键词）
- [x] 被取代的文档已移入 `Docs/archive/` 并标注作废
- [x] 已订正的数值冲突记入 `AGENTS.md` 与各模块验收文档
- [x] **`requirements.md` §7 中文字体与程序集口径已按现行代码订正**（2026-09-23 裁定；原「固定走动态图集」表述已标注为旧口径）
- [x] 行为变化已更新对应验收文档 —— M7 的 A11–A14 附 2026-09-15 运行记录；**新增 M0 的 `00-composition-root.md` / `-test.md`**
- [x] 引用了已迁走文案位置（`EmberGame.UpgradeNames` → `EmberCopy.UpgradeNames`）的文档已订正：`06-ui-flow.md`、`07-art-presentation.md`、`04-weapon-system.md`、`02-run-progression.md`
- [ ] 其余模块验收文档的自动化项目前仍为「待建/未验证」
- [ ] 所有「已验收」模块的适用自动化检查均已通过 —— **无模块达到「已验收」**，因自动化基础设施缺失

## 前几轮暴露的流程教训（字体）

静态图集**不会告诉你少了什么**——只会静默画方框或打警告。2026-09-15 那轮连续踩到四类漏字，各自成因不同：

| # | 漏的是什么 | 为什么常规手段抓不到 |
|---|------------|----------------------|
| 1 | 难度页整句文案 | 是 `ChooseLength` **方法体内的内联字面量**，反射与数组遍历都够不到 |
| 2 | 设置页 / 敌人图鉴 / 结算文案 | 同上，散落在多个方法体 |
| 3 | `_`（U+005F） | **TMP 的内部要求**，不来自任何文案；又是 ASCII，看着"显然存在" |
| 4 | 图集只烘了卡面字 | `AllGlyphs` 的来源是人工枚举的数组清单，漏一个来源就整片缺字 |

因此把判断依据从"代码里有哪些来源"换成**"编译产物里有哪些字面量"**：`Tools/atlas_glyph_check.py` 读 `Assembly-CSharp.dll` 的 `#US` 堆，能看见方法体内的字符串，并额外显式检查 TMP 的两个特殊码点。**这是目前唯一不依赖人记得同步的检查。**
